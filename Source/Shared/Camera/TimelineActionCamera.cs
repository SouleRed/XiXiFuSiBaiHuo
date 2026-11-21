using Soule.MotionCamera;
using CameraCue = Soule.MotionCamera.MotionCameraCue;
using Shake = Soule.MotionCamera.MotionCameraShake;
using Clip = Soule.MotionCamera.MotionCameraSequence;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using 裁缝之王.Presentation.Actions;
using 裁缝之王.Presentation.Effects;

namespace 裁缝之王.Presentation.Camera;

// 动作时钟驱动镜头状态；LateUpdate 求最终画面并借用整帧视觉朝向。
// 各摄像机仅在自身渲染期间借用最终投影，结束后归还原生状态。
internal sealed partial class TimelineActionCamera
{
    // 对齐 Unity 演示的普通绕点镜头；动画特写仍由后续采样覆盖。
    internal const bool UseStandardViewForOrbit = true;

    private sealed class NativeCamera
    {
        public UnityEngine.Camera Camera;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool Orthographic, CustomProjection;
        public float Size, Fov, Near;
        public Matrix4x4 Projection;
        public void Capture()
        {
            Position = Camera.transform.localPosition; Rotation = Camera.transform.localRotation;
            Size = Camera.orthographicSize; Near = Camera.nearClipPlane;
            CaptureProjection();
        }
        public void CaptureProjection()
        {
            Orthographic = Camera.orthographic; Fov = Camera.fieldOfView; Projection = Camera.projectionMatrix;
            Camera.ResetProjectionMatrix();
            var automatic = Camera.projectionMatrix;
            CustomProjection = false;
            for (int i = 0; i < 16; i++) if (!Mathf.Approximately(Projection[i], automatic[i])) { CustomProjection = true; break; }
            if (CustomProjection) Camera.projectionMatrix = Projection;
        }
        public void Restore()
        {
            if (!Camera) return;
            Camera.transform.localPosition = Position; Camera.transform.localRotation = Rotation;
            Camera.orthographicSize = Size; Camera.nearClipPlane = Near;
            RestoreProjection();
        }
        public void RestoreProjection()
        {
            if (!Camera) return;
            Camera.orthographic = Orthographic; Camera.fieldOfView = Fov;
            if (CustomProjection) Camera.projectionMatrix = Projection;
            else Camera.ResetProjectionMatrix();
        }
    }
    private struct CameraFrame
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Size, Fov;
        public Matrix4x4 ShakeBasis;
        public bool Animated, DirectPortrait;
    }

    private static TimelineActionCamera current;
    private int preparedFrame = -1, coinGeneration;
    private readonly BattleUnitView actor;
    private BattleUnitView target;
    private readonly bool standalone;
    private BattleUnitView[] previousFocusTargets;
    private readonly List<NativeCamera> previousFocusProjections = new();
    private BattleCamManager.CamControlMode previousFocusMode;
    private bool previousStopFocus;

    private readonly List<UnityEngine.Camera> nativeCameras = new();
    private readonly BattleCamManager manager;
    private Clip clip;
    private CameraCue cameraCue, rotateCue, pendingSecondCamera;
    private Shake shake;
    private MotionCameraShakeMath.Wave shakeWave;
    private CameraFrame frame;
    private float cameraAge, rotateAge, shakeAge, sourceTime, realTime, evaluatedTime;
    private bool phase2Pending, waitingForNextCoin, stopped;
    private int nextCamera, nextShake, nextHit;
    private CameraCue[] orderedCameras;
    private Shake[] orderedShakes;
    private float[] orderedHits, orderedHitDelays;
    private readonly List<float> deferredHitTimes = new();
    private float hitPresentationTime;
    internal delegate float SourceClock(float time, out float rate);
    private SourceClock sourceClock;
    private float actionRate = 1f;
    private readonly MotionCameraSampler animationSampler = new();
    private readonly List<AnimationEntry> animationEntries = new();
    private float groupSourceOffset;
    private sealed class AnimationEntry { public CameraCue Cue; public float Start; }
    private Func<bool> portraitEnabled;
    private Vector3 libraryFollowPosition, nativeFocus;
    private Quaternion libraryFollowRotation;
    private float libraryFollowSize;
    private float actorDirection => actor.model.direction == Direction.LEFT ? -1f : 1f;
    private const float RotationCenterScale = .8f;
    private const float FocusCenterHeight = 1.1f;
    private float CameraSourceUnits => ActionMotion.AppearanceUnits(actor);
    private float CameraEventUnits => CameraSourceUnits * mapScale;
    private float ShakeReferenceHalfHeight
    {
        get
        {
            var size = SingletonBehavior<BattleSceneRoot>.Instance.currentMapObject.mapSize;
            float mapFactor = size == MapSize.S ? 1.5f : size == MapSize.M ? 1.27f : 1f;
            return MotionCameraShakeMath.ReferenceHalfHeight(manager._curStdOrtho, 13f, mapFactor, Mathf.Abs(actor.transform.localScale.y), Mathf.Abs(target.transform.localScale.y), ActionMotion.HeightScale(actor), ActionMotion.HeightScale(target), manager._focusZoomFactor, standalone);
        }
    }
    private UnityEngine.Camera previewCamera => manager.mainCam;
    private float mapScale => clip.SourceMapScale;
    private static float Ease(int ease, float u, AnimationCurve curve) => ActionMotion.EvaluateEase(ease, Mathf.Clamp01(u), curve);
    private static float CameraValue(AnimationCurve curve, float at, float fallback = 0f) => curve != null && curve.length > 0 ? curve.Evaluate(at) : fallback;
    private float SourceHeight(BattleUnitView unit) => clip.Height * ActionMotion.HeightScale(unit);

    private TimelineActionCamera(BattleUnitView actor, BattleUnitView target, bool standalone = false)
    {
        this.actor = actor; this.target = target; this.standalone = standalone;
        manager = SingletonBehavior<BattleCamManager>.Instance;
        nativeCameras.Add(manager.mainCam);
        foreach (var camera in new[] { manager._characterCam, manager._characterUiCam, manager._uiCam, manager._effectCam, manager._fgCamTransform.GetComponent<UnityEngine.Camera>() }) if (camera && camera != manager.mainCam) nativeCameras.Add(camera);
        ReadNative();
    }

    // 在 PrependEntry 之前绑定：短切入不消耗正式镜头事件的时钟。
    internal static void Bind(Clip clip, List<RencounterManager.MovingAction> actions, BattleUnitView actor, BattleUnitView target, RencounterEffects.State state, SourceClock sourceClock, Func<bool> continues, ref RencounterManager.ActionAfterBehaviour self, Func<float> presentationClock, Func<bool> portraitEnabled = null)
    {
        TimelineActionCamera playback = null;
        int generation = 0;
        foreach (var action in actions)
        {
            var move = action._moveFunc;
            if (action.moveState == CharMoveState.Stop) action.moveState = CharMoveState.Custom;
            action._moveFunc = (dt, elapsed) =>
            {
                float before = presentationClock();
                bool done = move?.Invoke(dt, elapsed) ?? true;
                float step = presentationClock() - before;
                if (step <= 0f) return done;
                if (playback == null)
                {
                    playback = state.Camera;
                    if (playback == null || playback.stopped || playback != current)
                    {
                        Clear(); playback = new TimelineActionCamera(actor, target); state.Camera = playback; Activate(playback);
                    }
                    playback.Begin(clip, sourceClock, portraitEnabled);
                    generation = playback.coinGeneration;
                }
                playback.realTime = presentationClock();
                return done;
            };
        }
        self.endMoveRoutineEvent += () =>
        {
            if (playback == null || playback != current || generation != playback.coinGeneration) return;
            if (!continues()) { Clear(); return; }
            // 切币只结清事件游标；帧时钟仍推进已启动的缓动与震动，不提前触发下一币事件。
            playback.ReadNative();
            playback.AdvancePending(0f);
            playback.waitingForNextCoin = true;
        };
    }
    private void Begin(Clip value, SourceClock clock, Func<bool> allowPortrait)
    {
        if (clip != null) groupSourceOffset += sourceTime;
        animationEntries.RemoveAll(e => e.Cue.scope == 0);
        foreach (var cue in value.Cameras) if (cue.enabled && cue.animation) animationEntries.Add(new AnimationEntry { Cue = cue, Start = groupSourceOffset + cue.time });
        // OrderBy 保持同优先级的插入顺序；仅在切币改变集合时重新排序。
        var sortedAnimations = animationEntries.OrderBy(e => e.Cue.priority).ToArray();
        animationEntries.Clear(); animationEntries.AddRange(sortedAnimations);
        clip = value; sourceClock = clock; portraitEnabled = allowPortrait;
        sourceTime = realTime = evaluatedTime = 0f;
        preparedFrame = -1;
        coinGeneration++;
        waitingForNextCoin = false;
        orderedCameras = clip.Cameras.Where(c => c.enabled && !c.isPhase && !c.animation).OrderBy(c => c.time).ToArray();
        orderedShakes = clip.Shakes.Where(s => s.enabled).OrderBy(s => s.time).ToArray();
        var hitOrder = Enumerable.Range(0, clip.Hits.Length).OrderBy(i => clip.Hits[i]).ToArray();
        orderedHits = hitOrder.Select(i => clip.Hits[i]).ToArray();
        orderedHitDelays = hitOrder.Select(i => clip.HitDelays != null && i < clip.HitDelays.Length ? clip.HitDelays[i] : 0f).ToArray();
        deferredHitTimes.Clear(); hitPresentationTime = 0f;
        nextCamera = nextShake = nextHit = 0;
    }
    private void ReadNative()
    {
        libraryFollowPosition = previewCamera.transform.position;
        libraryFollowRotation = previewCamera.transform.rotation;
        libraryFollowSize = previewCamera.orthographicSize;
        nativeFocus = (actor.WorldPosition + target.WorldPosition) * .5f;
        var focus = manager._focusTargetList;
        if (focus.Count > 2 || (previousFocusTargets != null && focus.Count > 0))
        {
            nativeFocus = Vector3.zero;
            foreach (var view in focus) nativeFocus += view.transform.position;
            nativeFocus /= focus.Count;
        }
    }
    // 只保存实际渲染中的借用状态，不保存一份供原版更新反复读取的替代状态。
    // Canvas 与 Sprite 在整个渲染帧使用同一朝向；不在各相机之间反复旋转父节点。
    private sealed class VisualBorrow
    {
        public Transform Root;
        public Quaternion LocalRotation;
    }
    private static readonly List<VisualBorrow> visualBorrows = new();
    internal static void RestoreVisualFrame()
    {
        for (int i = visualBorrows.Count - 1; i >= 0; i--) if (visualBorrows[i].Root) visualBorrows[i].Root.localRotation = visualBorrows[i].LocalRotation;
        visualBorrows.Clear();

    }
    private static void AlignVisual(BattleUnitView unit, Quaternion rotation)
    {
        var root = unit.camRotationFollower;
        visualBorrows.Add(new VisualBorrow { Root = root, LocalRotation = root.localRotation });
        root.rotation = rotation;
    }
    private static readonly List<NativeCamera> renderStack = new();
    internal static void Clear()
    {
        RestoreVisualFrame();
        for (int i = renderStack.Count - 1; i >= 0; i--) renderStack[i].Restore();
        renderStack.Clear();
        if (current != null) { current.RestoreStandaloneFocus(); current.stopped = true; current.animationSampler.Dispose(); current.animationEntries.Clear(); }
        current = null;
        UnityEngine.Camera.onPreCull -= RenderForCamera;
        UnityEngine.Camera.onPostRender -= RestoreAfterRender;
    }
    private static void Activate(TimelineActionCamera playback)
    {
        current = playback;
        if (!playback.manager.GetComponent<TimelineCameraDriver>()) playback.manager.gameObject.AddComponent<TimelineCameraDriver>();
        UnityEngine.Camera.onPreCull -= RenderForCamera;
        UnityEngine.Camera.onPostRender -= RestoreAfterRender;
        UnityEngine.Camera.onPreCull += RenderForCamera;
        UnityEngine.Camera.onPostRender += RestoreAfterRender;
    }
    internal static TimelineActionCamera BeginStandalone(Clip clip, BattleUnitView actor)
    {
        Clear();
        var playback = new TimelineActionCamera(actor, actor, standalone: true);
        playback.StartStandaloneClip(clip, (float time, out float rate) => { rate = 1f; return time; });
        Activate(playback);
        return playback;
    }

    internal static TimelineActionCamera BeginFocusedStandalone(BattleUnitView actor, BattleUnitView target, IReadOnlyList<BattleUnitView> targets)
    {
        Clear();
        var playback = new TimelineActionCamera(actor, target, standalone: true);
        playback.BeginStandaloneFocus(targets);
        Activate(playback);
        return playback;
    }
    internal void StartStandaloneClip(Clip clip, SourceClock clock) => Begin(clip, clock, null);
    internal void SetStandaloneTime(float time) => realTime = time;
    internal void EndStandalone() { if (current == this) Clear(); }

    // 独立于任何 Camera.Render 调用。一个逻辑帧只推进一次镜头状态。
    internal static void PrepareFrame()
    {
        // 异常终止的渲染事务在下一次逻辑更新前归还，正常路径由 onPostRender 成对结束。
        if (renderStack.Count != 0)
        {
            for (int i = renderStack.Count - 1; i >= 0; i--) renderStack[i].Restore();
            renderStack.Clear();
        }
        var p = current;
        if (p == null || p.preparedFrame == Time.frameCount) return;
        if (!p.actor || !p.manager) { Clear(); return; }
        if (p.previousFocusTargets != null)
        {
            p.manager._focusTargetList.RemoveAll(view => !view || view != p.actor && view.model.IsDead());
            if (!p.target || p.target.model.IsDead())
                p.target = p.manager._focusTargetList.Count > 1 ? p.manager._focusTargetList[1] : p.actor;
        }
        else if (!p.target || p.manager.IsCamIsReturning) { Clear(); return; }
        RestoreVisualFrame();
        p.ReadNative();
        // 原版只在正式对抗中自动刷新透视 FOV，独立演出沿用同一换算。
        if (p.previousFocusTargets != null) p.manager.SetFov(p.nativeFocus, p.previewCamera.aspect);
        if (p.clip == null) return;
        bool paused = !p.standalone && RencounterEffects.IsPaused;
        p.AdvancePending(paused ? 0f : Mathf.Max(0f, Time.deltaTime), !paused && !p.waitingForNextCoin);
        p.SampleAnimationCamera();
        if (p.shake != null) p.frame.Position += MotionCameraShakeMath.WorldOffset(p.shakeWave, p.shakeAge, p.frame.ShakeBasis, p.ShakeReferenceHalfHeight);
        if (p.standalone)
        {
            // 开场演出的朝向对齐覆盖场上角色，取景目标仍由原聚焦名单决定。
            foreach (var unit in BattleObjectManager.instance.GetList())
            {
                var view = unit.view;
                if (view && view.camRotationFollower && view.camRotationFollower.gameObject.activeInHierarchy)
                    AlignVisual(view, p.frame.Rotation);
            }
        }
        else if (p.frame.Animated)
        {
            AlignVisual(p.actor, p.frame.Rotation);
            if (p.target != p.actor) AlignVisual(p.target, p.frame.Rotation);
        }
        p.preparedFrame = Time.frameCount;
    }

    private void BeginStandaloneFocus(IReadOnlyList<BattleUnitView> targets)
    {
        previousFocusTargets = manager._focusTargetList.ToArray();
        previousFocusMode = manager._camMode;
        previousStopFocus = manager.stopFocusByRencounter;

        foreach (var camera in nativeCameras)
        {
            if (camera.transform == manager._fgCamTransform) continue;
            var snapshot = new NativeCamera { Camera = camera };
            snapshot.CaptureProjection();
            previousFocusProjections.Add(snapshot);
            camera.ResetProjectionMatrix();
        }
        var participants = new List<BattleUnitModel> { actor.model };
        foreach (var view in targets)
            if (view && view != actor && !view.model.IsDead()) participants.Add(view.model);
        manager.stopFocusByRencounter = false;
        manager.FollowUnits(isAction: true, participants);
    }

    private void RestoreStandaloneFocus()
    {
        if (previousFocusTargets == null || !manager) return;
        manager._focusTargetList.Clear();
        foreach (var view in previousFocusTargets) if (view && !view.model.IsDead()) manager._focusTargetList.Add(view);
        manager.stopFocusByRencounter = previousStopFocus;
        manager.ChangeCamMode(previousFocusMode);
        foreach (var camera in previousFocusProjections) camera.RestoreProjection();
        previousFocusProjections.Clear();
        previousFocusTargets = null;
    }

    private static void RenderForCamera(UnityEngine.Camera camera)
    {
        var p = current;
        if (p == null || p.preparedFrame != Time.frameCount || !p.nativeCameras.Contains(camera)) return;
        var borrow = new NativeCamera { Camera = camera };
        borrow.Capture();
        renderStack.Add(borrow);
        try
        {
            // 只改当前摄像机，下一台摄像机使用同一份最终结果；不依赖父子层级继承。
            var frame = p.frame;
            camera.transform.SetPositionAndRotation(frame.Position, frame.Rotation);
            camera.orthographic = false;
            camera.fieldOfView = frame.Fov;
            camera.orthographicSize = frame.Size;
            if (frame.DirectPortrait) camera.nearClipPlane = .01f;
            camera.ResetProjectionMatrix();

        }
        catch
        {
            renderStack.RemoveAt(renderStack.Count - 1);
            borrow.Restore();
            throw;
        }
    }
    private static void RestoreAfterRender(UnityEngine.Camera camera)
    {
        if (renderStack.Count == 0) return;
        var borrow = renderStack[renderStack.Count - 1];
        if (borrow.Camera != camera) return;
        renderStack.RemoveAt(renderStack.Count - 1);
        borrow.Restore();
    }
    // 动作时钟只决定事件何时发生和动画采样位置；已启动的时间型效果消耗本帧时间。
    // 分段结束丢弃的余量和协程空帧仍推进缓动；一次提交积压也不能多消耗一帧时间。
    private void AdvancePending(float frameDelta, bool consumeEvents = true)
    {
        float pending = consumeEvents ? Mathf.Max(0f, realTime - evaluatedTime) : 0f;
        float scale = pending > 0f ? Mathf.Min(1f, frameDelta / pending) : 0f;
        AdvanceCameraTime(Mathf.Max(0f, frameDelta - pending));
        if (!consumeEvents) { ComposeCamera(); return; }
        Fire();
        while (evaluatedTime < realTime - .0000001f)
        {
            float dt = Mathf.Min(1f / 120f, realTime - evaluatedTime);
            float next = float.PositiveInfinity;
            if (nextCamera < orderedCameras.Length) next = Mathf.Min(next, orderedCameras[nextCamera].time);
            if (nextShake < orderedShakes.Length) next = Mathf.Min(next, orderedShakes[nextShake].time);
            if (nextHit < orderedHits.Length) next = Mathf.Min(next, orderedHits[nextHit]);
            // 跨帧事件在自身触发时间分段推进，避免把进入镜头的年龄多算一帧。
            if (next > sourceTime + .0000001f && sourceClock(evaluatedTime + dt, out _) > next)
            {
                float low = 0f, high = dt;
                for (int i = 0; i < 20; i++) { float mid = (low + high) * .5f; if (sourceClock(evaluatedTime + mid, out _) < next) low = mid; else high = mid; }
                dt = high;
            }
            evaluatedTime += dt; sourceTime = sourceClock(evaluatedTime, out actionRate);
            AdvanceHitClock(dt * scale); Fire(); AdvanceCameraState(dt * scale); ComposeCamera();
        }
        ComposeCamera();
    }
    private void AdvanceCameraTime(float delta)
    {
        while (delta > .0000001f)
        {
            float step = Mathf.Min(1f / 120f, delta);
            AdvanceHitClock(step);
            AdvanceCameraState(step);
            ComposeCamera();
            delta -= step;
        }
    }
    private void AdvanceHitClock(float dt)
    {
        hitPresentationTime += dt;
        for (int i = deferredHitTimes.Count - 1; i >= 0; i--)
        {
            if (deferredHitTimes[i] > hitPresentationTime + .0000001f) continue;
            if (cameraCue != null && cameraCue.phase2Damage && pendingSecondCamera != null) phase2Pending = true;
            deferredHitTimes.RemoveAt(i);
        }
    }
    private void Fire()
    {
        // Damage 先于同刻 Camera，与演示 Fire 的轨道顺序一致。
        while (nextHit < orderedHits.Length && orderedHits[nextHit] <= sourceTime + .0000001f)
        {
            if (orderedHitDelays[nextHit] > 0f) deferredHitTimes.Add(hitPresentationTime + orderedHitDelays[nextHit]);
            else if (cameraCue != null && cameraCue.phase2Damage && pendingSecondCamera != null) phase2Pending = true;
            nextHit++;
        }
        while (nextCamera < orderedCameras.Length && orderedCameras[nextCamera].time <= sourceTime + .0000001f) StartCamera(orderedCameras[nextCamera++]);
        while (nextShake < orderedShakes.Length && orderedShakes[nextShake].time <= sourceTime + .0000001f) { shake = orderedShakes[nextShake++]; shakeAge = 0f; shakeWave = new MotionCameraShakeMath.Wave(shake.eventId, shake.duration, shake.strength, shake.vibrato, shake.randomness, shake.fade, shake.direction); }
    }
    private void StartCamera(CameraCue c, bool nextPhase = false)
    {
        if (c.rotate) { rotateCue = c; rotateAge = 0f; sourceRotateStart = sourceRotateCurrent; if (cameraCue == null) rotationFrameFrom = overlayFrameRatio; return; }
        phase2Pending = false;
        pendingSecondCamera = c.nextCamera >= 0 && c.nextCamera < clip.Cameras.Length && clip.Cameras[c.nextCamera].enabled ? clip.Cameras[c.nextCamera] : null;
        cameraCue = c; cameraAge = 0f; BeginCameraOverlay(c, nextPhase);
    }
    private Vector3 FocusPoint(CameraCue cue, BattleUnitView unit)
    {
        Vector3 point = unit.WorldPosition;
        if (!cue.useActorY) point.y = nativeFocus.y;
        if (cue.focusCenter) point.y += FocusCenterHeight * ActionMotion.StageScale(unit) * RotationCenterScale;
        return point;
    }
    private Vector3 FocusDelta(CameraCue cue)
    {
        Vector3 own = FocusPoint(cue, actor);
        Vector3 other = FocusPoint(cue, target);
        Vector3 focus = cue.actor && cue.target ? Vector3.Lerp(own, other, cue.between >= 0f ? cue.between : .5f) : cue.actor ? own : cue.target ? other : nativeFocus;
        ReadSourceHeightFraming(cue, out float lift, out _);
        focus.y += lift + cue.yAxis * CameraEventUnits;
        return focus - nativeFocus;
    }
    // 缓动与震动只消耗 PrepareFrame 分配的帧时间；事件回调和渲染不另行计时。
    private void AdvanceCameraState(float dt)
    {

        if (cameraCue != null && pendingSecondCamera != null && ((!cameraCue.phase2Damage && cameraAge >= cameraCue.duration) || (phase2Pending && actionRate >= 1f))) StartCamera(pendingSecondCamera, true);
        if (cameraCue != null)
        {
            cameraAge += dt;
            if (cameraCue.v2) entryAge += dt;
            // 与 Unity 演示一致，V1/V2 焦点移动均独立于缩放曲线推进。
            float smooth = (cameraCue.blend < 0f ? .45f : cameraCue.blend) * .65f;
            if (smooth > 0f) { if (dt > 0f) eventFocus = Vector3.SmoothDamp(eventFocus, FocusDelta(cameraCue), ref eventFocusVelocity, smooth, Mathf.Infinity, dt); }
            else { eventFocus = FocusDelta(cameraCue); eventFocusVelocity = Vector3.zero; }
            if (!cameraCue.v2)
            {
                float u = Mathf.Clamp01(cameraAge / Mathf.Max(.001f, cameraCue.zoomDuration >= 0f ? cameraCue.zoomDuration : cameraCue.duration));
                AdvanceV1Zoom(Ease(cameraCue.ease, u, cameraCue.curve), dt);
            }
        }
        if (rotateCue != null)
        {
            rotateAge += dt;
            sourceRotateCurrent = Vector3.LerpUnclamped(sourceRotateStart, rotateCue.angle, RotationBlend());
            AdvanceRenderedRotation(dt);
        }
        if (shake != null && !MotionCameraShakeMath.Advance(ref shakeAge, dt, shake.duration)) shake = null;
    }
    // 相同的逻辑状态加上本帧基础取景，完整重算构图；不复用旧世界坐标偏移。
    private void ComposeCamera()
    {
        Vector3 pos = libraryFollowPosition;
        Quaternion rotation = libraryFollowRotation;
        float size = libraryFollowSize, distance = Vector3.Distance(nativeFocus, pos);
        if (cameraCue != null)
        {
            var c = cameraCue;
            float u = Mathf.Clamp01(cameraAge / Mathf.Max(.001f, c.v2 ? c.duration : c.zoomDuration >= 0f ? c.zoomDuration : c.duration));
            float blend = Ease(c.ease, u, c.curve);
            Vector3 offset = Vector3.zero;
            float zoom;
            if (c.v2)
            {
                zoom = Mathf.LerpUnclamped(c.currentStart ? sourceStartSize : c.startSize, c.endSize, c.axisCurves ? CameraValue(c.sizeCurve, u, u) : blend);
                offset = CameraOffsetSample(c, u, blend);
            }
            else
            {
                zoom = c.size; eventCameraOffset = Vector3.zero;
            }
            var e = EvaluateCameraOverlay(c, offset, zoom, blend, u); pos = e.position; size = e.halfHeight; distance = e.projectionDistance;
        }
        if (rotateCue != null)
        {
            rotation = UseStandardViewForOrbit ? libraryFollowRotation : libraryFollowRotation * Quaternion.Euler(renderedRotate);
            if (cameraCue == null) size = Mathf.Max(.001f, libraryFollowSize * Mathf.LerpUnclamped(rotationFrameFrom, rotateCue.frameScale, RotationBlend()));
        }
        Vector3 pivot = nativeFocus + (cameraCue != null ? eventFocus : Vector3.zero);
        if (rotateCue != null) pos = UseStandardViewForOrbit ? pos : pivot + (rotation * Quaternion.Inverse(libraryFollowRotation)) * (pos - pivot);
        overlayFrameRatio = size / libraryFollowSize;
        frame = new CameraFrame
        {
            Position = pos,
            Rotation = rotation,
            Size = size,
            Fov = 2f * Mathf.Atan(size / Mathf.Max(.0001f, cameraCue != null ? distance : Vector3.Distance(pivot, pos))) * Mathf.Rad2Deg,
            ShakeBasis = Matrix4x4.Rotate(rotation)
        };
    }
    private void SampleAnimationCamera()
    {
        if (portraitEnabled != null && !portraitEnabled()) return;
        animationSampler.BeginFrame();
        foreach (var entry in animationEntries)
        {
            var c = entry.Cue;
            float elapsed = groupSourceOffset + sourceTime - entry.Start;
            if (!ActionAnimation.IsInClipWindow(elapsed, c.duration, c.preRoll, c.postRoll, .0000001f, hold: c.hold || c.duration <= 0f)) continue;
            float duration = c.duration > 0f ? c.duration : Mathf.Max(0f, elapsed);
            float sampleTime = ActionAnimation.ClipTime(elapsed, duration, c.clipIn, c.speed);
            float weight = MotionCameraBlendMath.Weight(elapsed, c.duration, c.blendInDuration, c.blendOutDuration, c.blendInCurve, c.blendOutCurve);
            bool blend = c.blendInDuration > 0f || c.blendOutDuration > 0f;
            var subject = c.targetSubject ? target : actor;
            var other = c.targetSubject ? actor : target;
            float scale = ActionMotion.Scale(subject);
            if (!c.animation.directPortrait) scale *= RotationCenterScale;
            float direction = subject.model.direction == Direction.LEFT ? -1f : 1f;
            animationSampler.Sample(c.animation, sampleTime, subject.WorldPosition, scale, direction, other.WorldPosition, weight, blend);
        }
        if (!animationSampler.TryGet(out Vector3 position, out Quaternion rotation, out float fov)) return;
        frame.Position = position;
        frame.Rotation = rotation;
        frame.Fov = fov;
        frame.ShakeBasis = animationSampler.ShakeBasis;
        frame.DirectPortrait = animationSampler.IsDirectPortrait;
        frame.Animated = true;
    }
}

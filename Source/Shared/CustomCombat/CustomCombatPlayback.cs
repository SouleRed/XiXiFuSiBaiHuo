using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using 裁缝之王.Presentation.Actions;

namespace 裁缝之王.Presentation.CustomCombat;

// 动作组提供副目标姿态，本组件执行姿态、聚焦切层及逐骰结果显示。
[DefaultExecutionOrder(10000)]
public sealed class CustomCombatPlayback : MonoBehaviour
{
    private sealed class Body
    {
        public BattleUnitView View;

        public Vector3 Position;

        public bool PositionControlled;

        public ActionDetail? MotionInStep;

        public CustomCombatResolution.Hit Track;
        public Action<CustomCombatResolution.Hit, bool> Exit;
        public bool OwnsPosition, AddedToCamera;

        public Vector3 Translation;

        public Quaternion RotationTransform;

        public float DamageTime;

        public ActionDetail Motion;

        public bool LockY, Focused, Dead, VisualSuppressed;

        public Direction Direction;

        public BattleUnitView.RotationState Rotation;

        public readonly Dictionary<Transform, int> Layers = new();

        public Color SpriteColor;

        public readonly Dictionary<SpriteRenderer, Color> Colors = new();

        public readonly Dictionary<Canvas, KeyValuePair<string, int>> Sorting = new();
    }

    private static readonly CustomCombatActionPlan DefaultPlan = new();

    private readonly Dictionary<BattleUnitModel, Body> bodies = new();

    private BattleUnitView replacedFocusTarget;

    private int replacedFocusIndex;

    private CustomCombatRencounterManager manager;

    // 聚焦可以跨骰保留，动作计划只在本次正式动作内运行。
    private CustomCombatResolution.Frame activeFrame;

    private CustomCombatActionPlan plan;

    private List<CustomCombatActionPlan.Segment> secondaryTrack;

    private RencounterManager.MovingAction actorStep, opponentStep;

    private Body formalTarget;

    private CustomCombatResolution.Frame barrageFormation;

    private Body suppressedBarrageMain;

    public bool HasRunningDeath => bodies.Values.Any(body => body.View && body.View.IsRunningDeadEffect);

    public static CustomCombatPlayback Get(CustomCombatRencounterManager owner)
    {
        var playback = owner.GetComponent<CustomCombatPlayback>();
        if (!playback) playback = owner.gameObject.AddComponent<CustomCombatPlayback>();
        playback.manager = owner;
        return playback;
    }

    public void Capture(BattleUnitView enemy, BattleUnitView librarian)
    {
        Clear();
        foreach (var actor in new[] { enemy, librarian })
        {
            foreach (var result in actor.model.battleCardResultLog.BehaviourResultList)
            {
                var frame = CustomCombatResolution.Find(result);
                if (frame == null) continue;
                if (frame.IsRandomBarrage)
                {
                    foreach (var target in frame.Page.Targets) CaptureTarget(target);
                }
                if (frame.HasDifferentTarget) CaptureTarget(frame.Formal.Target);
                foreach (var hit in frame.ExternalHits) CaptureTarget(hit.Target);
            }
        }
    }

    private void CaptureTarget(CustomCombatResolution.Target target)
    {
        var unit = target.Unit;
        if (!unit.view || bodies.ContainsKey(unit)) return;
        var view = unit.view;
        bodies.Add(unit, new Body { View = view });
        var initial = target.Participant;
        view.unitBottomStatUI.UpdateStatUI(initial.Hp, initial.Break, initial.Buffs);
        SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfile(unit, unit.faction, initial.Hp, initial.Break, initial.Buffs);
    }

    internal static void BindFormal(ref RencounterManager.ActionAfterBehaviour self, CustomCombatActionPlan actionPlan,
        List<RencounterManager.MovingAction> reactions)
    {
        // 原版管理器下保留原回调和动作列表，不访问专用管理器或注册额外事件。
        var owner = CustomCombatRuntime.Playback;
        if (!owner || !owner.IsRunning || actionPlan == null) return;
        var frame = CustomCombatResolution.Find(self.behaviourResultData);
        if (frame == null) return;
        var playback = owner.SecondaryPlayback;
        var track = actionPlan.SecondaryTrack ?? CustomCombatActionPlan.CopyReactions(reactions);
        playback.Prepare(frame, actionPlan);
        var begin = self.startMoveRoutineEvent;
        self.startMoveRoutineEvent = () =>
        {
            if (playback && owner.IsRunning)
            {
                playback.BeginFormal(frame, actionPlan, track);
            }
            begin?.Invoke();
        };
    }

    public void BeforeRoll(BattleCardBehaviourResult enemy, BattleCardBehaviourResult librarian)
    {
        var next = manager.CurrentFrame;
        if (activeFrame != null)
        {
            if (next == null || next.Page != activeFrame.Page
                || (barrageFormation == null && next.ActualTarget != activeFrame.ActualTarget)
                || !KingContinuation.IsExpected(next.Record))
            {
                EndTracks();
            }
        }

        if (next?.IsRandomBarrage == true)
        {
            if (next.IsContinuation && next.Main.view.IsReadyDeadScene) next.Main.view.StartDeadEffect();
            return;
        }
        if (suppressedBarrageMain != null) RestoreBarrageMain();
        if (!manager.HasDifferentFrameTarget)
        {
            LeaveFormalFocus();
            return;
        }
        if (next.IsContinuation && next.Main.view.IsReadyDeadScene) next.Main.view.StartDeadEffect();
        if (formalTarget?.View != next.ActualTarget.view)
        {
            LeaveFormalFocus();
            formalTarget = bodies[next.ActualTarget];
            LeaveTrack(formalTarget, false);
            EnterFocus(formalTarget, next.Main.view);
        }
        if (next.IsContinuation) replacedFocusTarget = null;
    }

    internal void EndFrame(CustomCombatResolution.Frame frame)
    {
        if (frame?.Formal.Receiver.isDead != true) return;
        if (frame.IsRandomBarrage)
        {
            if (bodies.TryGetValue(frame.ActualTarget, out var body))
            {
                body.Dead = true;
                RemoveCameraTarget(body);
                SingletonBehavior<BattleCamManager>.Instance._focusTargetList.Remove(body.View);
            }
            return;
        }
        if (manager.HasDifferentFrameTarget)
        {
            if (formalTarget != null)
            {
                formalTarget.Dead = true;
                RemoveCameraTarget(formalTarget);
            }
            RestoreReplacedFocusTarget();
        }
    }

    private CustomCombatResolution.Frame preparedFrame;

    private CustomCombatActionPlan preparedPlan;

    private void Prepare(CustomCombatResolution.Frame frame, CustomCombatActionPlan actionPlan)
    {
        preparedFrame = frame;
        preparedPlan = actionPlan;
    }

    internal bool HasPlan(BattleCardBehaviourResult record) => preparedFrame?.Record == record && preparedPlan != null;

    internal void BeginMotion(CustomCombatResolution.Frame frame)
    {
        if (frame?.IsRandomBarrage != true) RestoreBarrageMain();
        if (frame == null) return;
        var actionPlan = preparedFrame == frame ? preparedPlan : DefaultPlan;
        Prepare(frame, actionPlan);
        if (actionPlan == DefaultPlan)
        {
            BeginFormal(frame, actionPlan, null);
        }
    }

    internal void EndMotion()
    {
        var frame = preparedFrame;
        var actionPlan = preparedPlan;
        if (frame == null || actionPlan == null) return;
        FinishFormal(frame);
        bool retain = (actionPlan != DefaultPlan || frame.IsRandomBarrage) && manager.CanContinueSecondaryFocus(frame.Record, barrageFormation != null);
        if (!retain) EndTracks();
        preparedFrame = null;
        preparedPlan = null;
    }

    private void BeginFormal(CustomCombatResolution.Frame frame, CustomCombatActionPlan actionPlan,
        List<CustomCombatActionPlan.Segment> track)
    {
        if (!manager || !manager.IsRunning) return;
        if (activeFrame != null && activeFrame.Page != frame.Page) EndTracks();
        activeFrame = frame;
        plan = actionPlan;
        secondaryTrack = track;
        actorStep = opponentStep = null;
        if (frame.IsRandomBarrage)
        {
            BeginBarrageGroup(frame, actionPlan);
            if (frame.IsContinuation && frame.Main?.view && frame.Main.view.IsReadyDeadScene)
                frame.Main.view.StartDeadEffect();
            if (manager.HasDifferentFrameTarget)
            {
                var actor = frame.Page.Card.owner?.view;
                var victim = frame.ActualTarget?.view;
                if (actor && victim)
                {
                    actor.model.UpdateDirection(victim.WorldPosition);
                    victim.model.UpdateDirection(actor.WorldPosition);
                }
            }
        }
        foreach (var body in bodies.Values)
        {
            if (!body.Dead && body.Track != null && !frame.ExternalHits.Exists(hit => hit.Unit.view == body.View)) LeaveTrack(body, false);
        }

        Vector3[] positions = null;
        if (!frame.IsRandomBarrage && plan.DefaultPosition && plan.InitialPositions != null && frame.ExternalHits.Count > 0)
            positions = plan.InitialPositions(frame.ExternalHits.Count);

        int positionIndex = 0;
        foreach (var hit in frame.ExternalHits)
        {
            int currentSlot = positionIndex++;
            if (!bodies.TryGetValue(hit.Unit, out Body body) || body.Dead || !body.View) continue;
            bool focus = plan.DefaultFocus;
            bool position = plan.DefaultPosition;
            if (body.Track != null && (body.Focused != focus || body.OwnsPosition != position)) LeaveTrack(body, false);
            body.MotionInStep = null;
            if (body.Track != null) continue;

            body.Track = hit;
            body.OwnsPosition = position;
            if (position)
            {
                var view = body.View;
                if (!body.PositionControlled) body.Position = view.WorldPosition;
                body.Motion = view.charAppearance.GetCurrentMotionDetail();
                body.LockY = view._bLockPosY;
                body.Direction = view.model.direction;
                body.Rotation = view.rotState;
                body.Translation = view.characterTranslationCenter.localPosition;
                body.RotationTransform = view.characterRotationCenter.localRotation;
                body.DamageTime = view._remainDmgTime;
                view.model.moveDetail.Stop();
                view.LockPosY(false);
                if (!frame.IsRandomBarrage)
                {
                    if (positions != null && currentSlot < positions.Length)
                        body.View.WorldPosition = positions[currentSlot];
                    else if (plan.InitialOffset != null)
                        body.View.WorldPosition = frame.ActualTarget.view.WorldPosition + plan.InitialOffset(hit.Target.Index);
                }
            }
            body.PositionControlled = position;
            if (focus) EnterFocus(body, null);
            if (!frame.IsRandomBarrage)
            {
                body.Exit = plan.OnExit;
                plan.OnEnter?.Invoke(hit);
            }
        }
        ApplyPlan();
    }

    // 广域乱射采用群攻式全员聚焦排位，但仅实际受击目标执行受击与扣血。
    private void BeginBarrageGroup(CustomCombatResolution.Frame frame, CustomCombatActionPlan actionPlan)
    {
        if (barrageFormation != null) return;
        barrageFormation = frame;
        var anchor = !frame.IsContinuation && frame.Main?.view ? frame.Main : frame.ActualTarget;
        var origin = anchor.view.WorldPosition;
        var attackerPos = frame.Page.Card.owner?.view ? frame.Page.Card.owner.view.WorldPosition : origin;
        var others = new List<Body>();
        foreach (var target in frame.Page.Targets)
        {
            if (target.Unit == frame.Main) continue;
            if (!bodies.TryGetValue(target.Unit, out var body) || !body.View || body.Dead) continue;
            others.Add(body);
        }
        if (others.Count == 0) return;

        bool canPosition = actionPlan.DefaultPosition && (actionPlan.InitialPositions != null || actionPlan.InitialOffset != null);
        Vector3[] positions = canPosition ? actionPlan.InitialPositions?.Invoke(others.Count) : null;
        int offsetIndex = 0;
        for (int i = 0; i < others.Count; i++)
        {
            var body = others[i];
            if (canPosition && body.View.model != anchor)
            {
                if (!body.PositionControlled) body.Position = body.View.WorldPosition;
                body.PositionControlled = true;
                body.View.model.moveDetail.Stop();
                body.View.WorldPosition = positions != null && i < positions.Length
                    ? positions[i]
                    : origin + actionPlan.InitialOffset(offsetIndex++);
                body.View.model.UpdateDirection(attackerPos);
            }
            if (actionPlan.DefaultFocus && !body.Focused) EnterFocus(body, null);
        }
    }

    private void SwitchBarrageFocus(CustomCombatResolution.Frame frame, bool soloFocus)
    {
        if (!manager.HasDifferentFrameTarget)
        {
            LeaveFormalFocus();
            RestoreBarrageMain();
            return;
        }
        if (soloFocus)
        {
            if (suppressedBarrageMain == null && bodies.TryGetValue(frame.Main, out var main))
            {
                SuppressBarrageMain(main, frame.IsContinuation);
            }
            if (frame.IsContinuation && suppressedBarrageMain != null)
            {
                suppressedBarrageMain.Dead = true;
                if (suppressedBarrageMain.View.IsReadyDeadScene) suppressedBarrageMain.View.StartDeadEffect();
            }
        }
        else if (frame.IsContinuation && frame.Main?.view && frame.Main.view.IsReadyDeadScene)
        {
            frame.Main.view.StartDeadEffect();
        }

        if (formalTarget?.View == frame.ActualTarget.view) return;
        LeaveFormalFocus();
        formalTarget = bodies[frame.ActualTarget];
        LeaveTrack(formalTarget, false);
        EnterFocus(formalTarget, null);
    }

    private void SuppressBarrageMain(Body main, bool isContinuation)
    {
        suppressedBarrageMain = main;
        var view = main.View;
        SingletonBehavior<BattleCamManager>.Instance._focusTargetList.Remove(view);
        if (isContinuation || !view) return;

        SaveVisualSnapshot(main);
        main.VisualSuppressed = true;

        view.charAppearance.ChangeLayer("Character");
        view.unitBottomStatUI.ChangeLayer("CharacterUI", "Character");
        view.unitBottomStatUI.ChangeChildCanvasOrder("CharacterUI", 0);
        view.unitBottomStatUI.EnableCanvas(false);
        view.unitBottomStatUI.SetIdentityLightActive(false);
        RencounterFocus.Dim(view.charAppearance, main.Colors);
        view.model.OnLayerChanged("Character");
    }

    private void RestoreBarrageMain()
    {
        if (suppressedBarrageMain == null) return;
        LeaveFormalFocus();
        var main = suppressedBarrageMain;
        suppressedBarrageMain = null;
        bool wasSuppressed = main.VisualSuppressed;
        main.VisualSuppressed = false;
        var view = main.View;
        if (!wasSuppressed || main.Dead || !view || view.model.IsDead() || view.IsReadyDeadScene) return;

        RestoreVisualSnapshot(main);
        view.charAppearance.ChangeLayer("UI_WORLD");
        view.unitBottomStatUI.ChangeLayer("UI_WORLD", "UI_WORLD");
        view.unitBottomStatUI.ChangeChildCanvasOrder("Character", 0);
        view.unitBottomStatUI.EnableCanvas(true);
        view.unitBottomStatUI.SetIdentityLightActive(true);
        view.model.OnLayerChanged("UI_WORLD");

        var targets = SingletonBehavior<BattleCamManager>.Instance._focusTargetList;
        if (!targets.Contains(view)) targets.Add(view);
    }

    private static void SaveVisualSnapshot(Body body)
    {
        var view = body.View;
        body.Layers.Clear();
        foreach (var transform in view.GetComponentsInChildren<Transform>(true)) body.Layers.Add(transform, transform.gameObject.layer);
        body.Sorting.Clear();
        foreach (var canvas in view.unitBottomStatUI.GetComponentsInChildren<Canvas>(true)) body.Sorting.Add(canvas, new KeyValuePair<string, int>(canvas.sortingLayerName, canvas.sortingOrder));
        body.SpriteColor = view.charAppearance.CurSpritesColor;
        body.Colors.Clear();
    }

    private static void RestoreVisualSnapshot(Body body)
    {
        var view = body.View;
        foreach (var pair in body.Layers) if (pair.Key) pair.Key.gameObject.layer = pair.Value;
        if (!body.Dead)
        {
            foreach (var pair in body.Colors) if (pair.Key) pair.Key.color = pair.Value;
            view.charAppearance._curSpriteColor = body.SpriteColor;
        }
        foreach (var pair in body.Sorting)
        {
            if (!pair.Key) continue;
            pair.Key.sortingLayerName = pair.Value.Key;
            pair.Key.sortingOrder = pair.Value.Value;
        }
    }

    private void ApplyPlan()
    {
        if (plan == null || activeFrame.IsRandomBarrage) return;
        var pose = plan.Sample?.Invoke() ?? default;
        foreach (var hit in activeFrame.ExternalHits)
        {
            if (!bodies.TryGetValue(hit.Unit, out var body) || body.Dead || !body.View) continue;
            var sampled = pose;
            if (plan.SecondaryTrack != null && body.MotionInStep.HasValue) sampled.Motion = body.MotionInStep;
            else sampled.Motion ??= body.MotionInStep;
            ApplyPose(body.View, sampled);
        }
    }

    private static void ApplyPose(BattleUnitView view, CustomCombatActionPlan.TargetPose pose)
    {
        if (pose.Motion.HasValue && view.charAppearance.GetCurrentMotionDetail() != pose.Motion.Value) view.charAppearance.ChangeMotion(pose.Motion.Value);
        if (pose.Direction.HasValue) view.model.direction = pose.Direction.Value;
        if (pose.Rotation.HasValue) view.rotState = pose.Rotation.Value;
        if (pose.RotationTransform.HasValue) view.characterRotationCenter.localRotation = pose.RotationTransform.Value;
        if (pose.Translation.HasValue) view.characterTranslationCenter.localPosition = pose.Translation.Value;
        if (pose.DamageTime.HasValue) view._remainDmgTime = pose.DamageTime.Value;
    }

    // 兼容裁缝之王时间轴按本骰聚焦副目标派发受击特效与击退。
    internal void ForEachFocusedTarget(BattleCardBehaviourResult record, Action<BattleUnitView> apply)
    {
        if (plan == null || activeFrame?.Record != record || activeFrame.IsRandomBarrage) return;
        foreach (var hit in activeFrame.ExternalHits)
        {
            if (bodies.TryGetValue(hit.Unit, out Body body) && !body.Dead && body.Focused && body.View) apply(body.View);
        }
    }

    internal void BeginMainAction(BattleUnitView view, RencounterManager.MovingAction action)
    {
        if (secondaryTrack == null || activeFrame.IsRandomBarrage || action == null) return;
        bool reaction = view == activeFrame.ActualTarget.view;
        ref var last = ref (reaction ? ref opponentStep : ref actorStep);
        if (last == action) return;
        last = action;
        foreach (var segment in secondaryTrack)
        {
            if (segment.Cue != action) continue;
            foreach (var hit in activeFrame.ExternalHits)
            {
                if (!bodies.TryGetValue(hit.Unit, out var body) || body.Dead || !body.View) continue;
                body.MotionInStep = segment.Motion;
                if (segment.Motion.HasValue) body.View.charAppearance.ChangeMotion(segment.Motion.Value);
                if (segment.MoveState.HasValue) body.View.KnockDown(segment.MoveState == CharMoveState.KnockDown);
                segment.OnStart?.Invoke(hit);
            }
        }
    }

    private void EnterFocus(Body body, BattleUnitView replace)
    {
        var view = body.View;
        body.Focused = true;
        ReplaceFocusTarget(replace);
        var cameraTargets = SingletonBehavior<BattleCamManager>.Instance._focusTargetList;
        SaveVisualSnapshot(body);
        view.charAppearance.ChangeLayer("UI_WORLD");
        RencounterFocus.Highlight(view.charAppearance, body.Colors);
        view.unitBottomStatUI.ChangeLayer("UI_WORLD", "UI_WORLD");
        view.unitBottomStatUI.ChangeChildCanvasOrder("Character", 0);
        view.unitBottomStatUI.EnableCanvas(true);
        view.unitBottomStatUI.SetIdentityLightActive(true);
        view.model.OnLayerChanged("UI_WORLD");

        if (!cameraTargets.Contains(view))
        {
            cameraTargets.Add(view);
            body.AddedToCamera = true;
        }
    }

    internal void Print(CustomCombatResolution.Frame frame)
    {
        if (frame == null || frame.Printed || !manager || !manager.IsRunning) return;
        frame.Printed = true;
        if (frame.HasDifferentTarget && !manager.HasDifferentFrameTarget)
        {
            using var target = new CustomCombatResolution.TargetScope(frame.Page.Card, frame.Formal.Target);
            PrintReceiver(frame, frame.ActualTarget, frame.Formal.Receiver, printEffect: true);
            frame.Formal.Receiver.takeDamageEvent?.Invoke();
        }
        foreach (var hit in frame.ExternalHits)
        {
            if (!hit.Unit.view) continue;
            using var target = new CustomCombatResolution.TargetScope(frame.Page.Card, hit.Target);
            PrintReceiver(frame, hit.Unit, hit.Receiver, printEffect: false);
            hit.Sender.afterActionEvent?.Invoke();
            hit.Sender.succeedAtkEvent?.Invoke();
            hit.Receiver.takeDamageEvent?.Invoke();
            hit.Sender.takeDamageEvent?.Invoke();
        }
    }

    internal static void PrintReceiver(CustomCombatResolution.Frame frame, BattleUnitModel unit, BattleCardBehaviourResult record, bool printEffect)
    {
        var view = unit.view;
        if (printEffect) record.printDamagedEffectEvent?.Invoke();
        foreach (var damage in record.dmgListTaken) SingletonBehavior<AttackEffectManager>.Instance.CreateDamagedTextEffect(damage.dmg, damage.damagedType, unit, frame.Page.Card.owner, damage.atkResist, false);
        foreach (var damage in record.breakListTaken) view.BreakDamaged(damage.dmg, damage.damagedType, frame.Page.Card.owner, damage.atkResist);
        foreach (int recovered in record.recoveredHpList) view.RecoverHp(recovered);
        foreach (int recovered in record.recoveredBpList) view.RecoverBp(recovered);
        var ui = SingletonBehavior<BattleManagerUI>.Instance;
        foreach (var coin in record.GetAddedEmotionCoins()) ui.ui_battleEmotionCoinUI.OnAcquireCoin(unit, coin, 1);
        view.unitBottomStatUI.UpdateStatUI(record.curHp, record.curBreakGauge, record.bufDataList);
        ui.ui_unitListInfoSummary.UpdateCharacterProfile(unit, unit.faction, record.curHp, record.curBreakGauge, record.bufDataList);
    }

    private void FinishFormal(CustomCombatResolution.Frame frame)
    {
        if (activeFrame != frame || !manager || !manager.IsRunning) return;
        Print(frame);
        ApplyPlan();
        // 结束当前计划但保留聚焦状态；接续等待期间冻结最后姿态。
        plan = null;
        secondaryTrack = null;
        actorStep = opponentStep = null;
        if (frame.HasDifferentTarget && !manager.HasDifferentFrameTarget) FinishReceiver(frame.Formal);
        foreach (var hit in frame.ExternalHits) FinishReceiver(hit);
    }

    private void FinishReceiver(CustomCombatResolution.Hit hit)
    {
        if (!hit.Receiver.isDead || !bodies.TryGetValue(hit.Unit, out Body body) || body.Dead) return;
        body.Dead = true;
        // 死亡演出从当前受击姿态开始，图层与退出回调留到统一退场。
        RemoveCameraTarget(body);
        if (body.View) body.View.StartDeadEffect();
    }

    private static void RemoveCameraTarget(Body body)
    {
        if (!body.AddedToCamera) return;
        SingletonBehavior<BattleCamManager>.Instance._focusTargetList.Remove(body.View);
        body.AddedToCamera = false;
    }

    // 退出聚焦只恢复显示与姿态，保留聚集和击退后的世界位置。
    private static void LeaveTrack(Body body, bool interrupted)
    {
        if (body.Track == null) return;
        var hit = body.Track;
        body.Track = null;
        body.Exit?.Invoke(hit, interrupted);
        body.Exit = null;
        var view = body.View;
        if (!view) return;
        if (body.OwnsPosition)
        {
            view.model.moveDetail.Stop();
            view.LockPosY(body.LockY);
            view.model.direction = body.Direction;
            view.rotState = body.Rotation;
            view.characterTranslationCenter.localPosition = body.Translation;
            view.characterRotationCenter.localRotation = body.RotationTransform;
            view._remainDmgTime = body.DamageTime;
            if (!body.Dead) view.charAppearance.ChangeMotion(body.Motion);
            body.OwnsPosition = false;
        }
        LeaveFocus(body);
    }

    private static void LeaveFocus(Body body)
    {
        if (!body.Focused) return;
        var view = body.View;
        RemoveCameraTarget(body);
        RestoreVisualSnapshot(body);
        view.charAppearance.ChangeLayer("Character");
        view.unitBottomStatUI.ChangeLayer("CharacterUI", "Character");
        view.unitBottomStatUI.ChangeChildCanvasOrder("CharacterUI", 0);
        view.unitBottomStatUI.EnableCanvas(false);
        view.unitBottomStatUI.SetIdentityLightActive(false);
        view.model.OnLayerChanged("Character");
        body.Focused = false;
    }

    private void LeaveFormalFocus()
    {
        if (formalTarget == null) return;
        LeaveFocus(formalTarget);
        formalTarget = null;
        RestoreReplacedFocusTarget();
    }

    private void ReplaceFocusTarget(BattleUnitView previous)
    {
        if (!previous) return;
        var targets = SingletonBehavior<BattleCamManager>.Instance._focusTargetList;
        int index = targets.IndexOf(previous);
        if (index < 0) return;
        replacedFocusTarget = previous;
        replacedFocusIndex = index;
        targets.RemoveAt(index);
    }

    private void RestoreReplacedFocusTarget()
    {
        if (replacedFocusTarget)
        {
            var targets = SingletonBehavior<BattleCamManager>.Instance._focusTargetList;
            if (!targets.Contains(replacedFocusTarget)) targets.Insert(Math.Min(replacedFocusIndex, targets.Count), replacedFocusTarget);
        }
        replacedFocusTarget = null;
    }

    internal void EndTracks(bool interrupted = false)
    {
        try
        {
            foreach (var body in bodies.Values)
            {
                LeaveTrack(body, interrupted);
                if (barrageFormation != null && body.Focused) LeaveFocus(body);
            }
        }
        finally
        {
            activeFrame = null;
            barrageFormation = null;
            plan = null;
            secondaryTrack = null;
            actorStep = opponentStep = null;
        }
    }

    public void Clear(bool restorePositions = false)
    {
        try
        {
            EndTracks(restorePositions);
            RestoreBarrageMain();
            LeaveFormalFocus();
            if (restorePositions)
            {
                // 异常中断才回退位置，包含此前已经正常退出聚焦的副目标。
                foreach (var body in bodies.Values)
                {
                    if (!body.View || !body.PositionControlled) continue;
                    body.View.model.moveDetail.Stop();
                    body.View.WorldPosition = body.Position;
                }
            }
        }
        finally
        {
            preparedFrame = null;
            preparedPlan = null;
            suppressedBarrageMain = null;
            bodies.Clear();
        }
    }

    private void LateUpdate()
    {
        if (!manager || !manager.IsRunning || plan == null) return;
        try
        {
            ApplyPlan();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            manager.EndRencounterForcely();
        }
    }
}

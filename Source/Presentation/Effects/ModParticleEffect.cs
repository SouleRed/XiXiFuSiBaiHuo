using System;
using System.Collections.Generic;
using Battle.DiceAttackEffect;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;


using 裁缝之王.Presentation.Actions;

namespace 裁缝之王.Presentation.Effects;

public enum ParticleEndMode { Natural, Timed, StopEmission, Handoff, GroupEnd }
public enum ParticleClock { Effect, Action, Presentation }

// 单项特效的资源、粒子播放和清理；演出顺序由原生动作组负责。
public abstract class ModParticleEffect : DiceAttackEffect
{


    protected AssetBundle bundle;

    protected BattleUnitView actor, victim;

    protected CharacterAppearance appearance;

    protected float unitScale;

    protected bool completed;

    private bool disposed;

    private float age;

    private float emptyTime;

    private float trailTail;

    private float emissionWindow;

    private bool hasLoop;

    private ParticleSystem[] roots = Array.Empty<ParticleSystem>();

    private ParticleSystem[] systems = Array.Empty<ParticleSystem>();
    private Renderer[] timelineRenderers = Array.Empty<Renderer>();
    private TrailRenderer[] timelineTrails = Array.Empty<TrailRenderer>();
    private readonly MaterialPropertyBlock timelineProperties = new();

    private static readonly HashSet<ModParticleEffect> liveEffects = new();

    public Action Interrupted;

    public float ParticleSpeed = 1f;

    // 源事件的播放倍率独立于命中慢放，恢复速度时仍保留该倍率。
    public float PlaybackSpeed = 1f;

    // 仅长期/循环特效使用主动截止时长；一次性特效等待自身粒子结束。
    public float PlaybackDuration = -1f;

    // 原生动作转译的单实例寿命；未配置时仍沿用现有特效类规则。
    private int timelineMode = -1, timelineClock;
    private float timelineDuration, timelineExpected, timelineActionAge, timelinePresentationAge;
    private bool timelineEmissionStopped;
    private bool timelineReleaseCoin, timelineReleaseGroup;
    private Transform timelinePoint;
    private Vector3 timelinePosition;
    private bool timelineFollow;
    private float hitFacing;
    public float ActionSpeed = 1f;
    internal RencounterEffects.State TimelineRates;
    public bool TimelineControlled => timelineMode >= 0;

    // 源事件使用外观挂点时，替代旧派生类在初始化阶段的世界空间脱离。
    public void ConfigureTimelineAttachment()
    {
        transform.SetParent(_selfTransform, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    public void ConfigureTimelinePosition(BattleUnitView reference, bool center, bool follow, bool initialGround)
    {
        timelinePoint = center ? reference.atkEffectRoot : reference.transform;
        timelinePosition = center ? timelinePoint.position : reference.WorldPosition;
        if (initialGround) timelinePosition.y = 0f;
        timelineFollow = follow;
        // 受击根在皮肤之外；持续受击实例挂在该根，保持已有世界尺寸并独立表达方向。
        // 世界根定位与跟随不会因此获得镜头旋转。
        if (center && follow && transform.parent != reference.atkEffectRoot)
        {
            transform.SetParent(reference.atkEffectRoot, true);
            ConfigureHitFacing();
            ApplyCameraAlignment();
        }
        transform.position = timelinePosition;
    }

    private sealed class HandoffPose
    {
        public Transform Parent, Point;
        public Vector3 LocalPosition, LocalScale, Position;
        public Quaternion LocalRotation;
        public bool Follow;
    }
    private HandoffPose handoffPose;
    private bool handoffConsumed;
    public bool HasTimelineHandoff => !handoffConsumed && (Instance || handoffPose != null);
    private HandoffPose CaptureHandoffPose() => new() { Parent = transform.parent, Point = timelinePoint, LocalPosition = transform.localPosition, LocalRotation = transform.localRotation, LocalScale = transform.localScale, Position = timelinePosition, Follow = timelineFollow };
    public void InheritTimelinePosition(ModParticleEffect previous)
    {
        var pose = previous.Instance ? previous.CaptureHandoffPose() : previous.handoffPose;
        timelinePoint = pose.Point;
        timelinePosition = pose.Position;
        timelineFollow = pose.Follow;
        transform.SetParent(pose.Parent, false);
        transform.localPosition = pose.LocalPosition;
        transform.localRotation = pose.LocalRotation;
        transform.localScale = pose.LocalScale;
        previous.handoffConsumed = true;
        previous.handoffPose = null;
    }

    // 只写入已转译的局部姿态；事件时间与曲线仍在各动作组中显式编排。
    public void SetTimelinePose(string path, Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null, Vector3? eulerAngles = null)
    {
        var node = string.IsNullOrEmpty(path) ? Instance.transform : Instance.transform.Find(path);
        if (position.HasValue) node.localPosition = position.Value;
        if (rotation.HasValue) node.localRotation = rotation.Value;
        if (scale.HasValue) node.localScale = scale.Value;
        if (eulerAngles.HasValue) node.localEulerAngles = eulerAngles.Value;
    }

    private bool timelineSourceTimed, timelineIgnoreSpeed;
    private float timelineActivationDuration, timelineDisableAge;
    private bool timelineWindowReleased;
    private Func<float> timelineSourceClock;
    private float timelineSourceStart;
    private int lastOwnedFrame = -1;
    private int timelineJoinedFrame = -1;
    internal bool JoinedTimelineThisFrame => timelineJoinedFrame == Time.frameCount;

    internal void AttachTimeline(RencounterEffects.State state)
    {
        TimelineRates = state;
        timelineJoinedFrame = Time.frameCount;
    }

    internal float GetTimelineStepStart(Func<float> sourceClock, ActionClock clock, float start) =>
        sourceClock != null && timelineSourceClock == sourceClock ? Mathf.Max(start, clock.PresentationAt(timelineSourceStart)) : start;

    internal void BindSourceWindow(Func<float> sourceClock, float sourceStart)
    {
        timelineSourceClock = sourceClock;
        timelineSourceStart = sourceStart;
    }

    internal void DetachSourceWindow(Func<float> sourceClock)
    {
        if (timelineSourceClock == null || timelineSourceClock != sourceClock) return;
        timelineActionAge = Mathf.Max(0f, timelineSourceClock() - timelineSourceStart);
        timelineWindowReleased |= timelineActionAge + .000001f >= timelineActivationDuration;
        timelineSourceClock = null;
    }

    private bool AdvanceSourceWindow(float presentationDelta)
    {
        if (presentationDelta <= 0f) return false;
        // 窗口退出与停用计时分开；退出后即使源时间冻结也不重新关上门槛。
        // 原生特效timer不等于粒子simulationSpeed，零延迟在下一生命周期更新关闭。
        if (!timelineWindowReleased)
        {
            timelineWindowReleased = timelineActionAge + .000001f >= timelineActivationDuration;
            return false;
        }
        if (timelineDuration <= 0f || timelineDisableAge > timelineDuration) return true;
        timelineDisableAge += presentationDelta;
        return false;
    }
    public void ConfigureTimeline(ParticleEndMode mode, ParticleClock clock, float duration, float expectedDuration, bool releaseAtCoinEnd = false, bool releaseAtGroupEnd = false, bool sourceTimed = false, float activationDuration = 0f, bool ignoreSpeed = false)
    {
        timelineIgnoreSpeed = ignoreSpeed; timelineSourceTimed = sourceTimed; timelineActivationDuration = activationDuration; timelineDisableAge = 0f; timelineWindowReleased = false;
        timelineMode = (int)mode;
        timelineClock = (int)clock;
        timelineDuration = duration;
        timelineExpected = expectedDuration;
        timelineReleaseCoin = releaseAtCoinEnd;
        timelineReleaseGroup = releaseAtGroupEnd;
    }

    // 与 Unity ParticleLifetime.ReleaseCoin / ReleaseGroup 使用相同策略。
    private bool ReleaseCoinTimeline()
    {
        if (!timelineReleaseCoin) return false;
        if (timelineMode == (int)ParticleEndMode.StopEmission) { StopTimelineEmission(); return false; }
        return timelineMode == (int)ParticleEndMode.Timed || timelineMode == (int)ParticleEndMode.GroupEnd;
    }

    public void ReleaseTimeline(bool groupEnd)
    {
        if (groupEnd && timelineReleaseGroup && timelineMode == (int)ParticleEndMode.StopEmission) { StopTimelineEmission(); return; }
        bool release = groupEnd ? timelineMode == (int)ParticleEndMode.Handoff || timelineMode == (int)ParticleEndMode.GroupEnd || timelineReleaseGroup || ReleaseCoinTimeline() : ReleaseCoinTimeline();
        if (release) CloseTimeline();
    }

    internal void ReleaseRencounter(bool interrupted)
    {
        DetachSourceWindow(timelineSourceClock);
        TimelineRates = null;
        Interrupted = null;
        ManagedByRencounter = false;
        ActionSpeed = ParticleSpeed = 1f;
        if (interrupted) { CloseTimeline(); return; }
        if (TimelineControlled) ReleaseTimeline(true);
        else StopEffect();
        // 自然/定时尾迹保留原挂载及实例时钟，不因清除对抗引用而销毁或重新生成。
    }

    public void CloseTimeline()
    {
        // 保留前段的定位数据而非可见对象；字段引用由整次对抗持有，后段只消费一次。
        if (timelineMode == (int)ParticleEndMode.Handoff && !handoffConsumed && Instance) handoffPose = CaptureHandoffPose();
        Abort();
    }

    public void StopTimelineEmission()
    {
        if (timelineEmissionStopped) return;
        timelineEmissionStopped = true;
        foreach (var particles in systems) if (particles) { var emission = particles.emission; emission.enabled = false; }
        foreach (var particles in roots) if (particles) particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // 对应实例链共享布局；保留各子粒子的固定种子关系，不统一覆写成同一种子。
    public void SetTimelineSeeds(uint batchSeed, bool randomizeAuthored)
    {
        foreach (var particles in roots) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (var particles in systems)
        {
            uint seed = particles.useAutoRandomSeed ? batchSeed : randomizeAuthored ? (uint)(((ulong)particles.randomSeed + batchSeed - 1UL) % uint.MaxValue + 1UL) : particles.randomSeed;
            particles.useAutoRandomSeed = false;
            particles.randomSeed = seed == 0 ? 1u : seed;
        }
        foreach (var particles in roots) { particles.Simulate(0f, true, true, true); particles.Pause(true); }
    }

    public bool ManagedByRencounter { get; set; }

    public GameObject Instance { get; private set; }

    protected virtual AssetBundle EffectBundle => NativeKingSkin.LoadedBundle;

    protected virtual string PrefabName => null;

    protected virtual string PrefabFolder => "";

    protected virtual bool IgnoreParticleSpeed => false;

    protected virtual float Lifetime => 2f;

    protected virtual bool HoldAtEnd => false;

    protected virtual bool TimedEnd => false;

    // 黑派良一次性特效独立播放至粒子耗尽，不用动作窗口或空父节点Loop标记截止。
    protected virtual bool NaturalLifetime => false;

    protected virtual bool HardFinish => !NaturalLifetime && (HoldAtEnd || TimedEnd || hasLoop);

    // LimbusFxRoot 用 Y 轴半转对齐皮肤的原生朝向；尺寸由 Common 和各专属挂点提供。
    protected virtual string MountName => null;

    protected virtual Transform Attachment => appearance.transform.Find(MountName == null ? "LimbusFxRoot/Common" : "LimbusFxRoot/" + MountName);

    protected virtual BattleUnitView WorldOrigin => null;

    protected float WorldFacing => actor.model.direction == Direction.LEFT ? -1f : 1f;

    protected virtual bool OnGround => false;

    protected virtual CharacterAppearance ResolveAppearance(BattleUnitView self, BattleUnitView target) => self.charAppearance;

    // 挂点已继承地图缩放，只补原公共工具的身高倍率，避免重复叠加地图比例。
    protected static float GetAttachedHeightScale(BattleUnitModel model)
    {
        int height = model?.UnitData?.unitData?.customizeData?.height ?? 0;
        return height > 0 ? height / 170f : 1f;
    }

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        _bHasDamagedEffect = true;
        _self = self.model;
        actor = self;
        victim = target;
        hitFacing = target && self.WorldPosition.x > target.WorldPosition.x ? -1f : 1f;
        appearance = ResolveAppearance(self, target);
        bundle = EffectBundle;
        unitScale = Mathf.Abs(appearance.transform.lossyScale.y) / 0.5f;
        _selfTransform = Attachment;
        if (WorldOrigin)
        {
            // 世界特效保留初始旋转；原始巴士资源朝右，按施放者朝向镜像。
            // WorldOrigin仅选择定位对象，扎在受击者身上的钉子也不使用受击者朝向。
            Vector3 position = WorldOrigin.WorldPosition;
            if (OnGround) position.y = 0f;
            transform.position = position;
            Vector3 scale = _selfTransform.lossyScale;
            scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            scale.x *= WorldFacing;
            transform.localScale = scale;
        }
        else { transform.SetParent(_selfTransform, false); }
        if (victim && transform.parent == victim.atkEffectRoot)
        {
            transform.localPosition = Vector3.zero;
            ConfigureHitFacing();
            ApplyCameraAlignment();
        }

        gameObject.name = GetType().Name;
        _elapsed = 0f;
        _destroyTime = destroyTime;
        InitializeEffect();
        liveEffects.Add(this);
    }

    protected virtual void InitializeEffect()
    {
        // Portrait 只控制相机，不创建粒子。
        if (PrefabName == null) return;
        var prefab = bundle.LoadAsset<GameObject>(PrefabFolder + PrefabName);
        Instance = Instantiate(prefab, transform, false);
        ConfigureInstance();
        timelineRenderers = Instance.GetComponentsInChildren<Renderer>(true);
        timelineTrails = Instance.GetComponentsInChildren<TrailRenderer>(true);
        Instance.SetActive(true);
        // 默认使用预制件的Layer和排序；需要角色图层的特效在自身类中处理。
        var list = new List<ParticleSystem>();
        systems = Instance.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var particles in systems)
        {
            var main = particles.main;
            if (particles.gameObject.activeInHierarchy)
            {
                hasLoop |= main.loop;
                var emission = particles.emission;
                if (emission.enabled)
                {
                    // 纯Burst特效不必空等lengthInSec（钉子和冲刺资源常设为5秒）。
                    float lastEmission = 0f;
                    if (emission.rateOverTime.constantMax > 0f || emission.rateOverDistance.constantMax > 0f) lastEmission = main.duration;
                    var bursts = new ParticleSystem.Burst[emission.burstCount];
                    emission.GetBursts(bursts);
                    foreach (var burst in bursts)
                    {
                        float last = burst.cycleCount == 0 ? main.duration : burst.time + (burst.cycleCount - 1) * burst.repeatInterval;
                        lastEmission = Mathf.Max(lastEmission, Mathf.Min(last, main.duration));
                    }

                    emissionWindow = Mathf.Max(emissionWindow, main.startDelay.constantMax + lastEmission);
                }
            }

            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;
            // 粒子死亡后仍可能有独立拖尾；空粒子状态下继续推进其最长尾段。
            var trails = particles.trails;
            if (trails.enabled && !trails.dieWithParticles) trailTail = Mathf.Max(trailTail, main.startLifetime.constantMax * trails.lifetime.constantMax);
            // Simulate(withChildren: true) 只从最外层粒子开始，避免子粒子重复推进。
            bool nested = false;
            for (var parent = particles.transform.parent; parent != Instance.transform.parent; parent = parent.parent)
            {
                if (!parent.GetComponent<ParticleSystem>()) continue;
                nested = true;
                break;
            }

            if (!nested) list.Add(particles);
        }

        roots = list.ToArray();
        foreach (var trail in Instance.GetComponentsInChildren<TrailRenderer>(true)) trailTail = Mathf.Max(trailTail, trail.time);
        Instance.SetActive(true);
        foreach (var particles in roots)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Simulate(0f, true, true, true);
            particles.Pause(true);
        }
    }

    protected virtual void ConfigureInstance()
    {
    }

    public override void Start()
    {
    }

    public override void SetScale(float scaleFactor)
    {
    }

    protected virtual void TickEffect(float deltaTime)
    {
    }

    protected virtual void LateEffect()
    {
    }

    protected virtual void RestoreEffect()
    {
    }

    public override void Update()
    {
        // 受击特效在自身更新中沿用原版俯仰，不参与专用特写的额外适配。
        ApplyCameraAlignment();
        if (TimelineControlled && (TimelineRates != null || lastOwnedFrame == Time.frameCount)) return;
        float deltaTime = Time.deltaTime;
        AdvanceEffect(deltaTime, deltaTime * ActionSpeed, deltaTime * (timelineMode < 0 && IgnoreParticleSpeed ? 1f : ParticleSpeed));
    }

    internal void AdvanceTimeline(float presentationDelta, float sourceDelta, float particleDelta, float unpausedParticleDelta)
    {
        lastOwnedFrame = Time.frameCount;
        AdvanceEffect(presentationDelta, sourceDelta, timelineIgnoreSpeed ? unpausedParticleDelta : particleDelta);
    }

    private void AdvanceEffect(float deltaTime, float actionDelta, float particleDelta)
    {
        if (disposed) return;
        _elapsed += deltaTime;
        if (timelineMode < 0 && !NaturalLifetime && !ManagedByRencounter && _elapsed > _destroyTime)
        {
            Abort();
            return;
        }

        if (Instance)
        {
            particleDelta *= PlaybackSpeed;
            float lifetime = PlaybackDuration >= 0f ? PlaybackDuration : Lifetime;
            if (timelineMode < 0 && HoldAtEnd && !completed) particleDelta = Mathf.Min(particleDelta, Mathf.Max(0f, lifetime - age));
            timelineActionAge = timelineSourceClock != null ? Mathf.Max(0f, timelineSourceClock() - timelineSourceStart) : timelineActionAge + actionDelta;
            timelinePresentationAge += deltaTime;
            age += particleDelta;
            float policyAge = timelineClock == 1 ? timelineActionAge : timelineClock == 2 ? timelinePresentationAge : age;
            if (timelineSourceTimed && AdvanceSourceWindow(deltaTime)) { CloseTimeline(); return; }
            if (!timelineSourceTimed && timelineMode == 1 && policyAge + .000001f >= timelineDuration) { CloseTimeline(); return; }
            if (timelineMode == 2 && policyAge + .000001f >= timelineDuration) StopTimelineEmission();
            if (timelineMode >= 0)
            {
                foreach (var renderer in timelineRenderers)
                {
                    if (!renderer) continue;
                    renderer.GetPropertyBlock(timelineProperties);
                    timelineProperties.SetFloat("_RebuildUseControlledTime", 1f);
                    timelineProperties.SetFloat("_RebuildTime", age);
                    renderer.SetPropertyBlock(timelineProperties);
                }
            }
            if (particleDelta > 0f)
            {
                foreach (var particles in roots)
                {
                    particles.Simulate(particleDelta, true, false, false);
                    particles.Pause(true);
                }
            }

            if (timelineMode < 0 && !completed && !HoldAtEnd && HardFinish && age >= lifetime) Complete();
            if (Instance && (timelineMode == 0 || timelineMode == 2 && timelineEmissionStopped) && TimelineFinished())
            {
                DestroyInstance();
                Complete();
            }
            if (Instance && timelineMode < 0 && !(HoldAtEnd && !completed) && particleDelta > 0f)
            {
                // 手动Simulate/Pause下按实际粒子数及未来发射批次判断，不依赖暂停态IsAlive。
                bool alive = age < emissionWindow || systems.Any(particles => particles && particles.gameObject.activeInHierarchy && particles.particleCount > 0);
                if (!alive) StopTrails();
                emptyTime = alive ? 0f : emptyTime + particleDelta;
                if (!alive && emptyTime >= trailTail)
                {
                    DestroyInstance();
                    Complete();
                }
            }
        }

        TickEffect(deltaTime);
        if (completed && !Instance) Destroy(gameObject);
    }

    // 与Unity演示ParticleLifetime.Finished一致，不关闭拖尾或估算额外等待时间。
    private bool TimelineFinished()
    {
        if (age + .00001f < timelineExpected) return false;
        foreach (var particles in systems) if (particles && particles.gameObject.activeInHierarchy && particles.particleCount > 0) return false;
        foreach (var trail in timelineTrails) if (trail && trail.enabled && trail.positionCount > 0) return false;
        return true;
    }

    private void LateUpdate()
    {
        LateEffect();
        if (timelineMode < 0 || !timelinePoint) return;
        // 零偏移附着已由父级完成跟随；脱离父级或有初始偏移时仍保留原有定位语义。
        if (timelineFollow && transform.parent == timelinePoint && transform.localPosition == Vector3.zero) return;
        transform.position = timelineFollow ? timelinePoint.position : timelinePosition;
    }

    private void ConfigureHitFacing()
    {
        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * hitFacing * Mathf.Sign(transform.parent.lossyScale.x);
        transform.localScale = scale;
    }

    private void ApplyCameraAlignment()
    {
        // 只维护皮肤外的原版受击根；皮肤内附着及世界空间特效保留自身旋转。
        if (victim && transform.parent == victim.atkEffectRoot) transform.rotation = Quaternion.Euler(SingletonBehavior<BattleCamManager>.Instance.RotationX, 0f, 0f);
    }

    private void DestroyInstance()
    {
        if (!Instance) return;
        Instance.SetActive(false);
        Destroy(Instance);
        Instance = null;
    }

    private void StopTrails()
    {
        if (!Instance) return;
        foreach (var trail in Instance.GetComponentsInChildren<TrailRenderer>(true)) trail.emitting = false;
    }

    public void Complete()
    {
        if (completed) return;
        completed = true;
        var callback = Interrupted;
        Interrupted = null;
        callback?.Invoke();
        TimelineRates = null;
        RestoreEffect();
        ParticleSpeed = 1f;
        // 驻留/长寿命钉子、循环护盾连同附属粒子整体销毁，不保留其尾段。
        if (HardFinish) DestroyInstance();
        if (!NaturalLifetime) StopTrails();
        // 结束动作控制后保留当时的世界姿态，尾迹不再被下一动作的位移/翻面拖走。
        if (transform.parent) transform.SetParent(null, true);
    }

    public void StopEffect()
    {
        Complete();
        if (!Instance) Destroy(gameObject);
    }

    public static void ForceStopAll()
    {
        foreach (var effect in liveEffects.ToArray())
            if (effect) effect.Abort();
    }

    private void Abort(bool destroy = true)
    {
        if (disposed) return;
        disposed = true;
        TimelineRates = null;
        var callback = Interrupted;
        Interrupted = null;
        callback?.Invoke();
        RestoreEffect();
        if (Instance)
        {
            Instance.SetActive(false);
            Destroy(Instance);
            Instance = null;
        }

        if (destroy) Destroy(gameObject);
    }

    private void OnDisable() => Abort();

    public override void OnDestroy()
    {
        liveEffects.Remove(this);
        Abort(false);
        base.OnDestroy();
    }
}

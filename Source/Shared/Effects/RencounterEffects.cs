using UnityEngine;
using System;
using System.Collections.Generic;


using 裁缝之王.Presentation.Actions;
using 裁缝之王.Presentation.Camera;
using 裁缝之王.Presentation.CustomCombat;

namespace 裁缝之王.Presentation.Effects;

// 在对抗推进之后统一提交连续状态，镜头在更晚的阶段读取结果。
[DefaultExecutionOrder(11000)]
public sealed class RencounterEffects : MonoBehaviour
{
    public sealed class State
    {
        private readonly HashSet<ModParticleEffect> effects = new();
        private readonly List<ModParticleEffect> timelineEffects = new();

        // 只有跨币读取或同源再次生成需要持有具名实例；其余实例只登记生命周期。

        internal TimelineActionCamera Camera;
        internal Action Interrupted;
        private readonly RencounterEffects owner;

        internal State(RencounterEffects owner) => this.owner = owner;

        private float damageSourceTime;
        private const float DamageMass = 1.5f, DamageDamping = 8f, DamageStep = .02f, DamageStopSpeed = .1f;

        public void AddDamageImpulse(BattleUnitView actor, BattleUnitView victim, float force, int direction, float scale, int type = 2, float angle = 90f)
        {
            if ((type != 2 && type != 3) || force == 0f || !victim || victim.UnStopppable) return;
            float sign = victim.WorldPosition.x >= actor.WorldPosition.x ? 1f : -1f;
            if (direction == 4) sign = -sign;
            else if (direction == 1) sign = actor.model.direction == Direction.LEFT ? -1f : 1f;
            else if (direction == 2) sign = actor.model.direction == Direction.LEFT ? 1f : -1f;
            float radians = type == 3 ? angle * Mathf.Deg2Rad : 0f;
            AddDamageImpulse(victim, force, scale, new Vector3(sign * Mathf.Cos(radians), Mathf.Sin(radians), 0f));
        }

        internal void AddDamageImpulse(BattleUnitView victim, float force, float scale, Vector3 direction)
        {
            if (!victim || victim.UnStopppable) return;
            if (!owner.damageMotions.TryGetValue(victim, out var motion)) owner.damageMotions.Add(victim, motion = new DamageMotion());
            victim.model.moveDetail.Stop();
            motion.Owner = this;
            motion.Scale = scale;
            motion.Velocity += direction * (force * 3f / DamageMass);
            motion.Active = true;
        }

        // 显式动作回调提供源时间；同一时刻的重复采样不会重复积分。
        public void AdvanceDamage(float sourceTime)
        {
            float delta = Mathf.Max(0f, sourceTime - damageSourceTime);
            damageSourceTime = Mathf.Max(damageSourceTime, sourceTime);
            foreach (var entry in owner.damageMotions)
            {
                var victim = entry.Key;
                var motion = entry.Value;
                if (motion.Owner != this || !motion.Active) continue;
                if (!victim || victim.UnStopppable) { motion.Active = false; motion.Velocity = Vector3.zero; continue; }
                motion.Accumulator += delta;
                while (motion.Accumulator >= DamageStep)
                {
                    motion.Accumulator -= DamageStep;
                    motion.Velocity += Physics.gravity * DamageStep;
                    if (motion.Velocity.y > 1f) motion.Velocity += Vector3.down * 1.2f;
                    else if (motion.Velocity.y < -10f) motion.Velocity += Vector3.up * 7.2f;
                    motion.Velocity *= Mathf.Max(0f, 1f - DamageDamping * DamageStep);
                    var position = victim.WorldPosition + motion.Velocity * (DamageStep * motion.Scale);
                    if (position.y < 0f) { position.y = 0f; motion.Velocity.y = Mathf.Max(0f, motion.Velocity.y); }
                    Knockback.Move(victim, position, ref motion.Velocity);
                    if (motion.Velocity.magnitude >= DamageStopSpeed) continue;
                    // Unity只结束本次积分，不清零残余速度；后续冲量仍按源逻辑累加。
                    motion.Active = false;
                    break;
                }
            }
        }

        public bool DamageMoving(BattleUnitView victim) => owner.damageMotions.TryGetValue(victim, out var motion) && motion.Active;

        public void ClearDamage()
        {
            foreach (var victim in new List<BattleUnitView>(owner.damageMotions.Keys)) if (owner.damageMotions[victim].Owner == this) owner.damageMotions.Remove(victim);
        }

        // 与Unity StartTween接管受击者一致，终止冲量但保留物理积分余量。
        public void ClearDamage(BattleUnitView victim)
        {
            if (!owner.damageMotions.TryGetValue(victim, out var motion)) return;
            motion.Velocity = Vector3.zero;
            motion.Active = false;
        }

        internal sealed class Coin
        {
            internal readonly Action Finish;
            internal readonly ActionClock Clock;

            internal Coin(Action finish, ActionClock clock)
            {
                Finish = finish;
                Clock = clock;
            }
        }

        private Coin currentCoin;
        private BattleUnitView airborneActor;
        private Func<float> coinSourceClock;
        private ActionClock CoinClock => currentCoin?.Clock;
        internal bool IsOpen => owner.IsOpen && !released;
        private float sampledSourceTime, consumedPresentation, particleHitPauseRate;
        private float frameSourceDelta;
        private int advancedFrame = -1;
        private bool released;
        internal float FrameSourceDelta => advancedFrame == Time.frameCount ? frameSourceDelta : released && !Paused ? Time.deltaTime : 0f;
        private readonly Dictionary<int, uint> layoutSeeds = new();

        public uint LayoutSeed(int group)
        {
            if (!layoutSeeds.TryGetValue(group, out uint seed)) layoutSeeds.Add(group, seed = (uint)UnityEngine.Random.Range(1, int.MaxValue));
            return seed;
        }

        private void AdvanceEffectsToEvent(float sourceTime)
        {
            if (CoinClock != null) AdvanceEffects(Mathf.Min(CoinClock.PresentationTime, CoinClock.PresentationAt(sourceTime)));
        }

        private void AdvanceEffects(float presentationEnd)
        {
            float start = consumedPresentation;
            if (presentationEnd <= start) return;
            var clock = CoinClock;
            consumedPresentation = presentationEnd;
            sampledSourceTime = clock.Sample(presentationEnd, out _);
            float delta = presentationEnd - start;
            RecordSourceDelta(Mathf.Clamp(sampledSourceTime - clock.Sample(start, out _), 0f, delta));
            StepEffects(start, presentationEnd, clock);
        }

        internal void FlushCoin()
        {
            if (CoinClock == null) return;
            AdvanceDamage(CoinClock.SourceTime);
            AdvanceEffects(CoinClock.PresentationTime);
        }

        internal void AdvancePresentation()
        {
            FlushCoin();
            if (advancedFrame == Time.frameCount || Paused) return;
            float delta = Time.deltaTime;
            if (delta <= 0f) return;
            float sourceDelta = CoinClock == null ? delta : 0f;
            RecordSourceDelta(sourceDelta);
            StepEffects(0f, delta, null);
        }

        private void RecordSourceDelta(float delta)
        {
            int frame = Time.frameCount;
            if (advancedFrame != frame) frameSourceDelta = 0f;
            advancedFrame = frame;
            frameSourceDelta += delta;
        }

        private void StepEffects(float start, float end, ActionClock clock)
        {
            bool particlesSlowing = false;
            float slowTimeAtEnd = clock?.SampleParticleSlowTime(end, out _) ?? 0f;
            if (clock == null) CoinClock?.SampleParticleSlowTime(consumedPresentation, out particlesSlowing);
            // 推进可能完成实例并将其从所有权集合移除。
            timelineEffects.AddRange(effects);
            try
            {
                foreach (var effect in timelineEffects)
                {
                    if (!effect || !effect.isActiveAndEnabled || !effect.TimelineControlled) continue;
                    if (clock == null)
                    {
                        if (effect.JoinedTimelineThisFrame) continue;
                        float delta = end - start;
                        effect.AdvanceTimeline(delta, CoinClock == null ? delta : 0f, particlesSlowing ? delta * particleHitPauseRate : delta, delta);
                        continue;
                    }

                    float effectStart = effect.GetTimelineStepStart(coinSourceClock, clock, start);
                    float elapsed = end - effectStart;
                    if (elapsed <= 0f) continue;
                    float sourceDelta = Mathf.Clamp(sampledSourceTime - clock.Sample(effectStart, out _), 0f, elapsed);
                    float slowDelta = Mathf.Clamp(slowTimeAtEnd - clock.SampleParticleSlowTime(effectStart, out _), 0f, elapsed);
                    float particleDelta = elapsed - slowDelta * (1f - particleHitPauseRate);
                    effect.AdvanceTimeline(elapsed, sourceDelta, particleDelta, elapsed);
                }
            }
            finally { timelineEffects.Clear(); }
        }

        private readonly Dictionary<(string Address, BattleUnitView Target), ModParticleEffect> kingEffects = new();

        internal void ReplaceKingEffect(string address, BattleUnitView target, ModParticleEffect next, float time)
        {
            var key = (address, target);
            if (kingEffects.TryGetValue(key, out var previous)) Close(ref previous, time);
            kingEffects[key] = next;
        }

        private bool Paused => RencounterEffects.IsPaused;

        public void ReleaseTimeline(bool groupEnd)
        {
            foreach (var effect in new List<ModParticleEffect>(effects)) if (effect && effect.TimelineControlled) effect.ReleaseTimeline(groupEnd);

        }

        public void Close(ref ModParticleEffect instance, float sourceTime)
        {
            AdvanceEffectsToEvent(sourceTime);
            var effect = instance;
            instance = null;
            if (!effect) return;
            effects.Remove(effect);
            effect.CloseTimeline();
        }

        // 同一源对象重新生成时替换旧实例，不按资源名称或类型查找。
        public void Replace(ref ModParticleEffect instance, ModParticleEffect effect)
        {
            if (CoinClock != null)
                AdvanceEffects(Mathf.Min(CoinClock.PresentationTime, effect.GetTimelineStepStart(coinSourceClock, CoinClock, consumedPresentation)));
            if (instance && instance != effect)
            {
                effects.Remove(instance);
                if (instance.TimelineControlled) instance.CloseTimeline();
                else instance.StopEffect();
            }
            instance = effect;
            Track(effect);
        }

        public void Track(ModParticleEffect effect)
        {
            if (!effects.Add(effect)) return;
            effect.ManagedByRencounter = true;
            effect.AttachTimeline(this);
            // 仅捕获生命周期集合，不持有已经结束的动作闭包或局部槽位。
            effect.Interrupted = () => effects.Remove(effect);
        }

        internal Coin PrepareCoin(Action finish, HitPause[] pauses = null, bool timed = true)
            => new(finish, timed ? new ActionClock(pauses) : null);

        internal void BeginCoin(Coin coin, float particleHitPauseRate = .05f)
        {
            airborneActor = null;
            currentCoin = coin;
            this.particleHitPauseRate = particleHitPauseRate;
            consumedPresentation = CoinClock?.PresentationTime ?? 0f;
            sampledSourceTime = CoinClock?.SourceTime ?? 0f;
            coinSourceClock = CoinClock == null ? null : () => sampledSourceTime;
            damageSourceTime = 0f;
        }

        // 宿主等待本条目的返回值；动作源时间只在宿主给出的这一步内推进。
        internal bool IsCurrent(Coin coin) => currentCoin == coin;

        internal float AdvanceCoin(float deltaTime, float sectionEnd) => CoinClock.AdvanceTo(deltaTime, sectionEnd);

        // 有效后序从当前高度接手；若对抗在交接前结束，由同一个状态完成落地。
        internal void SettleActorHeight(BattleUnitView actor, bool continues)
        {
            airborneActor = null;
            if (!actor) return;
            var position = actor.WorldPosition;
            if (position.y == 0f) return;
            if (continues) airborneActor = actor;
            else
            {
                position.y = 0f;
                actor.WorldPosition = position;
            }
        }

        public void BindSourceWindow(ModParticleEffect effect, float sourceStart)
        {
            effect.BindSourceWindow(coinSourceClock, sourceStart);
        }

        internal void EndCoin(Coin coin)
        {
            if (!IsCurrent(coin)) return;
            foreach (var effect in effects) if (effect) effect.DetachSourceWindow(coinSourceClock);
            coinSourceClock = null;
            currentCoin = null;
        }

        internal void Clear(bool interrupted)
        {
            try
            {
                currentCoin?.Finish();
            }
            finally
            {
                released = true;
                SettleActorHeight(airborneActor, false);
                var remaining = new List<ModParticleEffect>(effects);
                effects.Clear();
                kingEffects.Clear();
                var cancel = Interrupted;
                Interrupted = null;
                try
                {
                    if (interrupted) cancel?.Invoke();
                }
                finally
                {
                    foreach (var effect in remaining) if (effect) effect.ReleaseRencounter(interrupted);
                    coinSourceClock = null;
                    currentCoin = null;
                    layoutSeeds.Clear();
                    ClearDamage();
                }
            }
        }
    }

    // 每个受击者仅有一个冲量状态，硬币与技能状态交接时不叠加多个运动写入者。
    private sealed class DamageMotion
    {
        internal State Owner;
        internal Vector3 Velocity;
        internal float Accumulator, Scale;
        internal bool Active;
    }

    private readonly Dictionary<BattleUnitView, DamageMotion> damageMotions = new();

    private readonly Dictionary<(BattleUnitView Actor, BattlePlayingCardDataInUnitModel Card, string Skill), State> states = new();

    private StageController stage;
    private bool opening;
    private (BattleCardTotalResult First, BattleCardTotalResult Second) logs;

    private bool IsOpen => stage != null;

    internal static bool IsPaused
    {
        get
        {
            if (CustomCombatRuntime.IsRunning) return CustomCombatRuntime.Playback.Paused;
            var manager = SingletonBehavior<RencounterManager>.Instance;
            return SingletonBehavior<BattleCamManager>.Instance.stopFocusByRencounter || (manager && manager._isRunning && manager._bPause);
        }
    }

    internal static bool EnsureOpen(BattleUnitView actor, BattleUnitView target, bool opening = false)
    {
        var stage = Singleton<StageController>.Instance;
        if (stage == null || !(opening ? stage.Phase == StageController.StagePhase.WaitStartBattleEffect : IsPlaybackPhase(stage.Phase))) return false;

        var manager = SingletonBehavior<RencounterManager>.Instance;
        if (!manager) return false;
        if (CustomCombatRuntime.Playback is { IsRunning: true } custom)
            (actor, target) = custom.OriginalParticipants;
        var effects = manager.GetComponent<RencounterEffects>();
        var logs = (Actor: actor.model.battleCardResultLog, Target: target.model.battleCardResultLog);
        if (effects && effects.stage == stage
            && (effects.logs == logs || effects.logs == (logs.Target, logs.Actor))) return true;

        if (!effects) effects = manager.gameObject.AddComponent<RencounterEffects>();
        effects.End(interrupted: true);
        effects.logs = logs;
        effects.stage = stage;
        effects.opening = opening;
        return true;
    }

    private static bool IsPlaybackPhase(StageController.StagePhase phase) => phase is
        StageController.StagePhase.EndParrying or StageController.StagePhase.EndOneSideAction or StageController.StagePhase.ProcessViewAction;

    internal static State Acquire(BattleUnitView actor, BattleUnitView target, BattlePlayingCardDataInUnitModel card, string skill)
    {
        var manager = SingletonBehavior<RencounterManager>.Instance;
        var effects = manager ? manager.GetComponent<RencounterEffects>() : null;
        if (!effects || !effects.IsOpen) throw new InvalidOperationException("动作组只能在已开启的对抗演出中取得共享状态。");

        var key = (actor, card, skill);
        if (!effects.states.TryGetValue(key, out var state)) effects.states.Add(key, state = new State(effects));
        return state;
    }

    internal static void CancelActive(RencounterManager manager, bool stopAllEffects = false)
        => Cancel(manager ? manager.GetComponent<RencounterEffects>() : null, stopAllEffects);

    private static void Cancel(RencounterEffects effects, bool stopAllEffects)
    {
        try
        {
            if (effects) effects.End(interrupted: true);
        }
        finally
        {
            if (stopAllEffects) ModParticleEffect.ForceStopAll();
        }
    }

    internal void End(bool interrupted = false)
    {
        if (!IsOpen) return;
        stage = null;
        try
        {
            foreach (var state in states.Values)
            {
                try { state.Clear(interrupted); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }
        finally
        {
            states.Clear();
            damageMotions.Clear();
            logs = default;
            KingContinuation.Clear();
            TimelineActionCamera.Clear();
        }
    }

    private void LateUpdate()
    {
        if (!IsOpen) return;
        if (!(opening ? stage.Phase == StageController.StagePhase.WaitStartBattleEffect : IsPlaybackPhase(stage.Phase)))
        {
            End();
            return;
        }

        foreach (var state in states.Values) state.AdvancePresentation();
    }

    // Unity 销毁启用中的组件时会先执行 OnDisable。
    private void OnDisable() => Cancel(this, stopAllEffects: true);
}

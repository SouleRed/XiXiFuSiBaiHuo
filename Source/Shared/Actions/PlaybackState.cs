using System;
using System.Collections.Generic;
using 裁缝之王.Presentation.Camera;
using 裁缝之王.Presentation.CustomCombat;
using 裁缝之王.Presentation.Effects;
using Soule.MotionCamera;
using UnityEngine;

namespace 裁缝之王.Presentation.Actions;

internal class PlaybackState
{
    private readonly List<ModParticleEffect> effects = new();
    private bool continuesAfterPlayback;
    private List<MotionTrack> authoredMotions;
    private List<RepeatedHit> repeatedHits;

    private readonly RencounterEffects.State.Coin coin;
    internal ActionClock Clock => coin.Clock;
    internal bool Active => Continuity.IsCurrent(coin);
    internal bool Finished { get; set; }

    internal BattleUnitView Actor { get; }
    internal BattleUnitView Target { get; }
    internal BattleCardBehaviourResult Result { get; }
    internal RencounterEffects.State Continuity { get; }
    internal bool Continues { get; }
    protected float LastVisualHit { get; set; } = -1f;

    #region 生命周期和宿主接入

    internal PlaybackState(ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent, string skill,
        HitPause[] pauses = null, bool unlockTargetY = false, bool faceEachOther = true, bool stopTarget = true,
        bool allowContinuation = true)
    {
        Actor = self.view;
        Target = opponent.view;
        Result = self.behaviourResultData;
        Continues = allowContinuation && KingContinuation.TryGetContinuousNext(Actor, Target, Result, out _);
        var continuityTarget = CustomCombatResolution.Find(Result)?.Main?.view ?? Target;
        Continuity = RencounterEffects.Acquire(Actor, continuityTarget, Result.playingCard, skill);
        bool actorYLock = false, targetYLock = false;
        coin = Continuity.PrepareCoin(Finish, pauses);
        self.startMoveRoutineEvent += Begin;
        self.endMoveRoutineEvent += Finish;
        self.preventOverlap = opponent.preventOverlap = true;

        void Begin()
        {
            if (Active || !Continuity.IsOpen) return;
            actorYLock = Actor._bLockPosY;
            targetYLock = Target._bLockPosY;
            Continuity.BeginCoin(coin, .05f);
            Actor.model.moveDetail.Stop();
            if (stopTarget) Target.model.moveDetail.Stop();
            Actor.LockPosY(false);
            if (unlockTargetY) Target.LockPosY(false);
            if (faceEachOther)
            {
                Actor.model.UpdateDirection(Target.WorldPosition);
                Target.model.UpdateDirection(Actor.WorldPosition);
            }
        }

        void Finish()
        {
            if (!Active) return;
            Continuity.FlushCoin();
            BeforeRestore();
            Actor.LockPosY(actorYLock);
            Target.LockPosY(targetYLock);
            BattleCardBehaviourResult next = null;
            continuesAfterPlayback = Continuity.IsOpen && Finished && Continues
                && KingContinuation.TryGetContinuousNext(Actor, Target, Result, out next);
            if (allowContinuation) KingContinuation.Expect(next);
            KingContinuation.RestoreDelay();
            Continuity.ReleaseTimeline(groupEnd: !continuesAfterPlayback);
            if (!continuesAfterPlayback) Continuity.ClearDamage();
            Continuity.EndCoin(coin);
            if (!Finished) foreach (var effect in effects) if (effect) effect.CloseTimeline();
            Continuity.SettleActorHeight(Actor, continuesAfterPlayback);
        }
    }

    protected virtual void BeforeRestore() { }

    internal List<RencounterManager.MovingAction> BindAction(CoinTimeline timeline, MotionCameraSequence camera,
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent,
        Func<int, Vector3[]> secondaryPositions = null)
    {
        var actions = timeline.Actions;
        opponent.infoList = timeline.Reactions;
        CustomCombatActionPlan.BindTargets(ref self, opponent, secondaryPositions);
        if (camera) TimelineActionCamera.Bind(camera, actions, Actor, Target, Continuity, Clock.Sample,
            () => continuesAfterPlayback, ref self, () => Clock.PresentationTime);
        return actions;
    }

    #endregion

    #region 位移与连续采样

    internal MotionTrack CreateMotion(BattleUnitView unit, float sourceTime, float duration, int ease,
        bool limitToMap = false, Func<float, Vector3> axisProgress = null)
    {
        var track = new MotionTrack(unit, Actor, Clock.PresentationAt(sourceTime), duration, ease, limitToMap, axisProgress);
        authoredMotions ??= new List<MotionTrack>();
        authoredMotions.Add(track);
        return track;
    }

    protected void SampleMove(float time)
    {
        if (authoredMotions == null) return;
        foreach (var track in authoredMotions) track.Sample(time);
    }

    internal CoinTimeline CreateTimeline(Action<float, float> sample, float completionSourceTime)
        => new(this, sample, completionSourceTime);

    #endregion

    #region 命中与受击反馈

    internal void SampleTargetPose(float sourceTime)
    {
        if (LastVisualHit >= 0f)
        {
            var pose = sourceTime - LastVisualHit < .35f || Continuity.DamageMoving(Target) ? ActionDetail.Damaged : ActionDetail.Default;
            if (Target.charAppearance.GetCurrentMotionDetail() != pose) Target.charAppearance.ChangeMotion(pose);
        }
    }

    internal void ReactToHit(float sourceTime, float? force = null)
    {
        Target.charAppearance.ChangeMotion(ActionDetail.Damaged);
        LastVisualHit = sourceTime;
        if (force.HasValue) ApplyImpulseAt(sourceTime, force.Value);
    }

    internal void ApplyImpulseAt(float sourceTime, float force)
    {
        float scale = ActionMotion.StageScale(Target);
        Continuity.AdvanceDamage(sourceTime);
        Continuity.AddDamageImpulse(Actor, Target, force, 3, scale);
    }

    internal void ApplyImpulse(BattleUnitView target, float force) => Continuity.AddDamageImpulse(Actor, target, force, 3, ActionMotion.StageScale(target));

    private sealed class RepeatedHit
    {
        internal float Next;
        internal float Interval;
        internal int Remaining;
        internal Action<float, bool> Apply;
    }

    protected void ScheduleRepeatedHits(float time, int count, float duration, Action<float, bool> apply)
    {
        float interval = duration / count;
        repeatedHits ??= new List<RepeatedHit>();
        repeatedHits.Add(new RepeatedHit { Next = Clock.PresentationAt(time) + interval, Interval = interval, Remaining = count - 1, Apply = apply });
    }

    protected void AdvanceRepeatedHits(float time)
    {
        if (repeatedHits == null) return;
        for (int i = repeatedHits.Count - 1; i >= 0; i--)
        {
            var hit = repeatedHits[i];
            while (hit.Remaining > 0 && hit.Next <= time + .000001f)
            {
                float sourceTime = Clock.Sample(hit.Next, out _);
                Continuity.AdvanceDamage(sourceTime);
                hit.Apply(sourceTime, --hit.Remaining == 0);
                hit.Next += hit.Interval;
            }
            if (hit.Remaining == 0) repeatedHits.RemoveAt(i);
        }
    }

    #endregion

    #region 特效与实例交接

    internal void TrackEffect(ModParticleEffect effect, float? sourceStart = null)
    {
        Continuity.Track(effect);
        if (sourceStart.HasValue) Continuity.BindSourceWindow(effect, sourceStart.Value);
        effects.Add(effect);
    }

    #endregion
}

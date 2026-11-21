using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using 裁缝之王.Presentation.CustomCombat;
using 裁缝之王.Presentation.Effects;
using Object = UnityEngine.Object;

namespace 裁缝之王.Presentation.Actions;

internal record KingPose(float Time, ActionDetail Motion, string Sprite = null);
internal record KingMove(float Time, float Duration, int Ease, bool TargetMover, bool ToTarget, bool Refresh, int Direction, bool ChaseY, Vector3 Offset, float Radius, bool ActorRadius, bool TargetRadius);
internal record KingEffect(float Time, string Prefab, int Placement, float Activation, float Tail, bool IgnoreSpeed, bool FollowY, Vector3 Offset, Vector3 ExtraScale, Vector3 ScaleCorrection);

internal sealed partial class KingPlayback : PlaybackState
{
    private static readonly Dictionary<string, Sprite> TransferPoses = new(StringComparer.Ordinal);
    internal const string BundleKey = "needlekingenemy1496_skin";
    private readonly KingPose[] poses;
    private readonly bool deflection;
    private readonly AssetBundle actionBundle;
    private readonly MotionTrack[] tracks = new MotionTrack[2];
    private readonly SpriteRenderer standing;
    private readonly Sprite standingSprite;
    private readonly KingAllyPresentation allies;
    private readonly KingGrabPresentation grab;

    internal KingPlayback(ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent,
        string skill, KingPose[] poses, HitPause[] pauses = null,
        bool withAllies = false, bool grabTarget = false, bool opening = false, bool deflection = false, AssetBundle bundle = null)
        : base(ref self, ref opponent, skill, pauses, unlockTargetY: grabTarget,
            faceEachOther: !opening, stopTarget: !opening, allowContinuation: !deflection && !opening)
    {
        this.poses = poses;
        this.deflection = deflection;
        actionBundle = bundle ? bundle : NativeKingSkin.LoadedBundle;
        standing = Actor.charAppearance.transform.Find("Custom_Standing/Customize_Renderer")?.GetComponent<SpriteRenderer>()
            ?? Actor.charAppearance.transform.Find("Custom_Default/Customize_Renderer").GetComponent<SpriteRenderer>();
        standingSprite = standing.sprite;
        if (withAllies) allies = new KingAllyPresentation(Actor, Target, Result.playingCard, this);
        if (grabTarget) grab = new KingGrabPresentation(Actor, Target);
    }

    internal CoinTimeline.Cue MoveAt(KingMove move)
        => CoinTimeline.At(move.Time, _ => Move(move));

    internal CoinTimeline.Cue EffectAt(KingEffect cue, bool secondary = false)
        => CoinTimeline.At(cue.Time, _ =>
        {
            Spawn(cue, cue.Placement == 1 ? Actor : Target, replace: true);
            if (secondary) CustomCombatRuntime.Playback?.SecondaryPlayback.ForEachFocusedTarget(Result,
                target => Spawn(cue, target, replace: true));
        });

    internal CoinTimeline.Cue HitAt(float time, float force, int count = 1, float duration = 0f, bool secondary = false)
        => CoinTimeline.At(time, _ =>
        {
            ReactToHit(time, force == 0f ? null : force);
            if (secondary) CustomCombatRuntime.Playback?.SecondaryPlayback.ForEachFocusedTarget(Result,
                target => ApplyImpulse(target, force));
            if (count > 1) ScheduleRepeatedHits(time, count, duration, (hitTime, _) => ReactToHit(hitTime));
        });

    private KingPose PoseAt(float time)
    {
        if (poses == null) return null;
        for (int i = poses.Length - 1; i >= 0; i--) if (poses[i].Time <= time + .0000001f) return poses[i];
        return null;
    }

    internal void Sample(float time, float previous)
    {
        float at = Clock.Sample(time, out _);
        SampleMove(time);
        Continuity.AdvanceDamage(at);
        AdvanceRepeatedHits(time);
        var pose = PoseAt(at);
        var sprite = standingSprite;
        if (pose?.Sprite != null)
        {
            if (!TransferPoses.TryGetValue(pose.Sprite, out sprite))
            {
                sprite = actionBundle.LoadAssetWithSubAssets<Sprite>(pose.Sprite).Single();
                TransferPoses.Add(pose.Sprite, sprite);
            }
        }
        if (standing.sprite != sprite) standing.sprite = sprite;
        var slot = pose?.Motion ?? ActionDetail.Default;
        if (Actor.charAppearance.GetCurrentMotionDetail() != slot) Actor.charAppearance.ChangeMotion(slot);
        if (grab != null && at >= .13333334f)
        {
            if (Target.charAppearance.GetCurrentMotionDetail() != ActionDetail.Damaged)
                Target.charAppearance.ChangeMotion(ActionDetail.Damaged);
        }
        else if (!deflection) SampleTargetPose(at);
        if (grab?.Sample(at) == true) Continuity.ClearDamage(Target);
        allies?.Sample(at);
    }

    protected override void BeforeRestore()
    {
        if (standing) standing.sprite = standingSprite;
        allies?.Finish();
        grab?.Finish();
    }

    private void Move(KingMove move)
    {
        var unit = move.TargetMover ? Target : Actor;
        var other = move.TargetMover ? Actor : Target;
        int side = move.TargetMover ? 1 : 0;
        var point = unit.WorldPosition;
        if (move.Refresh || move.TargetMover)
        {
            float x = move.Direction == 0 ? point.x - 1f : move.Direction == 1 ? point.x + 1f : other.WorldPosition.x;
            unit.model.UpdateDirection(new Vector3(x, point.y, point.z));
        }
        float sign = unit.model.direction == Direction.LEFT ? -1f : 1f;
        var relative = move.ToTarget ? other : unit;
        float scale = ActionMotion.MoveScale(Actor, relative);
        Vector3 offset = move.Offset;
        offset.x *= sign * (move.ToTarget ? -1f : 1f);
        Vector3 end = relative.WorldPosition + offset * scale;
        if (move.ToTarget)
        {
            float arriveScale = Mathf.Max(ActionMotion.Scale(unit), ActionMotion.Scale(other, capTargetHeight: true));
            float clearance = (move.ActorRadius ? 2.5f * ActionMotion.Scale(unit) : 0f)
                + (move.TargetRadius ? 2.5f * ActionMotion.Scale(other, capTargetHeight: true) : 0f)
                + move.Radius * arriveScale;
            end.x -= sign * clearance;
            if (!move.ChaseY) end.y = 0f;
        }
        tracks[side]?.Stop();
        unit.model.moveDetail.Stop();
        if (move.Duration <= 0f) unit.WorldPosition = end;
        else
        {
            var track = CreateMotion(unit, move.Time, move.Duration, move.Ease);
            track.Begin(point, end);
            tracks[side] = track;
        }
    }

    internal KingParticle Spawn(KingEffect cue, BattleUnitView target, bool replace = false)
    {
        var effect = new GameObject("[裁缝之王] 动作特效").AddComponent<KingParticle>();
        effect.Cue = cue;
        effect.ActionBundle = actionBundle;
        effect.Initialize(Actor, target, 30f);
        if (replace) Continuity.ReplaceKingEffect(cue.Prefab, target, effect, cue.Time);
        effect.ConfigureTimeline(ParticleEndMode.Timed, ParticleClock.Effect, cue.Tail, 0f,
            sourceTimed: true, activationDuration: cue.Activation, ignoreSpeed: cue.IgnoreSpeed);
        TrackEffect(effect, cue.Time);
        return effect;
    }
}

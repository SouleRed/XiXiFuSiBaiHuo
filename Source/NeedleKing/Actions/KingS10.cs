using 裁缝之王.Presentation.Camera;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace 裁缝之王.Presentation.Actions;

public sealed class BehaviourAction_CFK_S10_Coin1 : KingBehaviourAction
{
    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.S8),
            new(0.1f, ActionDetail.S9),
            new(0.76666665f, ActionDetail.S10),
            new(0.81666665f, ActionDetail.S11),
            new(2.01666668f, ActionDetail.S12),
            new(2.08333334f, ActionDetail.S13),
            new(2.41666667f, ActionDetail.S14),
            new(2.48333334f, ActionDetail.S15),
            new(2.89999996f, (ActionDetail)31),
            new(2.9666666f, (ActionDetail)32),
        };
        var play = new KingPlayback(ref self, ref opponent, "S10", poses, pauses: new[] { new HitPause(2.9f, 0.3f) }, grabTarget: true);
        const float feedback = 2.9f;
        float end = 4.4333334f;
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: end);
        var ready = opponent.data.actionDetail;
        timeline.Section(feedback, ActionDetail.S8, ready, new[]
        {
            play.MoveAt(new KingMove(0.11666667f, 0.7f, 5, true, true, true, 4, false, Vector3.zero, 3f, false, false)),
            play.EffectAt(new KingEffect(0.11666667f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/027_fx_s10_ready.prefab", 2, 2.75f, 0f, false, true, Vector3.zero, Vector3.zero, Vector3.one)),
            CoinTimeline.PlaySound(0.11666667f, "magicwood_9_1_982a02a4", 1f),
            play.EffectAt(new KingEffect(0.68333334f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/028_fx_s10_ready_char.prefab", -1, 2.1833334f, 0f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            CoinTimeline.PlayVoice(1.55f, "10SV-098"),
            CoinTimeline.PlaySound(2.05f, "magicwood_10_1-1_06f9e641", 1f),
            play.EffectAt(new KingEffect(2.8333333f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/025_fx_bite.prefab", -1, 0.95f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            play.EffectAt(new KingEffect(2.9f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/026_fx_bite_hit.prefab", -1, 0.8833333f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            CoinTimeline.PlaySound(2.9f, "magicwood_10_1-2_56d189ce", 1f),
            play.HitAt(2.9f, 0f),
        });
        timeline.Section(end, (ActionDetail)31, ActionDetail.Damaged, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(KingPlayback.BundleKey, "NeedleKingEnemy1496_S10_Coin1");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }
}

internal sealed class KingGrabPresentation
{
    private const string Directory = "assets/裁缝之王敌方1496还原/角色/animations/抓取/";
    private readonly BattleUnitView actor, target;
    private readonly AnimationClip clip;
    private Transform sampler, point;
    private bool active;


    internal KingGrabPresentation(BattleUnitView actor, BattleUnitView target)
    {
        this.actor = actor;
        this.target = target;
        clip = NativeKingSkin.LoadedBundle.LoadAsset<AnimationClip>(Directory + "s10_抓取_-2583746835369294675.anim");
    }

    internal bool Sample(float time)
    {
        if (!clip) return false;
        float local = .6f + Mathf.Clamp(time - .75f, 0f, 3.1333334f);
        if (time < .75f || local < .6333333f || local >= 2.8666666f)
        {
            if (active && local >= 2.8666666f) ResetTargetHeight();
            return false;
        }

        if (!sampler)
        {
            sampler = new GameObject("[裁缝之王] 抓取采样").transform;
            sampler.gameObject.hideFlags = HideFlags.HideAndDontSave;
            point = new GameObject("grabTransform").transform;
            point.SetParent(sampler, false);
        }


        ActionAnimation.SampleTrack(clip, sampler, local);
        float facing = actor.model.direction == Direction.LEFT ? -1f : 1f;
        var root = Matrix4x4.TRS(actor.WorldPosition, actor.transform.rotation, new Vector3(facing, 1f, 1f) * (ActionMotion.Scale(actor) * .8f));
        Vector3 grabPos = point.localPosition;
        float liftWeight = Mathf.InverseLerp(0.6333333f, 1.0f, local);
        grabPos.y += 1.0f * liftWeight;
        target.WorldPosition = root.MultiplyPoint3x4(grabPos);
        active = true;
        return true;
    }

    internal void Finish()
    {
        if (active) ResetTargetHeight();
        if (sampler) Object.Destroy(sampler.gameObject);
    }

    private void ResetTargetHeight()
    {
        active = false;
        if (!target) return;
        Vector3 pos = target.WorldPosition;
        if (pos.y == 0f) return;
        pos.y = 0f;
        target.WorldPosition = pos;
    }
}

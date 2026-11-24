using 裁缝之王.Presentation.Camera;
using UnityEngine;
using System.Collections.Generic;

namespace 裁缝之王.Presentation.Actions;

public sealed class BehaviourAction_CFK_S8_Coin1 : KingBehaviourAction
{
    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.S1),
            new(0.18333331f, ActionDetail.Slash2),
            new(0.38333333f, ActionDetail.Default, "assets/裁缝之王敌方1496还原/角色/sprites/skill1_3_-2548473754123588126.png"),
            new(0.44999997f, ActionDetail.Slash),
            new(0.5833333f, ActionDetail.S3),
        };
        var play = new KingPlayback(ref self, ref opponent, "S8", poses, pauses: new[] { new HitPause(0.46666667f, 0.15f) });
        const float feedback = 0.46666667f;
        float end = play.Continues ? 0.78333336f : 1.0833334f;
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: end);
        var ready = opponent.data.actionDetail;
        timeline.Section(feedback, ActionDetail.S1, ready, new[]
        {
            CoinTimeline.PlaySound(0.16666664f, "human_cloth_e4f79cc4", 1f),
            play.MoveAt(new KingMove(0.4f, 1f, 18, false, true, true, 4, false, Vector3.zero, -7f, false, false)),
            play.EffectAt(new KingEffect(0.4f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/020_fx_dash.prefab", -1, 0.65f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            CoinTimeline.PlaySound(0.41666666f, "magicwood_1_1_d651b2db", 1f),
            play.EffectAt(new KingEffect(0.43333334f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/001_fx_mon_cp10_needle_king_slash.prefab", -1, 0.65f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            play.EffectAt(new KingEffect(0.43333334f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/003_fx_slash_hit.prefab", 2, 0.65f, 1f, false, true, Vector3.zero, Vector3.zero, Vector3.one)),
            play.HitAt(0.46666667f, 0f),
        });
        timeline.Section(end, ActionDetail.Slash, ActionDetail.Damaged, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(KingPlayback.BundleKey, "NeedleKingEnemy1496_S8_Coin1");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }
}

public sealed class BehaviourAction_CFK_S8_Coin2 : KingBehaviourAction
{
    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.Hit2),
            new(0.43333332f, ActionDetail.S7),
            new(0.5f, ActionDetail.Hit),
        };
        var play = new KingPlayback(ref self, ref opponent, "S8", poses, pauses: new[] { new HitPause(0.43333334f, 0.2f) });
        const float feedback = 0.43333334f;
        float end = 1.0166667f;
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: end);
        var ready = opponent.data.actionDetail;
        timeline.Section(feedback, ActionDetail.Hit2, ready, new[]
        {
            play.MoveAt(new KingMove(0f, 0.066f, 1, false, false, true, 4, false, new Vector3(-1f,0f,0f), 0f, false, false)),
            CoinTimeline.PlaySound(0f, "human_cloth_e4f79cc4", 1f),
            play.MoveAt(new KingMove(0.36666667f, 0.066f, 1, false, true, true, 4, false, Vector3.zero, 2f, false, false)),
            CoinTimeline.PlaySound(0.4f, "magicwood_8_2_2440e77d", 1f),
            play.EffectAt(new KingEffect(0.41666666f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/021_fx_mon_cp10_needle_king_slash (1).prefab", -1, 0.6f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            play.EffectAt(new KingEffect(0.41666666f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/003_fx_slash_hit.prefab", 2, 0.5f, 1f, false, true, Vector3.zero, Vector3.zero, Vector3.one)),
            play.HitAt(0.43333334f, 15f),
        });
        timeline.Section(end, ActionDetail.S7, ActionDetail.Damaged, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(KingPlayback.BundleKey, "NeedleKingEnemy1496_S8_Coin2");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }
}

using 裁缝之王.Presentation.Camera;
using UnityEngine;
using System.Collections.Generic;

namespace 裁缝之王.Presentation.Actions;

public sealed class BehaviourAction_CFK_S2_Coin1 : KingBehaviourAction
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
        };
        var play = new KingPlayback(ref self, ref opponent, "S2", poses);
        const float feedback = 0.46666667f;
        float end = play.Continues ? 0.6333333f : 0.73333335f;
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: end);
        var ready = opponent.data.actionDetail;
        timeline.Section(feedback, ActionDetail.S1, ready, new[]
        {
            play.MoveAt(new KingMove(0f, 0.066f, 1, false, true, true, 4, false, Vector3.zero, 5f, false, false)),
            play.MoveAt(new KingMove(0.18333334f, 0.066f, 6, false, false, true, 4, false, new Vector3(-1f,0f,0f), 0f, false, false)),
            play.EffectAt(new KingEffect(0.45f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/006_fx_pierce (1).prefab", -1, 0.28333333f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            CoinTimeline.PlaySound(0.45f, "magicwood_1_2_944a1b36", 1f),
            play.EffectAt(new KingEffect(0.46666667f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/005_fx_pierce_hit.prefab", 2, 0.26666668f, 1f, false, true, Vector3.zero, Vector3.zero, Vector3.one), secondary: true),
            play.HitAt(0.46666667f, 4f, secondary: true),
        });
        timeline.Section(end, ActionDetail.Slash, ActionDetail.Damaged, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(KingPlayback.BundleKey, "NeedleKingEnemy1496_S2_Coin1");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }
}

public sealed class BehaviourAction_CFK_S2_Coin2 : KingBehaviourAction
{
    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.Slash2),
            new(0.2f, ActionDetail.Default, "assets/裁缝之王敌方1496还原/角色/sprites/skill1_3_-2548473754123588126.png"),
            new(0.26666664f, ActionDetail.Slash),
        };
        var play = new KingPlayback(ref self, ref opponent, "S2", poses);
        const float feedback = 0.26666668f;
        float end = play.Continues ? 0.46666667f : 0.55f;
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: end);
        var ready = opponent.data.actionDetail;
        timeline.Section(feedback, ActionDetail.Slash2, ready, new[]
        {
            play.EffectAt(new KingEffect(0.25f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/006_fx_pierce (1).prefab", -1, 0.28333333f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            CoinTimeline.PlaySound(0.25f, "magicwood_1_2_944a1b36", 1f),
            play.EffectAt(new KingEffect(0.26666668f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/005_fx_pierce_hit.prefab", 2, 0.28333333f, 1f, false, true, Vector3.zero, Vector3.zero, Vector3.one), secondary: true),
            play.HitAt(0.26666668f, 5f, secondary: true),
        });
        timeline.Section(end, ActionDetail.Slash, ActionDetail.Damaged, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(KingPlayback.BundleKey, "NeedleKingEnemy1496_S2_Coin2");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }
}

public sealed class BehaviourAction_CFK_S2_Coin3 : KingBehaviourAction
{
    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.Hit2),
            new(0.41666666f, ActionDetail.S7),
            new(0.48333332f, ActionDetail.Hit),
        };
        var play = new KingPlayback(ref self, ref opponent, "S2", poses, pauses: new[] { new HitPause(0.41666666f, 0.3f) });
        const float feedback = 0.41666666f;
        float end = 1.4166666f;
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: end);
        var ready = opponent.data.actionDetail;
        timeline.Section(feedback, ActionDetail.Hit2, ready, new[]
        {
            play.MoveAt(new KingMove(0f, 0.066f, 6, false, false, true, 4, false, new Vector3(1f,0f,0f), 0f, false, false)),
            CoinTimeline.PlaySound(0.4f, "magicwood_1_2_944a1b36", 1f),
            play.EffectAt(new KingEffect(0.41666666f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/006_fx_pierce (1).prefab", -1, 1f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            play.EffectAt(new KingEffect(0.41666666f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/005_fx_pierce_hit.prefab", 2, 1f, 1f, false, true, Vector3.zero, Vector3.zero, Vector3.one), secondary: true),
            play.HitAt(0.41666666f, 10f, secondary: true),
        });
        timeline.Section(end, ActionDetail.S7, ActionDetail.Damaged, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(KingPlayback.BundleKey, "NeedleKingEnemy1496_S2_Coin3");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }
}

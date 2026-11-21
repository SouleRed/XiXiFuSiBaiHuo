using 裁缝之王.Presentation.Camera;
using UnityEngine;
using System.Collections.Generic;

namespace 裁缝之王.Presentation.Actions;

public sealed class BehaviourAction_CFK_S3_Coin1 : KingBehaviourAction
{
    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.Default, "assets/裁缝之王敌方1496还原/角色/sprites/skill3_1_1121105425491100448.png"),
            new(0.06666667f, ActionDetail.Penetrate2),
            new(0.25f, ActionDetail.S6),
            new(0.46666666f, ActionDetail.Penetrate),
        };
        var play = new KingPlayback(ref self, ref opponent, "S3", poses, pauses: new[] { new HitPause(0.46666667f, 0.15f) });
        const float feedback = 0.46666667f;
        float end = play.Continues ? 0.71666664f : 1.05f;
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: end);
        var ready = opponent.data.actionDetail;
        timeline.Section(feedback, ActionDetail.Default, ready, new[]
        {
            play.MoveAt(new KingMove(0.4f, 0.066f, 1, false, true, true, 4, false, Vector3.zero, 1.5f, false, false)),
            CoinTimeline.PlaySound(0.45f, "magicwood_3_1_ec825c16", 1f),
            play.EffectAt(new KingEffect(0.46666667f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/002_fx_weapon_pierce.prefab", -1, 0.5833333f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            play.HitAt(0.46666667f, 0f),
        });
        timeline.Section(end, ActionDetail.Penetrate, ActionDetail.Damaged, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(KingPlayback.BundleKey, "NeedleKingEnemy1496_S3_Coin1");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }
}

public sealed class BehaviourAction_CFK_S3_Coin2 : KingBehaviourAction
{
    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.Penetrate2),
            new(0.01666667f, ActionDetail.S6),
            new(0.16666666f, ActionDetail.Penetrate),
        };
        var play = new KingPlayback(ref self, ref opponent, "S3", poses, pauses: new[] { new HitPause(0.16666667f, 0.15f) });
        const float feedback = 0.16666667f;
        float end = play.Continues ? 0.36666667f : 0.51666665f;
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: end);
        var ready = opponent.data.actionDetail;
        timeline.Section(feedback, ActionDetail.Penetrate2, ready, new[]
        {
            play.MoveAt(new KingMove(0f, 0.066f, 6, false, false, true, 4, false, new Vector3(-1f,0f,0f), 0f, false, false)),
            play.MoveAt(new KingMove(0.13333334f, 0.066f, 1, false, true, true, 4, false, Vector3.zero, 1.5f, false, false)),
            CoinTimeline.PlaySound(0.15f, "magicwood_3_1_ec825c16", 1f),
            play.EffectAt(new KingEffect(0.16666667f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/002_fx_weapon_pierce.prefab", -1, 0.35f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            play.HitAt(0.16666667f, 0f),
        });
        timeline.Section(end, ActionDetail.Penetrate, ActionDetail.Damaged, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(KingPlayback.BundleKey, "NeedleKingEnemy1496_S3_Coin2");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }
}

public sealed class BehaviourAction_CFK_S3_Coin3 : KingBehaviourAction
{
    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.Penetrate2),
            new(0.01666667f, ActionDetail.S6),
            new(0.16666666f, ActionDetail.Penetrate),
        };
        var play = new KingPlayback(ref self, ref opponent, "S3", poses, pauses: new[] { new HitPause(0.16666667f, 0.15f) });
        const float feedback = 0.16666667f;
        float end = play.Continues ? 0.28333333f : 0.51666665f;
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: end);
        var ready = opponent.data.actionDetail;
        timeline.Section(feedback, ActionDetail.Penetrate2, ready, new[]
        {
            play.MoveAt(new KingMove(0f, 0.066f, 6, false, false, true, 4, false, new Vector3(-1f,0f,0f), 0f, false, false)),
            play.MoveAt(new KingMove(0.13333334f, 0.066f, 1, false, true, true, 4, false, Vector3.zero, 1.5f, false, false)),
            CoinTimeline.PlaySound(0.15f, "magicwood_3_1_ec825c16", 1f),
            play.EffectAt(new KingEffect(0.16666667f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/002_fx_weapon_pierce.prefab", -1, 0.35f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            play.HitAt(0.16666667f, 0f),
        });
        timeline.Section(end, ActionDetail.Penetrate, ActionDetail.Damaged, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(KingPlayback.BundleKey, "NeedleKingEnemy1496_S3_Coin3");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }
}

public sealed class BehaviourAction_CFK_S3_Coin4 : KingBehaviourAction
{
    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.S6),
            new(0.58333332f, ActionDetail.Penetrate),
        };
        var play = new KingPlayback(ref self, ref opponent, "S3", poses, pauses: new[] { new HitPause(0.5833333f, 0.15f) });
        const float feedback = 0.5833333f;
        float end = 1.5833334f;
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: end);
        var ready = opponent.data.actionDetail;
        timeline.Section(feedback, ActionDetail.S6, ready, new[]
        {
            play.MoveAt(new KingMove(4.440892e-16f, 0.066f, 6, false, false, true, 4, false, new Vector3(-1f,0f,0f), 0f, false, false)),
            play.MoveAt(new KingMove(0.55f, 0.066f, 1, false, true, true, 4, false, Vector3.zero, 1.5f, false, false)),
            CoinTimeline.PlaySound(0.55f, "magicwood_3_1_ec825c16", 1f),
            play.EffectAt(new KingEffect(0.5833333f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/007_fx_weapon_pierce (1).prefab", -1, 1f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            play.HitAt(0.5833333f, 0f),
        });
        timeline.Section(end, ActionDetail.Penetrate, ActionDetail.Damaged, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(KingPlayback.BundleKey, "NeedleKingEnemy1496_S3_Coin4");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }
}

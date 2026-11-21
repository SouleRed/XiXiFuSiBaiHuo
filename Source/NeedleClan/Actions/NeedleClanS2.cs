using System.Collections.Generic;
using UnityEngine;
using 裁缝之王.Presentation.Camera;

namespace 裁缝之王.Presentation.Actions;

public sealed class BehaviourAction_NeedleClan_S2_Coin1 : NeedleClanBehaviourAction
{
    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var pins = new NeedleClanSkin(self.view);
        var poses = new KingPose[]
        {
            pins.Pose(0f, NeedleClanPose.S2Ready),
            pins.Pose(0.266667f, NeedleClanPose.S2Thrust),
        };
        var play = new KingPlayback(ref self, ref opponent, "S2", poses,
            bundle: pins.Bundle);
        float end = play.Continues ? 0.433333f : 0.866667f;
        var timeline = play.CreateTimeline(play.Sample, end);
        var impact = pins.Effect(play, (pins.IsWeft ? 0.266667f : 0.3f), NeedleClanSkin.PierceFollowUp, (pins.IsWeft ? 0.6f : 0.566667f));
        timeline.Section(0.283333f, ActionDetail.Standing, opponent.data.actionDetail, new[]
        {
            play.MoveAt(new KingMove(0.216667f, 0.066f, 1, false, true, true, 4, false, Vector3.zero, 1f, false, false)),
            pins.IsWeft ? impact : default,
            CoinTimeline.PlaySound(0.283333f, "minineedle_2_1", 1f),
            play.HitAt(0.283333f, 3f),
        });
        timeline.Section(end, ActionDetail.Standing, ActionDetail.Damaged, new[]
        {
            pins.IsWeft ? default : impact,
        }, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(pins.BundleKey, "S2_Coin1");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }
}

public sealed class BehaviourAction_NeedleClan_S2_Coin2 : NeedleClanBehaviourAction
{
    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var pins = new NeedleClanSkin(self.view);
        var poses = new KingPose[]
        {
            pins.Pose(0f, NeedleClanPose.S2Ready),
            pins.Pose(0.183333f, NeedleClanPose.S2Thrust),
        };
        var play = new KingPlayback(ref self, ref opponent, "S2", poses,
            pauses: new[] { new HitPause(0.2f, 0.15f) }, bundle: pins.Bundle);
        float end = play.Continues ? 0.433333f : 0.483333f;
        var timeline = play.CreateTimeline(play.Sample, end);
        timeline.Section(0.2f, ActionDetail.Standing, opponent.data.actionDetail, new[]
        {
            play.MoveAt(new KingMove(0f, 0.2f, 6, false, false, true, 4, false, NeedleClanSkin.BackStep, 0f, false, false)),
            play.MoveAt(new KingMove(0.133333f, 0.6f, 18, false, true, true, 4, false, Vector3.zero, -4f, false, false)),
            pins.Effect(play, (pins.IsWeft ? 0.166667f : 0.133333f), NeedleClanSkin.Dash, (pins.IsWeft ? 0.816667f : 0.666667f)),
            pins.Effect(play, (pins.IsWeft ? 0.2f : 0.183333f), NeedleClanSkin.PierceFollowUp, (pins.IsWeft ? 0.783333f : 0.6f)),
            CoinTimeline.PlaySound(0.2f, "minineedle_2_1", 1f),
            play.HitAt(0.2f, 0f),
        });
        timeline.Section(end, ActionDetail.Standing, ActionDetail.Damaged, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(pins.BundleKey, "S2_Coin2");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }
}

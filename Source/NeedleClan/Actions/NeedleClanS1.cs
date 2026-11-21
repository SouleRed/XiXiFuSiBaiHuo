using System.Collections.Generic;
using UnityEngine;
using 裁缝之王.Presentation.Camera;

namespace 裁缝之王.Presentation.Actions;

public sealed class BehaviourAction_NeedleClan_S1 : NeedleClanBehaviourAction
{
    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var pins = new NeedleClanSkin(self.view);
        var poses = new KingPose[]
        {
            pins.Pose(0f, NeedleClanPose.S1Ready),
            pins.Pose(0.166667f, NeedleClanPose.S1Windup),
            pins.Pose(0.2f, NeedleClanPose.S1Thrust),
            pins.Pose(0.35f, NeedleClanPose.S1Ready),
            pins.Pose(0.483333f, NeedleClanPose.S1Windup),
            pins.Pose(0.516667f, NeedleClanPose.S1Thrust),
        };
        var play = new KingPlayback(ref self, ref opponent, "S1", poses,
            pauses: new[] { new HitPause(0.2f, 0.15f), new HitPause(0.516667f, 0.15f) }, bundle: pins.Bundle);
        float end = play.Continues ? 1f : 1.116667f;
        var timeline = play.CreateTimeline(play.Sample, end);
        timeline.Section(0.516667f, ActionDetail.Standing, opponent.data.actionDetail, new[]
        {
            play.MoveAt(new KingMove(0.133333f, 0.066f, 1, false, true, true, 4, false, Vector3.zero, 1f, false, false)),
            pins.Effect(play, 0.2f, NeedleClanSkin.Pierce, 0.316667f),
            CoinTimeline.PlaySound(0.2f, "minineedle_1_1", 1f),
            play.HitAt(0.2f, 0f),
            play.MoveAt(new KingMove(0.35f, 0.066f, 6, false, false, true, 4, false, NeedleClanSkin.BackStep, 0f, false, false)),
            play.MoveAt(new KingMove(0.45f, 0.066f, 1, false, true, true, 4, false, Vector3.zero, 1f, false, false)),
            pins.Effect(play, 0.516667f, NeedleClanSkin.Pierce, (pins.IsWeft ? 0.316667f : 0.166667f)),
            CoinTimeline.PlaySound(0.516667f, "minineedle_1_1", 1f),
            play.HitAt(0.516667f, 8f),
        });
        timeline.Section(end, ActionDetail.Standing, ActionDetail.Damaged, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(pins.BundleKey, "S1_Coin1");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }

}

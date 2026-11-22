using System.Collections.Generic;
using UnityEngine;
using 裁缝之王.Presentation.Camera;

namespace 裁缝之王.Presentation.Actions;

public sealed class BehaviourAction_NeedleClan_S3 : NeedleClanBehaviourAction
{
    private static readonly string[] OpeningVoices = { "10SV-100", "10SV-100-01", "10SV-100-02" };
    private static readonly System.Random VoiceRandom = new();

    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
        => IsRepeatRoll(ref self)
            ? S3_Coin2(ref self, ref opponent)
            : S3_Coin1(ref self, ref opponent);

    private static List<RencounterManager.MovingAction> S3_Coin1(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var pins = new NeedleClanSkin(self.view);
        var poses = new KingPose[]
        {
            pins.Pose(0f, NeedleClanPose.S3Ready),
            pins.Pose(0.066667f, NeedleClanPose.S3Windup),
            pins.Pose(0.166667f, NeedleClanPose.S3Thrust),
            pins.Pose(0.3f, NeedleClanPose.S3Windup),
            pins.Pose(0.4f, NeedleClanPose.S3Thrust),
            pins.Pose(0.55f, NeedleClanPose.S3Windup),
            pins.Pose(0.683333f, NeedleClanPose.S3Thrust),
        };
        var play = new KingPlayback(ref self, ref opponent, "S3", poses,
            bundle: pins.Bundle);
        float end = play.Continues ? 0.933333f : 1.216667f;
        var timeline = play.CreateTimeline(play.Sample, end);
        timeline.Section(0.7f, ActionDetail.Standing, opponent.data.actionDetail, new[]
        {
            CoinTimeline.At(1f / 30f, _ => ActionAudio.PlayVoice(OpeningVoices[VoiceRandom.Next(OpeningVoices.Length)], 2f)),
            play.MoveAt(new KingMove(0.15f, 0.066f, 1, false, true, true, 4, false, Vector3.zero, 1.5f, false, false)),
            pins.Effect(play, (pins.IsWeft ? 0.183333f : 0.166667f), NeedleClanSkin.PierceCombo, (pins.IsWeft ? 0.216667f : 0.233333f)),
            CoinTimeline.PlaySound(0.183333f, "minineedle_1_1", 1f),
            play.HitAt(0.183333f, 0f),
            pins.Effect(play, 0.4f, NeedleClanSkin.PierceCombo, 0.3f),
            CoinTimeline.PlaySound(0.4f, "minineedle_1_1", 1f),
            play.HitAt(0.4f, 0f),
            pins.Effect(play, 0.7f, NeedleClanSkin.PierceCombo, (pins.IsWeft ? 0.216667f : 0.3f)),
            CoinTimeline.PlaySound(0.7f, "minineedle_1_1", 1f),
            play.HitAt(0.7f, 0f),
        });
        timeline.Section(end, ActionDetail.Standing, ActionDetail.Damaged, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(pins.BundleKey, "S3_Coin1");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }

    private static List<RencounterManager.MovingAction> S3_Coin2(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var pins = new NeedleClanSkin(self.view);
        var poses = new KingPose[]
        {
            pins.Pose(0f, NeedleClanPose.S3Windup),
            pins.Pose(0.166667f, NeedleClanPose.S3Thrust),
            pins.Pose(0.3f, NeedleClanPose.S3Windup),
            pins.Pose(0.4f, NeedleClanPose.S3Thrust),
            pins.Pose(0.516667f, NeedleClanPose.S3Windup),
            pins.Pose(0.616667f, NeedleClanPose.S3Thrust),
        };
        var play = new KingPlayback(ref self, ref opponent, "S3", poses,
            pauses: new[] { new HitPause(0.616667f, 0.15f) }, bundle: pins.Bundle);
        float end = play.Continues ? 0.966667f : 1.133333f;
        var timeline = play.CreateTimeline(play.Sample, end);
        timeline.Section(0.616667f, ActionDetail.Standing, opponent.data.actionDetail, new[]
        {
            play.MoveAt(new KingMove(0.15f, 0.066f, 1, false, true, true, 4, false, Vector3.zero, 1.5f, false, false)),
            pins.Effect(play, (pins.IsWeft ? 0.183333f : 0.15f), NeedleClanSkin.PierceCombo, (pins.IsWeft ? 0.216667f : 0.25f)),
            CoinTimeline.PlaySound(0.183333f, "minineedle_1_1", 1f),
            play.HitAt(0.183333f, 0f),
            pins.Effect(play, 0.4f, NeedleClanSkin.PierceCombo, 0.216667f),
            CoinTimeline.PlaySound(0.4f, "minineedle_1_1", 1f),
            play.HitAt(0.4f, 0f),
            pins.Effect(play, 0.616667f, NeedleClanSkin.PierceCombo, (pins.IsWeft ? 0.216667f : 0.466667f)),
            CoinTimeline.PlaySound(0.616667f, "minineedle_1_1", 1f),
            play.HitAt(0.616667f, 12f),
        });
        timeline.Section(end, ActionDetail.Standing, ActionDetail.Damaged, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(pins.BundleKey, "S3_Coin2");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }
}

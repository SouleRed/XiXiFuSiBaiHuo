using System.Collections.Generic;
using LOR_DiceSystem;

namespace 裁缝之王.Presentation.Actions;

internal static class NeedleClanShortClashes
{
    internal static List<RencounterManager.MovingAction> Build(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
        => self.behaviourResultData.behaviourRawData.Detail == BehaviourDetail.Slash
            ? Parrying1_Coin1(ref self, ref opponent)
            : Parrying2_Coin1(ref self, ref opponent);

    private static List<RencounterManager.MovingAction> Parrying1_Coin1(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var pins = new NeedleClanSkin(self.view);
        var poses = new KingPose[]
        {
            pins.Pose(0f, NeedleClanPose.S1Windup),
            pins.Pose(0.066667f, NeedleClanPose.S1Thrust),
        };
        var play = new KingPlayback(ref self, ref opponent, "Parrying1", poses,
            deflection: true, bundle: pins.Bundle);
        float end = 0.333333f;
        var timeline = play.CreateTimeline(play.Sample, end);
        timeline.Section(end, ActionDetail.Standing, opponent.data.actionDetail, new[]
        {
            CoinTimeline.PlaySound(0f, "minineedle_p_1", 1f),
            pins.Effect(play, (pins.IsWeft ? 0.033333f : 0.05f), NeedleClanSkin.Pierce, (pins.IsWeft ? 0.3f : 0.283333f)),
        });
        timeline.Actions[0].SetEffectTiming(EffectTiming.NOT_PRINT, EffectTiming.NOT_PRINT, EffectTiming.NOT_PRINT);
        return play.BindAction(timeline, null, ref self, ref opponent);
    }

    private static List<RencounterManager.MovingAction> Parrying2_Coin1(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var pins = new NeedleClanSkin(self.view);
        var poses = new KingPose[]
        {
            pins.Pose(0f, NeedleClanPose.S2Ready),
            pins.Pose(0.066667f, NeedleClanPose.S2Thrust),
        };
        var play = new KingPlayback(ref self, ref opponent, "Parrying2", poses,
            deflection: true, bundle: pins.Bundle);
        float end = 0.333333f;
        var timeline = play.CreateTimeline(play.Sample, end);
        timeline.Section(end, ActionDetail.Standing, opponent.data.actionDetail, new[]
        {
            CoinTimeline.PlaySound(0f, "minineedle_p_2", 1f),
            pins.Effect(play, (pins.IsWeft ? 0.05f : 0.066667f), NeedleClanSkin.PierceFollowUp, (pins.IsWeft ? 0.283333f : 0.266667f)),
        });
        timeline.Actions[0].SetEffectTiming(EffectTiming.NOT_PRINT, EffectTiming.NOT_PRINT, EffectTiming.NOT_PRINT);
        return play.BindAction(timeline, null, ref self, ref opponent);
    }
}

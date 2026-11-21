using System.Collections.Generic;
using LOR_DiceSystem;
using UnityEngine;

namespace 裁缝之王.Presentation.Actions;

internal static class KingShortClashes
{
    internal static List<RencounterManager.MovingAction> Build(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
        => self.behaviourResultData.behaviourRawData.Detail switch
        {
            BehaviourDetail.Slash => BuildSlash(ref self, ref opponent),
            BehaviourDetail.Hit => BuildHit(ref self, ref opponent),
            _ => BuildPenetrate(ref self, ref opponent),
        };

    private static List<RencounterManager.MovingAction> BuildSlash(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.Slash2),
            new(0.050000004f, ActionDetail.Default, "assets/裁缝之王敌方1496还原/角色/sprites/skill1_3_-2548473754123588126.png"),
            new(0.116666664f, ActionDetail.Slash),
        };
        var play = new KingPlayback(ref self, ref opponent, "Parrying1", poses, deflection: true);
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: 0.33333334f);
        timeline.Section(0.33333334f, ActionDetail.Slash2, ActionDetail.Default, new[]
        {
            CoinTimeline.PlaySound(0f, "magicwood_p_1_18a6e520", 1f),
            play.EffectAt(new KingEffect(0.06666667f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/001_fx_mon_cp10_needle_king_slash.prefab", -1, 0.26666668f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
        });
        return BindDeflection(play, timeline, ref self, ref opponent);
    }

    private static List<RencounterManager.MovingAction> BuildPenetrate(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.Penetrate2),
            new(0.03333334f, ActionDetail.S6),
            new(0.1f, ActionDetail.Penetrate),
        };
        var play = new KingPlayback(ref self, ref opponent, "Parrying2", poses, deflection: true);
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: 1.1f);
        timeline.Section(1.1f, ActionDetail.Penetrate2, ActionDetail.Default, new[]
        {
            CoinTimeline.PlaySound(0f, "magicwood_p_2_feb59a98", 1f),
            play.EffectAt(new KingEffect(0.1f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/002_fx_weapon_pierce.prefab", -1, 1f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
        });
        return BindDeflection(play, timeline, ref self, ref opponent);
    }

    private static List<RencounterManager.MovingAction> BuildHit(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.Hit),
            new(0.083333335f, ActionDetail.S7),
            new(0.150000015f, ActionDetail.Hit2),
        };
        var play = new KingPlayback(ref self, ref opponent, "Parrying3", poses, deflection: true);
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: 0.33333334f);
        timeline.Section(0.33333334f, ActionDetail.Hit, ActionDetail.Default, new[]
        {
            CoinTimeline.PlaySound(0f, "magicwood_p_3_3e555c5b", 1f),
        });
        return BindDeflection(play, timeline, ref self, ref opponent);
    }

    private static List<RencounterManager.MovingAction> BindDeflection(KingPlayback play, CoinTimeline timeline,
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        timeline.Actions[0].SetEffectTiming(EffectTiming.NOT_PRINT, EffectTiming.NOT_PRINT, EffectTiming.NOT_PRINT);
        return play.BindAction(timeline, null, ref self, ref opponent);
    }
}

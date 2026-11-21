using 裁缝之王.Presentation.Camera;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace 裁缝之王.Presentation.Actions;

public sealed class BehaviourAction_CFK_S6_Coin1 : KingBehaviourAction
{
    private static readonly string[] CommandLines = { "10SV-094", "10SV-094-01", "10SV-094-02" };
    private static readonly ConditionalWeakTable<BattleUnitModel, CommandVoiceProgress> CommandVoices = new();

    private sealed class CommandVoiceProgress
    {
        internal int NextLine;
    }

    private static void PlayCommandVoice(BattleUnitModel actor)
    {
        var progress = CommandVoices.GetValue(actor, _ => new CommandVoiceProgress());
        if (progress.NextLine < CommandLines.Length)
            YinXiao.Play(CommandLines[progress.NextLine++]);
    }

    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.Default, "assets/裁缝之王敌方1496还原/角色/sprites/skill2_1_1990109907565178431.png"),
            new(0.11666667f, ActionDetail.S4),
            new(0.18333334f, ActionDetail.S5),
            new(0.25f, ActionDetail.Default, "assets/裁缝之王敌方1496还原/角色/sprites/skill2_4_5408101357228285246.png"),
            new(0.31666668f, ActionDetail.S4),
        };
        var play = new KingPlayback(ref self, ref opponent, "S6", poses);
        const float feedback = 0.78333336f;
        const float end = 1.7833333f;
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: end);
        var ready = opponent.data.actionDetail;
        var reaction = play.Actor.model.faction == play.Target.model.faction ? ready : ActionDetail.Damaged;
        timeline.Section(feedback, ActionDetail.Default, ready, new[]
        {
            CoinTimeline.At(0f, _ => PlayCommandVoice(play.Actor.model)),
            CoinTimeline.PlaySound(0f, "magicwood_4_1-1_e9fd2782", 1f),
            play.EffectAt(new KingEffect(0.18333334f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/016_fx_weapon_shine_pink.prefab", -1, 0.6f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            CoinTimeline.PlaySound(0.73333335f, "magicwood_6_1_5414af14", 1f),
            play.EffectAt(new KingEffect(0.78333336f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/017_fx_shine_flare_pink.prefab", -1, 1f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            play.EffectAt(new KingEffect(0.78333336f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/018_fx_shine_flare_pink_target.prefab", 2, 1f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
        });
        timeline.Section(end, ActionDetail.S4, reaction, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(KingPlayback.BundleKey, "NeedleKingEnemy1496_S6_Coin1");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }
}

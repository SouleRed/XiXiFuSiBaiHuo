using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using 裁缝之王.Presentation.Camera;
using 裁缝之王.Presentation.Effects;

namespace 裁缝之王.Presentation.Actions;

internal static class KingSummonPresentation
{
    private static readonly Queue<BattlePlayingCardDataInUnitModel> Pages = new();
    private static StartBattleEffect wait;

    internal static void Register(BattlePlayingCardDataInUnitModel card)
    {
        var stage = Singleton<StageController>.Instance;
        if (card?.owner == null || !NativeKingSkin.IsNative(card.owner.view) || stage.Phase != StageController.StagePhase.ActivateStartBattleEffect) return;
        var id = card.card.GetID();
        if (id.packageId != KingBehaviourAction.PackageId || id.id != 5 || Pages.Contains(card)) return;
        Pages.Enqueue(card);
        if (wait != null) return;
        wait = new StartBattleEffect { isDone = false };
        stage.RegisterStartBattleEffect(wait);
        CoroutineRunner.StartCoroutine(Play(stage));
    }

    private static IEnumerator Play(StageController stage)
    {
        try
        {
            while (stage.Phase == StageController.StagePhase.ActivateStartBattleEffect) yield return null;
            // 等待更早注册的开战演出（含其他 MOD）完全结束并归还相机，避免并发抢占渲染与聚焦状态。
            while (stage.Phase == StageController.StagePhase.WaitStartBattleEffect && stage._startBattleEffectList.IndexOf(wait) > 0)
                yield return null;
            while (stage.Phase == StageController.StagePhase.WaitStartBattleEffect && Pages.Count > 0)
            {
                var card = Pages.Dequeue();
                var actor = card.owner.view;
                if (!NativeKingSkin.IsNative(actor)) continue;
                var target = card.target?.view;
                if (!target) target = actor;
                if (!RencounterEffects.EnsureOpen(actor, target, opening: true)) continue;
                yield return KingPlayback.Opening(stage, card, actor, target);
            }
        }
        finally
        {
            Pages.Clear();
            if (wait != null) wait.isDone = true;
            wait = null;
        }
    }
}

internal sealed partial class KingPlayback
{
    internal static IEnumerator Opening(StageController stage, BattlePlayingCardDataInUnitModel card, BattleUnitView actor, BattleUnitView target)
    {
        const float end = 2.4f;
        var self = new RencounterManager.ActionAfterBehaviour { view = actor, behaviourResultData = new BattleCardBehaviourResult { playingCard = card } };
        var opponent = new RencounterManager.ActionAfterBehaviour { view = target };
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.Default, "assets/裁缝之王敌方1496还原/角色/sprites/skill2_1_1990109907565178431.png"),
            new(0.250000002f, ActionDetail.S4),
            new(0.55000003f, ActionDetail.S5),
            new(0.61666669f, ActionDetail.Default, "assets/裁缝之王敌方1496还原/角色/sprites/skill2_4_5408101357228285246.png"),
            new(0.68333337f, ActionDetail.S4),
        };
        var play = new KingPlayback(ref self, ref opponent, "S5", poses, opening: true);
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: end);
        timeline.Section(end, ActionDetail.Default, null, new[]
        {
            CoinTimeline.PlaySound(0.16666667f, "human_cloth_e4f79cc4", 1f),
            play.EffectAt(new KingEffect(0.53333336f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/013_fx_weapon_shine_yellow.prefab", -1, 0.16666667f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            CoinTimeline.PlaySound(0.53333336f, "magicwood_4_1-1_e9fd2782", 1f),
            play.EffectAt(new KingEffect(0.7f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/014_fx_weapon_shine_ready_yellow.prefab", -1, 0.4480139f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            CoinTimeline.PlaySound(1.1f, "magicwood_5_1_69ed4233", 1f),
            play.EffectAt(new KingEffect(1.148014f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/015_fx_shine_flare_yellow.prefab", -1, 1.2519861f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
        });
        var clip = TimelineActionCamera.Clips.Get(BundleKey, "NeedleKingEnemy1496_S5_Coin1");
        var camera = TimelineActionCamera.BeginFocusedStandalone(actor, target, new[] { actor, target });
        try
        {
            camera.StartStandaloneClip(clip, play.Clock.Sample);
            self.startMoveRoutineEvent?.Invoke();
            while (stage.Phase == StageController.StagePhase.WaitStartBattleEffect && play.Active)
            {
                bool complete = timeline.Actions[0]._moveFunc(Time.deltaTime, play.Clock.PresentationTime);
                camera.SetStandaloneTime(play.Clock.PresentationTime);
                if (complete) break;
                yield return null;
            }
        }
        finally
        {
            self.endMoveRoutineEvent?.Invoke();
            camera.EndStandalone();
        }
    }
}

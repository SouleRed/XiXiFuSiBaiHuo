using 裁缝之王.Presentation.Camera;
using 裁缝之王.Presentation.CustomCombat;
using UnityEngine;
using System.Collections.Generic;

namespace 裁缝之王.Presentation.Actions;

public sealed class BehaviourAction_CFK_S9_Coin1 : KingBehaviourAction
{
    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.Default, "assets/裁缝之王敌方1496还原/角色/sprites/skill2_1_1990109907565178431.png"),
            new(0.71666667f, ActionDetail.S4),
            new(0.78333334f, ActionDetail.S5),
            new(0.85f, ActionDetail.Default, "assets/裁缝之王敌方1496还原/角色/sprites/skill2_4_5408101357228285246.png"),
            new(0.91666668f, ActionDetail.S4),
        };
        var play = new KingPlayback(ref self, ref opponent, "S9", poses);
        const float feedback = 3.36333323f;
        float end = 3.8833334f;
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: end);
        var ready = opponent.data.actionDetail;
        timeline.Section(feedback, ActionDetail.Default, ready, new[]
        {
            play.MoveAt(new KingMove(0f, 0f, 1, true, true, true, 4, false, Vector3.zero, 6f, false, false)),
            CoinTimeline.PlayVoice(0f, "10SV-097"),
            CoinTimeline.PlaySound(0f, "scenetrans_short_4c693dda", 1f),
            play.EffectAt(new KingEffect(0.71666664f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/023_fx_weapon_shine_ready_2.prefab", -1, 2f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            CoinTimeline.PlaySound(0.71666664f, "magicwood_9_1_982a02a4", 1f),
            CoinTimeline.PlaySound(2.6666667f, "magicwood_4_1-2_9973fe4b", 1f),
            play.EffectAt(new KingEffect(2.7333333f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/022_fx_pierce_ground (1).prefab", 1, 1f, 4f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            play.EffectAt(new KingEffect(2.7333333f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/009_fx_shine_flare.prefab", -1, 1f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            play.EffectAt(new KingEffect(2.7333333f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/024_fx_pierce_hit_side.prefab", 2, 1f, 1f, false, true, Vector3.zero, Vector3.zero, Vector3.one), secondary: true),
            play.HitAt(2.7333333f, 0f, count: 10, duration: 0.7f),
            CoinTimeline.PlaySound(2.75f, "magicwood_4_1-2_9973fe4b", 1f),
            CoinTimeline.PlaySound(2.85f, "magicwood_4_1-2_9973fe4b", 1f),
            CoinTimeline.PlaySound(2.9f, "magicwood_4_1-2_9973fe4b", 1f),
            CoinTimeline.PlaySound(3f, "magicwood_4_1-2_9973fe4b", 1f),
            CoinTimeline.PlaySound(3.0666666f, "magicwood_4_1-2_9973fe4b", 1f),
            CoinTimeline.PlaySound(3.1666667f, "magicwood_4_1-2_9973fe4b", 1f),
            CoinTimeline.PlaySound(3.2166667f, "magicwood_4_1-2_9973fe4b", 1f),
            CoinTimeline.PlaySound(3.3166666f, "magicwood_4_1-2_9973fe4b", 1f),
        });
        timeline.Section(end, ActionDetail.S4, ActionDetail.Damaged, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(KingPlayback.BundleKey, "NeedleKingEnemy1496_S9_Coin1");
        return play.BindAction(timeline, camera, ref self, ref opponent,
            secondaryPositions: count => SurroundMainTarget(play.Actor, play.Target, count));
    }

    private static Vector3[] SurroundMainTarget(BattleUnitView actor, BattleUnitView mainTarget, int count)
    {
        float sign = mainTarget.WorldPosition.x >= actor.WorldPosition.x ? -1f : 1f;
        float arriveScale = Mathf.Max(ActionMotion.Scale(mainTarget), ActionMotion.Scale(actor, capTargetHeight: true));
        var mainPos = new Vector3(actor.WorldPosition.x - sign * 6f * arriveScale, 0f, actor.WorldPosition.z);
        var map = SingletonBehavior<HexagonalMapManager>.Instance;
        float left = map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.LEFT);
        float right = map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.RIGHT);
        float back = map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.DOWN);
        float front = map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.UP);

        var positions = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            var pos = mainPos + CustomCombatActionPlan.GetStaggeredOffset(actor, mainTarget, i);
            pos.x = Mathf.Clamp(pos.x, left, right);
            pos.z = Mathf.Clamp(pos.z, back, front);
            positions[i] = pos;
        }
        return positions;
    }
}

using 裁缝之王.Presentation.Camera;
using UnityEngine;
using System.Collections.Generic;

namespace 裁缝之王.Presentation.Actions;

public sealed class BehaviourAction_CFK_S4_Coin1 : KingBehaviourAction
{
    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.Default, "assets/裁缝之王敌方1496还原/角色/sprites/skill2_1_1990109907565178431.png"),
            new(0.06666667f, ActionDetail.S4),
            new(0.13333334f, ActionDetail.S5),
            new(0.25f, ActionDetail.Default, "assets/裁缝之王敌方1496还原/角色/sprites/skill2_4_5408101357228285246.png"),
            new(0.31666668f, ActionDetail.S4),
        };
        var play = new KingPlayback(ref self, ref opponent, "S4", poses);
        const float feedback = 2.29999995f;
        float end = 2.9666667f;
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: end);
        var ready = opponent.data.actionDetail;
        timeline.Section(feedback, ActionDetail.Default, ready, new[]
        {
            CoinTimeline.PlaySound(0f, "magicwood_4_1-1_e9fd2782", 1f),
            play.EffectAt(new KingEffect(0.05f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/008_fx_weapon_shine.prefab", -1, 0.3f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            play.EffectAt(new KingEffect(0.35f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/010_fx_weapon_shine_ready.prefab", -1, 1.35f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            CoinTimeline.PlaySound(1.7f, "magicwood_4_1-2_9973fe4b", 1f),
            play.EffectAt(new KingEffect(1.7666667f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/009_fx_shine_flare.prefab", -1, 1.1166667f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            play.EffectAt(new KingEffect(1.85f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/011_fx_pierce_ground.prefab", 1, 1.1166667f, 4f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            play.EffectAt(new KingEffect(1.85f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/012_fx_pierce_hit_top.prefab", 2, 1f, 1f, false, true, Vector3.zero, Vector3.zero, Vector3.one), secondary: true),
            CoinTimeline.PlaySound(1.85f, "magicwood_4_1-3_02140a92", 1f),
            play.HitAt(1.85f, 0f, count: 10, duration: 0.5f),
            CoinTimeline.PlaySound(1.9333333f, "magicwood_4_1-3_02140a92", 1f),
            CoinTimeline.PlaySound(2f, "magicwood_4_1-3_02140a92", 1f),
            CoinTimeline.PlaySound(2.0833333f, "magicwood_4_1-3_02140a92", 1f),
            CoinTimeline.PlaySound(2.1666667f, "magicwood_4_1-3_02140a92", 1f),
            CoinTimeline.PlaySound(2.25f, "magicwood_4_1-3_02140a92", 1f),
        });
        timeline.Section(end, ActionDetail.S4, ActionDetail.Damaged, new[]
        {
            CoinTimeline.PlaySound(2.3166666f, "magicwood_4_1-3_02140a92", 1f),
            CoinTimeline.PlaySound(2.4f, "magicwood_4_1-3_02140a92", 1f),
        }, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(KingPlayback.BundleKey, "NeedleKingEnemy1496_S4_Coin1");
        return play.BindAction(timeline, camera, ref self, ref opponent,
            secondaryPositions: count => ScatterTargets(play.Actor, play.Target, count));
    }

    private static Vector3[] ScatterTargets(BattleUnitView actor, BattleUnitView mainTarget, int count)
    {
        // 原版 149604 在开场以施术者为中心布置椭圆，场地边界压缩纵深。
        const float sourceRange = 6.5f, depthScale = 0.65f, keepOut = 1.5f, jitter = 0.08f;
        var center = actor.WorldPosition;
        var map = SingletonBehavior<HexagonalMapManager>.Instance;
        float scale = ActionMotion.Scale(actor);
        float radiusX = sourceRange * scale;
        float radiusZ = Mathf.Min(radiusX * depthScale, 3f * scale);
        float left = map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.LEFT);
        float right = map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.RIGHT);
        float back = map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.DOWN);
        float front = map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.UP);
        radiusX = Mathf.Min(radiusX, (right - left) * 0.5f);
        radiusZ = Mathf.Min(radiusZ, (front - back) * 0.5f);
        center.x = Mathf.Clamp(center.x, left + radiusX, right - radiusX);
        center.z = Mathf.Clamp(center.z, back + radiusZ, front - radiusZ);

        // 图书馆的对抗维持主目标原位，以其方向占用一个槽位。
        var toMain = mainTarget.WorldPosition - center;
        float phase = Mathf.Atan2(toMain.z * radiusX, toMain.x * radiusZ);
        float step = 2f * Mathf.PI / (count + 1);
        float minimumRadius = Mathf.Min(1f, keepOut / sourceRange);
        var random = new System.Random();
        var positions = new Vector3[count];
        for (int i = 0; i < positions.Length; i++)
        {
            float angle = phase + step * (i + 1 + ((float)random.NextDouble() * 2f - 1f) * jitter);
            float radius = Mathf.Clamp(1f + ((float)random.NextDouble() * 2f - 1f) * jitter, minimumRadius, 1f);
            positions[i] = center + new Vector3(Mathf.Cos(angle) * radiusX * radius, 0f, Mathf.Sin(angle) * radiusZ * radius);
        }
        return positions;
    }
}

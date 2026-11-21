using System;
using UnityEngine;

namespace 裁缝之王.Presentation.Actions;

internal static class ActionMotion
{
    internal const float SourceVisualScale = .5f;
    private const float ReferenceAppearanceScale = 1.176471f;
    private const float MaxTargetHeightScale = 220f / 170f;

    internal static float HeightScale(BattleUnitView view)
    {
        // 原版ChangeHeight直接将height/170写入此局部缩放，不包含地图或皮肤制作倍率。
        float ratio = Mathf.Abs(view.camRotationFollower.transform.localScale.y);
        return ratio > 0f ? ratio : 1f;
    }

    internal static float SkinScale(BattleUnitView reference)
    {
        float skinScale = reference && reference.charAppearance
            ? Mathf.Abs(reference.charAppearance.transform.localScale.y)
            : 0f;
        return skinScale > 0f ? skinScale : 1f;
    }

    internal static float AppearanceUnits(BattleUnitView view) => SkinScale(view) / SourceVisualScale * MapScale(view);

    internal static float Scale(BattleUnitView view, bool capTargetHeight = false)
    {
        float heightScale = HeightScale(view);
        if (capTargetHeight) heightScale = Mathf.Min(heightScale, MaxTargetHeightScale);
        return AppearanceUnits(view) * heightScale;
    }

    // 位移偏移使用施术者的外观单位和落点参照的逻辑身高。
    internal static float MoveScale(BattleUnitView actor, BattleUnitView reference)
    {
        float heightScale = HeightScale(reference);
        if (reference != actor) heightScale = Mathf.Min(heightScale, MaxTargetHeightScale);
        return AppearanceUnits(actor) * heightScale;
    }

    // 演示的受击反馈和中心挂点使用舞台基准，不随单个皮肤的外观校准改变。
    internal static float StageScale(BattleUnitView view)
        => ReferenceAppearanceScale / SourceVisualScale * MapScale(view) * HeightScale(view);

    private static float MapScale(BattleUnitView view) => Mathf.Abs(view.transform.localScale.y);

    internal static float EvaluateEase(int ease, float u, AnimationCurve custom)
    {
        u = Mathf.Clamp01(u);
        return ease switch
        {
            0 => custom != null && custom.length > 0 ? custom.Evaluate(u) : u,
            1 => u,
            2 => 1f - Mathf.Cos(u * Mathf.PI * .5f),
            3 => Mathf.Sin(u * Mathf.PI * .5f),
            4 => -(Mathf.Cos(Mathf.PI * u) - 1f) * .5f,
            5 => u * u,
            6 => 1f - (1f - u) * (1f - u),
            7 => u < .5f ? 2f * u * u : 1f - Mathf.Pow(-2f * u + 2f, 2f) * .5f,
            8 => u * u * u,
            9 => 1f - Mathf.Pow(1f - u, 3f),
            10 => u < .5f ? 4f * u * u * u : 1f - Mathf.Pow(-2f * u + 2f, 3f) * .5f,
            11 => u * u * u * u,
            12 => 1f - Mathf.Pow(1f - u, 4f),
            15 => 1f - Mathf.Pow(1f - u, 5f),
            18 => u >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * u),
            _ => throw new ArgumentOutOfRangeException(nameof(ease)),
        };
    }
}

internal sealed class MotionTrack
{
    private readonly BattleUnitView unit;
    private readonly BattleUnitView actor;
    private float startTime, duration;
    private int ease;
    private readonly bool limitToMap;
    private readonly Func<float, Vector3> axisProgress;
    private Vector3 start, destination;
    private bool running;

    internal MotionTrack(BattleUnitView unit, BattleUnitView actor, float startTime, float duration, int ease, bool limitToMap, Func<float, Vector3> axisProgress)
    {
        this.unit = unit;
        this.actor = actor;
        SetTiming(startTime, duration, ease);
        this.limitToMap = limitToMap;
        this.axisProgress = axisProgress;
        start = destination = default;
        running = false;
    }

    internal void SetTiming(float time, float length, int easing)
    {
        startTime = time;
        duration = length;
        ease = easing;
    }

    internal void Begin(Vector3 from, Vector3 to, bool ground = false)
    {
        start = from;
        destination = to;
        if (ground) destination.y = 0f;
        running = true;
    }

    internal float BeginRelative(BattleUnitView reference, BattleUnitView face, Vector3 offset, bool ground = false)
    {
        unit.model.moveDetail.Stop();
        var from = unit.WorldPosition;
        float sign = face.WorldPosition.x >= unit.WorldPosition.x ? 1f : -1f;
        unit.model.UpdateDirection(face.WorldPosition);
        BeginRelative(from, reference, offset, sign, ground);
        return sign;
    }

    internal void BeginRelative(BattleUnitView reference, Vector3 offset, float direction, bool ground = false)
    {
        unit.model.moveDetail.Stop();
        BeginRelative(unit.WorldPosition, reference, offset, direction, ground);
    }

    private void BeginRelative(Vector3 from, BattleUnitView reference, Vector3 offset, float direction, bool ground)
    {
        offset.x *= direction;
        Begin(from, reference.WorldPosition + offset * ActionMotion.MoveScale(actor, reference), ground);
    }

    internal void Stop() => running = false;

    internal void Sample(float time)
    {
        if (!running) return;
        float progress = Mathf.Clamp01((time - startTime) / duration);
        if (!limitToMap || !unit.UnStopppable)
        {
            Vector3 position = axisProgress == null
                ? Vector3.LerpUnclamped(start, destination, ActionMotion.EvaluateEase(ease, progress, null))
                : start + Vector3.Scale(destination - start, axisProgress(progress));
            if (limitToMap) Knockback.Place(unit, position);
            else unit.WorldPosition = position;
        }
        if (progress >= 1f) running = false;
    }
}

// 只约束受击者；地图边界是世界单位，不再乘角色视觉比例。
internal static class Knockback
{
    internal static Vector3 Limit(BattleUnitView victim, Vector3 target)
    {
        var map = SingletonBehavior<HexagonalMapManager>.Instance;
        var current = victim.WorldPosition;
        float margin = map.tileSize;
        float left = map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.LEFT) + margin;
        float right = map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.RIGHT) - margin;
        float back = map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.DOWN) + margin;
        float front = map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.UP) - margin;
        // 已经靠墙时允许向场内移动，不为留边距反向瞬移角色。
        target.x = Mathf.Clamp(target.x, Mathf.Min(left, current.x), Mathf.Max(right, current.x));
        target.z = Mathf.Clamp(target.z, Mathf.Min(back, current.z), Mathf.Max(front, current.z));
        return target;
    }

    internal static void Place(BattleUnitView victim, Vector3 target)
    {
        victim.WorldPosition = Limit(victim, target);
    }

    internal static Vector3 Move(BattleUnitView victim, Vector3 target, ref Vector3 velocity)
    {
        Vector3 limited = Limit(victim, target);
        if (Mathf.Abs(limited.x - target.x) > .00001f) velocity.x = 0f;
        if (Mathf.Abs(limited.z - target.z) > .00001f) velocity.z = 0f;
        // 该运动自行控制Y；不经过宿主WorldPosition的强制归零，避免吞掉源垂直位移。
        victim.transform.position = limited;
        return victim.WorldPosition;
    }
}

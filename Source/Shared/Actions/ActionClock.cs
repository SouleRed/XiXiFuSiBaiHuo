using System;
using UnityEngine;

namespace 裁缝之王.Presentation.Actions;

// 对齐演示的NONE/SMALL基础分支：正的afterDelay才进入0.06秒前段等待。
// 不根据图书馆伤害猜测巴士的伤害等级，不凭普通攻击额外叠加NORMAL分支时长。
// 时间轴暂停，粒子速度由独立通道设置；不把粒子的5%速度施加到动作。
internal readonly struct HitPause
{
    internal const float ParticleLeadDuration = .06f;
    internal readonly float SourceTime;
    private readonly float afterDelay;

    internal HitPause(float sourceTime, float afterDelay)
    {
        SourceTime = sourceTime;
        this.afterDelay = afterDelay;
    }

    internal float Duration => afterDelay > 0f ? afterDelay + ParticleLeadDuration : 0f;
}

// 每币一个时钟，仅我方条目推进；源时间边界不烘焙命中停顿。
internal sealed class ActionClock
{
    private readonly HitPause[] pauses;
    private float remainder;
    private float sourceTime;
    private float actionRate = 1f;

    internal float PresentationTime { get; private set; }
    internal float SourceTime => sourceTime;
    internal bool SectionComplete { get; private set; }

    internal ActionClock(HitPause[] pauses = null)
    {
        this.pauses = pauses ?? Array.Empty<HitPause>();
        sourceTime = ActionTime(0f, this.pauses, out actionRate);
    }

    internal float Sample(float presentationTime, out float rate)
    {
        if (presentationTime == PresentationTime)
        {
            rate = actionRate;
            return sourceTime;
        }
        return ActionTime(presentationTime, pauses, out rate);
    }

    // 统计截至该表现时间已走过的粒子慢放时间，命中停顿的前段仍按正常倍率推进。
    internal float SampleParticleSlowTime(float presentationTime, out bool slowing)
    {
        float slowTime = 0f, shift = 0f;
        slowing = false;
        foreach (var pause in pauses)
        {
            float duration = pause.Duration;
            if (duration <= 0f) continue;
            float start = pause.SourceTime + shift;
            float slowStart = start + HitPause.ParticleLeadDuration;
            float end = start + duration;
            if (presentationTime >= slowStart)
            {
                slowTime += Mathf.Min(presentationTime, end) - slowStart;
                slowing |= presentationTime < end;
            }
            shift += duration;
        }
        return slowTime;
    }

    // 边界上的停顿由从该边界开始的条目承担，先完成姿势/宿主受击切换。
    internal float PresentationAt(float sourceTime)
    {
        float time = sourceTime;
        foreach (var pause in pauses) if (pause.SourceTime < sourceTime - .000001f) time += pause.Duration;
        return time;
    }

    internal float Advance(float deltaTime, float sourceEnd) => AdvanceTo(deltaTime, PresentationAt(sourceEnd));

    internal float AdvanceTo(float deltaTime, float end)
    {
        float next = PresentationTime + Mathf.Max(0f, deltaTime) + remainder;
        PresentationTime = Mathf.Min(next, end);
        sourceTime = ActionTime(PresentationTime, pauses, out actionRate);
        remainder = Mathf.Max(0f, next - end);
        SectionComplete = PresentationTime >= end;
        return PresentationTime;
    }

    private static float ActionTime(float presentationTime, HitPause[] pauses, out float actionRate)
    {
        const float hitSpeed = 0f;
        float sourceTime = presentationTime, shift = 0f;
        actionRate = 1f;
        foreach (var pause in pauses)
        {
            float start = pause.SourceTime + shift;
            sourceTime -= Mathf.Clamp(presentationTime - start, 0f, pause.Duration) * (1f - hitSpeed);
            if (presentationTime >= start && presentationTime < start + pause.Duration) actionRate = hitSpeed;
            shift += pause.Duration * (1f - hitSpeed);
        }
        return sourceTime;
    }
}

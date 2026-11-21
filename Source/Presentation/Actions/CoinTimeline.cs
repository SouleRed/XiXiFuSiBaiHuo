using System;
using System.Collections.Generic;
using UnityEngine;

namespace 裁缝之王.Presentation.Actions;

// 一颗骰子只维护一对宿主队列。连续采样在事件边界与帧末运行，宿主推进条目。
internal sealed class CoinTimeline
{
    private readonly PlaybackState playback;
    private readonly Action<float, float> sample;
    private readonly float completionSourceTime;
    private bool feedbackPrinted;

    internal readonly List<RencounterManager.MovingAction> Actions = new();
    internal readonly List<RencounterManager.MovingAction> Reactions = new();

    internal CoinTimeline(PlaybackState playback, Action<float, float> sample, float completionSourceTime)
    {
        this.playback = playback;
        this.sample = sample;
        this.completionSourceTime = completionSourceTime;
    }

    internal void Section(float sourceEnd, ActionDetail pose, ActionDetail? reaction, Cue[] events = null,
        bool feedback = false)
    {
        var clock = playback.Clock;
        float sectionEnd = clock.PresentationAt(sourceEnd);
        // 计划建立时固定事件在表现时间的位置。同刻按声明顺序处理，逐帧只看游标。
        var eventTimes = events == null ? Array.Empty<float>() : new float[events.Length];
        for (int i = 0; i < eventTimes.Length; i++)
            if (events[i].Apply != null) eventTimes[i] = clock.PresentationAt(events[i].SourceTime);

        int next = 0;
        var action = new RencounterManager.MovingAction(pose, CharMoveState.Custom, updateDir: false, delay: 0f)
        {
            _moveFunc = (deltaTime, _) =>
            {
                if (!playback.Active) return true;
                if (deltaTime <= 0f) return false;

                float sampled = clock.PresentationTime;
                float time = playback.Continuity.AdvanceCoin(deltaTime, sectionEnd);
                while (next < eventTimes.Length)
                {
                    var cue = events[next];
                    if (cue.Apply != null)
                    {
                        float at = eventTimes[next];
                        if (at > time + .000001f) break;
                        at = Mathf.Clamp(at, sampled, time);
                        next++;
                        sample(at, sampled);
                        sampled = at;
                        cue.Apply(at);
                        if (!playback.Active) return true;
                    }
                    else next++;
                }

                if (feedback && !feedbackPrinted)
                {
                    feedbackPrinted = true;
                    playback.Result?.printAtkEffectEvent?.Invoke();
                    if (!playback.Active) return true;
                }
                sample(time, sampled);
                playback.Finished = clock.SectionComplete && sourceEnd >= completionSourceTime - .000002f;
                return clock.SectionComplete;
            },
        };
        if (feedback) action.SetEffectTiming(EffectTiming.NOT_PRINT, EffectTiming.PRE, EffectTiming.PRE);
        Actions.Add(action);
        if (reaction.HasValue)
            Reactions.Add(new RencounterManager.MovingAction(reaction.Value, CharMoveState.Stop, updateDir: false, delay: 0f));
    }

    internal readonly struct Cue
    {
        internal readonly float SourceTime;
        internal readonly Action<float> Apply;

        internal Cue(float sourceTime, Action<float> apply)
        {
            SourceTime = sourceTime;
            Apply = apply;
        }
    }

    internal static Cue At(float sourceTime, Action<float> apply) => new(sourceTime, apply);

    internal static Cue PlaySound(float sourceTime, string clip, float volume)
        => At(sourceTime, _ => ActionAudio.Play(clip, volume));

    internal static Cue PlayVoice(float sourceTime, string clip)
        => At(sourceTime, _ => ActionAudio.Play(clip));
}

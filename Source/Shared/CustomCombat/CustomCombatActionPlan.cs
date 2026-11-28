using System;
using System.Collections.Generic;
using UnityEngine;

namespace 裁缝之王.Presentation.CustomCombat;

// 额外动作信息独立于原版MovingAction；原版仍按原来的双人动作列表播放。
internal sealed class CustomCombatActionPlan
{
    // 只填写本动作需要控制的字段；留空的字段由目标保留自身状态。
    internal struct TargetPose
    {
        public Vector3? Translation;

        public Quaternion? RotationTransform;

        public Direction? Direction;

        public BattleUnitView.RotationState? Rotation;

        public float? DamageTime;

        public ActionDetail? Motion;
    }

    public bool DefaultFocus = true;
    public bool DefaultPosition = true;

    // 广域乱射时是否完全单靶聚焦（压暗非受击原主目标）；false 时维持原主目标在聚焦层不变。
    public bool SoloFocus;

    // 一段连续副轨道只进入、退出一次。退出时可区分自然结束与强制中断。
    public Action<CustomCombatResolution.Hit> OnEnter { get; set; }
    public Action<CustomCombatResolution.Hit, bool> OnExit { get; set; }

    // 副轨按演出顺序声明，通过主轨动作段共享播放时点。
    internal sealed class Segment
    {
        public RencounterManager.MovingAction Cue { get; }
        public ActionDetail? Motion { get; set; }
        public CharMoveState? MoveState { get; set; }
        public Action<CustomCombatResolution.Hit> OnStart { get; set; }

        public Segment(RencounterManager.MovingAction cue)
        {
            Cue = cue;
        }
    }

    // 留空时从正式受击轨生成。动作组也可以直接指定整条副轨。
    public List<Segment> SecondaryTrack { get; set; }

    internal static List<Segment> CopyReactions(IEnumerable<RencounterManager.MovingAction> reactions)
    {
        var track = new List<Segment>();
        if (reactions == null) return track;
        foreach (var reaction in reactions)
        {
            track.Add(new Segment(reaction) { Motion = reaction.actionDetail, MoveState = reaction.moveState });
        }
        return track;
    }

    public Func<int, Vector3> InitialOffset;

    public Func<int, Vector3[]> InitialPositions;

    public Func<TargetPose> Sample { get; set; }

    internal static void BindTargets(ref RencounterManager.ActionAfterBehaviour self, RencounterManager.ActionAfterBehaviour opponent,
        Func<int, Vector3[]> initialPositions = null, bool soloFocus = false)
    {
        var manager = CustomCombatRuntime.Playback;
        var frame = CustomCombatResolution.Find(self.behaviourResultData);
        if (!manager || !manager.IsRunning || frame == null || manager.SecondaryPlayback.HasPlan(self.behaviourResultData)) return;
        var actor = self.view;
        var victim = opponent.view;
        var plan = new CustomCombatActionPlan
        {
            SoloFocus = soloFocus,
            InitialOffset = index => GetStaggeredOffset(actor, victim, index),
            InitialPositions = initialPositions,
            Sample = () => new TargetPose { Motion = victim.charAppearance.GetCurrentMotionDetail(), DamageTime = victim._remainDmgTime },
        };
        CustomCombatPlayback.BindFormal(ref self, plan, opponent.infoList);
    }

    internal static Vector3 GetStaggeredOffset(BattleUnitView actor, BattleUnitView victim, int index)
    {
        float lane = index % 2 == 0 ? -1f : 1f;
        float side = victim.WorldPosition.x <= actor.WorldPosition.x ? -1f : 1f;
        float tileSize = SingletonBehavior<HexagonalMapManager>.Instance.tileSize;
        return new Vector3(-side * lane * tileSize * 1.3f, 0f, lane * tileSize * 3f);
    }
}

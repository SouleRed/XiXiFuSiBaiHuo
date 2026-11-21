using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace 裁缝之王.Presentation.CustomCombat;

// 额外动作信息独立于原版MovingAction；原版仍按原来的双人动作列表播放。
internal sealed class CustomCombatActionPlan
{
    // 只填写本动作需要控制的字段；留空的字段由目标保留自身状态。
    internal struct TargetPose
    {
        public float? DamageTime;

        public ActionDetail? Motion;
    }

    // 默认由管理器在整段动作前后切层；特殊动作可关闭并调用管理器接口。
    public bool AutoFocus = true;


    public Action<CustomCombatRencounterManager, BattleCardBehaviourResult> FormalStart;

    public Action<CustomCombatRencounterManager, BattleCardBehaviourResult> FormalEnd;

    internal sealed class TargetAction
    {
        public ActionDetail Motion;

        public CharMoveState MoveState;
    }

    public readonly Dictionary<RencounterManager.MovingAction, TargetAction> Actions = new();

    public Func<int, Vector3> InitialOffset;

    public Func<int, Vector3[]> InitialPositions;

    public Func<TargetPose> Sample { get; set; }

    internal void SetActions(IEnumerable<RencounterManager.MovingAction> reactions)
    {
        Actions.Clear();
        foreach (var reaction in reactions) Actions[reaction] = new TargetAction { Motion = reaction.actionDetail, MoveState = reaction.moveState };
    }

    internal static void BindTargets(ref RencounterManager.ActionAfterBehaviour self, RencounterManager.ActionAfterBehaviour opponent,
        Func<int, Vector3[]> initialPositions = null)
    {
        var manager = CustomCombatRuntime.Playback;
        if (!manager || !manager.IsRunning || CustomCombatResolution.Find(self.behaviourResultData) == null || manager.SecondaryPlayback.HasPlan(self.behaviourResultData)) return;
        var actor = self.view;
        var victim = opponent.view;
        var plan = new CustomCombatActionPlan
        {
            AutoFocus = false,
            InitialOffset = index => GetStaggeredOffset(actor, victim, index),
            InitialPositions = initialPositions,
            Sample = () => new TargetPose { Motion = victim.charAppearance.GetCurrentMotionDetail(), DamageTime = victim._remainDmgTime },
            FormalStart = (owner, record) => owner.EnterSecondaryFocus(record),
            FormalEnd = (owner, record) => { if (!owner.CanContinueSecondaryFocus(record)) owner.LeaveSecondaryFocus(); },
        };
        plan.SetActions(opponent.infoList);
        CustomCombatPlayback.BindFormal(ref self, plan);
    }

    private static Vector3 GetStaggeredOffset(BattleUnitView actor, BattleUnitView victim, int index)
    {
        float lane = index % 2 == 0 ? -1f : 1f;
        float side = victim.WorldPosition.x <= actor.WorldPosition.x ? -1f : 1f;
        float tileSize = SingletonBehavior<HexagonalMapManager>.Instance.tileSize;
        return new Vector3(-side * lane * tileSize * 1.3f, 0f, lane * tileSize * 3f);
    }
}

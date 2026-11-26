using System;
using System.Collections.Generic;
using LOR_DiceSystem;
using 裁缝之王.Presentation.CustomCombat;
using 裁缝之王.Presentation.Effects;

namespace 裁缝之王.Presentation.Actions;

public abstract class KingBehaviourAction : BehaviourActionBase
{
    internal const string PackageId = "CaiFengZhiKing";

    private static bool Playable(BattleCardBehaviourResult result)
        => result?.cardModel?.GetID().packageId == PackageId && result.hasBehaviour
            && result.diceBehaviourResult.result == Result.Win && !result.diceBehaviourResult.skip
            && !result.isBlockedDice && !result.isDestroyed;

    private static bool IsRangedAttackClash(BattleCardBehaviourResult self, BattleCardBehaviourResult opponent)
        => self.diceBehaviourResult.range == CardRange.Near && opponent?.hasBehaviour == true
            && opponent.diceBehaviourResult.range == CardRange.Far && opponent.diceBehaviourResult.actionType == ActionType.Atk;

    internal static bool IsNativeWinningAction(BattleUnitView view, BattleCardBehaviourResult result)
        => Playable(result)
            && Singleton<AssemblyManager>.Instance.CreateInstance_BehaviourAction(result.behaviourRawData?.ActionScript)
                is KingBehaviourAction action && action.MatchesSkin(view);

    protected virtual bool MatchesSkin(BattleUnitView view) => NativeKingSkin.IsNative(view);

    protected virtual List<RencounterManager.MovingAction> BuildDeflection(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
        => KingShortClashes.Build(ref self, ref opponent);

    internal static bool IsFormal(BattleCardBehaviourResult self, BattleCardBehaviourResult opponent)
        => Playable(self) && opponent != null && !opponent.diceBehaviourResult.skip
            && !IsRangedAttackClash(self, opponent)
            && Singleton<AssemblyManager>.Instance.CreateInstance_BehaviourAction(self.behaviourRawData?.ActionScript) is KingBehaviourAction;

    public sealed override List<RencounterManager.MovingAction> GetMovingAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var result = self.behaviourResultData;
        var opposingResult = opponent.behaviourResultData;
        if (!MatchesSkin(self.view) || !opponent.view || !Playable(result) || opposingResult?.diceBehaviourResult.skip == true)
            return base.GetMovingAction(ref self, ref opponent);

        bool deflection = IsRangedAttackClash(result, opposingResult);
        var anchor = CustomCombatResolution.Find(result)?.Main?.view ?? opponent.view;
        if ((deflection && result.diceBehaviourResult.actionType != ActionType.Atk)
            || !RencounterEffects.EnsureOpen(self.view, anchor))
            return base.GetMovingAction(ref self, ref opponent);

        return deflection ? BuildDeflection(ref self, ref opponent) : BuildAction(ref self, ref opponent);
    }

    protected abstract List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent);
}

internal static class KingContinuation
{
    private static BattleCardBehaviourResult pending;
    private static Action restoreDelay;

    internal static bool TryGetContinuousNext(BattleUnitView actor, BattleUnitView target,
        BattleCardBehaviourResult current, out BattleCardBehaviourResult next)
    {
        next = null;
        if (!actor || !target || current?.playingCard?.owner != actor.model || !KingBehaviourAction.IsNativeWinningAction(actor, current)) return false;
        var frame = CustomCombatResolution.Find(current);
        var logTarget = frame?.Main != null ? frame.Main : target.model;
        var own = actor.model.battleCardResultLog?.BehaviourResultList;
        var other = logTarget?.battleCardResultLog?.BehaviourResultList;
        if (own == null || other == null) return false;
        int index = own.IndexOf(current);
        if (index < 0 || index + 1 >= own.Count || index + 1 >= other.Count) return false;

        var candidate = own[index + 1];
        if (!KingBehaviourAction.IsFormal(current, other[index]) || !KingBehaviourAction.IsFormal(candidate, other[index + 1])
            || !ReferenceEquals(current.playingCard, candidate.playingCard)
            || (candidate.behaviourIdx != current.behaviourIdx + 1 && !NeedleClanBehaviourAction.IsRepeat(current, candidate)))
            return false;
        next = candidate;
        return true;
    }

    internal static bool IsExpected(BattleCardBehaviourResult record) => record != null && ReferenceEquals(pending, record);

    internal static void Expect(BattleCardBehaviourResult next) => pending = next;

    // 两种宿主都在实际取出下一对骰子时消费前一段留下的续段资格。
    internal static bool Prepare(BattleUnitView enemy, BattleUnitView librarian,
        BattleCardBehaviourResult enemyResult, BattleCardBehaviourResult librarianResult,
        bool auto, bool noDelay, Action<bool> setNoDelay)
    {
        var expected = pending;
        pending = null;
        if (expected == null) return false;
        bool matched = (ReferenceEquals(expected, enemyResult) && KingBehaviourAction.IsNativeWinningAction(enemy, enemyResult)
                && KingBehaviourAction.IsFormal(enemyResult, librarianResult))
            || (ReferenceEquals(expected, librarianResult) && KingBehaviourAction.IsNativeWinningAction(librarian, librarianResult)
                && KingBehaviourAction.IsFormal(librarianResult, enemyResult));
        if (!matched) return false;

        if (!enemy.model.moveDetail.isArrived) enemy.model.moveDetail.Stop();
        if (!librarian.model.moveDetail.isArrived) librarian.model.moveDetail.Stop();
        if (auto && !noDelay)
        {
            setNoDelay(true);
            restoreDelay = () => setNoDelay(noDelay);
        }
        return true;
    }

    internal static void RestoreDelay()
    {
        var restore = restoreDelay;
        restoreDelay = null;
        restore?.Invoke();
    }

    internal static void Clear()
    {
        pending = null;
        RestoreDelay();
    }
}

using System;
using Soule.CombatEntry;
using 裁缝之王.Presentation.Actions;


namespace 裁缝之王.Presentation.CustomCombat;

// 将前置库分配的行动交给本 MOD 的结算和演出管理器。
internal static class CustomCombatRuntime
{
    private static BattleUnitModel enemy, librarian;

    private static CustomCombatParryingManager parrying;

    private static CustomCombatOneSideManager oneSide;

    private static CombatActionSession action;

    internal static CustomCombatRencounterManager Playback { get; private set; }

    private static CombatActionPhase Phase => action?.Phase ?? CombatActionPhase.Inactive;

    internal static bool Active => Phase != CombatActionPhase.Inactive;

    internal static bool IsRunning => Phase == CombatActionPhase.Playing;

    internal static bool ActionRunning => IsRunning && Playback._actionAfterBehaviourIsRunning;

    internal static void Register()
    {
        CombatEntry.Register(KingBehaviourAction.PackageId, 0, CanHandle, Resolve, Play, Abort);
    }

    private static bool CanHandle(BattlePlayingCardDataInUnitModel first, BattlePlayingCardDataInUnitModel second)
    {
        return NeedsCustomCombat(first) || NeedsCustomCombat(second);
    }

    private static bool NeedsCustomCombat(BattlePlayingCardDataInUnitModel page)
    {
        return CustomCombatTargeting.HasSecondaryTargets(page);
    }

    private static void Resolve(StageController stage, BattlePlayingCardDataInUnitModel first, BattlePlayingCardDataInUnitModel second, CombatActionSession session)
    {
        action = session;

        // 与原版舞台对拼点、单方面攻击的双人槽位分配保持一致，允许双方同阵营。
        bool firstIsEnemySide = first.owner.faction == Faction.Enemy && (second != null || first.target.faction == Faction.Player);
        enemy = firstIsEnemySide ? first.owner : second?.owner ?? first.target;
        librarian = firstIsEnemySide ? second?.owner ?? first.target : first.owner;
        try
        {
            if (second == null)
            {
                stage.phase = StageController.StagePhase.ExecuteOneSideAction;
                first.owner.turnState = BattleUnitTurnState.DOING_ACTION;
                first.target.turnState = BattleUnitTurnState.DOING_ACTION;

                oneSide = new CustomCombatOneSideManager();
                oneSide.StartOneSidePlay(first);
            }
            else
            {
                stage.phase = StageController.StagePhase.ExecuteParrying;
                first.owner.turnState = second.owner.turnState = BattleUnitTurnState.DOING_PARRYING;
                parrying = new CustomCombatParryingManager();
                parrying.StartParrying(first.owner == enemy ? first : second, first.owner == librarian ? first : second);
            }
        }
        catch
        {
            // 已经执行的能力与伤害不能回退到原版重算。
            Complete(session);
            stage.dontUseUILog = false;
            stage.phase = StageController.StagePhase.SetCurrentDiceAction;
            throw;
        }
    }

    private static void Play(RencounterManager original, BattleUnitView enemyView, BattleUnitView librarianView)
    {
        if (enemyView?.model != enemy || librarianView?.model != librarian)
        {
            throw new InvalidOperationException("专用演出上下文与舞台行动不一致。");
        }

        Playback = original.GetComponent<CustomCombatRencounterManager>();
        if (!Playback)
        {
            Playback = original.gameObject.AddComponent<CustomCombatRencounterManager>();
        }

        Playback.StartRencounter(enemyView, librarianView, action);
    }

    internal static BattleUnitModel CurrentWinner => Phase == CombatActionPhase.Resolving ? parrying?.GetCurrentWinner() : null;

    internal static BattleUnitModel CurrentAttacker => Phase == CombatActionPhase.Resolving ? oneSide?.GetCurrentAttacker() ?? parrying?.ContinuingAttacker : null;

    internal static BattleCardBehaviourResult CurrentResult(BattleUnitView view)
    {
        if (!IsRunning) return null;
        if (Playback._librarian == view) return Playback._currentLibrarianBehaviourResult;
        if (Playback._enemy == view) return Playback._currentEnemyBehaviourResult;
        return Playback.CurrentFrame?.ExternalHits.Find(hit => hit.Unit.view == view)?.Receiver;
    }

    internal static void Complete(CombatActionSession session)
    {
        if (!ReferenceEquals(action, session))
        {
            return;
        }

        CustomCombatResolution.Clear();
        action = null;
        Playback = null;
        parrying = null;
        oneSide = null;
        enemy = librarian = null;

        session.Complete();
    }

    internal static void Abort()
    {
        if (!Active)
        {
            return;
        }

        var session = action;
        try
        {
            if (Playback)
            {
                Playback.EndRencounterForcely();
            }
        }
        finally
        {
            Complete(session);
        }
    }
}

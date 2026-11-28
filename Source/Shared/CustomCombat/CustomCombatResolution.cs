using System;
using System.Collections.Generic;
using System.Linq;
using UI;

namespace 裁缝之王.Presentation.CustomCombat;

// 专用结算分支共用的容量上下文；生命周期覆盖一次逻辑结算与对应演出。
internal static class CustomCombatResolution
{
    // 同一次行动中，两张书页可以指定同一个角色的不同速度骰。
    internal sealed class Participant
    {
        public BattleUnitModel Unit;
        public float Hp;
        public int Break;
        public BattleBufUIDataList Buffs;
    }

    internal sealed class Target
    {
        public Participant Participant;
        public BattleUnitModel Unit => Participant.Unit;
        public int Slot, Index;
    }

    internal readonly struct TargetScope : IDisposable
    {
        private readonly BattlePlayingCardDataInUnitModel card;
        private readonly BattleUnitModel previous;
        private readonly int slot;

        public TargetScope(BattlePlayingCardDataInUnitModel card, Target target)
        {
            this.card = card;
            previous = card.target;
            slot = card.targetSlotOrder;
            card.target = target.Unit;
            card.targetSlotOrder = target.Slot;
        }

        public void Dispose()
        {
            card.target = previous;
            card.targetSlotOrder = slot;
        }
    }

    internal sealed class Page
    {
        public BattlePlayingCardDataInUnitModel Card;

        public BattleDiceBehavior FirstDie;

        public Target BarrageTarget;

        public readonly List<Target> Targets = new();
    }

    internal sealed class Hit
    {
        public Target Target;
        public BattleUnitModel Unit => Target.Unit;

        public BattleCardBehaviourResult Sender, Receiver;
    }

    internal sealed class Frame
    {
        public Page Page;

        public BattleUnitModel Main;

        public BattleCardBehaviourResult Record;

        public bool IsRandomBarrage;

        public bool IsContinuation;

        public Hit Formal;

        // 正式目标和副轨道分别消费各自的命中日志。
        public readonly List<Hit> ExternalHits = new();

        public bool Printed;

        public bool HasDifferentTarget => Formal.Unit != Main;

        public BattleUnitModel ActualTarget => Formal.Unit;
    }

    private static readonly Dictionary<BattleUnitModel, Participant> Participants = new();

    private static readonly Dictionary<BattlePlayingCardDataInUnitModel, Page> Pages = new();

    private static readonly Dictionary<BattleCardBehaviourResult, Frame> Frames = new();

    public static bool ResolvingSecondary { get; private set; }

    private static Target GetTarget(BattleUnitModel unit, int slot, int index)
    {
        if (!Participants.TryGetValue(unit, out var participant))
        {
            participant = new Participant
            {
                Unit = unit,
                Hp = unit.hp,
                Break = unit.breakDetail.breakGauge,
                Buffs = unit.bufListDetail.GetBufUIDataList(),
            };
            Participants.Add(unit, participant);
        }
        return new Target { Participant = participant, Slot = slot, Index = index };
    }

    public static Page Prepare(BattlePlayingCardDataInUnitModel card)
    {
        if (Pages.TryGetValue(card, out Page existing)) return existing;
        var page = new Page { Card = card };
        if (CustomCombatTargeting.IsRandomBarragePage(card))
            page.FirstDie = card.GetDiceBehaviorList().Find(die => die.Index == 0);
        if (!CustomCombatTargeting.IsCapacityPage(card))
        {
            page.Targets.Add(GetTarget(card.target, card.targetSlotOrder, -1));
        }
        else if (CustomCombatTargeting.NormalizeTargets(card))
        {
            page.Targets.Add(GetTarget(card.target, card.targetSlotOrder, -1));
            foreach (var sub in card.subTargets)
                page.Targets.Add(GetTarget(sub.target, sub.targetSlotOrder, page.Targets.Count - 1));
        }
        Pages.Add(card, page);
        return page;
    }

    public static Frame Find(BattleCardBehaviourResult record)
        => record != null && Frames.TryGetValue(record, out Frame frame) && (!record.isBlockedDice || frame.HasDifferentTarget) ? frame : null;

    internal static void PrepareCapacity(BattlePlayingCardDataInUnitModel card)
    {
        if (CustomCombatTargeting.IsCapacityPage(card) && card.subTargets != null && card.subTargets.Count > 0)
            Prepare(card);
    }

    internal static Target GetSurvivingSecondary(BattlePlayingCardDataInUnitModel card, Target previous)
    {
        if (!Pages.TryGetValue(card, out var page) || page.Targets.Count < 2) return null;
        if (CustomCombatTargeting.IsRandomBarragePage(card))
            return SelectBarrageTarget(page, excludeMain: true);
        if (previous != null && IsAlive(previous)) return previous;
        return page.Targets.Find(target => target.Index >= 0 && IsAlive(target));
    }

    private static bool IsAlive(Target target) => !target.Unit.IsDead() && !target.Unit.IsExtinction();

    private static Target SelectBarrageTarget(Page page, bool excludeMain)
    {
        var candidates = page.Targets.Where(target => (!excludeMain || target.Index >= 0) && IsAlive(target)).ToList();
        return candidates.Count > 0 ? RandomUtil.SelectOne(candidates) : null;
    }

    internal static void PrepareBarrageRoll(BattleDiceBehavior die)
    {
        if (die == null || !CustomCombatTargeting.IsRandomBarragePage(die.card)) return;
        var page = Prepare(die.card);
        page.BarrageTarget = die == page.FirstDie
            ? page.Targets.Find(target => target.Index < 0)
            : SelectBarrageTarget(page, excludeMain: false);
    }

    public static void Clear()
    {
        Participants.Clear();
        Pages.Clear();
        Frames.Clear();
        ResolvingSecondary = false;
    }

    // 专用管理器逐目标分派；返回值表示主目标是否实际受击（广域乱射命中副目标时返回 false）。
    public static bool ResolveDie(BattleDiceBehavior dice, BattleUnitModel target)
    {
        if (target == null || !CustomCombatTargeting.IsCapacityPage(dice.card))
        {
            dice.GiveDamage(target);
            return true;
        }

        var page = Prepare(dice.card);
        var mainLog = dice.owner.battleCardResultLog;
        var record = mainLog.CurbehaviourResult;
        var main = page.Targets.Find(entry => entry.Unit == target) ?? GetTarget(target, dice.card.targetSlotOrder, -1);
        bool barrage = CustomCombatTargeting.IsRandomBarragePage(dice.card);
        var selected = main;
        if (barrage)
        {
            selected = page.BarrageTarget;
            if (selected == null || !IsAlive(selected)) return false;
        }

        var frame = new Frame { Page = page, Main = target, Record = record, IsRandomBarrage = barrage };
        if (selected != main)
        {
            frame.Formal = ResolveExternal(dice, selected, mainLog, frame, additionalHit: false);
            Frames[record] = frame;
            return false;
        }

        ResolveFormal(dice, main, mainLog, frame);
        if (barrage) return !record.isBlockedDice;
        return true;
    }

    internal static void ResolveContinuationDie(BattleDiceBehavior dice, Target target, BattleUnitModel originalTarget)
    {
        var log = dice.owner.battleCardResultLog;
        var frame = new Frame
        {
            Page = Pages[dice.card],
            Main = originalTarget,
            Record = log.CurbehaviourResult,
            IsContinuation = true,
            IsRandomBarrage = CustomCombatTargeting.IsRandomBarragePage(dice.card),
        };
        ResolveFormal(dice, target, log, frame);
    }

    private static void ResolveFormal(BattleDiceBehavior dice, Target target, BattleCardTotalResult log, Frame frame)
    {
        frame.Formal = ResolveTarget(dice, target);
        Frames[frame.Record] = frame;
        if (frame.Record.isBlockedDice || frame.IsRandomBarrage) return;
        foreach (var entry in frame.Page.Targets)
        {
            if (dice.owner.IsDead() || dice.owner.IsExtinction()) break;
            if (entry == target || (frame.IsContinuation && entry.Unit == frame.Main) || !IsAlive(entry)) continue;
            ResolveExternal(dice, entry, log, frame, additionalHit: true);
        }
    }

    private static Hit ResolveExternal(BattleDiceBehavior dice, Target entry, BattleCardTotalResult mainLog, Frame frame, bool additionalHit)
    {
        var unit = entry.Unit;
        using var scope = new SecondaryResolution(dice, entry, additionalHit);
        var hit = ResolveTarget(dice, entry);
        var sender = scope.Sender.CurbehaviourResult;
        var record = frame.Record;
        record.dmgListTaken.AddRange(sender.dmgListTaken);
        record.breakListTaken.AddRange(sender.breakListTaken);
        record.recoveredHpList.AddRange(sender.recoveredHpList);
        record.recoveredBpList.AddRange(sender.recoveredBpList);
        record.recoveredLight += sender.recoveredLight;
        if (additionalHit) frame.ExternalHits.Add(hit);
        else
        {
            record.dmgListGiven.AddRange(sender.dmgListGiven);
            record.breakListGiven.AddRange(sender.breakListGiven);
            record.addedAttackFilterList.AddRange(sender.addedAttackFilterList);
            record.isBlockedDice = sender.isBlockedDice;
            if (!sender.isBlockedDice) record.printAtkEffectEvent += sender.printAtkEffectEvent;
            record.afterActionEvent += sender.afterActionEvent;
            record.succeedAtkEvent += sender.succeedAtkEvent;
            record.takeDamageEvent += sender.takeDamageEvent;
            scope.Receiver.SetBehaviourResult(new DiceBehaviourResultData
            {
                result = Result.Lose,
                actionDetail = sender.isBlockedDice ? ActionDetail.Default : ActionDetail.Damaged,
            });
        }
        foreach (var coin in sender.GetAddedEmotionCoins()) mainLog.AddEmotionCoin(coin, 1);
        scope.Receiver.SetCurrentHp(unit.hp);
        scope.Receiver.SetCurrentBreakGauge(unit.breakDetail.breakGauge);
        scope.Receiver.SetCurrentBuf(unit.bufListDetail.GetBufUIDataList());
        scope.Receiver.SetDeathState(unit.IsDeadReal());
        scope.Receiver.SetBreakState(unit.IsBreakLifeZero());
        return hit;
    }

    private static Hit ResolveTarget(BattleDiceBehavior dice, Target entry)
    {
        var unit = entry.Unit;
        var hit = new Hit
        {
            Target = entry,
            Sender = dice.owner.battleCardResultLog.CurbehaviourResult,
            Receiver = unit.battleCardResultLog.CurbehaviourResult,
        };
        dice.GiveDamage(unit);
        return hit;
    }

    private sealed class SecondaryResolution : IDisposable
    {
        internal readonly BattleCardTotalResult Sender, Receiver;

        private readonly BattleDiceBehavior dice, targetDice;

        private readonly BattleUnitModel target;
        private readonly TargetScope targetScope;

        private readonly BattleCardTotalResult actorLog, targetLog;

        private readonly int guard, damage;

        private readonly bool parrying, blocked, previousSecondary;

        private readonly DiceStatBonus stats;

        private readonly List<DiceFlag> flags;

        private readonly LOR_DiceSystem.DiceBehaviour mainBehaviour;

        internal SecondaryResolution(BattleDiceBehavior dice, Target entry, bool additionalHit)
        {
            this.dice = dice;
            target = entry.Unit;
            targetDice = dice._targetDice;
            actorLog = dice.owner.battleCardResultLog;
            targetLog = target.battleCardResultLog;
            guard = dice._damageReductionByGuard;
            damage = dice._diceResultDamage;
            parrying = dice._isParrying;
            blocked = dice._isBlocked;
            previousSecondary = ResolvingSecondary;
            stats = dice._statBonus;
            flags = dice._flags;
            mainBehaviour = dice.behaviourInCard;
            Sender = new BattleCardTotalResult(dice.card);
            Receiver = new BattleCardTotalResult(null);
            // 所有分配在切换前完成，避免构造失败留下半切换上下文。
            var targetStats = stats.Copy();
            var targetFlags = new List<DiceFlag>(flags);
            var targetBehaviour = mainBehaviour.Copy();
            dice.owner.battleCardResultLog = Sender;
            target.battleCardResultLog = Receiver;
            targetScope = new TargetScope(dice.card, entry);
            dice._targetDice = null;
            dice._isParrying = false;
            dice._isBlocked = false;
            dice._damageReductionByGuard = 0;
            dice._statBonus = targetStats;
            dice._flags = targetFlags;
            dice.behaviourInCard = targetBehaviour;
            ResolvingSecondary = previousSecondary || additionalHit;
        }

        public void Dispose()
        {
            target.battleCardResultLog = targetLog;
            dice.owner.battleCardResultLog = actorLog;
            targetScope.Dispose();
            dice._targetDice = targetDice;
            dice._damageReductionByGuard = guard;
            dice._diceResultDamage = damage;
            dice._isParrying = parrying;
            dice._isBlocked = blocked;
            dice._statBonus = stats;
            dice._flags = flags;
            dice.behaviourInCard = mainBehaviour;
            ResolvingSecondary = previousSecondary;
            // isBonusAttack、骰子销毁和能力内部状态属于整次攻击，保留实际产生的结果。
        }
    }
}

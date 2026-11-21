using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UI;


namespace 裁缝之王.Presentation.CustomCombat;

// 专用结算分支共用的容量上下文；生命周期覆盖一次逻辑结算与对应演出。
internal static class CustomCombatResolution
{
    internal sealed class Snapshot
    {
        public float Hp;

        public int Break;

        public BattleBufUIDataList Buffs;

        public static Snapshot Capture(BattleUnitModel unit) => new() { Hp = unit.hp, Break = unit.breakDetail.breakGauge, Buffs = unit.bufListDetail.GetBufUIDataList() };
    }

    internal sealed class Page
    {
        public BattlePlayingCardDataInUnitModel Card;

        public readonly List<BattleUnitModel> Targets = new();

        public readonly Dictionary<BattleUnitModel, Snapshot> Initial = new();
    }

    internal sealed class Hit
    {
        public BattleUnitModel Unit;

        public BattleCardBehaviourResult Sender, Receiver;
    }

    internal sealed class Frame
    {
        public Page Page;

        public BattleUnitModel Main;

        public BattleCardBehaviourResult Record;

        public readonly List<Hit> Hits = new();

        public bool Printed;
    }

    private static readonly Dictionary<BattlePlayingCardDataInUnitModel, Page> Pages = new();

    private static readonly Dictionary<BattleCardBehaviourResult, Frame> Frames = new();


    public static Page Prepare(BattlePlayingCardDataInUnitModel card)
    {
        if (Pages.TryGetValue(card, out Page existing)) return existing;
        var page = new Page { Card = card };
        if (!CustomCombatTargeting.IsCapacityPage(card.card)) page.Targets.Add(card.target);
        else if (CustomCombatTargeting.NormalizeTargets(card))
        {
            page.Targets.Add(card.target);
            page.Targets.AddRange(card.subTargets.Select(sub => sub.target));
        }
        foreach (var unit in page.Targets) page.Initial.Add(unit, Snapshot.Capture(unit));
        Pages.Add(card, page);
        return page;
    }

    public static Frame Find(BattleCardBehaviourResult record)
        => record != null && !record.isBlockedDice && Frames.TryGetValue(record, out Frame frame) ? frame : null;

    public static void Clear()
    {
        Pages.Clear();
        Frames.Clear();
    }

    // 专用管理器逐目标分派，副目标复用本骰数值并分别记录结算。
    public static void ResolveDie(BattleDiceBehavior dice, BattleUnitModel target)
    {
        if (target == null || !CustomCombatTargeting.IsCapacityPage(dice.card?.card))
        {
            dice.GiveDamage(target);
            return;
        }

        var page = Prepare(dice.card);
        var actor = dice.owner;
        var mainLog = actor.battleCardResultLog;
        var record = mainLog.CurbehaviourResult;
        dice.GiveDamage(target);
        // 主目标完全挡下此骰时，保留原版打平记录，跳过本骰的副目标结算与演出。
        if (record.isBlockedDice) return;

        var frame = Find(record);
        if (frame == null)
        {
            frame = new Frame { Page = page, Main = target, Record = record };
            Frames.Add(record, frame);
        }

        foreach (var unit in page.Targets)
        {
            if (actor.IsDead() || actor.IsExtinction()) break;
            if (unit == target || unit.IsDead() || unit.IsExtinction()) continue;
            using (var scope = new SecondaryResolution(dice, unit))
            {
                dice.GiveDamage(unit);
                // 副目标反伤、命中恢复等造成的自身变化并入本骰主日志。
                var actorRecord = scope.Sender.CurbehaviourResult;
                record.dmgListTaken.AddRange(actorRecord.dmgListTaken);
                record.breakListTaken.AddRange(actorRecord.breakListTaken);
                record.recoveredHpList.AddRange(actorRecord.recoveredHpList);
                record.recoveredBpList.AddRange(actorRecord.recoveredBpList);
                record.recoveredLight += actorRecord.recoveredLight;
                foreach (var coin in actorRecord.GetAddedEmotionCoins()) mainLog.AddEmotionCoin(coin, 1);
                scope.Receiver.SetCurrentHp(unit.hp);
                scope.Receiver.SetCurrentBreakGauge(unit.breakDetail.breakGauge);
                scope.Receiver.SetCurrentBuf(unit.bufListDetail.GetBufUIDataList());
                scope.Receiver.SetDeathState(unit.IsDeadReal());
                scope.Receiver.SetBreakState(unit.IsBreakLifeZero());
                frame.Hits.Add(new Hit { Unit = unit, Sender = actorRecord, Receiver = scope.Receiver.CurbehaviourResult });
            }
        }
    }

    private sealed class SecondaryResolution : IDisposable
    {
        internal readonly BattleCardTotalResult Sender, Receiver;

        private readonly BattleDiceBehavior dice, targetDice;

        private readonly BattleUnitModel target, originalTarget;

        private readonly BattleCardTotalResult actorLog, targetLog;

        private readonly int guard, damage;

        private readonly bool parrying, blocked;

        private readonly DiceStatBonus stats;

        private readonly List<DiceFlag> flags;

        private readonly LOR_DiceSystem.DiceBehaviour mainBehaviour;

        internal SecondaryResolution(BattleDiceBehavior dice, BattleUnitModel target)
        {
            this.dice = dice;
            this.target = target;
            originalTarget = dice.card.target;
            targetDice = dice._targetDice;
            actorLog = dice.owner.battleCardResultLog;
            targetLog = target.battleCardResultLog;
            guard = dice._damageReductionByGuard;
            damage = dice._diceResultDamage;
            parrying = dice._isParrying;
            blocked = dice._isBlocked;
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
            dice.card.target = target;
            dice._targetDice = null;
            dice._isParrying = false;
            dice._isBlocked = false;
            dice._damageReductionByGuard = 0;
            dice._statBonus = targetStats;
            dice._flags = targetFlags;
            dice.behaviourInCard = targetBehaviour;
        }

        public void Dispose()
        {
            target.battleCardResultLog = targetLog;
            dice.owner.battleCardResultLog = actorLog;
            dice.card.target = originalTarget;
            dice._targetDice = targetDice;
            dice._damageReductionByGuard = guard;
            dice._diceResultDamage = damage;
            dice._isParrying = parrying;
            dice._isBlocked = blocked;
            dice._statBonus = stats;
            dice._flags = flags;
            dice.behaviourInCard = mainBehaviour;
            // isBonusAttack、骰子销毁和能力内部状态属于整次攻击，保留实际产生的结果。
        }
    }
}

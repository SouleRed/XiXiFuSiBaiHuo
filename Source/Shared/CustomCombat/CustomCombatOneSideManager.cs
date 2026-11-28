using LOR_DiceSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using Battle.BufEffect;
using Battle.CreatureEffect;
using Battle.DiceAttackEffect;
using LOR_XML;
using Sound;
using UI;
using UnityEngine;
using ActionAfterBehaviour = RencounterManager.ActionAfterBehaviour;
using MovingAction = RencounterManager.MovingAction;
using RencounterState = RencounterManager.RencounterState;

namespace 裁缝之王.Presentation.CustomCombat;

// 复用原版单方面行动时序，支持攻击容量多目标与主目标退场后续打。
internal sealed class CustomCombatOneSideManager
{
    public BattleUnitModel attacker;

    public BattleUnitModel victim;

    private BattlePlayingCardDataInUnitModel _playingCard;

    private int resultKnockbackEnergy;

    private BattleCardTotalResult _attackerCardBehavioursResult;

    private BattleCardTotalResult _victimCardBehavioursResult;

    public void StartOneSidePlay(BattlePlayingCardDataInUnitModel card)
    {
        Singleton<StageController>.Instance.dontUseUILog = true;
        resultKnockbackEnergy = 0;
        _playingCard = card;
        attacker = card.owner;
        victim = card.target;
        CustomCombatResolution.PrepareCapacity(card);
        attacker.currentDiceAction = card;
        _attackerCardBehavioursResult = new BattleCardTotalResult(card);
        _victimCardBehavioursResult = new BattleCardTotalResult(null);
        attacker.battleCardResultLog = _attackerCardBehavioursResult;
        victim.battleCardResultLog = _victimCardBehavioursResult;
        _ = card.speedDiceResultValue;
        _ = victim.GetSpeedDiceResult(card.targetSlotOrder).value;
        card.OnUseCard_before();
        card.owner.cardHistory.AddCardHistory(card, Singleton<StageController>.Instance.RoundTurn);
        attacker.OnUseCard(card);
        attacker.OnStartCardAction(card);
        card.OnStartOneSideAction();
        attacker.OnStartOneSideAction(card);
        victim.OnStartTargetedOneSide(card);
        _attackerCardBehavioursResult.usedDiceList = card.GetDiceBehaviorList();
        Singleton<StageController>.Instance.dontUseUILog = false;
        _ = card.GetDiceBehaviorList().Count;
        _ = 0;
        NextDice();
        if (victim.IsDead() || victim.IsExtinction()) FinishAgainstDefeatedTarget(append: false);
        else if (attacker.IsDead() || _playingCard.currentBehavior == null || attacker.IsExtinction()) EndCardAction();
        else PreDecision();
    }

    public BattleUnitModel GetCurrentAttacker()
    {
        return attacker;
    }

    private void NextDice()
    {
        _playingCard.NextDice();
    }

    private void PreDecision()
    {
        if (_playingCard.currentBehavior != null) Decision();
        else EndCardAction();
    }

    private void Decision()
    {
        if (_playingCard.currentBehavior != null)
        {
            _attackerCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
            _attackerCardBehavioursResult.SetSkip();
            if (_playingCard.currentBehavior.Type == BehaviourType.Atk)
            {
                CustomCombatResolution.PrepareBarrageRoll(_playingCard.currentBehavior);
                RollAttack(_playingCard.currentBehavior, victim, _attackerCardBehavioursResult, _victimCardBehavioursResult);
            }
            Action();
        }
        else if (_playingCard.cardBehaviorQueue.Count > 0)
        {
            _playingCard.currentBehavior = _playingCard.cardBehaviorQueue.Dequeue();
            _attackerCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
            _attackerCardBehavioursResult.SetSkip();
        }
        else EndCardAction();
    }

    private void Action()
    {
        if (_playingCard.currentBehavior != null)
        {
            if (_playingCard.currentBehavior.behaviourInCard.Type == BehaviourType.Atk)
            {
                bool hitVictim = CustomCombatResolution.ResolveDie(_playingCard.currentBehavior, victim);
                if (hitVictim)
                {
                    victim.UpdateDirection(attacker.view.WorldPosition);
                    resultKnockbackEnergy += _playingCard.currentBehavior.behaviourInCard.KnockbackPower;
                }
                _playingCard.AfterAction();
            }
            else if (_playingCard.currentBehavior.behaviourInCard.Type == BehaviourType.Def)
            {
                _playingCard.owner.cardSlotDetail.keepCard.AddBehaviour(_playingCard.card, _playingCard.currentBehavior);
            }
        }
        _attackerCardBehavioursResult.SetCurrentHp(attacker.hp);
        _victimCardBehavioursResult.SetCurrentHp(victim.hp);
        _attackerCardBehavioursResult.SetCurrentBreakGauge(attacker.breakDetail.breakGauge);
        _victimCardBehavioursResult.SetCurrentBreakGauge(victim.breakDetail.breakGauge);
        SetBehaviourResultData(_playingCard.currentBehavior, null, _attackerCardBehavioursResult, _playingCard.currentBehaviorUI);
        SetBehaviourResultData(null, _playingCard.currentBehavior, _victimCardBehavioursResult);
        _attackerCardBehavioursResult.SetCurrentBuf(attacker.bufListDetail.GetBufUIDataList());
        _victimCardBehavioursResult.SetCurrentBuf(victim.bufListDetail.GetBufUIDataList());
        _attackerCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
        _attackerCardBehavioursResult.SetSkip();
        BattleDiceBehavior currentBehavior = _playingCard.currentBehavior;
        if (currentBehavior != null && currentBehavior.isBonusAttack)
        {
            BattleDiceBehavior currentBehavior2 = _playingCard.currentBehavior;
            if (currentBehavior2 != null && !currentBehavior2.forbiddenBonusDice)
            {
                if (_playingCard.currentBehavior != null) _playingCard.currentBehavior.isBonusAttack = false;
                goto IL_021c;
            }
        }
        NextDice();
        goto IL_021c;
        IL_021c:
        CheckEndAction();
    }

    private void CheckEndAction()
    {
        if (victim.IsDead() || victim.IsExtinction())
        {
            FinishAgainstDefeatedTarget(append: true);
            return;
        }
        if (attacker.IsDead() || attacker.IsExtinction() || _playingCard.currentBehavior == null)
        {
            EndCardAction();
            return;
        }
        if (attacker.IsBreakLifeZero() || attacker.IsKnockout())
        {
            EndCardAction();
            return;
        }
        _attackerCardBehavioursResult.InitNextBehaviour();
        _victimCardBehavioursResult.InitNextBehaviour();
        PreDecision();
    }

    private void FinishAgainstDefeatedTarget(bool append)
    {
        ResolveRemainingDice(_playingCard, victim, _attackerCardBehavioursResult, _victimCardBehavioursResult, append);
        EndCardAction();
    }

    internal static void ResolveRemainingDice(BattlePlayingCardDataInUnitModel card, BattleUnitModel originalTarget,
        BattleCardTotalResult actorLog, BattleCardTotalResult originalTargetLog, bool append)
    {
        var actor = card.owner;
        CustomCombatResolution.Target target = null;
        while (card.currentBehavior is { } die && !actor.IsDead() && !actor.IsExtinction() && !actor.IsBreakLifeZero() && !actor.IsKnockout())
        {
            target = CustomCombatResolution.GetSurvivingSecondary(card, target);
            if (target == null) return;
            if (die.Type == BehaviourType.Def)
            {
                actor.cardSlotDetail.keepCard.AddBehaviour(card.card, die);
                card.NextDice();
                continue;
            }
            if (die.Type != BehaviourType.Atk)
            {
                card.NextDice();
                continue;
            }

            if (append)
            {
                actorLog.InitNextBehaviour();
                originalTargetLog.InitNextBehaviour();
            }
            append = true;
            _ = originalTargetLog.CurbehaviourResult;
            ResolveRemainingAttack(card, die, originalTarget, target, actorLog);
            if (die.isBonusAttack && !die.forbiddenBonusDice) die.isBonusAttack = false;
            else card.NextDice();
        }
    }

    private static void ResolveRemainingAttack(BattlePlayingCardDataInUnitModel card, BattleDiceBehavior die, BattleUnitModel originalTarget,
        CustomCombatResolution.Target target, BattleCardTotalResult actorLog)
    {
        using var scope = new CustomCombatResolution.TargetScope(card, target);
        var unit = target.Unit;
        var previousLog = unit.battleCardResultLog;
        var receiverLog = new BattleCardTotalResult(null);
        unit.battleCardResultLog = receiverLog;
        try
        {
            actorLog.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
            actorLog.SetSkip();
            die._isParrying = false;
            die.SetBlocked(false);
            die._damageReductionByGuard = 0;
            RollAttack(die, unit, actorLog, receiverLog);
            CustomCombatResolution.ResolveContinuationDie(die, target, originalTarget);
            card.AfterAction();
            SetUnitResult(card.owner, actorLog);
            SetUnitResult(unit, receiverLog);
            SetBehaviourResultData(die, null, actorLog, card.currentBehaviorUI);
            SetBehaviourResultData(null, die, receiverLog);
            actorLog.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
            actorLog.SetSkip();
        }
        finally
        {
            unit.battleCardResultLog = previousLog;
        }
    }

    private static void SetUnitResult(BattleUnitModel unit, BattleCardTotalResult log)
    {
        log.SetCurrentHp(unit.hp);
        log.SetCurrentBreakGauge(unit.breakDetail.breakGauge);
        log.SetCurrentBuf(unit.bufListDetail.GetBufUIDataList());
        log.SetDeathState(unit.IsDeadReal());
        log.SetBreakState(unit.IsBreakLifeZero());
    }

    private void EndCardAction()
    {
        if (_playingCard?.currentBehavior == null)
        {
            BattlePlayingCardDataInUnitModel playingCard = _playingCard;
            if (playingCard == null || !(playingCard.cardBehaviorQueue?.Count > 0)) goto IL_0122;
        }
        if (_playingCard.currentBehavior != null && _playingCard.currentBehavior.Type == BehaviourType.Def) _playingCard.owner.cardSlotDetail.keepCard.AddBehaviour(_playingCard.card, _playingCard.currentBehavior);
        if (_playingCard.cardBehaviorQueue.Count > 0)
        {
            foreach (BattleDiceBehavior item in _playingCard.cardBehaviorQueue)
            {
                if (item.Type == BehaviourType.Def) _playingCard.owner.cardSlotDetail.keepCard.AddBehaviour(_playingCard.card, item);
            }
        }
        goto IL_0122;
        IL_0122:
        attacker.OnEndOneSideAction_Before(_playingCard);
        victim.OnEndOnSideActionVictim_Before(_playingCard);
        Singleton<StageController>.Instance.OnEndOneSideAction(attacker, victim, resultKnockbackEnergy);
    }

    private static void RollAttack(BattleDiceBehavior die, BattleUnitModel target, BattleCardTotalResult actorLog, BattleCardTotalResult receiverLog)
    {
        SetAbilityDataBeforeRoll(die.owner, actorLog);
        SetAbilityDataBeforeRoll(target, receiverLog);
        die.BeforeRollDice(null);
        die.RollDice();
        die.UpdateDiceFinalValue();
        SetAbilityDataAfterRoll(die.owner, actorLog);
        SetAbilityDataAfterRoll(target, receiverLog);
    }

    private static void SetAbilityDataBeforeRoll(BattleUnitModel model, BattleCardTotalResult result)
    {
        foreach (BattleUnitBuf activatedBuf in model.bufListDetail.GetActivatedBufList())
        {
            result.SetBufs(activatedBuf);
        }
        foreach (BattleEmotionCardModel passive in model.emotionDetail.PassiveList)
        {
            result.SetEmotionAbility(passive);
        }
        foreach (GiftModel equippedGift in model.UnitData.unitData.GetEquippedGiftList())
        {
            result.SetGift(equippedGift);
        }
    }

    private static void SetAbilityDataAfterRoll(BattleUnitModel model, BattleCardTotalResult result)
    {
        foreach (BattleUnitBuf activatedBuf in model.bufListDetail.GetActivatedBufList())
        {
            result.SetNewBufs(activatedBuf);
        }
        foreach (BattleEmotionCardModel passive in model.emotionDetail.PassiveList)
        {
            result.SetEmotionAbility(passive);
        }
        foreach (GiftModel equippedGift in model.UnitData.unitData.GetEquippedGiftList())
        {
            result.SetGift(equippedGift);
        }
    }

    private static void SetBehaviourResultData(BattleDiceBehavior behaviour, BattleDiceBehavior opponentBehaviour, BattleCardTotalResult result,
        BattleDiceBehavior behaviorForDiceUI = null)
    {
        DiceBehaviourResultData behaviourResult = new()
        {
            passingEvasion = (behaviorForDiceUI?.passingEvasion ?? false),
            BreakState = (behaviorForDiceUI?.breakState ?? false)
        };
        if (behaviour != null)
        {
            behaviourResult.result = Result.Win;
            if (behaviour.behaviourInCard.Type == BehaviourType.Def) behaviourResult.skip = true;
            else behaviourResult.skip = false;
            behaviourResult.actionType = ((behaviour.behaviourInCard.Type != BehaviourType.Atk) ? ActionType.Def : ActionType.Atk);
            behaviourResult.behaviourDetail = behaviour.behaviourInCard.Detail;
            if (result.usedCard != null) _ = result.usedCard.GetSpec().Ranged == CardRange.Near;
            else
                _ = 0;
            behaviourResult.actionDetail = MotionConverter.MotionToAction(behaviour.behaviourInCard.MotionDetail);
            if (behaviour.behaviourInCard.MotionDetail == MotionDetail.N) behaviourResult.actionType = ActionType.None;
            if (behaviour.behaviourInCard.MotionDetailDefault != MotionDetail.N && behaviour.owner != null && !behaviour.owner.customBook.ContainsCategory(behaviour.card.card.GetCategory())) behaviourResult.actionDetail = MotionConverter.MotionToAction(behaviour.behaviourInCard.MotionDetail);
            behaviourResult.range = behaviour.card.card.GetSpec().Ranged;
            behaviourResult.actionStartDir = ActionDirection.Front;
        }
        else
        {
            behaviourResult.range = CardRange.Near;
            behaviourResult.result = Result.Lose;
            behaviourResult.actionType = ActionType.None;
            behaviourResult.behaviourDetail = BehaviourDetail.None;
            behaviourResult.actionDetail = ActionDetail.Default;
            if (opponentBehaviour.behaviourInCard.Type == BehaviourType.Atk) behaviourResult.actionDetail = ActionDetail.Damaged;
        }
        result.SetBehaviourResult(behaviourResult);
        SetDiceResultData(behaviour, result, behaviorForDiceUI);
    }

    private static void SetDiceResultData(BattleDiceBehavior diceBehavior, BattleCardTotalResult result, BattleDiceBehavior diceBehaviorforUI = null)
    {
        result.SetBehaviour((diceBehavior != null) ? diceBehavior : diceBehaviorforUI);
        if (diceBehavior == null)
        {
            result.SetVanillaDiceValue(-1);
            result.SetVanillaDiceFace(-1, -1);
            result.SetResultDiceValue(-1);
            result.SetResultDiceFace(-1, -1);
        }
        else
        {
            result.SetVanillaDiceValue(diceBehavior.DiceVanillaValue);
            result.SetVanillaDiceFace(diceBehavior.GetDiceVanillaMin(), diceBehavior.GetDiceVanillaMax());
            result.SetResultDiceValue(diceBehavior.DiceResultValue);
            result.SetResultDiceFace(diceBehavior.GetDiceMin(), diceBehavior.GetDiceMax());
        }
    }
}

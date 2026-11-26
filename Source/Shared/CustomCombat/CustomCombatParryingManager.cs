using LOR_DiceSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using 裁缝之王.Shared.Interop;
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

// 原版双人结算的本地分支，仅实际攻击入口扩展强化3的容量。
internal sealed class CustomCombatParryingManager
{
    public enum ParryingDiceType
    {
        Attack,
        Defense
    }

    public class ParryingTeam
    {
        public BattleUnitModel unit;

        public BattlePlayingCardDataInUnitModel playingCard;

        public bool isKeepedCard;

        private ParryingTeam opponent;

        public int diceValue;

        public int resultKnockbackEnergy;

        public void Init(BattleUnitModel unit, BattlePlayingCardDataInUnitModel playingCard)
        {
            this.unit = unit;
            this.playingCard = playingCard;
            unit.stopped = false;
            unit.currentDiceAction = playingCard;
            resultKnockbackEnergy = 0;
            isKeepedCard = playingCard.GetType() == typeof(BattleKeepedCardDataInUnitModel);
        }

        public void SetOpponent(ParryingTeam opponent)
        {
            this.opponent = opponent;
        }

        public void NextDice()
        {
            playingCard.NextDice();
        }

        public bool DiceExists()
        {
            return playingCard.currentBehavior != null;
        }

        public int GetEmotionMultiplier()
        {
            return playingCard.emotionMultiplier;
        }

        public ParryingDiceType GetParryingDiceType()
        {
            if (playingCard.currentBehavior == null) return ParryingDiceType.Defense;
            if (playingCard.currentBehavior.behaviourInCard.Type == BehaviourType.Atk) return ParryingDiceType.Attack;
            if (playingCard.currentBehavior.behaviourInCard.Type == BehaviourType.Def) return ParryingDiceType.Defense;
            ParryingDiceType result = ParryingDiceType.Defense;
            switch (playingCard.currentBehavior.behaviourInCard.Detail)
            {
            case BehaviourDetail.Slash:
                result = ParryingDiceType.Attack;
                break;
            case BehaviourDetail.Penetrate:
                result = ParryingDiceType.Attack;
                break;
            case BehaviourDetail.Hit:
                result = ParryingDiceType.Attack;
                break;
            case BehaviourDetail.Guard:
                result = ParryingDiceType.Defense;
                break;
            case BehaviourDetail.Evasion:
                result = ParryingDiceType.Defense;
                break;
            case BehaviourDetail.None:
                result = ParryingDiceType.Defense;
                break;
            }
            return result;
        }

        public CardRange GetRange()
        {
            if (playingCard.currentBehavior == null) return CardRange.Near;
            return playingCard.currentBehavior.card.card.GetSpec().Ranged;
        }

        public BehaviourType GetBehaviourType()
        {
            if (playingCard.currentBehavior == null) return BehaviourType.Def;
            return playingCard.currentBehavior.behaviourInCard.Type;
        }

        public BehaviourDetail GetBehaviourDetail()
        {
            if (playingCard.currentBehavior == null) return BehaviourDetail.None;
            return playingCard.currentBehavior.behaviourInCard.Detail;
        }

        public MotionDetail GetMotionDetail()
        {
            if (playingCard.currentBehavior == null) return MotionDetail.N;
            return playingCard.currentBehavior.behaviourInCard.MotionDetail;
        }

        public MotionDetail GetMotionDetailDefault()
        {
            if (playingCard.currentBehavior == null) return MotionDetail.N;
            return playingCard.currentBehavior.behaviourInCard.MotionDetailDefault;
        }

        public int GetDiceValue()
        {
            return diceValue;
        }

        public void BeforeRollDice(BattleDiceBehavior targetDice)
        {
            if (playingCard.currentBehavior != null) playingCard.currentBehavior.BeforeRollDice(targetDice);
        }

        public void RollDice()
        {
            if (playingCard.currentBehavior != null) playingCard.currentBehavior.RollDice();
        }

        public void UpdateDiceFinalValue()
        {
            if (playingCard.currentBehavior != null)
            {
                playingCard.currentBehavior.UpdateDiceFinalValue();
                diceValue = playingCard.currentBehavior.DiceResultValue;
            }
            else diceValue = 0;
        }
    }

    public enum ParryingDecisionResult
    {
        WinEnemy,
        WinLibrarian,
        Draw
    }

    private const int drawDiff = 1;

    private ParryingTeam _teamEnemy = new();

    private ParryingTeam _teamLibrarian = new();

    private readonly BattleParryingManager.ParryingTeam _redDiceEnemy = new();

    private readonly BattleParryingManager.ParryingTeam _redDiceLibrarian = new();

    private ParryingDecisionResult _decisionResult;

    private ParryingTeam _currentWinnerTeam;

    private ParryingTeam _currentLoserTeam;

    private ParryingTeam _currentAttackerTeam;

    private ParryingTeam _currentDefenderTeam;

    private BattleCardTotalResult _enemyCardBehavioursResult;

    private BattleCardTotalResult _librarianCardBehavioursResult;

    public BattleUnitModel GetCurrentWinner()
    {
        if (_decisionResult == ParryingDecisionResult.WinEnemy) return _teamEnemy.unit;
        if (_decisionResult == ParryingDecisionResult.WinLibrarian) return _teamLibrarian.unit;
        return null;
    }

    public void StartParrying(BattlePlayingCardDataInUnitModel cardEnemy, BattlePlayingCardDataInUnitModel cardLibrarian)
    {
        Singleton<StageController>.Instance.dontUseUILog = true;
        _enemyCardBehavioursResult = new BattleCardTotalResult(cardEnemy);
        _librarianCardBehavioursResult = new BattleCardTotalResult(cardLibrarian);
        cardEnemy.owner.battleCardResultLog = _enemyCardBehavioursResult;
        cardLibrarian.owner.battleCardResultLog = _librarianCardBehavioursResult;
        _teamEnemy.Init(cardEnemy.owner, cardEnemy);
        _teamLibrarian.Init(cardLibrarian.owner, cardLibrarian);
        _teamEnemy.SetOpponent(_teamLibrarian);
        _teamLibrarian.SetOpponent(_teamEnemy);
        SyncToRedDiceTeam(_teamEnemy, _redDiceEnemy);
        SyncToRedDiceTeam(_teamLibrarian, _redDiceLibrarian);
        _redDiceEnemy.SetOpponent(_redDiceLibrarian);
        _redDiceLibrarian.SetOpponent(_redDiceEnemy);
        cardEnemy.owner.cardHistory.AddCardHistory(cardEnemy, Singleton<StageController>.Instance.RoundTurn);
        cardLibrarian.owner.cardHistory.AddCardHistory(cardLibrarian, Singleton<StageController>.Instance.RoundTurn);
        cardEnemy.OnUseCard_before();
        cardLibrarian.OnUseCard_before();
        _teamEnemy.unit.OnUseCard(cardEnemy);
        _teamLibrarian.unit.OnUseCard(cardLibrarian);
        _teamEnemy.unit.OnStartCardAction(cardEnemy);
        _teamLibrarian.unit.OnStartCardAction(cardLibrarian);
        cardEnemy.OnStartParrying();
        cardLibrarian.OnStartParrying();
        _teamEnemy.unit.OnParryingStart(cardEnemy);
        _teamLibrarian.unit.OnParryingStart(cardLibrarian);
        Singleton<StageController>.Instance.dontUseUILog = false;
        _enemyCardBehavioursResult.usedDiceList = cardEnemy.GetDiceBehaviorList();
        _librarianCardBehavioursResult.usedDiceList = cardLibrarian.GetDiceBehaviorList();
        _teamEnemy.NextDice();
        _teamLibrarian.NextDice();
        _librarianCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
        _enemyCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
        if (_teamEnemy.unit.IsDead() || _teamLibrarian.unit.IsDead() || (!_teamEnemy.DiceExists() && !_teamLibrarian.DiceExists()) || _teamEnemy.unit.IsExtinction() || _teamLibrarian.unit.IsExtinction()) EndParrying();
        else FirstApproachPhase();
    }

    private void FirstApproachPhase()
    {
        Decision();
    }

    private void Decision()
    {
        RollDice();
        bool flag = false;
        if (_teamEnemy.DiceExists() && !_teamLibrarian.DiceExists())
        {
            _decisionResult = ParryingDecisionResult.WinEnemy;
            if (!_teamEnemy.isKeepedCard && _teamEnemy.GetBehaviourType() == BehaviourType.Def)
            {
                if (!_teamEnemy.playingCard.currentBehavior.isBonusEvasion) _teamEnemy.unit.cardSlotDetail.keepCard.AddBehaviour(_teamEnemy.playingCard.card, _teamEnemy.playingCard.currentBehavior);
                else _teamEnemy.playingCard.currentBehavior.passingEvasion = true;
            }
        }
        else if (!_teamEnemy.DiceExists() && _teamLibrarian.DiceExists())
        {
            _decisionResult = ParryingDecisionResult.WinLibrarian;
            if (!_teamLibrarian.isKeepedCard && _teamLibrarian.GetBehaviourType() == BehaviourType.Def)
            {
                if (!_teamLibrarian.playingCard.currentBehavior.isBonusEvasion) _teamLibrarian.unit.cardSlotDetail.keepCard.AddBehaviour(_teamLibrarian.playingCard.card, _teamLibrarian.playingCard.currentBehavior);
                else _teamLibrarian.playingCard.currentBehavior.passingEvasion = true;
            }
        }
        else if (_teamEnemy.DiceExists() && _teamLibrarian.DiceExists())
        {
            flag = true;
            _decisionResult = GetDecisionResult(_teamEnemy, _teamLibrarian);
        }
        if (_teamEnemy.GetRange() == CardRange.Far && _teamLibrarian.GetRange() == CardRange.Far)
        {
            if (_teamEnemy.GetParryingDiceType() == ParryingDiceType.Attack && _teamLibrarian.GetParryingDiceType() == ParryingDiceType.Defense)
            {
                _currentAttackerTeam = _teamEnemy;
                _currentDefenderTeam = _teamLibrarian;
            }
            else if (_teamLibrarian.GetParryingDiceType() == ParryingDiceType.Attack && _teamEnemy.GetParryingDiceType() == ParryingDiceType.Defense)
            {
                _currentAttackerTeam = _teamLibrarian;
                _currentDefenderTeam = _teamEnemy;
            }
        }
        else if (_teamEnemy.GetRange() == CardRange.Far && _teamLibrarian.GetRange() == CardRange.Near)
        {
            _currentAttackerTeam = _teamEnemy;
            _currentDefenderTeam = _teamLibrarian;
        }
        else if (_teamEnemy.GetRange() == CardRange.Near && _teamLibrarian.GetRange() == CardRange.Far)
        {
            _currentAttackerTeam = _teamLibrarian;
            _currentDefenderTeam = _teamEnemy;
        }
        else if (_teamEnemy.GetParryingDiceType() == ParryingDiceType.Attack && _teamLibrarian.GetParryingDiceType() == ParryingDiceType.Defense)
        {
            _currentAttackerTeam = _teamEnemy;
            _currentDefenderTeam = _teamLibrarian;
        }
        else if (_teamLibrarian.GetParryingDiceType() == ParryingDiceType.Attack && _teamEnemy.GetParryingDiceType() == ParryingDiceType.Defense)
        {
            _currentAttackerTeam = _teamLibrarian;
            _currentDefenderTeam = _teamEnemy;
        }
        if (_teamEnemy.GetParryingDiceType() == ParryingDiceType.Attack && _teamLibrarian.GetParryingDiceType() == ParryingDiceType.Defense)
        {
            _currentAttackerTeam = _teamEnemy;
            _currentDefenderTeam = _teamLibrarian;
        }
        else if (_teamLibrarian.GetParryingDiceType() == ParryingDiceType.Attack && _teamEnemy.GetParryingDiceType() == ParryingDiceType.Defense)
        {
            _currentAttackerTeam = _teamLibrarian;
            _currentDefenderTeam = _teamEnemy;
        }
        int num = 0;
        int num2 = 0;
        if (_decisionResult == ParryingDecisionResult.WinEnemy)
        {
            _currentWinnerTeam = _teamEnemy;
            _currentLoserTeam = _teamLibrarian;
            if (flag)
            {
                num += _currentWinnerTeam.GetEmotionMultiplier();
                num2 += _currentLoserTeam.GetEmotionMultiplier();
                int count = _currentWinnerTeam.unit.emotionDetail.CreateEmotionCoin(EmotionCoinType.Positive, num);
                int count2 = _currentLoserTeam.unit.emotionDetail.CreateEmotionCoin(EmotionCoinType.Negative, num2);
                _currentWinnerTeam.unit.battleCardResultLog?.AddEmotionCoin(EmotionCoinType.Positive, count);
                _currentLoserTeam.unit.battleCardResultLog?.AddEmotionCoin(EmotionCoinType.Negative, count2);
            }
        }
        else if (_decisionResult == ParryingDecisionResult.WinLibrarian)
        {
            _currentWinnerTeam = _teamLibrarian;
            _currentLoserTeam = _teamEnemy;
            if (flag)
            {
                num += _currentWinnerTeam.GetEmotionMultiplier();
                num2 += _currentLoserTeam.GetEmotionMultiplier();
                int count3 = _currentWinnerTeam.unit.emotionDetail.CreateEmotionCoin(EmotionCoinType.Positive, num);
                int count4 = _currentLoserTeam.unit.emotionDetail.CreateEmotionCoin(EmotionCoinType.Negative, num2);
                _currentWinnerTeam.unit.battleCardResultLog?.AddEmotionCoin(EmotionCoinType.Positive, count3);
                _currentLoserTeam.unit.battleCardResultLog?.AddEmotionCoin(EmotionCoinType.Negative, count4);
            }
        }
        else
        {
            if (_teamEnemy.GetParryingDiceType() == ParryingDiceType.Attack && _teamLibrarian.GetParryingDiceType() == ParryingDiceType.Attack)
            {
                int emotionMultiplier = _teamLibrarian.GetEmotionMultiplier();
                int emotionMultiplier2 = _teamEnemy.GetEmotionMultiplier();
                int count5 = _teamLibrarian.unit.emotionDetail.CreateEmotionCoin(EmotionCoinType.Positive, emotionMultiplier);
                int count6 = _teamEnemy.unit.emotionDetail.CreateEmotionCoin(EmotionCoinType.Positive, emotionMultiplier2);
                _teamLibrarian.unit.battleCardResultLog?.AddEmotionCoin(EmotionCoinType.Positive, count5);
                _teamEnemy.unit.battleCardResultLog?.AddEmotionCoin(EmotionCoinType.Positive, count6);
            }
            _currentWinnerTeam = null;
            _currentLoserTeam = null;
        }
        if (_teamLibrarian.DiceExists())
        {
            _teamLibrarian.playingCard.currentBehavior.isBonusEvasion = false;
            _teamLibrarian.playingCard.currentBehavior.isBonusAttack = false;
        }
        if (_teamEnemy.DiceExists())
        {
            _teamEnemy.playingCard.currentBehavior.isBonusEvasion = false;
            _teamEnemy.playingCard.currentBehavior.isBonusAttack = false;
        }
        ActionPhase();
    }

    private void ActionPhase()
    {
        if (_decisionResult == ParryingDecisionResult.Draw)
        {
            if (_teamEnemy.GetParryingDiceType() == ParryingDiceType.Attack)
            {
                if (_teamLibrarian.GetParryingDiceType() == ParryingDiceType.Attack) ActionPhaseAtkVSAtkDraw();
                else ActionPhaseAtkVSDfnDraw();
            }
            else if (_teamLibrarian.GetParryingDiceType() == ParryingDiceType.Attack)
            {
                ActionPhaseAtkVSDfnDraw();
            }
            else ActionPhaseDfnVSDfnDraw();
        }
        else if (_currentWinnerTeam.GetParryingDiceType() == ParryingDiceType.Attack)
        {
            if (_currentLoserTeam.GetParryingDiceType() == ParryingDiceType.Attack) ActionPhaseAtkVSAtk();
            else ActionPhaseAtkVSDfn();
        }
        else if (_currentLoserTeam.DiceExists())
        {
            if (_currentLoserTeam.GetParryingDiceType() == ParryingDiceType.Attack) ActionPhaseAtkVSDfn();
            else ActionPhaseDfnVSDfn();
        }
        EndAction();
    }

    private void ActionPhaseAtkVSAtk()
    {
        _ = _currentWinnerTeam.unit;
        BattleUnitModel unit = _currentLoserTeam.unit;
        if (_currentWinnerTeam.playingCard.currentBehavior == null) return;
        if (_currentLoserTeam.GetRange() == CardRange.Far)
        {
            if (_currentWinnerTeam.GetRange() == CardRange.Near)
            {
                if (!_currentWinnerTeam.playingCard.currentBehavior.forbiddenBonusDice)
                {
                    if (_currentWinnerTeam.GetBehaviourType() == BehaviourType.Standby) _currentWinnerTeam.playingCard.currentBehavior.isBonusAttack = true;
                    else
                    {
                        BattleDiceBehavior currentBehavior = _currentWinnerTeam.playingCard.currentBehavior;
                        BattleDiceBehavior battleDiceBehavior = _currentWinnerTeam.playingCard.CopyDiceBehaviour(currentBehavior);
                        battleDiceBehavior.winAgainstFarAtk = true;
                        _currentWinnerTeam.playingCard.AddDice(battleDiceBehavior);
                    }
                }
            }
            else
            {
                CustomCombatResolution.ResolveDie(_currentWinnerTeam.playingCard.currentBehavior, unit);
                if (_currentWinnerTeam.GetBehaviourType() == BehaviourType.Standby && !_currentLoserTeam.unit.IsDead()) _currentWinnerTeam.playingCard.currentBehavior.isBonusAttack = true;
            }
            if (_currentLoserTeam.DiceExists())
            {
                _currentWinnerTeam.playingCard.OnWinParryingDefense();
                _currentLoserTeam.playingCard.OnLoseParrying();
            }
        }
        else
        {
            bool mainHit = CustomCombatResolution.ResolveDie(_currentWinnerTeam.playingCard.currentBehavior, unit);
            if (mainHit)
            {
                int knockbackPower = _currentWinnerTeam.playingCard.currentBehavior.behaviourInCard.KnockbackPower;
                int num = 0;
                _currentLoserTeam.resultKnockbackEnergy += knockbackPower - num;
            }
            if (_currentLoserTeam.DiceExists())
            {
                _currentWinnerTeam.playingCard.OnWinParryingAttack();
                _currentLoserTeam.playingCard.OnLoseParrying();
            }
            if (_currentWinnerTeam.GetBehaviourType() == BehaviourType.Standby && !_currentLoserTeam.unit.IsDead()) _currentWinnerTeam.playingCard.currentBehavior.isBonusAttack = true;
        }
    }

    private void ActionPhaseAtkVSAtkDraw()
    {
        _teamEnemy.playingCard.OnDrawParrying();
        _teamLibrarian.playingCard.OnDrawParrying();
    }

    private void ActionPhaseAtkVSDfn()
    {
        if (!_currentDefenderTeam.DiceExists())
        {
            if (CustomCombatResolution.ResolveDie(_currentAttackerTeam.playingCard.currentBehavior, _currentDefenderTeam.unit))
            {
                int knockbackPower = _currentAttackerTeam.playingCard.currentBehavior.behaviourInCard.KnockbackPower;
                int num = 0;
                _currentDefenderTeam.resultKnockbackEnergy += knockbackPower - num;
            }
        }
        else if (_currentDefenderTeam.GetBehaviourDetail() == BehaviourDetail.Guard)
        {
            if (_currentDefenderTeam == _currentLoserTeam)
            {
                _currentAttackerTeam.playingCard.currentBehavior.SetDamageRedution(_currentDefenderTeam.GetDiceValue());
                if (CustomCombatResolution.ResolveDie(_currentAttackerTeam.playingCard.currentBehavior, _currentDefenderTeam.unit))
                {
                    int knockbackPower2 = _currentAttackerTeam.playingCard.currentBehavior.behaviourInCard.KnockbackPower;
                    int num2 = 0;
                    _currentDefenderTeam.resultKnockbackEnergy += knockbackPower2 - num2;
                }
                if (_currentLoserTeam.DiceExists())
                {
                    _currentWinnerTeam.playingCard.OnWinParryingAttack();
                    _currentLoserTeam.playingCard.OnLoseParrying();
                }
                if (_currentAttackerTeam.GetBehaviourType() == BehaviourType.Standby && !_currentLoserTeam.unit.IsDead()) _currentAttackerTeam.playingCard.currentBehavior.isBonusAttack = true;
            }
            else
            {
                CardRange ranged = _currentAttackerTeam.playingCard.currentBehavior.card.card.GetSpec().Ranged;
                if (ranged == CardRange.Near || ranged == CardRange.Special) _currentDefenderTeam.playingCard.currentBehavior.GiveDeflectDamage(_currentAttackerTeam.playingCard.currentBehavior);
                _currentDefenderTeam.playingCard.OnWinParryingDefense();
                _currentAttackerTeam.playingCard.OnLoseParrying();
                if (_currentDefenderTeam.GetBehaviourType() == BehaviourType.Standby && !_currentLoserTeam.unit.IsDead()) _currentDefenderTeam.playingCard.currentBehavior.isBonusEvasion = true;
            }
        }
        else
        {
            if (_currentDefenderTeam.GetBehaviourDetail() != BehaviourDetail.Evasion) return;
            if (_currentDefenderTeam == _currentLoserTeam)
            {
                if (CustomCombatResolution.ResolveDie(_currentAttackerTeam.playingCard.currentBehavior, _currentDefenderTeam.unit))
                {
                    int knockbackPower3 = _currentAttackerTeam.playingCard.currentBehavior.behaviourInCard.KnockbackPower;
                    int num3 = 0;
                    _currentDefenderTeam.resultKnockbackEnergy += knockbackPower3 - num3;
                }
                if (_currentLoserTeam.DiceExists())
                {
                    _currentWinnerTeam.playingCard.OnWinParryingAttack();
                    _currentLoserTeam.playingCard.OnLoseParrying();
                }
                if (_currentAttackerTeam.GetBehaviourType() == BehaviourType.Standby && !_currentLoserTeam.unit.IsDead()) _currentAttackerTeam.playingCard.currentBehavior.isBonusAttack = true;
            }
            else
            {
                if (_currentAttackerTeam.GetBehaviourType() == BehaviourType.Atk)
                {
                    int diceResultValue = _currentDefenderTeam.playingCard.currentBehavior.DiceResultValue;
                    _currentDefenderTeam.unit.breakDetail.OnRecoverBreakByEvaision(diceResultValue);
                    _currentDefenderTeam.playingCard.currentBehavior.isBonusEvasion = true;
                }
                else if (_currentDefenderTeam.GetBehaviourType() == BehaviourType.Standby && !_currentLoserTeam.unit.IsDead())
                {
                    _currentDefenderTeam.playingCard.currentBehavior.isBonusEvasion = true;
                }
                _currentDefenderTeam.playingCard.OnWinParryingDefense();
                _currentAttackerTeam.playingCard.OnLoseParrying();
            }
        }
    }

    private void ActionPhaseAtkVSDfnDraw()
    {
        _teamEnemy.playingCard.OnDrawParrying();
        _teamLibrarian.playingCard.OnDrawParrying();
    }

    private void ActionPhaseDfnVSDfn()
    {
        if (!_currentWinnerTeam.DiceExists() || !_currentLoserTeam.DiceExists()) return;
        if (_currentWinnerTeam.GetBehaviourDetail() == BehaviourDetail.Guard)
        {
            if (_currentLoserTeam.GetBehaviourDetail() == BehaviourDetail.Guard)
            {
                CardRange range = _currentLoserTeam.GetRange();
                if (range == CardRange.Near || range == CardRange.Far || range == CardRange.Special) _currentWinnerTeam.playingCard.currentBehavior.GiveDeflectDamage(_currentLoserTeam.playingCard.currentBehavior);
                _currentWinnerTeam.playingCard.OnWinParryingDefense();
                _currentLoserTeam.playingCard.OnLoseParrying();
            }
            else
            {
                CardRange range2 = _currentLoserTeam.GetRange();
                if (range2 == CardRange.Near || range2 == CardRange.Far || range2 == CardRange.Special) _currentWinnerTeam.playingCard.currentBehavior.GiveDeflectDamage(_currentLoserTeam.playingCard.currentBehavior);
                _currentWinnerTeam.playingCard.OnWinParryingDefense();
                _currentLoserTeam.playingCard.OnLoseParrying();
            }
        }
        else if (_currentLoserTeam.GetBehaviourDetail() == BehaviourDetail.Guard)
        {
            int diceResultValue = _currentWinnerTeam.playingCard.currentBehavior.DiceResultValue;
            _currentWinnerTeam.unit.breakDetail.OnRecoverBreakByEvaision(diceResultValue);
            _currentWinnerTeam.playingCard.OnWinParryingDefense();
            _currentLoserTeam.playingCard.OnLoseParrying();
        }
        if (_currentWinnerTeam.GetBehaviourType() == BehaviourType.Standby && !_currentLoserTeam.unit.IsDead()) _currentWinnerTeam.playingCard.currentBehavior.isBonusEvasion = true;
    }

    private void ActionPhaseDfnVSDfnDraw()
    {
        _teamEnemy.playingCard.OnDrawParrying();
        _teamLibrarian.playingCard.OnDrawParrying();
    }

    private ParryingDecisionResult GetDecisionResult(ParryingTeam teamA, ParryingTeam teamB)
    {
        if (teamA.DiceExists() && teamB.DiceExists())
        {
            if (teamA.GetBehaviourDetail() == BehaviourDetail.Evasion && teamB.GetBehaviourDetail() == BehaviourDetail.Evasion) return ParryingDecisionResult.Draw;
            int num = teamA.diceValue - teamB.diceValue;
            if (num >= 1) return ParryingDecisionResult.WinEnemy;
            if (num <= -1) return ParryingDecisionResult.WinLibrarian;
            return ParryingDecisionResult.Draw;
        }
        if (teamA.DiceExists() && !teamB.DiceExists()) return ParryingDecisionResult.WinEnemy;
        if (!teamA.DiceExists() && teamB.DiceExists()) return ParryingDecisionResult.WinLibrarian;
        return ParryingDecisionResult.Draw;
    }

    private void TryRequeueRedDice()
    {
        var checkParryingEnd = RedDiceInterop.CheckParryingEnd;
        if (checkParryingEnd == null) return;

        SyncToRedDiceTeam(_teamEnemy, _redDiceEnemy);
        SyncToRedDiceTeam(_teamLibrarian, _redDiceLibrarian);
        checkParryingEnd(_redDiceEnemy, _redDiceLibrarian);
        SyncFromRedDiceTeam(_redDiceEnemy, _teamEnemy);
        SyncFromRedDiceTeam(_redDiceLibrarian, _teamLibrarian);
    }

    private static void SyncToRedDiceTeam(ParryingTeam source, BattleParryingManager.ParryingTeam target)
    {
        target.unit = source.unit;
        target.playingCard = source.playingCard;
        target.isKeepedCard = source.isKeepedCard;
    }

    private static void SyncFromRedDiceTeam(BattleParryingManager.ParryingTeam source, ParryingTeam target)
    {
        target.playingCard = source.playingCard;
        target.isKeepedCard = source.isKeepedCard;
    }

    private void CheckParryingEnd()
    {
        TryRequeueRedDice();
        if (_teamEnemy.unit.IsDead() || _teamLibrarian.unit.IsDead() || (!_teamEnemy.DiceExists() && !_teamLibrarian.DiceExists()) || _teamEnemy.unit.IsExtinction() || _teamLibrarian.unit.IsExtinction()) EndParrying();
        else if (_teamEnemy.playingCard.isKeepedCard && _teamEnemy.DiceExists() && !_teamLibrarian.DiceExists())
        {
            if (_teamEnemy.playingCard.currentBehavior != null && _teamEnemy.playingCard.currentBehavior.isUsed) _teamEnemy.playingCard.currentBehavior = null;
            EndParrying();
        }
        else if (_teamLibrarian.playingCard.isKeepedCard && _teamLibrarian.DiceExists() && !_teamEnemy.DiceExists())
        {
            if (_teamLibrarian.playingCard.currentBehavior != null && _teamLibrarian.playingCard.currentBehavior.isUsed) _teamLibrarian.playingCard.currentBehavior = null;
            EndParrying();
        }
        else
        {
            _enemyCardBehavioursResult.InitNextBehaviour();
            _librarianCardBehavioursResult.InitNextBehaviour();
            _librarianCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
            _enemyCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
            Decision();
        }
    }

    private void EndParrying()
    {
        CheckStandyDiceBeforeEndParrying(_teamLibrarian);
        CheckStandyDiceBeforeEndParrying(_teamEnemy);
        _teamLibrarian.unit.OnEndParrying_Before(_teamLibrarian.playingCard);
        _teamEnemy.unit.OnEndParrying_Before(_teamEnemy.playingCard);
        Singleton<StageController>.Instance.OnEndParrying(_teamEnemy.unit, _teamLibrarian.unit, _teamEnemy.resultKnockbackEnergy, _teamLibrarian.resultKnockbackEnergy);
    }

    private void CheckStandyDiceBeforeEndParrying(ParryingTeam team)
    {
        if (!team.unit.IsDead() && team.DiceExists() && team.playingCard.isKeepedCard) team.playingCard.AddCurBehaviorToQueueFirst();
    }

    private void RollDice()
    {
        BeforeRollDice();
        _teamEnemy.BeforeRollDice(_teamLibrarian.playingCard.currentBehavior);
        _teamLibrarian.BeforeRollDice(_teamEnemy.playingCard.currentBehavior);
        _teamEnemy.RollDice();
        _teamLibrarian.RollDice();
        _teamEnemy.UpdateDiceFinalValue();
        _teamLibrarian.UpdateDiceFinalValue();
        AfterRollDice();
    }

    private void EndAction()
    {
        if (_teamEnemy.DiceExists()) _teamEnemy.playingCard.AfterAction();
        if (_teamLibrarian.DiceExists()) _teamLibrarian.playingCard.AfterAction();
        _enemyCardBehavioursResult?.SetCurrentHp(_teamEnemy.unit.hp);
        _enemyCardBehavioursResult?.SetCurrentBreakGauge(_teamEnemy.unit.breakDetail.breakGauge);
        if (!_teamEnemy.DiceExists() || !_teamEnemy.playingCard.currentBehavior.isBonusEvasion || _teamLibrarian.DiceExists()) SetBehaviourResultData(_teamEnemy, _teamLibrarian, _enemyCardBehavioursResult);
        _enemyCardBehavioursResult?.SetCurrentBuf(_teamEnemy.unit.bufListDetail.GetBufUIDataList());
        _librarianCardBehavioursResult?.SetCurrentHp(_teamLibrarian.unit.hp);
        _librarianCardBehavioursResult?.SetCurrentBreakGauge(_teamLibrarian.unit.breakDetail.breakGauge);
        if (!_teamLibrarian.DiceExists() || !_teamLibrarian.playingCard.currentBehavior.isBonusEvasion || _teamEnemy.DiceExists()) SetBehaviourResultData(_teamLibrarian, _teamEnemy, _librarianCardBehavioursResult);
        _librarianCardBehavioursResult?.SetCurrentBuf(_teamLibrarian.unit.bufListDetail.GetBufUIDataList());
        _enemyCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
        if (_teamEnemy.DiceExists() && ((!_teamEnemy.playingCard.currentBehavior.isBonusEvasion && !_teamEnemy.playingCard.currentBehavior.isBonusAttack) || _teamEnemy.playingCard.currentBehavior.forbiddenBonusDice)) _teamEnemy.NextDice();
        _librarianCardBehavioursResult.SetBehaviourDiceResultUI(CompareBehaviourUIType.Start);
        if (_teamLibrarian.DiceExists() && ((!_teamLibrarian.playingCard.currentBehavior.isBonusEvasion && !_teamLibrarian.playingCard.currentBehavior.isBonusAttack) || _teamLibrarian.playingCard.currentBehavior.forbiddenBonusDice)) _teamLibrarian.NextDice();
        CheckParryingEnd();
    }

    private void BeforeRollDice()
    {
        SetAbilityDataBeforeRoll(_teamEnemy.unit, _enemyCardBehavioursResult);
        SetAbilityDataBeforeRoll(_teamLibrarian.unit, _librarianCardBehavioursResult);
    }

    private void AfterRollDice()
    {
        SetAbilityDataAfterRoll(_teamEnemy.unit, _enemyCardBehavioursResult);
        SetAbilityDataAfterRoll(_teamLibrarian.unit, _enemyCardBehavioursResult);
    }

    private void SetAbilityDataBeforeRoll(BattleUnitModel model, BattleCardTotalResult result)
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

    private void SetAbilityDataAfterRoll(BattleUnitModel model, BattleCardTotalResult result)
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

    private void SetBehaviourResultData(ParryingTeam team, ParryingTeam opponentTeam, BattleCardTotalResult result)
    {
        DiceBehaviourResultData behaviourResult = default(DiceBehaviourResultData);
        switch (team.GetParryingDiceType())
        {
        case ParryingDiceType.Attack:
            behaviourResult.actionType = ActionType.Atk;
            break;
        case ParryingDiceType.Defense:
            behaviourResult.actionType = ActionType.Def;
            break;
        }
        if (behaviourResult.actionType == ActionType.Def && !opponentTeam.DiceExists())
        {
            behaviourResult.skip = true;
            result.SetSkip();
        }
        else behaviourResult.skip = false;
        behaviourResult.passingEvasion = false;
        behaviourResult.BreakState = false;
        if (team.playingCard != null && team.playingCard.currentBehavior != null)
        {
            behaviourResult.passingEvasion = team.playingCard.currentBehavior.passingEvasion;
            behaviourResult.BreakState = team.playingCard.currentBehavior.breakState;
        }
        behaviourResult.behaviourDetail = team.GetBehaviourDetail();
        if (team.DiceExists() && team.playingCard.currentBehavior.card.card.GetSpec().Ranged == CardRange.Far) behaviourResult.range = CardRange.Far;
        else behaviourResult.range = CardRange.Near;
        behaviourResult.actionDetail = MotionConverter.MotionToAction(team.GetMotionDetail());
        if (team.GetMotionDetailDefault() != MotionDetail.N && !team.unit.customBook.ContainsCategory(team.playingCard.card.GetCategory())) behaviourResult.actionDetail = MotionConverter.MotionToAction(team.GetMotionDetail());
        behaviourResult.actionStartDir = ActionDirection.Front;
        if (_decisionResult == ParryingDecisionResult.Draw) behaviourResult.result = Result.Draw;
        else if (_decisionResult == ParryingDecisionResult.WinEnemy)
        {
            if (team == _teamEnemy) behaviourResult.result = Result.Win;
            else behaviourResult.result = Result.Lose;
        }
        else if (team == _teamLibrarian)
        {
            behaviourResult.result = Result.Win;
        }
        else behaviourResult.result = Result.Lose;
        if (behaviourResult.result == Result.Lose && opponentTeam.GetBehaviourType() == BehaviourType.Atk && behaviourResult.range != CardRange.Far) behaviourResult.actionDetail = ActionDetail.Damaged;
        result.SetBehaviourResult(behaviourResult);
        SetDiceResultData(team.playingCard?.currentBehavior, result, team.playingCard?.currentBehaviorUI);
    }

    private void SetDiceResultData(BattleDiceBehavior diceBehavior, BattleCardTotalResult result, BattleDiceBehavior diceBehaviorforUI = null)
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

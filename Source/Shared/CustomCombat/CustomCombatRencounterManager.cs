using LOR_DiceSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using Battle.BufEffect;
using Battle.CreatureEffect;
using Battle.DiceAttackEffect;
using LOR_XML;
using Sound;
using Soule.CombatEntry;
using UI;
using UnityEngine;
using ActionAfterBehaviour = RencounterManager.ActionAfterBehaviour;
using MovingAction = RencounterManager.MovingAction;
using RencounterState = RencounterManager.RencounterState;

using 裁缝之王.Presentation.Actions;
using 裁缝之王.Presentation.Effects;

namespace 裁缝之王.Presentation.CustomCombat;

// 原版双人演出的本地分支；默认动作模板作为无状态工具复用，计时与协程由本类推进。
public sealed class CustomCombatRencounterManager : MonoBehaviour
{
    public float coefKnockback;

    public AudioClip Draw_AtkAtk;

    public AudioClip Draw_DefDef;

    public AudioClip Dice_Move;

    public AudioClip Dice_Rolled;

    private bool forbidAutoPlay
    {
        get => SingletonBehavior<RencounterManager>.Instance.forbidAutoPlay;
        set => SingletonBehavior<RencounterManager>.Instance.forbidAutoPlay = value;
    }

    private float _tEnableUI;

    private float _tMoveDice;

    private float _tPrintEnemyVanillaDice;

    private float _tPrintEnemyResultDice;

    private float _tWaitLibrarian;

    private float _tPrintLibrarianVanillaDice;

    private float _tPrintLibrarianResultDice;

    private float _tPrintActivatedAbility;

    private float _tChangeMotion;

    private float _tMoveUnits;

    private bool _testNodelay;

    private float _afterMovingSmoothTime;

    private float _curMovingSmoothTime;

    private float _delayRatio;

    private float _curDelayRatio;

    private RencounterState _curRencounterState;

    private bool _bPause;

    internal BattleUnitView _librarian;

    internal BattleUnitView _enemy;

    private BattleUnitView.SkinInfo _originalLibrarianSkin;

    private BattleUnitView.SkinInfo _originalEnemySkin;

    private Queue<BattleCardBehaviourResult> _librarianBehaviourResultQueue;

    private Queue<BattleCardBehaviourResult> _enemyBehaviourResultQueue;

    internal BattleCardBehaviourResult _currentLibrarianBehaviourResult;

    internal BattleCardBehaviourResult _currentEnemyBehaviourResult;

    private Dictionary<RencounterState, RencounterStateTimer> _stateTimerDic;

    private List<LibrarianPotentialTypo> _createdTypoList = new();

    internal bool _actionAfterBehaviourIsRunning;

    private bool _bMoveSlow;

    private int _countForNodelay;

    private bool _bNoDelay;

    private float _elapsedEndRencounter;

    private float _elapsedStartFocus;

    public bool isPlayPrintDamage;

    public bool isActionPrev;

    public bool isMotionStartPrev;

    public bool isPlayEffectSound;

    private float _elapsedConfrontation;

    private float _elapsedCompetition;

    public bool IsRunning => action?.Phase == CombatActionPhase.Playing;

    internal bool Paused => _bPause || SingletonBehavior<BattleCamManager>.Instance.stopFocusByRencounter;

    internal CustomCombatPlayback SecondaryPlayback { get; private set; }

    internal void EnterSecondaryFocus(BattleCardBehaviourResult record) => SecondaryPlayback?.EnterFocus(record);

    internal void LeaveSecondaryFocus() => SecondaryPlayback?.EndFocus();

    internal bool CanContinueSecondaryFocus(BattleCardBehaviourResult record)
    {
        var actor = record?.playingCard?.owner?.view;
        var target = actor == _enemy ? _librarian : _enemy;
        if (!KingContinuation.TryGetContinuousNext(actor, target, record, out var next) || !KingContinuation.IsExpected(next)) return false;
        var current = CustomCombatResolution.Find(record);
        var following = CustomCombatResolution.Find(next);
        return current != null && following != null && ReferenceEquals(current.Page, following.Page);
    }

    internal float MotionTime { get; private set; }

    internal float MotionDelta => !Paused && motionFrame == Time.frameCount ? motionDelta : 0f;

    private int motionFrame = -1;

    private float motionDelta;

    private CombatActionSession action;

    public bool playAuto
    {
        get => SingletonBehavior<RencounterManager>.Instance.playAuto;
        set => SingletonBehavior<RencounterManager>.Instance.playAuto = value;
    }

    public float curMovingSmoothTime => _curMovingSmoothTime;

    public RencounterState CurRencounterState => _curRencounterState;

    public void SetNodelay(bool b)
    {
        _bNoDelay = b;
    }

    internal void StartRencounter(BattleUnitView enemy, BattleUnitView librarian, CombatActionSession session)
    {
        action = session;
        MotionTime = 0f;
        motionFrame = -1;
        motionDelta = 0f;
        SecondaryPlayback = CustomCombatPlayback.Get(this);
        SecondaryPlayback.Capture(enemy, librarian);
        _curDelayRatio = _delayRatio;
        _curMovingSmoothTime = _afterMovingSmoothTime;
        foreach (KeyValuePair<RencounterState, RencounterStateTimer> item in _stateTimerDic)
        {
            item.Value.ResetWaitTime();
        }
        _countForNodelay = 0;
        _bNoDelay = false;
        forbidAutoPlay = false;
        List<BattleUnitModel> list = new();
        list.Add(enemy.model);
        list.Add(librarian.model);
        foreach (BattleUnitModel alive in BattleObjectManager.instance.GetAliveList())
        {
            if (alive != enemy.model && alive != librarian.model)
            {
                alive.view.unitBottomStatUI.EnableCanvas(enable: false);
                alive.view.unitBottomStatUI.SetIdentityLightActive(on: false);
            }
            else alive.view.unitBottomStatUI.SetIdentityLightActive(on: true);
        }
        SingletonBehavior<BattleCamManager>.Instance.FollowOneUnit(null, b: false);
        SingletonBehavior<BattleCamManager>.Instance.FollowUnits(isAction: true, list);
        SingletonBehavior<BattleCamManager>.Instance.BlurCharacterCam(active: true);
        SingletonBehavior<BattleCamManager>.Instance.ChangeCharacterCamRGB(active: true);
        SingletonBehavior<BattleManagerUI>.Instance.ui_unitInformationPlayer.CloseUnitInformation(forcehide: true);
        SingletonBehavior<BattleManagerUI>.Instance.ui_unitInformation.CloseUnitInformation(forcehide: true);
        session.PlaybackStarted();
        _curRencounterState = RencounterState.EnableUI;
        _elapsedStartFocus = 0f;
        _librarian = librarian;
        _enemy = enemy;
        _originalLibrarianSkin = null;
        _originalEnemySkin = null;
        if (librarian.model.battleCardResultLog.usedCard != null)
        {
            string skin = librarian.model.battleCardResultLog.usedCard.GetSkin();
            CardSkinType skinType = librarian.model.battleCardResultLog.usedCard.GetSkinType();
            if (!string.IsNullOrEmpty(skin))
            {
                _originalLibrarianSkin = librarian.GetCurrentSkinInfo();
                switch (skinType)
                {
                case CardSkinType.EGO:
                    librarian.ChangeEgoSkin(skin, bookNameChange: false);
                    break;
                case CardSkinType.Normal:
                    librarian.ChangeSkin(skin);
                    break;
                }
                int skinHeight = librarian.model.battleCardResultLog.usedCard.GetSkinHeight();
                if (skinHeight > 0) librarian.ChangeHeight(skinHeight);
                librarian.StartEgoSkinChangeEffect("UI_WORLD");
            }
        }
        if (enemy.model.battleCardResultLog.usedCard != null)
        {
            string skin2 = enemy.model.battleCardResultLog.usedCard.GetSkin();
            if (!string.IsNullOrEmpty(skin2))
            {
                _originalEnemySkin = enemy.GetCurrentSkinInfo();
                enemy.ChangeEgoSkin(skin2, bookNameChange: false);
                int skinHeight2 = enemy.model.battleCardResultLog.usedCard.GetSkinHeight();
                if (skinHeight2 > 0) enemy.ChangeHeight(skinHeight2);
                enemy.StartEgoSkinChangeEffect("UI_WORLD");
            }
        }
        _librarianBehaviourResultQueue = librarian.model.battleCardResultLog.BehaviourResultQueue;
        _enemyBehaviourResultQueue = enemy.model.battleCardResultLog.BehaviourResultQueue;
        _librarian.charAppearance.ChangeLayer("UI_WORLD");
        _enemy.charAppearance.ChangeLayer("UI_WORLD");
        _librarian.unitBottomStatUI.ChangeLayer("UI_WORLD", "UI_WORLD");
        _librarian.unitBottomStatUI.ChangeChildCanvasOrder("Character", 0);
        _enemy.unitBottomStatUI.ChangeLayer("UI_WORLD", "UI_WORLD");
        _enemy.unitBottomStatUI.ChangeChildCanvasOrder("Character", 0);
        foreach (KeyValuePair<RencounterState, RencounterStateTimer> item2 in _stateTimerDic)
        {
            item2.Value.Reset();
        }
        _librarian.model.OnLayerChanged("UI_WORLD");
        _enemy.model.OnLayerChanged("UI_WORLD");
        _librarian.keepUI.Init(b: false);
        _enemy.keepUI.Init(b: false);
        _bPause = false;
    }

    public void EndRencounter()
    {
        forbidAutoPlay = false;
        if (_elapsedEndRencounter <= Mathf.Epsilon)
        {
            _librarian.model.battleCardResultLog.endCardActionEvent?.Invoke();
            _enemy.model.battleCardResultLog.endCardActionEvent?.Invoke();
            if (_librarian.model.IsDeadReal())
            {
                _librarian.StartDeadEffect();
                _librarian.diceActionUI.EndAction();
            }
            if (_enemy.model.IsDeadReal())
            {
                _enemy.StartDeadEffect();
                _enemy.diceActionUI.EndAction();
            }
        }
        _elapsedEndRencounter += Time.deltaTime;
        SingletonBehavior<BattleCamManager>.Instance.BlurCharacterCam(active: false, 10f);
        if (_elapsedEndRencounter < 0.2f || _librarian.IsRunningDeadEffect || _enemy.IsRunningDeadEffect || SecondaryPlayback.HasRunningDeath) return;
        foreach (BattleUnitModel alive in BattleObjectManager.instance.GetAliveList())
        {
            if (!alive.IsExtinction())
            {
                alive.view.unitBottomStatUI.EnableCanvas(enable: true);
                alive.view.unitBottomStatUI.SetIdentityLightActive(on: false);
            }
        }
        foreach (BattleUnitModel item in BattleObjectManager.instance.GetList())
        {
            try
            {
                if (item.IsDeadReal() && item.view.IsReadyDeadScene)
                {
                    item.view.StartDeadEffect();
                    SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfile(item, item.faction, item.hp, item.breakDetail.breakGauge);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
        SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfileAll();
        _librarian.KnockDown(v: false);
        _enemy.KnockDown(v: false);
        if (_originalLibrarianSkin != null)
        {
            _librarian.ChangeSkinBySkinInfo(_originalLibrarianSkin);
            _librarian.StartEgoSkinChangeEffect("Character");
        }
        if (_originalEnemySkin != null)
        {
            _enemy.ChangeSkinBySkinInfo(_originalEnemySkin);
            _enemy.StartEgoSkinChangeEffect("Character");
        }
        _librarian.charAppearance.ChangeLayer("Character");
        _enemy.charAppearance.ChangeLayer("Character");
        _librarian.unitBottomStatUI.ChangeLayer("CharacterUI", "Character");
        _librarian.unitBottomStatUI.ChangeChildCanvasOrder("CharacterUI", 0);
        _enemy.unitBottomStatUI.ChangeLayer("CharacterUI", "Character");
        _enemy.unitBottomStatUI.ChangeChildCanvasOrder("CharacterUI", 0);
        _librarian.charAppearance.ChangeMotion(ActionDetail.Default);
        _enemy.charAppearance.ChangeMotion(ActionDetail.Default);

        action.PlaybackStopped();
        _curRencounterState = RencounterState.None;
        _librarian.diceActionUI.EndAction();
        _enemy.diceActionUI.EndAction();
        foreach (KeyValuePair<RencounterState, RencounterStateTimer> item2 in _stateTimerDic)
        {
            item2.Value.Reset();
        }
        BattleObjectManager.instance.GetAliveList().FindAll((BattleUnitModel x) => x.currentDiceAction != null);
        SingletonBehavior<BattleCamManager>.Instance.ChangeCharacterCamRGB(active: false);
        SingletonBehavior<BattleCamManager>.Instance.FollowOneUnit(null, b: false);
        _elapsedEndRencounter = 0f;
        _bMoveSlow = false;
        foreach (LibrarianPotentialTypo createdTypo in _createdTypoList)
        {
            if (createdTypo.gameObject != null) UnityEngine.Object.Destroy(createdTypo.gameObject);
        }
        _createdTypoList.Clear();
        _librarian.model.OnLayerChanged("Character");
        _enemy.model.OnLayerChanged("Character");
        _librarian.keepUI.Init(b: true);
        _enemy.keepUI.Init(b: true);
        Complete();
    }

    public void EndRencounterForcely()
    {
        if (action == null || action.Phase == CombatActionPhase.Inactive)
        {
            return;
        }

        try
        {
            if (_originalLibrarianSkin != null && _librarian) _librarian.ChangeSkinBySkinInfo(_originalLibrarianSkin);
            if (_originalEnemySkin != null && _enemy) _enemy.ChangeSkinBySkinInfo(_originalEnemySkin);
            foreach (var unit in BattleObjectManager.instance.GetList())
            {
                if (!unit.view) continue;
                if (unit == _librarian?.model || unit == _enemy?.model) unit.moveDetail.Stop();
                unit.view.unitBottomStatUI.EnableCanvas(!unit.IsDead());
                unit.view.unitBottomStatUI.SetIdentityLightActive(false);
            }
            forbidAutoPlay = false;
            if (_librarian != null)
            {
                _librarian.charAppearance.ChangeMotion(ActionDetail.Default);
                _librarian.charAppearance.ChangeLayer("Character");
                _librarian.unitBottomStatUI.ChangeLayer("CharacterUI", "Character");
                _librarian.unitBottomStatUI.ChangeChildCanvasOrder("CharacterUI", 0);
            }
            if (_enemy != null)
            {
                _enemy.charAppearance.ChangeMotion(ActionDetail.Default);
                _enemy.charAppearance.ChangeLayer("Character");
                _enemy.unitBottomStatUI.ChangeLayer("CharacterUI", "Character");
                _enemy.unitBottomStatUI.ChangeChildCanvasOrder("CharacterUI", 0);
            }
            action.PlaybackStopped();
            _curRencounterState = RencounterState.None;
            if (_librarian != null) _librarian.diceActionUI.EndAction();
            if (_enemy != null) _enemy.diceActionUI.EndAction();
            if (_stateTimerDic != null)
            {
                foreach (KeyValuePair<RencounterState, RencounterStateTimer> item in _stateTimerDic)
                {
                    item.Value.Reset();
                }
            }
            LeaveSecondaryFocus();
            SingletonBehavior<BattleCamManager>.Instance.FollowOneUnit(null, b: false);
            SingletonBehavior<BattleCamManager>.Instance.FollowUnits(isAction: false, BattleObjectManager.instance.GetAliveList());
            SingletonBehavior<BattleCamManager>.Instance.VignetteCharacterCam(active: false);
            SingletonBehavior<BattleCamManager>.Instance.TiltCam(0, exitForcely: true);
            SingletonBehavior<BattleCamManager>.Instance.ChangeCharacterCamRGB(active: false);
            SingletonBehavior<BattleCamManager>.Instance.BlurCharacterCam(active: false, 10f);
            _elapsedEndRencounter = 0f;
            _bMoveSlow = false;
            foreach (LibrarianPotentialTypo createdTypo in _createdTypoList)
            {
                if (createdTypo.gameObject != null) UnityEngine.Object.Destroy(createdTypo.gameObject);
            }
            _createdTypoList.Clear();
        }
        finally
        {
            Complete(restorePositions: true);
        }
    }

    private void Awake()
    {
        var source = SingletonBehavior<RencounterManager>.Instance;
        coefKnockback = source.coefKnockback;
        Draw_AtkAtk = source.Draw_AtkAtk;
        Draw_DefDef = source.Draw_DefDef;
        Dice_Move = source.Dice_Move;
        Dice_Rolled = source.Dice_Rolled;
        _tEnableUI = source._tEnableUI;
        _tMoveDice = source._tMoveDice;
        _tPrintEnemyVanillaDice = source._tPrintEnemyVanillaDice;
        _tPrintEnemyResultDice = source._tPrintEnemyResultDice;
        _tWaitLibrarian = source._tWaitLibrarian;
        _tPrintLibrarianVanillaDice = source._tPrintLibrarianVanillaDice;
        _tPrintLibrarianResultDice = source._tPrintLibrarianResultDice;
        _tPrintActivatedAbility = source._tPrintActivatedAbility;
        _tChangeMotion = source._tChangeMotion;
        _tMoveUnits = source._tMoveUnits;
        _testNodelay = source._testNodelay;
        _afterMovingSmoothTime = source._afterMovingSmoothTime;
        _delayRatio = source._delayRatio;
        isPlayPrintDamage = source.isPlayPrintDamage;
        isActionPrev = source.isActionPrev;
        isMotionStartPrev = source.isMotionStartPrev;
        isPlayEffectSound = source.isPlayEffectSound;

        _stateTimerDic = new Dictionary<RencounterState, RencounterStateTimer>
        {
            [RencounterState.MoveBeforeStart] = new RencounterStateTimer(0.5f),
            [RencounterState.EnableUI] = new RencounterStateTimer(_tEnableUI),
            [RencounterState.MoveDice] = new RencounterStateTimer(_tMoveDice),
            [RencounterState.PrintEnemyVanillaDice] = new RencounterStateTimer(_tPrintEnemyVanillaDice),
            [RencounterState.PrintEnemyResultDice] = new RencounterStateTimer(_tPrintEnemyResultDice),
            [RencounterState.WaitLibrarian] = new RencounterStateTimer(_tWaitLibrarian),
            [RencounterState.PrintLibrarianVanillaDice] = new RencounterStateTimer(_tPrintLibrarianVanillaDice),
            [RencounterState.PrintLibrarianResultDice] = new RencounterStateTimer(_tPrintLibrarianResultDice),
            [RencounterState.ChangeMotion] = new RencounterStateTimer(_tChangeMotion),
            [RencounterState.PrintActivatedAbility] = new RencounterStateTimer(_tPrintActivatedAbility),
            [RencounterState.MoveUnits] = new RencounterStateTimer(_tMoveUnits)
        };
    }

    public void Pause(bool b)
    {
        _bPause = b;
    }

    private void Update()
    {
        try
        {
            Tick();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EndRencounterForcely();
        }
    }

    private void Tick()
    {
        if (!SingletonBehavior<BattleCamManager>.Instance.stopFocusByRencounter && !_bPause && IsRunning)
        {
            RencounterStateTimer rencounterStateTimer = _stateTimerDic[_curRencounterState];
            switch (_curRencounterState)
            {
            case RencounterState.EnableUI:
                EnableUI(rencounterStateTimer);
                break;
            case RencounterState.MoveDice:
                MoveDice(rencounterStateTimer);
                break;
            case RencounterState.PrintEnemyVanillaDice:
                PrintEnemyVanillaDice(rencounterStateTimer);
                break;
            case RencounterState.PrintEnemyResultDice:
                PrintEnemyResultDice(rencounterStateTimer);
                break;
            case RencounterState.WaitLibrarian:
                WaitLibrarian(rencounterStateTimer);
                break;
            case RencounterState.PrintLibrarianVanillaDice:
                PrintLibrarianVanillaDice(rencounterStateTimer);
                break;
            case RencounterState.PrintLibrarianResultDice:
                PrintLibrarianResultDice(rencounterStateTimer);
                break;
            case RencounterState.PrintActivatedAbility:
                PrintActivatedAbility(rencounterStateTimer);
                break;
            case RencounterState.MoveUnits:
                MoveUnits(rencounterStateTimer);
                break;
            }
            rencounterStateTimer.Tick(Time.deltaTime);
            if (_bMoveSlow && Vector3.Distance(_librarian.WorldPosition, _enemy.WorldPosition) > SingletonBehavior<HexagonalMapManager>.Instance.tileSize * 2.5f)
            {
                _librarian.WorldPosition += Time.deltaTime * (_enemy.WorldPosition - _librarian.WorldPosition).normalized * 0.1f;
                _enemy.WorldPosition += Time.deltaTime * (_librarian.WorldPosition - _enemy.WorldPosition).normalized * 0.1f;
            }
        }
    }

    private void EnableUI(RencounterStateTimer timer)
    {
        _elapsedStartFocus += Time.deltaTime;
        if (SingletonBehavior<BattleCamManager>.Instance.IsCamFarFromFocusUnit() && _elapsedStartFocus < 5f) return;
        if (timer.IsStart)
        {
            if (timer.IsEnd && _librarian.diceActionUI.IsInitAnimEnd && _enemy.diceActionUI.IsInitAnimEnd)
            {
                _curRencounterState = RencounterState.MoveDice;
                timer.Reset();
            }
            return;
        }
        if (_librarian.model.battleCardResultLog.usedCard != null)
        {
            _librarian.diceActionUI.Init(_enemy.model);
            _librarian.model.battleCardResultLog.useCardEvent?.Invoke();
            _librarian.model.UpdateDirection(_enemy.WorldPosition);
        }
        else
        {
            Queue<BattleCardBehaviourResult> behaviourResultQueue = _librarian.model.battleCardResultLog.BehaviourResultQueue;
            if (behaviourResultQueue != null && behaviourResultQueue.Count > 0 && _librarian.model.battleCardResultLog.BehaviourResultQueue?.Peek().cardModel != null)
            {
                _librarian.diceActionUI.Init(_enemy.model);
                _librarian.model.battleCardResultLog.useCardEvent?.Invoke();
                _librarian.model.UpdateDirection(_enemy.WorldPosition);
            }
            else _librarian.diceActionUI.OnInitAnimEnd();
        }
        if (_enemy.model.battleCardResultLog.usedCard != null)
        {
            _enemy.diceActionUI.Init(_librarian.model);
            _enemy.model.battleCardResultLog.useCardEvent?.Invoke();
            _enemy.model.UpdateDirection(_librarian.WorldPosition);
        }
        else
        {
            Queue<BattleCardBehaviourResult> behaviourResultQueue2 = _enemy.model.battleCardResultLog.BehaviourResultQueue;
            if (behaviourResultQueue2 != null && behaviourResultQueue2.Count > 0 && _enemy.model.battleCardResultLog.BehaviourResultQueue?.Peek().cardModel != null)
            {
                _enemy.diceActionUI.Init(_librarian.model);
                _enemy.model.battleCardResultLog.useCardEvent?.Invoke();
                _enemy.model.UpdateDirection(_librarian.WorldPosition);
            }
            else _enemy.diceActionUI.OnInitAnimEnd();
        }
        timer.Start();

        _librarian.unitBottomStatUI.EnableCanvas(true);
        _enemy.unitBottomStatUI.EnableCanvas(true);
    }

    private void MoveDice(RencounterStateTimer timer)
    {
        if (_testNodelay)
        {
            if (_countForNodelay >= 2) _bNoDelay = true;
            else _bNoDelay = false;
        }
        _librarian.diceActionUI.HideOffPrevTypo();
        _enemy.diceActionUI.HideOffPrevTypo();
        if (_librarianBehaviourResultQueue.Count == 0 || _enemyBehaviourResultQueue.Count == 0)
        {
            EndRencounter();
            return;
        }
        if (timer.IsStart)
        {
            if (!timer.IsEnd && !_bNoDelay) return;
            if (_librarian.model.moveDetail.isArrived && _enemy.model.moveDetail.isArrived)
            {
                _curRencounterState = RencounterState.PrintEnemyVanillaDice;
                timer.Reset();
                _bMoveSlow = true;
            }
            else
            {
                if (_currentLibrarianBehaviourResult?.cardModel != null)
                {
                    BattleCardBehaviourResult currentLibrarianBehaviourResult = _currentLibrarianBehaviourResult;
                    if (currentLibrarianBehaviourResult != null && currentLibrarianBehaviourResult.behaviourRawData?.Type == BehaviourType.Atk)
                    {
                        BattleCardBehaviourResult currentLibrarianBehaviourResult2 = _currentLibrarianBehaviourResult;
                        if (currentLibrarianBehaviourResult2 != null && currentLibrarianBehaviourResult2.cardModel?.GetSpec().Ranged == CardRange.Far)
                        {
                            _enemy.model.moveDetail.Stop();
                            _curRencounterState = RencounterState.PrintEnemyVanillaDice;
                            timer.Reset();
                        }
                    }
                }
                if (_currentEnemyBehaviourResult?.cardModel != null)
                {
                    BattleCardBehaviourResult currentEnemyBehaviourResult = _currentEnemyBehaviourResult;
                    if (currentEnemyBehaviourResult != null && currentEnemyBehaviourResult.behaviourRawData?.Type == BehaviourType.Atk)
                    {
                        BattleCardBehaviourResult currentEnemyBehaviourResult2 = _currentEnemyBehaviourResult;
                        if (currentEnemyBehaviourResult2 != null && currentEnemyBehaviourResult2.cardModel?.GetSpec().Ranged == CardRange.Far)
                        {
                            _librarian.model.moveDetail.Stop();
                            _curRencounterState = RencounterState.PrintEnemyVanillaDice;
                            timer.Reset();
                        }
                    }
                }
            }
            ChangeTimerDelayByRatio(timer, _curDelayRatio);
            return;
        }
        _currentLibrarianBehaviourResult = _librarianBehaviourResultQueue.Peek();
        _currentEnemyBehaviourResult = _enemyBehaviourResultQueue.Peek();
        _curMovingSmoothTime = _afterMovingSmoothTime * _curDelayRatio;
        if (_curMovingSmoothTime < 0.035f) _curMovingSmoothTime = 0.035f;
        _enemy.diceActionUI.currentDice = null;
        _librarian.diceActionUI.currentDice = null;
        if (_currentLibrarianBehaviourResult.diceBehaviourResult.skip || _currentEnemyBehaviourResult.diceBehaviourResult.skip)
        {
            EndBehaviourResult();
            return;
        }
        if (_librarian.model.battleCardResultLog.usedCard != null || _currentLibrarianBehaviourResult?.cardModel != null) _librarian.diceActionUI.SetFirstDiceNew();
        if (_enemy.model.battleCardResultLog.usedCard != null || _currentEnemyBehaviourResult?.cardModel != null) _enemy.diceActionUI.SetFirstDiceNew();
        CheckMovingStateBeforeRoll();
        PlaySound(Dice_Move);
        timer.Start();

    }

    private void CheckMovingStateBeforeRoll()
    {
        KingContinuation.RestoreDelay();
        SecondaryPlayback.BeforeRoll(_currentEnemyBehaviourResult, _currentLibrarianBehaviourResult);
        if (KingContinuation.Prepare(_enemy, _librarian, _currentEnemyBehaviourResult, _currentLibrarianBehaviourResult,
            playAuto && !forbidAutoPlay, _bNoDelay, SetNodelay)) return;
        bool flag = false;
        float num = 50f * (0.5f * _countForNodelay + 1f);
        if (num > 100f) num = 100f;
        bool flag2 = false;
        if (_currentEnemyBehaviourResult.hasBehaviour)
        {
            BattleDiceCardModel battleDiceCardModel = _enemy.model?.battleCardResultLog?.usedCard;
            if (battleDiceCardModel == null) battleDiceCardModel = _currentEnemyBehaviourResult?.cardModel;
            BehaviourActionBase behaviourActionBase = CreateBehaviourAction(_currentEnemyBehaviourResult?.behaviourRawData?.ActionScript);
            bool flag3 = false;
            if (_currentEnemyBehaviourResult?.behaviourRawData != null) flag3 = _currentEnemyBehaviourResult.behaviourRawData.Type == BehaviourType.Atk;
            bool flag4 = true;
            if (battleDiceCardModel != null && battleDiceCardModel.GetSpec().Ranged == CardRange.Far && flag3)
            {
                flag4 = false;
                flag2 = true;
            }
            else if (!behaviourActionBase.IsMovable(_currentEnemyBehaviourResult, _currentLibrarianBehaviourResult))
            {
                flag4 = false;
            }
            else if (_currentLibrarianBehaviourResult.hasBehaviour)
            {
                flag4 = CreateBehaviourAction(_currentLibrarianBehaviourResult?.behaviourRawData?.ActionScript).IsOpponentMovable(_currentLibrarianBehaviourResult, _currentEnemyBehaviourResult);
            }
            if (flag4)
            {
                _enemy.model.UpdateDirection(_librarian.WorldPosition);
                _enemy.model.moveDetail.Move(_librarian.model, num);
            }
            if (behaviourActionBase != null) flag = behaviourActionBase.IsOpponentMovable(_currentEnemyBehaviourResult, _currentLibrarianBehaviourResult);
        }
        else flag = true;
        bool flag5 = false;
        if (_currentLibrarianBehaviourResult.hasBehaviour)
        {
            BattleDiceCardModel battleDiceCardModel2 = _librarian.model?.battleCardResultLog?.usedCard;
            if (battleDiceCardModel2 == null) battleDiceCardModel2 = _currentLibrarianBehaviourResult?.cardModel;
            BehaviourActionBase behaviourActionBase2 = CreateBehaviourAction(_currentLibrarianBehaviourResult?.behaviourRawData?.ActionScript);
            bool flag6 = false;
            if (_currentLibrarianBehaviourResult?.behaviourRawData != null) flag6 = _currentLibrarianBehaviourResult.behaviourRawData.Type == BehaviourType.Atk;
            bool flag7 = true;
            if (battleDiceCardModel2 != null && battleDiceCardModel2.GetSpec().Ranged == CardRange.Far && flag6)
            {
                flag7 = false;
                flag5 = true;
            }
            else if (!behaviourActionBase2.IsMovable(_currentLibrarianBehaviourResult, _currentEnemyBehaviourResult))
            {
                flag7 = false;
            }
            else if (!flag)
            {
                flag7 = false;
            }
            if (flag7)
            {
                _librarian.model.UpdateDirection(_enemy.WorldPosition);
                _librarian.model.moveDetail.Move(_enemy.model, num);
            }
        }
        if (flag2) _librarian.model.moveDetail.Stop();
        if (flag5) _enemy.model.moveDetail.Stop();
    }

    private void PrintEnemyVanillaDice(RencounterStateTimer timer)
    {
        if ((_enemy.model.battleCardResultLog.usedCard == null && _currentEnemyBehaviourResult?.cardModel == null) || !_currentEnemyBehaviourResult.hasBehaviour)
        {
            if (_bNoDelay)
            {
                _curRencounterState = RencounterState.WaitLibrarian;
                WaitLibrarian(_stateTimerDic[_curRencounterState]);
            }
            else _curRencounterState = RencounterState.WaitLibrarian;
        }
        else if (_bNoDelay)
        {
            _curRencounterState = RencounterState.PrintEnemyResultDice;
            PrintEnemyResultDice(_stateTimerDic[_curRencounterState]);
        }
        else if (timer.IsStart)
        {
            if (timer.IsEnd)
            {
                List<int> vanillaDiceValueList = _currentEnemyBehaviourResult.vanillaDiceValueList;
                if (vanillaDiceValueList != null && vanillaDiceValueList.Count > 1)
                {
                    _currentEnemyBehaviourResult.vanillaDiceValueList.RemoveAt(0);
                    timer.Reset();
                    timer.ResetWaitTime();
                    ChangeTimerDelayByRatio(timer, _curDelayRatio);
                }
                else
                {
                    _curRencounterState = ((_currentEnemyBehaviourResult.vanillaDiceValue != _currentEnemyBehaviourResult.resultDiceValue) ? RencounterState.PrintEnemyResultDice : RencounterState.WaitLibrarian);
                    timer.Reset();
                    timer.ResetWaitTime();
                    ChangeTimerDelayByRatio(timer, _curDelayRatio);
                }
            }
        }
        else
        {
            List<int> vanillaDiceValueList2 = _currentEnemyBehaviourResult.vanillaDiceValueList;
            if (vanillaDiceValueList2 != null && vanillaDiceValueList2.Count > 1)
            {
                int num = _currentEnemyBehaviourResult.vanillaDiceValueList[0];
                _enemy.diceActionUI.SetDiceNew(_currentEnemyBehaviourResult.resultDiceMax, num, num == _currentEnemyBehaviourResult.resultDiceValue);
                timer.ChangeWaitTime(0.3f);
            }
            else _enemy.diceActionUI.SetDiceNew(_currentEnemyBehaviourResult.resultDiceMax, _currentEnemyBehaviourResult.vanillaDiceValue, _currentEnemyBehaviourResult.vanillaDiceValue == _currentEnemyBehaviourResult.resultDiceValue);
            timer.Start();

        }
    }

    private void PrintEnemyResultDice(RencounterStateTimer timer)
    {
        if (_bNoDelay)
        {
            if (_enemy.model.battleCardResultLog?.usedCard == null || !_currentEnemyBehaviourResult.hasBehaviour)
            {
                _curRencounterState = RencounterState.WaitLibrarian;
                WaitLibrarian(_stateTimerDic[_curRencounterState]);
                return;
            }
            int resultDiceValue = _currentEnemyBehaviourResult.resultDiceValue;
            int vanillaDiceValue = _currentEnemyBehaviourResult.vanillaDiceValue;
            BattleDiceValueColor colorstate = BattleDiceValueColor.Normal;
            if (resultDiceValue > vanillaDiceValue) colorstate = BattleDiceValueColor.Increase;
            else if (resultDiceValue < vanillaDiceValue)
            {
                colorstate = BattleDiceValueColor.Decrease;
            }
            _enemy.diceActionUI.SetDiceNew(_currentEnemyBehaviourResult.resultDiceMax, _currentEnemyBehaviourResult.resultDiceValue, isResultDice: true, colorstate);
            _enemy.diceActionUI.ActivateEmotionCards(_currentEnemyBehaviourResult.GetActivatedEmotionCards());
            _curRencounterState = RencounterState.WaitLibrarian;
            WaitLibrarian(_stateTimerDic[_curRencounterState]);
        }
        else if ((_enemy.model.battleCardResultLog.usedCard == null && _currentEnemyBehaviourResult?.cardModel == null) || !_currentEnemyBehaviourResult.hasBehaviour)
        {
            _curRencounterState = RencounterState.WaitLibrarian;
        }
        else if (timer.IsStart)
        {
            if (timer.IsEnd)
            {
                _curRencounterState = RencounterState.WaitLibrarian;
                timer.Reset();
                ChangeTimerDelayByRatio(timer, _curDelayRatio);
            }
        }
        else
        {
            int resultDiceValue2 = _currentEnemyBehaviourResult.resultDiceValue;
            int vanillaDiceValue2 = _currentEnemyBehaviourResult.vanillaDiceValue;
            BattleDiceValueColor colorstate2 = BattleDiceValueColor.Normal;
            if (resultDiceValue2 > vanillaDiceValue2) colorstate2 = BattleDiceValueColor.Increase;
            else if (resultDiceValue2 < vanillaDiceValue2)
            {
                colorstate2 = BattleDiceValueColor.Decrease;
            }
            _enemy.diceActionUI.SetDiceNew(_currentEnemyBehaviourResult.resultDiceMax, _currentEnemyBehaviourResult.resultDiceValue, isResultDice: true, colorstate2);
            _enemy.diceActionUI.ActivateEmotionCards(_currentEnemyBehaviourResult.GetActivatedEmotionCards());
            timer.Start();

        }
    }

    private void WaitLibrarian(RencounterStateTimer timer)
    {
        if ((_librarian.model.battleCardResultLog.usedCard != null || _currentLibrarianBehaviourResult?.cardModel != null) && _currentLibrarianBehaviourResult != null && _currentLibrarianBehaviourResult.hasBehaviour)
        {
            if (_bNoDelay)
            {
                if (!playAuto)
                {
                    if (timer.IsStart)
                    {
                        if (timer.IsEnd) timer.Start();
                    }
                    else
                    {
                        timer.Start();
                        _librarian.diceActionUI.RollRoulette(_currentLibrarianBehaviourResult.GetActivatedEmotionCards());

                        StartCoroutine(PotentialEffect(_currentLibrarianBehaviourResult.GetAllAbilityStrings(), _librarian));
                        StartCoroutine(PotentialEffect(_currentEnemyBehaviourResult.GetAllAbilityStrings(), _enemy));

                    }
                    if (!UIManualPanel.instance.IsOpened() && BattleUIInputController.Instance.CheckInputRencounter())
                    {
                        PlaySound(Dice_Rolled);
                        _librarian.diceActionUI.StopRoulette();

                        timer.Reset();
                        OnRollDiceInRecounter();
                        _curRencounterState = RencounterState.PrintLibrarianResultDice;
                        PrintLibrarianResultDice(_stateTimerDic[_curRencounterState]);
                    }
                }
                else
                {
                    _librarian.diceActionUI.RollRoulette(_currentLibrarianBehaviourResult.GetActivatedEmotionCards());

                    StartCoroutine(PotentialEffect(_currentLibrarianBehaviourResult.GetAllAbilityStrings(), _librarian));
                    StartCoroutine(PotentialEffect(_currentEnemyBehaviourResult.GetAllAbilityStrings(), _enemy));
                    _librarian.diceActionUI.StopRoulette();

                    OnRollDiceInRecounter();
                    _curRencounterState = RencounterState.PrintLibrarianResultDice;
                    PrintLibrarianResultDice(_stateTimerDic[_curRencounterState]);
                }
            }
            else if (!playAuto || forbidAutoPlay)
            {
                if (timer.IsStart)
                {
                    if (timer.IsEnd) timer.Start();
                }
                else
                {
                    timer.Start();
                    _librarian.diceActionUI.RollRoulette(_currentLibrarianBehaviourResult.GetActivatedEmotionCards());

                    StartCoroutine(PotentialEffect(_currentLibrarianBehaviourResult.GetAllAbilityStrings(), _librarian));
                    StartCoroutine(PotentialEffect(_currentEnemyBehaviourResult.GetAllAbilityStrings(), _enemy));

                }
                if (!UIManualPanel.instance.IsOpened() && BattleUIInputController.Instance.CheckInputRencounter())
                {
                    PlaySound(Dice_Rolled);
                    _curRencounterState = RencounterState.PrintLibrarianVanillaDice;
                    _librarian.diceActionUI.StopRoulette();

                    timer.Reset();
                    OnRollDiceInRecounter();
                    ChangeTimerDelayByRatio(timer, _curDelayRatio);
                }
            }
            else if (timer.IsStart)
            {
                if (timer.IsEnd)
                {
                    if (_librarian.diceActionUI.rootCanvas.enabled) PlaySound(Dice_Rolled);
                    _curRencounterState = RencounterState.PrintLibrarianVanillaDice;
                    _librarian.diceActionUI.StopRoulette();

                    timer.Reset();
                    OnRollDiceInRecounter();
                    ChangeTimerDelayByRatio(timer, _curDelayRatio);
                }
            }
            else
            {
                timer.Start();
                _librarian.diceActionUI.RollRoulette(_currentLibrarianBehaviourResult.GetActivatedEmotionCards());

                StartCoroutine(PotentialEffect(_currentLibrarianBehaviourResult.GetAllAbilityStrings(), _librarian));
                StartCoroutine(PotentialEffect(_currentEnemyBehaviourResult.GetAllAbilityStrings(), _enemy));

            }
        }
        else
        {
            _curRencounterState = RencounterState.PrintActivatedAbility;
            timer.Reset();
        }
    }

    private void OnRollDiceInRecounter()
    {
        _librarian.model.OnRollDiceInRecounter();
    }

    private IEnumerator PotentialEffect(List<string> strList, BattleUnitView view)
    {
        yield return null;
    }

    private void PrintLibrarianVanillaDice(RencounterStateTimer timer)
    {
        if (_bNoDelay)
        {
            if ((_librarian.model.battleCardResultLog.usedCard == null && _currentLibrarianBehaviourResult?.cardModel == null) || !_currentLibrarianBehaviourResult.hasBehaviour)
            {
                _curRencounterState = RencounterState.PrintActivatedAbility;
                PrintActivatedAbility(_stateTimerDic[_curRencounterState]);
            }
            else
            {
                _curRencounterState = RencounterState.PrintLibrarianResultDice;
                PrintLibrarianResultDice(_stateTimerDic[_curRencounterState]);
            }
        }
        else if ((_librarian.model.battleCardResultLog.usedCard == null && _currentLibrarianBehaviourResult?.cardModel == null) || !_currentLibrarianBehaviourResult.hasBehaviour)
        {
            _curRencounterState = RencounterState.PrintActivatedAbility;
        }
        else if (timer.IsStart)
        {
            if (timer.IsEnd)
            {
                List<int> vanillaDiceValueList = _currentLibrarianBehaviourResult.vanillaDiceValueList;
                if (vanillaDiceValueList != null && vanillaDiceValueList.Count > 1)
                {
                    _currentLibrarianBehaviourResult.vanillaDiceValueList.RemoveAt(0);
                    timer.Reset();
                    timer.ResetWaitTime();
                    ChangeTimerDelayByRatio(timer, _curDelayRatio);
                }
                else
                {
                    _curRencounterState = ((_currentLibrarianBehaviourResult.resultDiceValue != _currentLibrarianBehaviourResult.vanillaDiceValue) ? RencounterState.PrintLibrarianResultDice : RencounterState.PrintActivatedAbility);
                    timer.Reset();
                    timer.ResetWaitTime();
                    ChangeTimerDelayByRatio(timer, _curDelayRatio);
                }
            }
        }
        else
        {
            List<int> vanillaDiceValueList2 = _currentLibrarianBehaviourResult.vanillaDiceValueList;
            if (vanillaDiceValueList2 != null && vanillaDiceValueList2.Count > 1)
            {
                int num = _currentLibrarianBehaviourResult.vanillaDiceValueList[0];
                _librarian.diceActionUI.SetDiceNew(_currentLibrarianBehaviourResult.resultDiceMax, num, num == _currentLibrarianBehaviourResult.resultDiceValue);
                timer.ChangeWaitTime(0.3f);
            }
            else if (_currentLibrarianBehaviourResult.vanillaDiceValue >= 0)
            {
                _currentLibrarianBehaviourResult.rolldiceEvent?.Invoke();
                _librarian.diceActionUI.SetDiceNew(_currentLibrarianBehaviourResult.resultDiceMax, _currentLibrarianBehaviourResult.vanillaDiceValue, _currentLibrarianBehaviourResult.resultDiceValue == _currentLibrarianBehaviourResult.vanillaDiceValue);
            }
            timer.Start();

        }
    }

    private void PrintLibrarianResultDice(RencounterStateTimer timer)
    {
        if (_bNoDelay)
        {
            int resultDiceValue = _currentLibrarianBehaviourResult.resultDiceValue;
            int vanillaDiceValue = _currentLibrarianBehaviourResult.vanillaDiceValue;
            BattleDiceValueColor colorstate = BattleDiceValueColor.Normal;
            if (resultDiceValue > vanillaDiceValue) colorstate = BattleDiceValueColor.Increase;
            else if (resultDiceValue < vanillaDiceValue)
            {
                colorstate = BattleDiceValueColor.Decrease;
            }
            _librarian.diceActionUI.SetDiceNew(_currentLibrarianBehaviourResult.resultDiceMax, _currentLibrarianBehaviourResult.resultDiceValue, isResultDice: true, colorstate);
            _curRencounterState = RencounterState.PrintActivatedAbility;
            PrintActivatedAbility(_stateTimerDic[_curRencounterState]);
        }
        else if (timer.IsStart)
        {
            if (timer.IsEnd)
            {
                _curRencounterState = RencounterState.PrintActivatedAbility;
                timer.Reset();
                ChangeTimerDelayByRatio(timer, _curDelayRatio);
            }
        }
        else
        {
            int resultDiceValue2 = _currentLibrarianBehaviourResult.resultDiceValue;
            int vanillaDiceValue2 = _currentLibrarianBehaviourResult.vanillaDiceValue;
            BattleDiceValueColor colorstate2 = BattleDiceValueColor.Normal;
            if (resultDiceValue2 > vanillaDiceValue2) colorstate2 = BattleDiceValueColor.Increase;
            else if (resultDiceValue2 < vanillaDiceValue2)
            {
                colorstate2 = BattleDiceValueColor.Decrease;
            }
            _librarian.diceActionUI.SetDiceNew(_currentLibrarianBehaviourResult.resultDiceMax, _currentLibrarianBehaviourResult.resultDiceValue, isResultDice: true, colorstate2);
            timer.Start();

        }
    }

    private void PrintActivatedAbility(RencounterStateTimer timer)
    {
        if (_bNoDelay)
        {
            _bMoveSlow = false;
            if (_currentLibrarianBehaviourResult != null && _currentLibrarianBehaviourResult.hasBehaviour) _librarian.diceActionUI.SetTypo(_currentLibrarianBehaviourResult.GetAbilityDataAfterRoll());
            if (_currentEnemyBehaviourResult != null && _currentEnemyBehaviourResult.hasBehaviour) _enemy.diceActionUI.SetTypo(_currentEnemyBehaviourResult.GetAbilityDataAfterRoll());
            if (isMotionStartPrev)
            {
                switch (_currentEnemyBehaviourResult.diceBehaviourResult.result)
                {
                case Result.Win:
                    _librarian.diceActionUI.StartActionResultDestroyedDice();
                    break;
                case Result.Lose:
                    _enemy.diceActionUI.StartActionResultDestroyedDice();
                    break;
                }
            }
            ChangePreMotion();
            SetMovingStateByActionResult();
            _curRencounterState = RencounterState.MoveUnits;
            MoveUnits(_stateTimerDic[_curRencounterState]);
            return;
        }
        if (timer.IsStart)
        {
            if (timer.IsEnd)
            {
                SetMovingStateByActionResult();
                _curRencounterState = RencounterState.MoveUnits;
                timer.Reset();
                ChangeTimerDelayByRatio(timer, _curDelayRatio);
            }
            return;
        }
        _bMoveSlow = false;
        if (_currentLibrarianBehaviourResult != null && _currentLibrarianBehaviourResult.hasBehaviour) _librarian.diceActionUI.SetTypo(_currentLibrarianBehaviourResult.GetAbilityDataAfterRoll());
        if (_currentEnemyBehaviourResult != null && _currentEnemyBehaviourResult.hasBehaviour) _enemy.diceActionUI.SetTypo(_currentEnemyBehaviourResult.GetAbilityDataAfterRoll());
        if (isMotionStartPrev)
        {
            switch (_currentEnemyBehaviourResult.diceBehaviourResult.result)
            {
            case Result.Win:
                _librarian.diceActionUI.StartActionResultDestroyedDice();
                break;
            case Result.Lose:
                _enemy.diceActionUI.StartActionResultDestroyedDice();
                break;
            }
        }
        ChangePreMotion();
        timer.Start();
    }

    private void MoveUnits(RencounterStateTimer timer)
    {
        if (timer.IsStart)
        {
            if (timer.IsEnd && !_actionAfterBehaviourIsRunning)
            {
                if (_librarian.model.moveDetail.isArrived && _currentLibrarianBehaviourResult.isDead)
                {
                    _librarian.StartDeadEffect();
                    List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(Faction.Player);
                    aliveList.Remove(_librarian.model);
                    List<BattleUnitModel> list = new();
                    list.Add(_librarian.model);
                    DisplayDialogues(aliveList, DialogType.COLLEAGUE_DEATH, list);
                    DisplayDialogues(_enemy, DialogType.KILLS_OPPONENT, list);
                    _librarian.diceActionUI.EndAction();
                }
                if (_enemy.model.moveDetail.isArrived && _currentEnemyBehaviourResult.isDead)
                {
                    _enemy.StartDeadEffect();
                    BattleObjectManager.instance.GetAliveList(Faction.Enemy).Remove(_enemy.model);
                    List<BattleUnitModel> list2 = new();
                    list2.Add(_enemy.model);
                    DisplayDialogues(BattleObjectManager.instance.GetAliveList(Faction.Enemy), DialogType.COLLEAGUE_DEATH, list2);
                    DisplayDialogues(_librarian, DialogType.KILLS_OPPONENT, list2);
                    _enemy.diceActionUI.EndAction();
                }
                EndBehaviourResult();
                timer.Reset();
                ChangeTimerDelayByRatio(timer, _curDelayRatio);
            }
        }
        else
        {
            timer.Start();
            if (_currentLibrarianBehaviourResult.isDead)
            {
                List<BattleUnitModel> list3 = new();
                list3.Add(_enemy.model);
                DisplayDialogues(_librarian, DialogType.DEATH, list3);
            }
            if (_currentEnemyBehaviourResult.isDead)
            {
                new List<BattleUnitModel>().Add(_librarian.model);
                DisplayDialogues(_enemy, DialogType.DEATH, null);
            }

        }
    }

    private void ChangeTimerDelayByRatio(RencounterStateTimer timer, float delayRatio)
    {
        float t = timer.OriginWaitingTime * delayRatio;
        timer.ChangeWaitTime(t);
    }

    private void DisplayDialogues(List<BattleUnitModel> list, DialogType dlgType, List<BattleUnitModel> targets)
    {
        foreach (BattleUnitModel item in list)
        {
            item.view.DisplayDlg(dlgType, targets);
        }
    }

    private void DisplayDialogues(BattleUnitView view, DialogType dlgType, List<BattleUnitModel> targets)
    {
        view.DisplayDlg(dlgType, targets);
    }

    private void ChangePreMotion()
    {
        if (_currentEnemyBehaviourResult.hasBehaviour)
        {
            ActionDetail actionDetail = _currentEnemyBehaviourResult.diceBehaviourResult.actionDetail;
            ActionDetail currentMotionDetail = _enemy.charAppearance.GetCurrentMotion().actionDetail;
            if (actionDetail == currentMotionDetail)
            {
                if (actionDetail == ActionDetail.Fire) _enemy.charAppearance.ChangeMotion(ActionDetail.Aim);
                else _enemy.charAppearance.ChangeMotion(ActionDetail.Default);
            }
        }
        if (!_currentLibrarianBehaviourResult.hasBehaviour) return;
        ActionDetail actionDetail2 = _currentLibrarianBehaviourResult.diceBehaviourResult.actionDetail;
        ActionDetail currentMotionDetail2 = _librarian.charAppearance.GetCurrentMotion().actionDetail;
        if (actionDetail2 == currentMotionDetail2)
        {
            if (actionDetail2 == ActionDetail.Fire) _librarian.charAppearance.ChangeMotion(ActionDetail.Aim);
            else _librarian.charAppearance.ChangeMotion(ActionDetail.Default);
        }
    }

    private void PrintEffect(bool printLibrarian, bool printEnemy, ActionDetail librarianActionDetail, ActionDetail enemyActionDetail, string liCustomRes, string enCustomRes, bool librarianAtkEffectEvent, bool enemyAtkEffectEvent, bool librarianPrintEffectForcely, bool enemyPrintEffectForcely)
    {
        if (isPlayEffectSound)
        {
            switch (_currentEnemyBehaviourResult.diceBehaviourResult.result)
            {
            case Result.Win:
                _librarian.diceActionUI.StartActionResultDestroyedDice();
                break;
            case Result.Lose:
                _enemy.diceActionUI.StartActionResultDestroyedDice();
                break;
            }
        }
        if (_currentLibrarianBehaviourResult.diceBehaviourResult.result == Result.Draw && _currentLibrarianBehaviourResult.diceBehaviourResult.actionType == ActionType.Atk && _currentEnemyBehaviourResult.diceBehaviourResult.actionType == ActionType.Atk) PlaySound(Draw_AtkAtk);
        else if (_currentLibrarianBehaviourResult.diceBehaviourResult.result == Result.Draw && _currentLibrarianBehaviourResult.diceBehaviourResult.actionType == ActionType.Def && _currentEnemyBehaviourResult.diceBehaviourResult.actionType == ActionType.Def)
        {
            PlaySound(Draw_DefDef);
        }
        else if (_currentLibrarianBehaviourResult.diceBehaviourResult.actionType == ActionType.Def && _currentEnemyBehaviourResult.diceBehaviourResult.actionType == ActionType.Def)
        {
            PlaySound(Draw_DefDef);
        }
        else
        {
            PrintSound(_librarian, _currentLibrarianBehaviourResult, _currentEnemyBehaviourResult, librarianActionDetail);
            PrintSound(_enemy, _currentEnemyBehaviourResult, _currentLibrarianBehaviourResult, enemyActionDetail);
        }
        if (librarianAtkEffectEvent) _currentLibrarianBehaviourResult.printAtkEffectEvent?.Invoke();
        if (enemyAtkEffectEvent) _currentEnemyBehaviourResult.printAtkEffectEvent?.Invoke();
        if (_currentLibrarianBehaviourResult.hasBehaviour)
        {
            string resource = (string.IsNullOrEmpty(liCustomRes) ? _currentLibrarianBehaviourResult.behaviourRawData.EffectRes : liCustomRes);
            float num = _currentLibrarianBehaviourResult.resultDiceValue / (float)_currentLibrarianBehaviourResult.vanillaDiceFace;
            if (num > 2f) num = 2f;
            if (_currentLibrarianBehaviourResult.diceBehaviourResult.result != Result.Lose || _currentEnemyBehaviourResult.behaviourRawData.Type != BehaviourType.Atk || librarianPrintEffectForcely)
            {
                if (_currentLibrarianBehaviourResult.behaviourRawData.Type != BehaviourType.Def)
                {
                    if ((_currentLibrarianBehaviourResult.diceBehaviourResult.result != Result.Draw || librarianPrintEffectForcely) && printLibrarian)
                    {
                        DiceAttackEffect diceAttackEffect = SingletonBehavior<DiceEffectManager>.Instance.CreateBehaviourEffect(resource, num, _librarian, _enemy);
                        if (diceAttackEffect != null)
                        {
                            diceAttackEffect.SetLayer("Effect");
                            if (_currentLibrarianBehaviourResult.behaviourRawData.Type == BehaviourType.Atk)
                            {
                                foreach (Type addedAttackFilter in _currentLibrarianBehaviourResult.addedAttackFilterList)
                                {
                                    diceAttackEffect.AddFilter(addedAttackFilter);
                                }
                            }
                        }
                        if (_currentLibrarianBehaviourResult.diceBehaviourResult.result == Result.Win)
                        {
                            BehaviourDetail detail = _currentLibrarianBehaviourResult.behaviourRawData.Detail;
                            bool flag = true;
                            if (diceAttackEffect != null && diceAttackEffect.HasDamagedEffect) flag = false;
                            if (flag) SingletonBehavior<DiceEffectManager>.Instance.CreateDamagedEffectByBehaviour(detail, num, _enemy);
                        }
                    }
                }
                else if (_currentLibrarianBehaviourResult.diceBehaviourResult.result == Result.Win && _currentLibrarianBehaviourResult.behaviourRawData.Detail == BehaviourDetail.Guard && _currentEnemyBehaviourResult.hasBehaviour)
                {
                    if (printLibrarian)
                    {
                        DiceAttackEffect diceAttackEffect2 = SingletonBehavior<DiceEffectManager>.Instance.CreateBehaviourEffect(resource, num, _librarian, _enemy);
                        if (diceAttackEffect2 != null) diceAttackEffect2.SetLayer("Effect");
                    }
                    SingletonBehavior<DiceEffectManager>.Instance.CreateDefSuccess(_librarian.charAppearance.GetAtkEffectPivot(ActionDetail.Guard).position);
                    if (_currentEnemyBehaviourResult.diceBehaviourResult.range == CardRange.Near) _enemy.criticalUI.SetRebounded();
                }
            }
        }
        if (_currentEnemyBehaviourResult.hasBehaviour)
        {
            string resource2 = (string.IsNullOrEmpty(enCustomRes) ? _currentEnemyBehaviourResult.behaviourRawData.EffectRes : enCustomRes);
            float num2 = _currentEnemyBehaviourResult.resultDiceValue / (float)_currentEnemyBehaviourResult.vanillaDiceFace;
            if (num2 > 2f) num2 = 2f;
            if (_currentEnemyBehaviourResult.diceBehaviourResult.result != Result.Lose || _currentLibrarianBehaviourResult.behaviourRawData.Type != BehaviourType.Atk || enemyPrintEffectForcely)
            {
                if (_currentEnemyBehaviourResult.behaviourRawData.Type != BehaviourType.Def)
                {
                    if ((_currentEnemyBehaviourResult.diceBehaviourResult.result != Result.Draw || enemyPrintEffectForcely) && printEnemy)
                    {
                        DiceAttackEffect diceAttackEffect3 = SingletonBehavior<DiceEffectManager>.Instance.CreateBehaviourEffect(resource2, num2, _enemy, _librarian);
                        if (diceAttackEffect3 != null)
                        {
                            diceAttackEffect3.SetLayer("Effect");
                            if (_currentEnemyBehaviourResult.behaviourRawData.Type == BehaviourType.Atk)
                            {
                                foreach (Type addedAttackFilter2 in _currentEnemyBehaviourResult.addedAttackFilterList)
                                {
                                    diceAttackEffect3.AddFilter(addedAttackFilter2);
                                }
                            }
                        }
                        if (_currentEnemyBehaviourResult.diceBehaviourResult.result == Result.Win)
                        {
                            BehaviourDetail detail2 = _currentEnemyBehaviourResult.behaviourRawData.Detail;
                            bool flag2 = true;
                            if (diceAttackEffect3 != null && diceAttackEffect3.HasDamagedEffect) flag2 = false;
                            if (flag2) SingletonBehavior<DiceEffectManager>.Instance.CreateDamagedEffectByBehaviour(detail2, num2, _librarian);
                        }
                    }
                }
                else if (_currentEnemyBehaviourResult.diceBehaviourResult.result == Result.Win && _currentEnemyBehaviourResult.behaviourRawData.Detail == BehaviourDetail.Guard && _currentLibrarianBehaviourResult.hasBehaviour)
                {
                    if (printEnemy)
                    {
                        DiceAttackEffect diceAttackEffect4 = SingletonBehavior<DiceEffectManager>.Instance.CreateBehaviourEffect(resource2, num2, _enemy, _librarian);
                        if (diceAttackEffect4 != null) diceAttackEffect4.SetLayer("Effect");
                    }
                    SingletonBehavior<DiceEffectManager>.Instance.CreateDefSuccess(_enemy.charAppearance.GetAtkEffectPivot(ActionDetail.Guard).position);
                    if (_currentLibrarianBehaviourResult.diceBehaviourResult.range == CardRange.Near) _librarian.criticalUI.SetRebounded();
                }
            }
        }
        if (_currentLibrarianBehaviourResult.hasBehaviour && _currentEnemyBehaviourResult.hasBehaviour && _currentLibrarianBehaviourResult.behaviourRawData.Type == BehaviourType.Atk && _currentEnemyBehaviourResult.behaviourRawData.Type == BehaviourType.Atk)
        {
            if (_currentLibrarianBehaviourResult.diceBehaviourResult.result == Result.Draw)
            {
                Vector3 pos = (_librarian.atkEffectRoot.position + _enemy.atkEffectRoot.position) * 0.5f;
                if (_currentLibrarianBehaviourResult.cardModel.GetSpec().Ranged == CardRange.Far) pos = _enemy.charAppearance.atkEffectRoot.position;
                else if (_currentEnemyBehaviourResult.cardModel.GetSpec().Ranged == CardRange.Far)
                {
                    pos = _librarian.charAppearance.atkEffectRoot.position;
                }
                SingletonBehavior<DiceEffectManager>.Instance.CreateSparkByAtkVsAtk(pos);
            }
            else if (_currentEnemyBehaviourResult.diceBehaviourResult.result == Result.Win)
            {
                if (_currentEnemyBehaviourResult.diceBehaviourResult.range == CardRange.Near && _currentLibrarianBehaviourResult.cardModel.GetSpec().Ranged == CardRange.Far)
                {
                    Vector3 position = _enemy.charAppearance.atkEffectRoot.position;
                    SingletonBehavior<DiceEffectManager>.Instance.CreateSparkByAtkVsAtk(position);
                }
            }
            else if (_currentLibrarianBehaviourResult.diceBehaviourResult.result == Result.Win && _currentLibrarianBehaviourResult.diceBehaviourResult.range == CardRange.Near && _currentEnemyBehaviourResult.cardModel.GetSpec().Ranged == CardRange.Far)
            {
                Vector3 position2 = _enemy.charAppearance.atkEffectRoot.position;
                SingletonBehavior<DiceEffectManager>.Instance.CreateSparkByAtkVsAtk(position2);
            }
        }
        foreach (string addedAbnormalityEffect in _currentLibrarianBehaviourResult.addedAbnormalityEffectList)
        {
            DiceAttackEffect diceAttackEffect5 = SingletonBehavior<DiceEffectManager>.Instance.CreateAbnormalityEffect(addedAbnormalityEffect, 1f, _librarian, _enemy);
            if (diceAttackEffect5 != null) diceAttackEffect5.SetLayer("Effect");
        }
        foreach (string addedAbnormalityEffect2 in _currentEnemyBehaviourResult.addedAbnormalityEffectList)
        {
            DiceAttackEffect diceAttackEffect6 = SingletonBehavior<DiceEffectManager>.Instance.CreateAbnormalityEffect(addedAbnormalityEffect2, 1f, _enemy, _librarian);
            if (diceAttackEffect6 != null) diceAttackEffect6.SetLayer("Effect");
        }
        foreach (BattleCardBehaviourResult.CreatureEffectInfo addedCreatureEffect in _currentLibrarianBehaviourResult.addedCreatureEffectList)
        {
            CreatureEffect creatureEffect = SingletonBehavior<DiceEffectManager>.Instance.CreateCreatureEffect(addedCreatureEffect.path, 1f, _enemy, _librarian, addedCreatureEffect.time);
            if ((bool)creatureEffect) creatureEffect.SetLayer("Effect");
        }
        foreach (BattleCardBehaviourResult.CreatureEffectInfo addedCreatureEffect2 in _currentEnemyBehaviourResult.addedCreatureEffectList)
        {
            CreatureEffect creatureEffect2 = SingletonBehavior<DiceEffectManager>.Instance.CreateCreatureEffect(addedCreatureEffect2.path, 1f, _librarian, _enemy, addedCreatureEffect2.time);
            if ((bool)creatureEffect2) creatureEffect2.SetLayer("Effect");
        }
        foreach (CreatureEffect creatureEffect3 in _currentLibrarianBehaviourResult.creatureEffects)
        {
            if ((bool)creatureEffect3) creatureEffect3.OnPrintEffect();
        }
        foreach (CreatureEffect creatureEffect4 in _currentEnemyBehaviourResult.creatureEffects)
        {
            if ((bool)creatureEffect4) creatureEffect4.OnPrintEffect();
        }
        foreach (string item in _currentLibrarianBehaviourResult.GetAddedBufEffect())
        {
            BufEffect bufEffect = SingletonBehavior<DiceEffectManager>.Instance.CreateBufEffect(item, _librarian);
            if ((bool)bufEffect) bufEffect.SetLayer("Effect");
        }
        foreach (string item2 in _currentEnemyBehaviourResult.GetAddedBufEffect())
        {
            BufEffect bufEffect2 = SingletonBehavior<DiceEffectManager>.Instance.CreateBufEffect(item2, _enemy);
            if ((bool)bufEffect2) bufEffect2.SetLayer("Effect");
        }
        foreach (string craetureEffectSound in _currentLibrarianBehaviourResult.craetureEffectSounds)
        {
            SingletonBehavior<SoundEffectManager>.Instance.PlayClip(craetureEffectSound);
        }
        foreach (string craetureEffectSound2 in _currentEnemyBehaviourResult.craetureEffectSounds)
        {
            SingletonBehavior<SoundEffectManager>.Instance.PlayClip(craetureEffectSound2);
        }
    }

    private void PrintRecovery()
    {
        foreach (int recoveredHp in _currentLibrarianBehaviourResult.recoveredHpList) _librarian.RecoverHp(recoveredHp);
        foreach (int recoveredHp in _currentEnemyBehaviourResult.recoveredHpList) _enemy.RecoverHp(recoveredHp);
        foreach (int recoveredBp in _currentLibrarianBehaviourResult.recoveredBpList) _librarian.RecoverBp(recoveredBp);
        foreach (int recoveredBp in _currentEnemyBehaviourResult.recoveredBpList) _enemy.RecoverBp(recoveredBp);
    }

    private void PrintDamageWithoutText(BattleUnitView loser)
    {
        loser.DamagedWithoutText();
    }

    private void PrintDamage()
    {
        int num = 0;
        float curHp = _currentLibrarianBehaviourResult.curHp;
        bool flag = false;
        bool showLibrarianDamage = _currentLibrarianBehaviourResult.dmgListTaken.Count > 0;
        bool showEnemyDamage = _currentEnemyBehaviourResult.dmgListTaken.Count > 0;
        _currentLibrarianBehaviourResult.printDamagedEffectEvent?.Invoke();
        _currentEnemyBehaviourResult.printDamagedEffectEvent?.Invoke();
        if (isPlayPrintDamage)
        {
            switch (_currentEnemyBehaviourResult.diceBehaviourResult.result)
            {
            case Result.Win:
                _librarian.diceActionUI.StartActionResultDestroyedDice();
                break;
            case Result.Lose:
                _enemy.diceActionUI.StartActionResultDestroyedDice();
                break;
            }
        }
        if (showLibrarianDamage)
        {
            foreach (DmgTakenData item in _currentLibrarianBehaviourResult.dmgListTaken)
            {
                _librarian.Damaged(item.dmg, item.damagedType, _currentEnemyBehaviourResult.resultDiceMax, _enemy.model, item.atkResist);
                if (item.dmg > num) num = item.dmg;
                if (!flag && _currentEnemyBehaviourResult.vanillaDiceFace > _currentEnemyBehaviourResult.vanillaDiceMin + 2 && _currentEnemyBehaviourResult.vanillaDiceValue >= _currentEnemyBehaviourResult.vanillaDiceFace && _currentEnemyBehaviourResult.diceBehaviourResult.actionType == ActionType.Atk) flag = true;
            }
            _librarian.PrintBloodSprites(num, curHp);
        }
        int num2 = 0;
        float curHp2 = _currentEnemyBehaviourResult.curHp;
        bool flag2 = false;
        if (showEnemyDamage)
        {
            foreach (DmgTakenData item2 in _currentEnemyBehaviourResult.dmgListTaken)
            {
                _enemy.Damaged(item2.dmg, item2.damagedType, _currentLibrarianBehaviourResult.resultDiceMax, _librarian.model, item2.atkResist);
                if (item2.dmg > num2) num2 = item2.dmg;
                if (!flag2 && _currentLibrarianBehaviourResult.vanillaDiceFace > _currentLibrarianBehaviourResult.vanillaDiceMin + 2 && _currentLibrarianBehaviourResult.vanillaDiceValue >= _currentLibrarianBehaviourResult.vanillaDiceFace && _currentLibrarianBehaviourResult.diceBehaviourResult.actionType == ActionType.Atk) flag2 = true;
            }
            _enemy.PrintBloodSprites(num2, curHp2);
        }
        int curBreakGauge = _currentLibrarianBehaviourResult.curBreakGauge;
        if (_currentLibrarianBehaviourResult.breakListTaken.Count > 0)
        {
            foreach (DmgTakenData item3 in _currentLibrarianBehaviourResult.breakListTaken)
            {
                _librarian.BreakDamaged(item3.dmg, item3.damagedType, _enemy.model, item3.atkResist);
            }
        }
        int curBreakGauge2 = _currentEnemyBehaviourResult.curBreakGauge;
        if (_currentEnemyBehaviourResult.breakListTaken.Count > 0)
        {
            foreach (DmgTakenData item4 in _currentEnemyBehaviourResult.breakListTaken)
            {
                _enemy.BreakDamaged(item4.dmg, item4.damagedType, _librarian.model, item4.atkResist);
            }
        }
        SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfile(_librarian.model, _librarian.model.faction, curHp, curBreakGauge, _currentLibrarianBehaviourResult.bufDataList);
        SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfile(_enemy.model, _enemy.model.faction, curHp2, curBreakGauge2, _currentEnemyBehaviourResult.bufDataList);
        foreach (EmotionCoinType addedEmotionCoin in _currentLibrarianBehaviourResult.GetAddedEmotionCoins())
        {
            SingletonBehavior<BattleManagerUI>.Instance.ui_battleEmotionCoinUI.OnAcquireCoin(_librarian.model, addedEmotionCoin, 1);
        }
        foreach (EmotionCoinType addedEmotionCoin2 in _currentEnemyBehaviourResult.GetAddedEmotionCoins())
        {
            SingletonBehavior<BattleManagerUI>.Instance.ui_battleEmotionCoinUI.OnAcquireCoin(_enemy.model, addedEmotionCoin2, 1);
        }
        if (showLibrarianDamage && showEnemyDamage)
        {
            if (_librarian.model.direction == Direction.RIGHT) SingletonBehavior<BattleCamManager>.Instance.BloodFilter(0, num / curHp, num2 / curHp2);
            else SingletonBehavior<BattleCamManager>.Instance.BloodFilter(0, num2 / curHp2, num / curHp);
        }
        else if (showLibrarianDamage)
        {
            SingletonBehavior<BattleCamManager>.Instance.BloodFilter((_librarian.model.direction != Direction.RIGHT) ? 1 : (-1), num / curHp);
        }
        else if (showEnemyDamage)
        {
            SingletonBehavior<BattleCamManager>.Instance.BloodFilter((_enemy.model.direction != Direction.RIGHT) ? 1 : (-1), num2 / curHp2);
        }
        bool breaked = false;
        bool breaked2 = false;
        if (_currentLibrarianBehaviourResult.isBreaked || _currentEnemyBehaviourResult.isBreaked)
        {
            if (_currentLibrarianBehaviourResult.isBreaked) breaked2 = true;
            if (_currentEnemyBehaviourResult.isBreaked) breaked = true;
        }
        _librarian.SetCriticals(breaked2, flag);
        _enemy.SetCriticals(breaked, flag2);
        SecondaryPlayback.PrintCurrent(_currentEnemyBehaviourResult, _currentLibrarianBehaviourResult);
    }

    private void PrintSound(BattleUnitView view, BattleCardBehaviourResult selfResult, BattleCardBehaviourResult opponentResult, ActionDetail actionDetail)
    {
        if (!selfResult.hasBehaviour) return;
        DiceBehaviourResultData diceBehaviourResult = selfResult.diceBehaviourResult;
        if (diceBehaviourResult.actionType == ActionType.Atk)
        {
            if (diceBehaviourResult.result == Result.Win)
            {
                if (diceBehaviourResult.range == CardRange.Near && opponentResult.hasBehaviour && opponentResult.diceBehaviourResult.range == CardRange.Far && opponentResult.diceBehaviourResult.actionType == ActionType.Atk) PlaySound(Draw_AtkAtk);
                else view.charAppearance.soundInfo?.PlaySound(MotionConverter.ActionToMotion(actionDetail), win: true);
            }
            else if (diceBehaviourResult.result == Result.Lose)
            {
                if (opponentResult.diceBehaviourResult.actionType == ActionType.Def) view.charAppearance.soundInfo?.PlaySound(MotionConverter.ActionToMotion(actionDetail), win: false);
                else if (opponentResult.diceBehaviourResult.actionType == ActionType.Atk && diceBehaviourResult.range == CardRange.Far && diceBehaviourResult.actionDetail == ActionDetail.Fire)
                {
                    view.charAppearance.soundInfo?.PlaySound(MotionDetail.F, win: false);
                }
            }
            else if (opponentResult.diceBehaviourResult.actionType == ActionType.Atk)
            {
                if (diceBehaviourResult.range == CardRange.Far && diceBehaviourResult.actionDetail == ActionDetail.Fire) view.charAppearance.soundInfo?.PlaySound(MotionDetail.F, win: false);
            }
            else if (opponentResult.diceBehaviourResult.actionType == ActionType.Def)
            {
                view.charAppearance.soundInfo?.PlaySound(MotionConverter.ActionToMotion(actionDetail), win: false);
            }
        }
        else
        {
            if (diceBehaviourResult.actionType != ActionType.Def) return;
            if (diceBehaviourResult.result == Result.Win) if (opponentResult.hasBehaviour && opponentResult.diceBehaviourResult.actionType == ActionType.Atk) view.charAppearance.soundInfo?.PlaySound(MotionConverter.ActionToMotion(diceBehaviourResult.actionDetail), win: true);
            else if (diceBehaviourResult.result == Result.Lose)
            {
                if (opponentResult.diceBehaviourResult.actionType == ActionType.Atk) view.charAppearance.soundInfo?.PlaySound(MotionConverter.ActionToMotion(diceBehaviourResult.actionDetail), win: false);
            }
            else if (opponentResult.diceBehaviourResult.actionType == ActionType.Atk)
            {
                bool win = false;
                if (diceBehaviourResult.actionDetail == ActionDetail.Evade) win = true;
                MotionDetail motion = MotionConverter.ActionToMotion(diceBehaviourResult.actionDetail);
                view.charAppearance.soundInfo?.PlaySound(motion, win);
            }
            else
            {
                _ = opponentResult.diceBehaviourResult.actionType;
                _ = 1;
            }
        }
    }

    private void SetMovingStateByActionResult()
    {
        DiceBehaviourResultData diceBehaviourResult = _currentEnemyBehaviourResult.diceBehaviourResult;
        DiceBehaviourResultData diceBehaviourResult2 = _currentLibrarianBehaviourResult.diceBehaviourResult;
        Result result = diceBehaviourResult.result;
        ActionAfterBehaviour self = default(ActionAfterBehaviour);
        ActionAfterBehaviour opponent = default(ActionAfterBehaviour);
        self.infoList = new List<MovingAction>();
        opponent.infoList = new List<MovingAction>();
        self.result = Result.Win;
        opponent.result = Result.Lose;
        switch (result)
        {
        case Result.Win:
            self.view = _enemy;
            opponent.view = _librarian;
            self.data = diceBehaviourResult;
            opponent.data = diceBehaviourResult2;
            self.behaviourResultData = _currentEnemyBehaviourResult;
            opponent.behaviourResultData = _currentLibrarianBehaviourResult;
            break;
        case Result.Lose:
            self.view = _librarian;
            opponent.view = _enemy;
            self.data = diceBehaviourResult2;
            opponent.data = diceBehaviourResult;
            self.behaviourResultData = _currentLibrarianBehaviourResult;
            opponent.behaviourResultData = _currentEnemyBehaviourResult;
            break;
        default:
            self.view = _enemy;
            opponent.view = _librarian;
            self.result = Result.Draw;
            opponent.result = Result.Draw;
            self.data = diceBehaviourResult;
            opponent.data = diceBehaviourResult2;
            self.behaviourResultData = _currentEnemyBehaviourResult;
            opponent.behaviourResultData = _currentLibrarianBehaviourResult;
            break;
        }
        if (self.behaviourResultData.isBlockedDice && !HasOriginalBlockMotion(self))
        {
            self.result = Result.Draw;
            opponent.result = Result.Draw;
            self.behaviourResultData.diceBehaviourResult.result = Result.Draw;
            opponent.behaviourResultData.diceBehaviourResult.result = Result.Draw;
            if (opponent.data.actionType == ActionType.None)
            {
                opponent.data.actionType = ActionType.Atk;
                opponent.data.actionDetail = ActionDetail.Hit;
                opponent.behaviourResultData.diceBehaviourResult.actionDetail = ActionDetail.Hit;
                opponent.behaviourResultData.diceBehaviourResult.actionType = ActionType.Atk;
            }
        }
        self.preventOverlap = true;
        opponent.preventOverlap = true;
        self.infoList = GetBehaviourAction(ref self, ref opponent);
        {
            if (opponent.infoList.Count == 0) opponent.infoList = GetBehaviourAction(ref opponent, ref self);
        }
        StartCoroutine(RunMotion(self, opponent));
    }

    private BehaviourActionBase CreateBehaviourAction(string s)
    {
        BehaviourActionBase behaviourActionBase = null;
        if (s == null || s == string.Empty) behaviourActionBase = new BehaviourActionBase();
        else
        {
            behaviourActionBase = Singleton<AssemblyManager>.Instance.CreateInstance_BehaviourAction(s);
            if (behaviourActionBase == null) behaviourActionBase = new BehaviourActionBase();
        }
        return behaviourActionBase;
    }

    private bool HasOriginalBlockMotion(ActionAfterBehaviour self)
    {
        if (self.behaviourResultData?.behaviourRawData?.ActionScript == "RedMist_BigSeveringVert") return true;
        return false;
    }

    private List<MovingAction> GetBehaviourAction(ref ActionAfterBehaviour self, ref ActionAfterBehaviour opponent, bool isBlocked = false)
    {
        string s = self.behaviourResultData?.behaviourRawData?.ActionScript;
        return CreateBehaviourAction(s).GetMovingAction(ref self, ref opponent);
    }

    // Unity会独立推进协程；动作脚本异常时必须释放聚焦及舞台等待状态。
    private IEnumerator RunMotion(ActionAfterBehaviour winner, ActionAfterBehaviour loser)
    {
        var routine = MoveRoutine(winner, loser);
        while (true)
        {
            // 唯一动作时钟；暂停或相机持有期间不推进协程、事件与副目标计划。
            if (Paused)
            {
                yield return null;
                continue;
            }
            if (motionFrame != Time.frameCount)
            {
                motionFrame = Time.frameCount;
                motionDelta = Time.deltaTime;
                MotionTime += motionDelta;
            }
            bool next;
            try
            {
                next = routine.MoveNext();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EndRencounterForcely();
                next = false;
            }
            if (!next) yield break;
            yield return routine.Current;
        }
    }

    private IEnumerator MoveRoutine(ActionAfterBehaviour winner, ActionAfterBehaviour loser)
    {
        _actionAfterBehaviourIsRunning = true;
        winner.view.model.UpdateDirection(loser.view.WorldPosition);
        loser.view.model.UpdateDirection(winner.view.WorldPosition);
        yield return YieldCache.waitFrame;
        SecondaryPlayback.BeginMotion(_currentEnemyBehaviourResult, _currentLibrarianBehaviourResult);
        winner.startMoveRoutineEvent?.Invoke();
        loser.startMoveRoutineEvent?.Invoke();
        if (!winner.infoList.Exists((MovingAction x) => x.IsExistEffectTiming()) && !loser.infoList.Exists((MovingAction x) => x.IsExistEffectTiming()))
        {
            ActionDetail actionDetail = _currentLibrarianBehaviourResult.diceBehaviourResult.actionDetail;
            ActionDetail actionDetail2 = _currentEnemyBehaviourResult.diceBehaviourResult.actionDetail;
            if (winner.view == _librarian)
            {
                PrintEffect(winner.infoList.Exists((MovingAction x) => !x.IsAtkEffectTiming(EffectTiming.NOT_PRINT)), loser.infoList.Exists((MovingAction x) => !x.IsAtkEffectTiming(EffectTiming.NOT_PRINT)), actionDetail, actionDetail2, "", "", librarianAtkEffectEvent: true, enemyAtkEffectEvent: true, librarianPrintEffectForcely: false, enemyPrintEffectForcely: false);
            }
            else PrintEffect(loser.infoList.Exists((MovingAction x) => !x.IsAtkEffectTiming(EffectTiming.NOT_PRINT)), winner.infoList.Exists((MovingAction x) => !x.IsAtkEffectTiming(EffectTiming.NOT_PRINT)), actionDetail, actionDetail2, "", "", librarianAtkEffectEvent: true, enemyAtkEffectEvent: true, librarianPrintEffectForcely: false, enemyPrintEffectForcely: false);
            PrintRecovery();
            PrintDamage();
        }
        _currentLibrarianBehaviourResult.afterActionEvent?.Invoke();
        _currentEnemyBehaviourResult.afterActionEvent?.Invoke();
        _currentLibrarianBehaviourResult.succeedAtkEvent?.Invoke();
        _currentEnemyBehaviourResult.succeedAtkEvent?.Invoke();
        _currentLibrarianBehaviourResult.takeDamageEvent?.Invoke();
        _currentEnemyBehaviourResult.takeDamageEvent?.Invoke();
        int sign = 0;
        if (_currentLibrarianBehaviourResult.diceBehaviourResult.actionDetail == ActionDetail.Damaged) sign = ((!(_librarian.WorldPosition.x > _enemy.WorldPosition.x)) ? 1 : (-1));
        else if (_currentEnemyBehaviourResult.diceBehaviourResult.actionDetail == ActionDetail.Damaged)
        {
            sign = ((_librarian.WorldPosition.x > _enemy.WorldPosition.x) ? 1 : (-1));
        }
        SingletonBehavior<BattleCamManager>.Instance.TiltCam(sign);
        yield return YieldCache.waitFrame;
        _librarian.diceActionUI.UpdateDiceAnimation(CompareBehaviourUIType.Start, DiceUITiming.AttackAfter);
        _enemy.diceActionUI.UpdateDiceAnimation(CompareBehaviourUIType.Start, DiceUITiming.AttackAfter);
        while (winner.infoList.Count > 0 || loser.infoList.Count > 0)
        {
            SecondaryPlayback.BeginMainAction(winner.view, winner.infoList.Count > 0 ? winner.infoList[0] : null);
            SecondaryPlayback.BeginMainAction(loser.view, loser.infoList.Count > 0 ? loser.infoList[0] : null);
            bool winnerCanMove = true;
            bool loserCanMove = true;
            bool winnerClockWise = UnityEngine.Random.Range(0f, 1f) > 0.5f;
            if (winner.infoList.Count > 0)
            {
                MovingAction movingAction = winner.infoList[0];
                winner.view.charAppearance.ChangeMotion(movingAction.actionDetail);
                if (movingAction.moveState == CharMoveState.Knockback)
                {
                    int sum = 0;
                    float num;
                    if (movingAction.knockbackPower < 0f)
                    {
                        winner.behaviourResultData.breakListTaken.ForEach(delegate(DmgTakenData x)
                        {
                            sum += x.dmg;
                        });
                        num = coefKnockback * sum;
                    }
                    else num = movingAction.knockbackPower;
                    if (num <= 5f) num = 5f;
                    if (num >= 15f) num = 15f;
                    int num2 = winner.behaviourResultData.resultDiceValue + sum;
                    winner.view.model.moveDetail.Knockback(loser.view.model, num, 50f, num2);
                }
                if (isActionPrev)
                {
                    switch (_currentEnemyBehaviourResult.diceBehaviourResult.result)
                    {
                    case Result.Win:
                        _librarian.diceActionUI.StartActionResultDestroyedDice();
                        break;
                    case Result.Lose:
                        _enemy.diceActionUI.StartActionResultDestroyedDice();
                        break;
                    }
                }
                if (movingAction.atkEffectTiming == EffectTiming.PRE)
                {
                    ActionDetail actionDetail3 = ActionDetail.NONE;
                    bool flag2 = false;
                    bool flag3 = false;
                    if (loser.infoList.Count > 0)
                    {
                        actionDetail3 = loser.infoList[0].actionDetail;
                        flag2 = loser.infoList[0].callAtkEffectEvent;
                        flag3 = loser.infoList[0].printEffectForcely;
                    }
                    if (winner.view == _librarian)
                    {
                        PrintEffect(printLibrarian: true, printEnemy: true, movingAction.actionDetail, actionDetail3, movingAction.customEffectRes, "None", movingAction.callAtkEffectEvent, flag2, movingAction.printEffectForcely, flag3);
                    }
                    else PrintEffect(printLibrarian: true, printEnemy: true, actionDetail3, movingAction.actionDetail, "None", movingAction.customEffectRes, flag2, movingAction.callAtkEffectEvent, flag3, movingAction.printEffectForcely);
                    if (movingAction.damagedEffectTiming == EffectTiming.WITHOUT_DMGTEXT) PrintDamageWithoutText(loser.view);
                }
                if (movingAction.recoverEffectTiming == EffectTiming.PRE) PrintRecovery();
                if (movingAction.damagedEffectTiming == EffectTiming.PRE) PrintDamage();
            }
            else winnerCanMove = false;
            if (loser.infoList.Count > 0)
            {
                MovingAction movingAction2 = loser.infoList[0];
                if (loser.view.model.Book.GetBookClassInfoId() == 150013 && winner.data.range == CardRange.Far && movingAction2.actionDetail == ActionDetail.Damaged) loser.view.charAppearance.ChangeMotion(ActionDetail.Guard);
                else loser.view.charAppearance.ChangeMotion(movingAction2.actionDetail);
                if (movingAction2.moveState == CharMoveState.Knockback)
                {
                    int sum2 = 0;
                    float num3;
                    if (movingAction2.knockbackPower < 0f)
                    {
                        loser.behaviourResultData.breakListTaken.ForEach(delegate(DmgTakenData x)
                        {
                            sum2 += x.dmg;
                        });
                        num3 = coefKnockback * sum2;
                    }
                    else num3 = movingAction2.knockbackPower;
                    if (num3 <= 3f) num3 = 3f;
                    if (num3 >= 15f) num3 = 15f;
                    int num4 = loser.behaviourResultData.resultDiceValue + sum2;
                    loser.view.model.moveDetail.Knockback(winner.view.model, num3, 50f, num4);
                }
            }
            else loserCanMove = false;
            Vector3 winnerStartPos = winner.view.WorldPosition;
            Vector3 loserStartPos = loser.view.WorldPosition;
            while (winnerCanMove || loserCanMove)
            {
                if (loserCanMove)
                {
                    MovingAction movingAction3 = loser.infoList[0];
                    bool flag5 = false;
                    switch (movingAction3.moveState)
                    {
                    case CharMoveState.None:
                        flag5 = true;
                        break;
                    case CharMoveState.Stop:
                    case CharMoveState.KnockDown:
                        flag5 = StopMoving();
                        break;
                    case CharMoveState.MoveForward:
                        flag5 = MoveForward(loser.view, loserStartPos, loserStartPos + Quaternion.AngleAxis(movingAction3.dstRotation, Vector3.up) * (winnerStartPos - loserStartPos).normalized * movingAction3.dstRatio, movingAction3.speed);
                        break;
                    case CharMoveState.MoveBack:
                        flag5 = MoveBack(loser.view, loserStartPos, loserStartPos + Quaternion.AngleAxis(movingAction3.dstRotation, Vector3.up) * (loserStartPos - winnerStartPos).normalized * movingAction3.dstRatio, movingAction3.speed);
                        break;
                    case CharMoveState.TeleportFront:
                        flag5 = TeleportFront(loser.view, winner.view, movingAction3.dstDistance, movingAction3.dstRotation);
                        movingAction3.moveState = CharMoveState.None;
                        break;
                    case CharMoveState.TeleportBack:
                        flag5 = TeleportBack(loser.view, winner.view, movingAction3.dstDistance, movingAction3.dstRotation);
                        movingAction3.moveState = CharMoveState.None;
                        break;
                    case CharMoveState.Knockback:
                        flag5 = Knockback(loser.view, winner.view);
                        break;
                    case CharMoveState.BackBlink:
                        flag5 = BlinkBack(loser.view, loserStartPos + (loserStartPos - winnerStartPos).normalized * SingletonBehavior<HexagonalMapManager>.Instance.tileSize * 2f);
                        break;
                    case CharMoveState.Confrontation:
                        flag5 = Confrontation(loser.view, winner.view, (loserStartPos - winnerStartPos).normalized * SingletonBehavior<HexagonalMapManager>.Instance.tileSize * 2f, winnerClockWise);
                        break;
                    case CharMoveState.Custom:
                        flag5 = movingAction3.MoveCustom(MotionDelta, movingAction3.elapsed);
                        break;
                    }
                    if (movingAction3.moveState == CharMoveState.KnockDown) loser.view.KnockDown(v: true);
                    else loser.view.KnockDown(v: false);
                    if (flag5 && movingAction3.updateDirectionAfterAction && loser.view != null && winner.view.model != null && winner.view != null) loser.view.model.UpdateDirection(winner.view.WorldPosition);
                    movingAction3.elapsed += MotionDelta;
                    if (movingAction3.elapsed < movingAction3.delay) flag5 = false;
                    loserCanMove = !flag5;
                }
                if (winnerCanMove)
                {
                    MovingAction movingAction4 = winner.infoList[0];
                    bool flag6 = false;
                    switch (movingAction4.moveState)
                    {
                    case CharMoveState.None:
                        flag6 = true;
                        break;
                    case CharMoveState.Stop:
                    case CharMoveState.KnockDown:
                        flag6 = StopMoving();
                        break;
                    case CharMoveState.MoveForward:
                        flag6 = MoveForward(winner.view, winnerStartPos, winnerStartPos + Quaternion.AngleAxis(movingAction4.dstRotation, Vector3.up) * (loserStartPos - winnerStartPos).normalized * movingAction4.dstRatio, movingAction4.speed);
                        break;
                    case CharMoveState.MoveBack:
                        flag6 = MoveBack(winner.view, winnerStartPos, winnerStartPos + Quaternion.AngleAxis(movingAction4.dstRotation, Vector3.up) * (winnerStartPos - loserStartPos).normalized * movingAction4.dstRatio, movingAction4.speed);
                        break;
                    case CharMoveState.MoveRight:
                        flag6 = MoveForward(winner.view, winnerStartPos, winnerStartPos + Vector3.right * movingAction4.dstRatio, movingAction4.speed);
                        break;
                    case CharMoveState.MoveLeft:
                        flag6 = MoveForward(winner.view, winnerStartPos, winnerStartPos + Vector3.left * movingAction4.dstRatio, movingAction4.speed);
                        break;
                    case CharMoveState.MoveOpponent:
                        flag6 = MoveToOponnent(winner.view, loser.view, winnerStartPos, movingAction4.speed);
                        break;
                    case CharMoveState.MoveOpponent_Near:
                        flag6 = MoveToOponnent_Near(winner.view, loser.view, winnerStartPos, movingAction4.speed);
                        break;
                    case CharMoveState.MoveOpponent_Near2:
                        flag6 = MoveToOponnent_Near2(winner.view, loser.view, winnerStartPos, movingAction4.speed);
                        break;
                    case CharMoveState.TeleportFront:
                        flag6 = TeleportFront(winner.view, loser.view, movingAction4.dstDistance, movingAction4.dstRotation);
                        movingAction4.moveState = CharMoveState.None;
                        break;
                    case CharMoveState.TeleportBack:
                        flag6 = TeleportBack(winner.view, loser.view, movingAction4.dstDistance, movingAction4.dstRotation);
                        movingAction4.moveState = CharMoveState.None;
                        break;
                    case CharMoveState.Knockback:
                        flag6 = Knockback(winner.view, loser.view);
                        break;
                    case CharMoveState.BackBlink:
                        flag6 = BlinkBack(winner.view, winnerStartPos + (winnerStartPos - loserStartPos).normalized * SingletonBehavior<HexagonalMapManager>.Instance.tileSize * 2f);
                        break;
                    case CharMoveState.Confrontation:
                        flag6 = Confrontation(winner.view, loser.view, (winnerStartPos - loserStartPos).normalized * SingletonBehavior<HexagonalMapManager>.Instance.tileSize * 2f, winnerClockWise);
                        break;
                    case CharMoveState.Custom:
                        flag6 = movingAction4.MoveCustom(MotionDelta, movingAction4.elapsed);
                        break;
                    }
                    if (movingAction4.moveState == CharMoveState.KnockDown) winner.view.KnockDown(v: true);
                    else winner.view.KnockDown(v: false);
                    if (flag6 && movingAction4.updateDirectionAfterAction && winner.view != null && winner.view.model != null && loser.view != null) winner.view.model.UpdateDirection(loser.view.WorldPosition);
                    movingAction4.elapsed += MotionDelta;
                    if (movingAction4.elapsed < movingAction4.delay) flag6 = false;
                    winnerCanMove = !flag6;
                }
                yield return YieldCache.waitFrame;
            }
            if (!winnerCanMove && !loserCanMove)
            {
                _elapsedCompetition = 0f;
                _elapsedConfrontation = 0f;

            }
            if (winner.infoList.Count > 0)
            {
                MovingAction movingAction5 = winner.infoList[0];
                if (movingAction5.atkEffectTiming == EffectTiming.POST)
                {
                    ActionDetail actionDetail4 = ActionDetail.NONE;
                    bool flag7 = false;
                    bool flag8 = false;
                    if (loser.infoList.Count > 0)
                    {
                        actionDetail4 = loser.infoList[0].actionDetail;
                        flag7 = loser.infoList[0].callAtkEffectEvent;
                        flag8 = loser.infoList[0].printEffectForcely;
                    }
                    if (winner.view == _librarian)
                    {
                        PrintEffect(printLibrarian: true, printEnemy: true, movingAction5.actionDetail, actionDetail4, movingAction5.customEffectRes, "None", movingAction5.callAtkEffectEvent, flag7, movingAction5.printEffectForcely, flag8);
                    }
                    else PrintEffect(printLibrarian: true, printEnemy: true, actionDetail4, movingAction5.actionDetail, "None", movingAction5.customEffectRes, flag7, movingAction5.callAtkEffectEvent, flag8, movingAction5.printEffectForcely);
                    if (movingAction5.damagedEffectTiming == EffectTiming.WITHOUT_DMGTEXT) PrintDamageWithoutText(loser.view);
                }
                if (movingAction5.recoverEffectTiming == EffectTiming.POST) PrintRecovery();
                if (movingAction5.damagedEffectTiming == EffectTiming.POST) PrintDamage();
                winner.infoList.RemoveAt(0);
            }
            if (loser.infoList.Count > 0) loser.infoList.RemoveAt(0);
            yield return YieldCache.waitFrame;
        }
        SingletonBehavior<BattleCamManager>.Instance.TiltCam(0, exitForcely: true);
        _actionAfterBehaviourIsRunning = false;
        winner.endMoveRoutineEvent?.Invoke();
        loser.endMoveRoutineEvent?.Invoke();
        // 双方动作整理结束后再处理副目标死亡与手动/自动退出。
        SecondaryPlayback.EndMotion();
    }

    private bool StopMoving()
    {
        return true;
    }

    private bool MoveForward(BattleUnitView self, Vector3 src, Vector3 dst, float speed = 1f)
    {
        Vector3 vector = (dst - src).normalized * SingletonBehavior<HexagonalMapManager>.Instance.tileSize * 2f;
        Vector3 viewPos = self.WorldPosition + vector;
        if (SingletonBehavior<HexagonalMapManager>.Instance.IsWall(viewPos) != HexagonalMapManager.WallDirection.NONE) return true;
        if (dst.y > 0f) dst.y = 0f;
        Vector3 currentVelocity = Vector3.zero;
        self.WorldPosition = Vector3.SmoothDamp(self.WorldPosition, dst, ref currentVelocity, _curMovingSmoothTime / speed);
        return Vector3.Distance(self.WorldPosition, dst) < 0.1f;
    }

    private bool MoveBack(BattleUnitView self, Vector3 src, Vector3 dst, float speed = 1f)
    {
        Vector3 vector = (dst - src).normalized * SingletonBehavior<HexagonalMapManager>.Instance.tileSize;
        Vector3 viewPos = self.WorldPosition + vector;
        if (SingletonBehavior<HexagonalMapManager>.Instance.IsWall(viewPos) != HexagonalMapManager.WallDirection.NONE) return true;
        if (dst.y > 0f) dst.y = 0f;
        Vector3 currentVelocity = Vector3.zero;
        self.WorldPosition = Vector3.SmoothDamp(self.WorldPosition, dst, ref currentVelocity, _curMovingSmoothTime / speed);
        return Vector3.Distance(self.WorldPosition, dst) < 0.1f;
    }

    private bool MoveToOponnent(BattleUnitView self, BattleUnitView opponent, Vector3 src, float speed = 1f)
    {
        Vector3 vector = opponent.WorldPosition + new Vector3(0f, 0f, -0.1f);
        _ = (vector - src).normalized * SingletonBehavior<HexagonalMapManager>.Instance.tileSize * 2f;
        Vector3 worldPosition = opponent.WorldPosition;
        if (SingletonBehavior<HexagonalMapManager>.Instance.IsWall(worldPosition) != HexagonalMapManager.WallDirection.NONE) return true;
        if (vector.y > 0f) vector.y = 0f;
        Vector3 currentVelocity = Vector3.zero;
        self.WorldPosition = Vector3.SmoothDamp(self.WorldPosition, vector, ref currentVelocity, _curMovingSmoothTime / speed);
        return Vector3.Distance(self.WorldPosition, vector) < 0.1f;
    }

    private bool MoveToOponnent_Near(BattleUnitView self, BattleUnitView opponent, Vector3 src, float speed = 1f)
    {
        Vector3 vector = opponent.WorldPosition + new Vector3(0f, 0f, -0.2f);
        _ = (vector - src).normalized * SingletonBehavior<HexagonalMapManager>.Instance.tileSize * 2f;
        Vector3 worldPosition = opponent.WorldPosition;
        if (SingletonBehavior<HexagonalMapManager>.Instance.IsWall(worldPosition) != HexagonalMapManager.WallDirection.NONE) return true;
        if (vector.y > 0f) vector.y = 0f;
        Vector3 currentVelocity = Vector3.zero;
        self.WorldPosition = Vector3.SmoothDamp(self.WorldPosition, vector, ref currentVelocity, _curMovingSmoothTime / speed);
        return Vector3.Distance(self.WorldPosition, vector) < 2f;
    }

    private bool MoveToOponnent_Near2(BattleUnitView self, BattleUnitView opponent, Vector3 src, float speed = 1f)
    {
        Vector3 vector = opponent.WorldPosition + new Vector3(0f, 0f, -0.1f);
        _ = (vector - src).normalized * SingletonBehavior<HexagonalMapManager>.Instance.tileSize * 2f;
        Vector3 worldPosition = opponent.WorldPosition;
        if (SingletonBehavior<HexagonalMapManager>.Instance.IsWall(worldPosition) != HexagonalMapManager.WallDirection.NONE) return true;
        if (vector.y > 0f) vector.y = 0f;
        Vector3 currentVelocity = Vector3.zero;
        self.WorldPosition = Vector3.SmoothDamp(self.WorldPosition, vector, ref currentVelocity, _curMovingSmoothTime / speed);
        return Vector3.Distance(self.WorldPosition, vector) < 7f;
    }

    private bool TeleportFront(BattleUnitView self, BattleUnitView opponent, float distance, float dstRotation)
    {
        Vector3 worldPosition = self.WorldPosition;
        Vector3 worldPosition2 = opponent.WorldPosition;
        Vector3 vector = Quaternion.AngleAxis(dstRotation, Vector3.up) * (worldPosition2 - worldPosition).normalized * SingletonBehavior<HexagonalMapManager>.Instance.tileSize;
        Vector3 vector2 = opponent.WorldPosition - vector * distance;
        if (vector2.y > 0f) vector2.y = 0f;
        Vector3 viewPos = vector2;
        if (SingletonBehavior<HexagonalMapManager>.Instance.IsWall(viewPos) != HexagonalMapManager.WallDirection.NONE) return true;
        _ = Vector3.zero;
        self.WorldPosition = vector2;
        return true;
    }

    private bool TeleportBack(BattleUnitView self, BattleUnitView opponent, float distance, float dstRotation)
    {
        Vector3 worldPosition = self.WorldPosition;
        Vector3 worldPosition2 = opponent.WorldPosition;
        Vector3 vector = Quaternion.AngleAxis(dstRotation, Vector3.up) * (worldPosition - worldPosition2).normalized * SingletonBehavior<HexagonalMapManager>.Instance.tileSize;
        Vector3 vector2 = opponent.WorldPosition - vector * distance;
        if (vector2.y > 0f) vector2.y = 0f;
        Vector3 viewPos = vector2;
        if (SingletonBehavior<HexagonalMapManager>.Instance.IsWall(viewPos) != HexagonalMapManager.WallDirection.NONE) return true;
        _ = Vector3.zero;
        self.WorldPosition = vector2;
        return true;
    }

    private bool Knockback(BattleUnitView self, BattleUnitView opponent)
    {
        return self.model.moveDetail.isArrived;
    }

    private bool BlinkBack(BattleUnitView self, Vector3 dst)
    {
        dst = self.model.moveDetail.CalcBlinkDst(dst);
        self.model.moveDetail.Blink(dst);
        return true;
    }

    private bool Confrontation(BattleUnitView self, BattleUnitView opponent, Vector3 dst, bool clockWise, float speed = 2f)
    {
        Vector3 normalized = (opponent.WorldPosition - self.WorldPosition).normalized;
        Vector3 zero = Vector3.zero;
        if (clockWise)
        {
            zero.x = normalized.z;
            zero.y = normalized.y;
            zero.z = 0f - normalized.x;
        }
        else
        {
            zero.x = 0f - normalized.z;
            zero.y = normalized.y;
            zero.z = normalized.x;
        }
        if (SingletonBehavior<HexagonalMapManager>.Instance.IsWall(self.WorldPosition + zero) != HexagonalMapManager.WallDirection.NONE)
        {
            zero.x *= -1f;
            zero.z *= -1f;
        }
        self.WorldPosition += zero * MotionDelta * speed;
        _elapsedConfrontation += MotionDelta;
        if (_elapsedConfrontation >= 1.25f) return true;
        return false;
    }

    private bool Competition(BattleUnitView self, Vector3 dst)
    {
        _elapsedCompetition += MotionDelta;
        if (_elapsedCompetition < 0.5f) return false;
        return true;
    }

    private void EndBehaviourResult()
    {
        _curRencounterState = RencounterState.MoveDice;
        _enemyBehaviourResultQueue.Dequeue();
        _librarianBehaviourResultQueue.Dequeue();

        _currentEnemyBehaviourResult = null;
        _currentLibrarianBehaviourResult = null;
        _curDelayRatio -= _delayRatio * 0.5f;
        if (_curDelayRatio < 0f) _curDelayRatio = 0f;
        _countForNodelay++;
        if (_countForNodelay >= 10) _countForNodelay = 10;
    }

    public void PlaySound_draw()
    {
        PlaySound(Draw_AtkAtk);
    }

    private void PlaySound(AudioClip clip)
    {
        BattleEffectSound battleEffectSound = UnityEngine.Object.Instantiate(SingletonBehavior<BattleSoundManager>.Instance.effectSoundPrefab, base.transform);
        float volume = 1f;
        if (SingletonBehavior<BattleSoundManager>.Instance != null) volume = SingletonBehavior<BattleSoundManager>.Instance.VolumeFX;
        battleEffectSound.Init(clip, volume);
    }

    private void Complete(bool restorePositions = false)
    {
        var session = action;
        if (session == null)
        {
            return;
        }

        action = null;
        StopAllCoroutines();
        session.PlaybackStopped();
        _actionAfterBehaviourIsRunning = false;
        _bPause = false;
        motionDelta = 0f;
        try
        {
            SecondaryPlayback?.Clear(restorePositions);
        }
        finally
        {
            try
            {
                GetComponent<RencounterEffects>()?.End(interrupted: restorePositions);
            }
            finally
            {
                CustomCombatRuntime.Complete(session);
            }
        }
    }

    private void OnDisable()
    {
        EndRencounterForcely();
    }

    private void OnDestroy()
    {
        EndRencounterForcely();
    }
}

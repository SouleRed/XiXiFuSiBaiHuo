using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using 裁缝之王.Presentation.Actions;

using 裁缝之王.Presentation.Effects;

namespace 裁缝之王.Presentation.CustomCombat;

// 动作组提供副目标姿态，本组件负责演出切层和逐骰结果显示；基础相机由对抗管理器跟踪双方。
[DefaultExecutionOrder(10000)]
public sealed class CustomCombatPlayback : MonoBehaviour
{
    private sealed class Body
    {
        public BattleUnitView View;

        public Vector3 Position;

        public int Index;

        public CustomCombatActionPlan.TargetAction Action;

        public Vector3 Translation;

        public Quaternion RotationTransform;

        public float DamageTime;

        public ActionDetail Motion;

        public bool LockY, CanvasEnabled, Focused, Dead;


        public Direction Direction;

        public BattleUnitView.RotationState Rotation;

        public readonly Dictionary<Transform, int> Layers = new();

        public Color SpriteColor;

        public readonly Dictionary<SpriteRenderer, Color> Colors = new();

        public readonly Dictionary<Canvas, KeyValuePair<string, int>> Sorting = new();
    }

    private readonly Dictionary<BattleUnitModel, Body> bodies = new();

    private CustomCombatRencounterManager manager;

    // 聚焦可以跨骰保留，动作计划只在本次正式动作内运行。
    private CustomCombatResolution.Frame focusedFrame;

    private CustomCombatActionPlan plan;

    // 聚焦上下文可跨动作保留；动作计划不再存放切层指令。
    private CustomCombatActionPlan focusPlan;

    private RencounterManager.MovingAction mainAction;

    public bool HasRunningDeath => bodies.Values.Any(body => body.View && body.View.IsRunningDeadEffect);

    public static CustomCombatPlayback Get(CustomCombatRencounterManager owner)
    {
        var playback = owner.GetComponent<CustomCombatPlayback>();
        if (!playback) playback = owner.gameObject.AddComponent<CustomCombatPlayback>();
        playback.manager = owner;
        return playback;
    }

    public void Capture(BattleUnitView enemy, BattleUnitView librarian)
    {
        Clear();
        foreach (var actor in new[] { enemy, librarian })
        {
            foreach (var result in actor.model.battleCardResultLog.BehaviourResultList)
            {
                var frame = CustomCombatResolution.Find(result);
                if (frame == null) continue;
                foreach (var unit in frame.Page.Targets)
                {
                    if (unit == frame.Main || !unit.view || bodies.ContainsKey(unit)) continue;
                    var view = unit.view;
                    var body = new Body
                    {
                        View = view,
                        Position = view.WorldPosition,
                        Motion = view.charAppearance.GetCurrentMotion().actionDetail,
                        LockY = view._bLockPosY,
                        CanvasEnabled = view.unitBottomStatUI._canvas.enabled,
                        Direction = view.model.direction,
                        Rotation = view.rotState,
                        Translation = view.characterTranslationCenter.localPosition,
                        RotationTransform = view.characterRotationCenter.localRotation,
                        DamageTime = view._remainDmgTime,
                    };
                    foreach (var transform in view.GetComponentsInChildren<Transform>(true)) body.Layers.Add(transform, transform.gameObject.layer);
                    foreach (var canvas in view.unitBottomStatUI.GetComponentsInChildren<Canvas>(true)) body.Sorting.Add(canvas, new KeyValuePair<string, int>(canvas.sortingLayerName, canvas.sortingOrder));
                    bodies.Add(unit, body);
                    var initial = frame.Page.Initial[unit];
                    view.unitBottomStatUI.UpdateStatUI(initial.Hp, initial.Break, initial.Buffs);
                    SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfile(unit, unit.faction, initial.Hp, initial.Break, initial.Buffs);
                }
            }
        }
    }

    // 动作组在PrependEntry之前绑定；切入包装保存本回调，直到正式动作开始才执行。
    internal static void BindFormal(ref RencounterManager.ActionAfterBehaviour self, CustomCombatActionPlan actionPlan)
    {
        // 原版管理器下保留原回调和动作列表，不访问专用管理器或注册额外事件。
        var owner = CustomCombatRuntime.Playback;
        if (!owner || !owner.IsRunning || actionPlan == null) return;
        var frame = CustomCombatResolution.Find(self.behaviourResultData);
        if (frame == null) return;
        var playback = owner.SecondaryPlayback;
        playback.Prepare(frame, actionPlan);
        var begin = self.startMoveRoutineEvent;
        self.startMoveRoutineEvent = () =>
        {
            if (playback && owner.IsRunning)
            {
                playback.BeginFormal(frame, actionPlan);
                actionPlan.FormalStart?.Invoke(owner, frame.Record);
            }
            begin?.Invoke();
        };
    }

    public void BeforeRoll(BattleCardBehaviourResult enemy, BattleCardBehaviourResult librarian)
    {
        if (focusedFrame == null) return;
        var next = CustomCombatResolution.Find(enemy) ?? CustomCombatResolution.Find(librarian);
        if (next == null || next.Page != focusedFrame.Page || !KingContinuation.IsExpected(next.Record)) manager.LeaveSecondaryFocus();
    }

    private CustomCombatResolution.Frame preparedFrame;

    private CustomCombatActionPlan preparedPlan;

    private void Prepare(CustomCombatResolution.Frame frame, CustomCombatActionPlan actionPlan)
    {
        preparedFrame = frame;
        preparedPlan = actionPlan;
    }

    internal bool HasPlan(BattleCardBehaviourResult record) => preparedFrame?.Record == record && preparedPlan != null;

    internal void BeginMotion(BattleCardBehaviourResult enemy, BattleCardBehaviourResult librarian)
    {
        var frame = CustomCombatResolution.Find(enemy) ?? CustomCombatResolution.Find(librarian);
        if (frame == null) return;
        var actionPlan = preparedFrame == frame ? preparedPlan : new CustomCombatActionPlan();
        Prepare(frame, actionPlan);
        if (actionPlan.AutoFocus)
        {
            BeginFormal(frame, actionPlan);
            manager.EnterSecondaryFocus(frame.Record);
        }
    }

    internal void EndMotion()
    {
        var frame = preparedFrame;
        var actionPlan = preparedPlan;
        if (frame == null || actionPlan == null) return;
        FinishFormal(frame);
        if (actionPlan.AutoFocus) manager.LeaveSecondaryFocus();
        else actionPlan.FormalEnd?.Invoke(manager, frame.Record);
        preparedFrame = null;
        preparedPlan = null;
    }

    private void BeginFormal(CustomCombatResolution.Frame frame, CustomCombatActionPlan actionPlan)
    {
        if (!manager || !manager.IsRunning) return;
        if (focusedFrame != null && focusedFrame.Page != frame.Page) manager.LeaveSecondaryFocus();
        focusedFrame = frame;
        plan = focusPlan = actionPlan;
        mainAction = null;
        foreach (var hit in frame.Hits)
        {
            if (!bodies.TryGetValue(hit.Unit, out Body body) || body.Dead || !body.View) continue;
            body.Index = frame.Page.Targets.IndexOf(hit.Unit) - 1;
            body.Action = null;
        }
        ApplyPlan();
    }

    private void ApplyPlan()
    {
        if (focusedFrame == null || !focusedFrame.Main.view || plan == null) return;
        var pose = plan.Sample?.Invoke() ?? default(CustomCombatActionPlan.TargetPose);
        foreach (var body in bodies.Values)
        {
            if (!body.Focused || body.Dead || !body.View) continue;
            var view = body.View;
            if (!pose.Motion.HasValue && body.Action != null && view.charAppearance.GetCurrentMotion().actionDetail != body.Action.Motion) view.charAppearance.ChangeMotion(body.Action.Motion);
            ApplyPose(view, pose);
        }
    }

    private static void ApplyPose(BattleUnitView view, CustomCombatActionPlan.TargetPose pose)
    {
        if (pose.Motion.HasValue && view.charAppearance.GetCurrentMotionDetail() != pose.Motion.Value) view.charAppearance.ChangeMotion(pose.Motion.Value);
        if (pose.DamageTime.HasValue) view._remainDmgTime = pose.DamageTime.Value;
    }

    internal void ForEachFocusedTarget(BattleCardBehaviourResult record, Action<BattleUnitView> apply)
    {
        if (plan == null || focusedFrame.Record != record) return;
        foreach (var hit in focusedFrame.Hits)
            if (bodies.TryGetValue(hit.Unit, out var body) && body.Focused && !body.Dead && body.View)
                apply(body.View);
    }

    internal void BeginMainAction(BattleUnitView view, RencounterManager.MovingAction action)
    {
        if (plan == null || focusedFrame?.Main.view != view || action == null || mainAction == action) return;
        if (!plan.Actions.TryGetValue(action, out var targetAction)) return;
        mainAction = action;
        foreach (var body in bodies.Values)
        {
            if (!body.Focused || body.Dead || !body.View) continue;
            body.Action = targetAction;
            if (body.Action == null) continue;
            body.View.charAppearance.ChangeMotion(body.Action.Motion);
            body.View.KnockDown(body.Action.MoveState == CharMoveState.KnockDown);
        }
    }

    internal void EnterFocus(BattleCardBehaviourResult record)
    {
        if (focusedFrame?.Record != record || focusPlan == null) return;
        // 同一主线程调用内先布置所有新目标，再统一切层，无协程或帧间等待。
        // 先解除Y锁再定位，确保升空动作前的初始高度不会被旧锁覆盖。
        var anchor = focusedFrame.Main.view.WorldPosition;
        var entering = new List<Body>();
        foreach (var hit in focusedFrame.Hits)
        {
            if (bodies.TryGetValue(hit.Unit, out Body body) && !body.Dead && body.View && !body.Focused)
                entering.Add(body);
        }
        if (entering.Count == 0) return;
        var positions = focusPlan.InitialPositions?.Invoke(entering.Count);
        for (int i = 0; i < entering.Count; i++)
        {
            var body = entering[i];
            body.View.model.moveDetail.Stop();
            body.View.LockPosY(false);
            if (positions != null) body.View.WorldPosition = positions[i];
            else if (focusPlan.InitialOffset != null) body.View.WorldPosition = anchor + focusPlan.InitialOffset(body.Index);
        }
        foreach (var body in entering) EnterFocus(body);
    }

    private void EnterFocus(Body body)
    {
        // 接续只更新轨道，不重复靠拢或覆盖最初的恢复快照。
        if (body.Focused) return;
        var view = body.View;
        body.Focused = true;
        view.model.moveDetail.Stop();
        view.LockPosY(false);
        view.charAppearance.ChangeLayer("UI_WORLD");
        body.SpriteColor = view.charAppearance.CurSpritesColor;
        body.Colors.Clear();
        RencounterFocus.Highlight(view.charAppearance, body.Colors);
        view.unitBottomStatUI.ChangeLayer("UI_WORLD", "UI_WORLD");
        view.unitBottomStatUI.ChangeChildCanvasOrder("Character", 0);
        view.unitBottomStatUI.EnableCanvas(true);
        view.model.OnLayerChanged("UI_WORLD");
    }

    public void PrintCurrent(BattleCardBehaviourResult enemy, BattleCardBehaviourResult librarian)
    {
        var frame = CustomCombatResolution.Find(enemy) ?? CustomCombatResolution.Find(librarian);
        if (frame != null) Print(frame);
    }

    private void Print(CustomCombatResolution.Frame frame)
    {
        if (frame.Printed || !manager || !manager.IsRunning) return;
        frame.Printed = true;
        foreach (var hit in frame.Hits)
        {
            if (!hit.Unit.view) continue;
            var record = hit.Receiver;
            var view = hit.Unit.view;
            {
                // 只显示结算数字和状态，不消费副目标的printAtk/printDamaged粒子事件。
                foreach (var damage in record.dmgListTaken) SingletonBehavior<AttackEffectManager>.Instance.CreateDamagedTextEffect(damage.dmg, damage.damagedType, hit.Unit, frame.Page.Card.owner, damage.atkResist, false);
                foreach (var damage in record.breakListTaken) view.BreakDamaged(damage.dmg, damage.damagedType, frame.Page.Card.owner, damage.atkResist);
                foreach (int recovered in record.recoveredHpList) view.RecoverHp(recovered);
                foreach (int recovered in record.recoveredBpList) view.RecoverBp(recovered);
            }
            foreach (var coin in record.GetAddedEmotionCoins()) SingletonBehavior<BattleManagerUI>.Instance.ui_battleEmotionCoinUI.OnAcquireCoin(hit.Unit, coin, 1);
            view.unitBottomStatUI.UpdateStatUI(record.curHp, record.curBreakGauge, record.bufDataList);
            SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfile(hit.Unit, hit.Unit.faction, record.curHp, record.curBreakGauge, record.bufDataList);
            var originalTarget = frame.Page.Card.target;
            try
            {
                frame.Page.Card.target = hit.Unit;
                hit.Sender.afterActionEvent?.Invoke();
                {
                    hit.Sender.succeedAtkEvent?.Invoke();
                    record.takeDamageEvent?.Invoke();
                }
                hit.Sender.takeDamageEvent?.Invoke();
            }
            finally
            {
                frame.Page.Card.target = originalTarget;
            }
        }
    }

    private void FinishFormal(CustomCombatResolution.Frame frame)
    {
        if (focusedFrame != frame || !manager || !manager.IsRunning) return;
        Print(frame);
        ApplyPlan();
        // 结束当前计划但保留聚焦状态；接续等待期间冻结最后姿态。
        plan = null;
        mainAction = null;
        foreach (var hit in frame.Hits)
        {
            if (!hit.Receiver.isDead || !bodies.TryGetValue(hit.Unit, out Body body) || body.Dead) continue;
            body.Dead = true;
            if (body.View) body.View.StartDeadEffect();
        }
    }

    // 退出聚焦只恢复显示与姿态，保留聚集和击退后的世界位置。
    private static void LeaveFocus(Body body)
    {
        if (!body.Focused || !body.View) return;
        var view = body.View;
        {
            view.model.moveDetail.Stop();
            view.LockPosY(body.LockY);
            view.model.direction = body.Direction;
            view.rotState = body.Rotation;
            view.characterTranslationCenter.localPosition = body.Translation;
            view.characterRotationCenter.localRotation = body.RotationTransform;
            view._remainDmgTime = body.DamageTime;
        }
        view.charAppearance.ChangeLayer("Character");
        view.unitBottomStatUI.ChangeLayer("CharacterUI", "Character");
        view.model.OnLayerChanged("Character");
        foreach (var pair in body.Layers) if (pair.Key) pair.Key.gameObject.layer = pair.Value;
        foreach (var pair in body.Colors) if (pair.Key) pair.Key.color = pair.Value;
        view.charAppearance._curSpriteColor = body.SpriteColor;
        foreach (var pair in body.Sorting)
        {
            if (!pair.Key) continue;
            pair.Key.sortingLayerName = pair.Value.Key;
            pair.Key.sortingOrder = pair.Value.Value;
        }
        // 使用已播放的死亡状态，避免恢复快照重新打开死亡流程已关闭的UI。
        view.unitBottomStatUI.EnableCanvas(!body.Dead && body.CanvasEnabled);
        if (!body.Dead) view.charAppearance.ChangeMotion(body.Motion);
        body.Focused = false;
        body.Action = null;
    }

    internal void EndFocus()
    {
        try
        {
            foreach (var body in bodies.Values) LeaveFocus(body);
        }
        finally
        {
            focusedFrame = null;
            plan = focusPlan = null;
            mainAction = null;
        }
    }

    public void Clear(bool restorePositions = false)
    {
        try
        {
            manager.LeaveSecondaryFocus();
            if (restorePositions)
            {
                // 异常中断才回退位置，包含此前已经正常退出聚焦的副目标。
                foreach (var body in bodies.Values)
                {
                    if (!body.View) continue;
                    body.View.model.moveDetail.Stop();
                    body.View.WorldPosition = body.Position;
                }
            }
            foreach (var body in bodies.Values)
                if (body.View && !body.View.model.IsDead()) body.View.unitBottomStatUI.EnableCanvas(true);
        }
        finally
        {
            preparedFrame = null;
            preparedPlan = null;
            bodies.Clear();
        }
    }

    private void LateUpdate()
    {
        if (!manager || !manager.IsRunning || plan == null) return;
        try
        {
            ApplyPlan();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            manager.EndRencounterForcely();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UI;
using UnityEngine;

namespace 裁缝之王.Presentation.CustomCombat;

internal interface ICustomAttackCapacity
{
    int AttackCapacity { get; }
}

internal interface ICustomRandomBarrage : ICustomAttackCapacity
{
}

// 目标只存原生 target/subTargets；交互状态只负责玩家当前正在编辑的多目标出牌。
internal static class CustomCombatTargeting
{
    private static BattleUnitCardsInHandUI ui;

    private static BattlePlayingCardDataInUnitModel editing;

    private static int rightConfirmFrame = -1;

    private static readonly Func<int, bool> GetMouseButtonDown = ResolveGetMouseButtonDown();

    private static Func<int, bool> ResolveGetMouseButtonDown()
    {
        var type = Type.GetType("UnityEngine.Input, UnityEngine.InputLegacyModule")
            ?? Type.GetType("UnityEngine.Input, UnityEngine");
        var method = type?.GetMethod("GetMouseButtonDown", new[] { typeof(int) });
        return method != null ? (Func<int, bool>)Delegate.CreateDelegate(typeof(Func<int, bool>), method) : null;
    }

    internal static bool Active => ui != null && editing != null;

    private static ICustomAttackCapacity GetAbility(BattleDiceCardModel card)
    {
        if (card == null) return null;
        if (card._script == null) card._script = card.CreateDiceCardSelfAbilityScript();
        return card._script as ICustomAttackCapacity;
    }

    private static ICustomAttackCapacity GetAbility(BattlePlayingCardDataInUnitModel page)
        => page?.cardAbility as ICustomAttackCapacity ?? GetAbility(page?.card);

    private static int Capacity(BattlePlayingCardDataInUnitModel page)
        => GetAbility(page) is { AttackCapacity: > 1 } ability ? ability.AttackCapacity : 1;

    internal static bool IsCapacityPage(BattleDiceCardModel card)
        => GetAbility(card) is { AttackCapacity: > 1 };

    internal static bool IsCapacityPage(BattlePlayingCardDataInUnitModel page)
        => Capacity(page) > 1;

    internal static bool IsRandomBarragePage(BattlePlayingCardDataInUnitModel page)
        => GetAbility(page) is ICustomRandomBarrage { AttackCapacity: > 1 };

    internal static void FillAutomatic(BattlePlayingCardSlotDetail slots, BattleDiceCardModel card)
    {
        if (slots == null || !IsCapacityPage(card)) return;
        var page = slots.cardAry.FirstOrDefault(data => data?.card == card);
        AssignTargets(page);
    }

    internal static void FillPlayerAuto(BattleUnitModel owner, int slotOrder)
    {
        var cards = owner?.cardSlotDetail?.cardAry;
        if (cards == null || slotOrder < 0 || slotOrder >= cards.Count) return;
        var page = cards[slotOrder];
        if (!IsCapacityPage(page)) return;
        AssignTargets(page, allowPlayer: true);
        SingletonBehavior<BattleManagerUI>.Instance?.ui_TargetArrow?.UpdateTargetList();
    }

    internal static void AssignTargets(BattlePlayingCardDataInUnitModel page, bool allowPlayer = false)
    {
        if (page?.owner == null || (!allowPlayer && page.owner.faction == Faction.Player)) return;
        int limit = Capacity(page) - 1;
        if (limit <= 0 || page.target == null) return;

        page.subTargets ??= new List<BattlePlayingCardDataInUnitModel.SubTarget>();
        page.subTargets.Clear();
        var candidates = Candidates(page, randomSlot: true).ToList();

        while (page.subTargets.Count < limit && candidates.Count > 0)
        {
            int index = RandomUtil.Range(0, candidates.Count - 1);
            page.subTargets.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
    }

    internal static bool HasSecondaryTargets(BattlePlayingCardDataInUnitModel page)
        => IsCapacityPage(page) && Valid(page, page.target, page.targetSlotOrder)
            && page.subTargets != null && page.subTargets.Any(sub => ValidSecondary(page, sub));

    private static IEnumerable<BattlePlayingCardDataInUnitModel.SubTarget> Candidates(BattlePlayingCardDataInUnitModel page, bool randomSlot = false)
    {
        bool allowFriendly = page.owner.TeamKill();
        var units = BattleObjectManager.instance.GetAliveList(false)
            .Where(unit => unit != page.target && (allowFriendly || unit.faction != page.owner.faction));
        foreach (var unit in units.Distinct())
        {
            if (page.subTargets != null && page.subTargets.Any(sub => sub?.target == unit)) continue;
            var slots = Enumerable.Range(0, unit.speedDiceResult?.Count ?? 0)
                .Where(slot => ValidSecondary(page, unit, slot))
                .ToList();
            if (slots.Count > 0)
            {
                yield return new BattlePlayingCardDataInUnitModel.SubTarget
                {
                    target = unit,
                    targetSlotOrder = randomSlot ? RandomUtil.SelectOne(slots) : slots[0],
                };
            }
        }
    }

    private static bool Valid(BattlePlayingCardDataInUnitModel page, BattleUnitModel unit, int slot)
    {
        return page?.owner != null && page.card != null && unit != null && unit != page.owner
            && !unit.IsDead() && !unit.IsExtinction()
            && slot >= 0 && slot < (unit.speedDiceResult?.Count ?? 0)
            && unit.IsTargetable(page.owner)
            && (slot != unit.speedDiceResult.Count - 1 || unit.IsTargetable_theLast())
            && BattleUnitModel.IsTargetableUnit(page.card, page.owner, unit, slot);
    }

    private static bool ValidSecondary(BattlePlayingCardDataInUnitModel page, BattlePlayingCardDataInUnitModel.SubTarget sub)
        => sub != null && ValidSecondary(page, sub.target, sub.targetSlotOrder);

    private static bool ValidSecondary(BattlePlayingCardDataInUnitModel page, BattleUnitModel unit, int slot)
        => unit != page.target && Valid(page, unit, slot) && unit.speedDiceResult[slot].isControlable;

    internal static bool NormalizeTargets(BattlePlayingCardDataInUnitModel page)
    {
        page.subTargets ??= new List<BattlePlayingCardDataInUnitModel.SubTarget>();
        if (!Valid(page, page.target, page.targetSlotOrder))
        {
            page.subTargets.Clear();
            return false;
        }

        int limit = Capacity(page) - 1;
        var seen = new HashSet<BattleUnitModel>();
        page.subTargets.RemoveAll(sub => seen.Count >= limit || !ValidSecondary(page, sub) || !seen.Add(sub.target));
        return true;
    }

    internal static bool Apply(BattleUnitCardsInHandUI hand, BattleUnitModel target, int slot)
    {
        var selected = hand.GetSelectedCard();
        var owner = hand._selectedUnit;
        if (owner?.faction != Faction.Player || !IsCapacityPage(selected?.CardModel)) return false;
        if (Active && (ui != hand || editing.owner != owner || editing.card != selected.CardModel)) Reset();
        if (!Active)
        {
            // 通过原版 AddCard 完成费用、个人书页和速度骰绑定，只接管后续追加目标选择。
            var probe = new BattlePlayingCardDataInUnitModel { owner = owner, card = selected.CardModel };
            if (!Valid(probe, target, slot)) return true;
            owner.cardSlotDetail.AddCard(selected.CardModel, target, slot);
            editing = owner.cardSlotDetail.cardAry.FirstOrDefault(data => data?.card == selected.CardModel);
            if (editing == null) return true;
            editing.subTargets = new List<BattlePlayingCardDataInUnitModel.SubTarget>();
            ui = hand;
            foreach (var cardUI in hand.GetCardUIList())
            {
                if (!cardUI || cardUI == selected) continue;
                cardUI.SetDefault();
                cardUI.ResetSiblingIndex();
            }
        }
        else
        {
            if (!ValidSecondary(editing, target, slot) || editing.subTargets.Any(sub => sub?.target == target)) return true;
            editing.subTargets.Add(new BattlePlayingCardDataInUnitModel.SubTarget { target = target, targetSlotOrder = slot });
        }
        SingletonBehavior<BattleSoundManager>.Instance.PlaySound(EffectSoundType.CARD_APPLY);
        SingletonBehavior<BattleManagerUI>.Instance.ui_TargetArrow.UpdateTargetList();
        if (editing.subTargets.Count >= Capacity(editing) - 1 || !Candidates(editing).Any()) Finish();
        return true;
    }

    internal static bool Update(BattleUnitCardsInHandUI hand)
    {
        if (!Active || ui != hand) return true;
        if (hand.GetSelectedCard()?.CardModel != editing.card || hand._selectedUnit != editing.owner
            || !editing.owner.cardSlotDetail.cardAry.Contains(editing))
        {
            Reset();
            return true;
        }
        if (GetMouseButtonDown == null || !GetMouseButtonDown(1)) return true;
        HandleRightClick();
        return false;
    }

    // 右键无论落在空白还是速度骰上都只确认，不穿透到原版卸牌操作。
    internal static bool HandleRightClick()
    {
        if (rightConfirmFrame == Time.frameCount) return true;
        if (!Active) return false;
        rightConfirmFrame = Time.frameCount;
        Finish();
        return true;
    }

    internal static bool Blocks(BattleDiceCardUI card) => Active && card != null && card != ui.GetSelectedCard();

    internal static void Finish()
    {
        if (!Active) return;
        var hand = ui;
        var owner = editing.owner;
        var selected = hand.GetSelectedCard();
        Reset();
        if (selected)
        {
            selected.SetDefault();
            selected.ResetSiblingIndex();
            selected.gameObject.SetActive(false);
        }
        owner.view.speedDiceSetterUI.ReleaseSelection();
        hand._selectedUnit = null;
        hand.SetSelectedCardUI(null);
        hand.UnblockOtherUnitsDice();
        hand.Deactivate();
        var battleUI = SingletonBehavior<BattleManagerUI>.Instance;
        battleUI.ui_TargetArrow.UpdateTargetList();
        battleUI.selectedAllyDice = null;
        battleUI.selectedEnemyDice = null;
    }

    internal static void OnDeactivate(BattleUnitCardsInHandUI hand)
    {
        if (ui == hand) Reset();
    }

    internal static void Reset()
    {
        ui = null;
        editing = null;
    }
}

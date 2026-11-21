using System.Collections.Generic;
using System.Linq;
using 裁缝之王.Presentation.Actions;

namespace 裁缝之王.Presentation.CustomCombat;

internal static class CustomCombatTargeting
{
    private static int Capacity(BattleDiceCardModel card)
    {
        var id = card?.GetID();
        if (id == null || id.packageId != KingBehaviourAction.PackageId) return 1;
        return id.id switch
        {
            2 => 2,
            4 or 7 or 10 => 5,
            _ => 1,
        };
    }

    internal static bool IsCapacityPage(BattleDiceCardModel card) => Capacity(card) > 1;

    internal static void AssignTargets(BattlePlayingCardDataInUnitModel page)
    {
        int limit = Capacity(page.card) - 1;
        if (limit <= 0 || page.target == null) return;

        page.subTargets ??= new List<BattlePlayingCardDataInUnitModel.SubTarget>();
        page.subTargets.Clear();
        bool allowFriendly = page.owner.TeamKill();
        var candidates = BattleObjectManager.instance.GetAliveList(false)
            .Where(unit => unit != page.target && (allowFriendly || unit.faction != page.owner.faction))
            .Select(unit => (Unit: unit, Slots: Enumerable.Range(0, unit.speedDiceResult?.Count ?? 0)
                .Where(slot => ValidSecondary(page, unit, slot)).ToList()))
            .Where(candidate => candidate.Slots.Count > 0)
            .ToList();

        while (page.subTargets.Count < limit && candidates.Count > 0)
        {
            int index = RandomUtil.Range(0, candidates.Count - 1);
            var (unit, slots) = candidates[index];
            page.subTargets.Add(new BattlePlayingCardDataInUnitModel.SubTarget
            {
                target = unit,
                targetSlotOrder = RandomUtil.SelectOne(slots),
            });
            candidates.RemoveAt(index);
        }
    }

    internal static bool HasSecondaryTargets(BattlePlayingCardDataInUnitModel page)
        => IsCapacityPage(page?.card) && Valid(page, page.target, page.targetSlotOrder)
            && page.subTargets != null && page.subTargets.Any(sub => ValidSecondary(page, sub));

    private static bool Valid(BattlePlayingCardDataInUnitModel page, BattleUnitModel unit, int slot)
    {
        return unit != null && unit != page.owner && !unit.IsDead() && !unit.IsExtinction()
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

        int limit = Capacity(page.card) - 1;
        var seen = new HashSet<BattleUnitModel>();
        page.subTargets.RemoveAll(sub => seen.Count >= limit || !ValidSecondary(page, sub) || !seen.Add(sub.target));
        return true;
    }
}

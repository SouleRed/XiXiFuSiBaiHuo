using LOR_DiceSystem;
using System.Collections.Generic;

namespace 裁缝之王.Shared.Tool;

public static class BattleTools
{
    public static BattleDiceCardModel GetCard(LorId cardId, BattleUnitModel owner)
    {
        DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(cardId, false);
        if (cardItem == null) return null;

        BattleDiceCardModel card = BattleDiceCardModel.CreatePlayingCard(cardItem);
        card.owner = owner;
        return card;
    }

    public static void AddCardAttack(BattleUnitModel owner, BattleUnitModel target, LorId cardId)
    {
        BattleDiceCardModel card = GetCard(cardId, owner);
        if (card == null) return;

        BattlePlayingCardDataInUnitModel playingCard = new()
        {
            card = card,
            owner = owner,
            speedDiceResultValue = 9999,
            target = target,
            subTargets = new List<BattlePlayingCardDataInUnitModel.SubTarget>(),
            earlyTarget = target,
            earlyTargetOrder = -1,
            cardAbility = card.CreateDiceCardSelfAbilityScript()
        };
        if (playingCard.cardAbility != null)
        {
            playingCard.cardAbility.card = playingCard;
            playingCard.cardAbility.OnApplyCard();
        }
        playingCard.ResetCardQueue();
        Singleton<StageController>.Instance.AddAllCardListInBattle(playingCard, target, -1);

        CardRange ranged = playingCard.card.GetSpec().Ranged;
        if (ranged is CardRange.FarArea or CardRange.FarAreaEach)
        {
            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(owner.faction == Faction.Player ? Faction.Enemy : Faction.Player);
            foreach (BattleUnitModel unit in aliveList)
            {
                if (unit != playingCard.target && unit.IsTargetable(owner) && !unit.IsDead())
                {
                    playingCard.subTargets.Add(new BattlePlayingCardDataInUnitModel.SubTarget
                    {
                        targetSlotOrder = 0,
                        target = unit
                    });
                }
            }
        }
    }
}

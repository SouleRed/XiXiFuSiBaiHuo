using System.Collections.Generic;
using 裁缝之王.Shared.Tool;

namespace 裁缝之王
{
    public class DiceCardAbility_TX_ShouGongFengZhi : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]若目标为友方单位则使其对敌方单位使用一张费用最高的书页";
        public override void OnSucceedAttack()
        {
            BattleUnitModel target = base.card.target;
            if (target == null || target.faction != base.owner.faction) return;

            int maxCost = 0;
            foreach (BattleDiceCardModel card in target.allyCardDetail.GetAllDeck())
            {
                if (card.GetCost() > maxCost) maxCost = card.GetCost();
            }

            List<BattleDiceCardModel> candidates = target.allyCardDetail.GetAllDeck().FindAll(x => x.GetCost() == maxCost);
            if (candidates.Count == 0) return;

            BattleDiceCardModel chosen = RandomUtil.SelectOne(candidates);
            if (chosen == null) return;

            foreach (BattleUnitModel enemy in BattleObjectManager.instance.GetAliveList_random(base.owner.faction == Faction.Enemy ? Faction.Player : Faction.Enemy, 1))
            {
                BattleTools.AddCardAttack(target, enemy, chosen.GetID());
            }
        }
    }
}
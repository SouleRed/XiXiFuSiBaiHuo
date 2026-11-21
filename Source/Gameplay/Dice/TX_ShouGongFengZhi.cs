using BaseMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 裁缝之王
{
    public class DiceCardAbility_TX_ShouGongFengZhi : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]若目标为友方单位则使其对敌方单位使用一张费用最高的书页";
        public override void OnSucceedAttack()
        {
            BattleUnitModel target = base.card.target;
            if (target == null || target.faction != base.owner.faction)
            {
                return;
            }
            int CardCost = 0;
            foreach (BattleDiceCardModel battleDiceCardModel in target.allyCardDetail.GetAllDeck())
            {
                if (battleDiceCardModel.GetCost() > CardCost)
                {
                    CardCost = battleDiceCardModel.GetCost();
                }
            }
            List<BattleDiceCardModel> list = new List<BattleDiceCardModel>();
            list.AddRange(target.allyCardDetail.GetAllDeck().FindAll((BattleDiceCardModel X) => X.GetCost() == CardCost));
            if (list.Count > 0)
            {
                BattleDiceCardModel battleDiceCardModel2 = RandomUtil.SelectOne<BattleDiceCardModel>(list);
                if (battleDiceCardModel2 != null)
                {
                    foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList_random((base.owner.faction == Faction.Enemy) ? Faction.Player : Faction.Enemy, 1))
                    {
                        ExtraTools.AddCardAttack(target, battleUnitModel, battleDiceCardModel2.GetID());
                    }
                }
            }
        }

    }
}
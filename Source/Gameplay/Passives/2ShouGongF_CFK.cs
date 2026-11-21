using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 裁缝之王
{
    public class PassiveAbility_2ShouGongF_CFK : PassiveAbilityBase
    {
        public override BattleUnitModel ChangeAttackTarget(BattleDiceCardModel card, int idx)
        {
            if (card == null || card.owner == null)
                return base.ChangeAttackTarget(card, idx);

            if (card.GetID() == new LorId("CaiFengZhiKing", 6))
            {
                List<BattleUnitModel> aliveList2 = BattleObjectManager.instance.GetAliveList(this.owner.faction);
                aliveList2.Remove(this.owner);
                aliveList2.RemoveAll((BattleUnitModel x) => !x.IsTargetable(this.owner));
                if (aliveList2.Count > 0)
                {
                    return RandomUtil.SelectOne<BattleUnitModel>(aliveList2);
                }
            }

            return base.ChangeAttackTarget(card, idx);
        }

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            int allyCount = 0;
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList(base.owner.faction))
            {
                if (battleUnitModel != null && battleUnitModel != base.owner)
                {
                    allyCount++;
                }
            }
            if (allyCount > 0)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    power = allyCount
                });
            }
        }
    }
}

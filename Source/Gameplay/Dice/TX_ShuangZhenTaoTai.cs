using BaseMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 裁缝之王
{
    public class DiceCardAbility_TX_ShuangZhenTaoTai : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]若目标带有[标本化]则造成的伤害+100%";
        public override void BeforeRollDice()
        {
            BattlePlayingCardDataInUnitModel card = base.card;
            BattleUnitModel battleUnitModel = (card != null) ? card.target : null;
            if (battleUnitModel != null)
            {
                BattleUnitBuf battleUnitBuf = battleUnitModel.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf x) => x.bufType == KeywordHao.BF_BiaoBenHua);
                if (battleUnitBuf != null)
                {
                    int stack = battleUnitBuf.stack;
                    BattleDiceBehavior behavior = this.behavior;
                    if (behavior == null)
                    {
                        return;
                    }
                    behavior.ApplyDiceStatBonus(new DiceStatBonus
                    {
                        dmgRate = 100
                    });
                }
            }
        }
    }
}
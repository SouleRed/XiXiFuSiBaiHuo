using BaseMod;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Y_LimbusBuff.Method;

namespace  裁缝之王
{
    public class DiceCardSelfAbility_CX_XiaoDaZhao : DiceCardSelfAbilityBase
    {
        private int add;
        public static string Desc = "[使用时]目标每带有4层[流血]则使本书页所有骰子威力+1(至多+2)";
        public override void OnUseCard()
        {
            
            this.add = 0;
            var target = base.card?.target;
            if (target == null)
            {
                return;
            }
            var burnBuf = target.bufListDetail.GetActivatedBuf(KeywordBuf.Bleeding);
            int burnStack = burnBuf?.stack ?? 0;
            this.add += burnStack;
            int powerBonus = Math.Min(this.add / 4, 2);
            if (powerBonus > 0)
            {
                this.card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    power = 1
                });
            }
        }
       
    }
}

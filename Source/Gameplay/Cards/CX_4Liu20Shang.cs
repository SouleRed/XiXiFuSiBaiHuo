using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace  裁缝之王
{
    public class DiceCardSelfAbility_CX_4Liu20Shang : DiceCardSelfAbilityBase
    {
        private int add;
        public static string Desc = "[使用时]若目标至少带有4层[流血]则使本书页造成的伤害+20%";
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
            int powerBonus = Math.Min(this.add / 4, 1);
            if (powerBonus > 0)
            {
                this.card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
                {
                    dmgRate = 20
                });
            }
        }
       
    }
}

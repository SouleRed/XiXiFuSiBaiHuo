using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 裁缝之王
{
    public class DiceCardAbility_TX_1ZhenLiu : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]对目标施加1层[流血]与[针钉]";
        public override void OnSucceedAttack()
        {
            BattleUnitModel target = base.card.target;
            if (target == null)
            {
                return;
            }

                target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Bleeding, 1, base.owner);
                target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordHao.BF_ZhenDing, 1, base.owner);
            
        }
    }
}
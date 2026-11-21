using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 裁缝之王
{
    public class DiceCardAbility_TX_1Zhen : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]对目标施加1层[针钉]";
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            if (target == null)
            {
                return;
            }
            target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordHao.BF_ZhenDing, 1, base.owner);

        }

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 裁缝之王
{
    public class DiceCardAbility_TX_BiaoBenHua : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]若目标[针钉]层数不低于5则对其施加2层[标本化]";
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            if (target == null)
            {
                return;
            }
            BattleUnitBuf_BF_ZhenDing battleUnitBuf_BF_ZhenDing = target.bufListDetail.GetActivatedBuf(KeywordHao.BF_ZhenDing) as BattleUnitBuf_BF_ZhenDing;
            if (battleUnitBuf_BF_ZhenDing != null && battleUnitBuf_BF_ZhenDing.stack >= 5)
            {
                target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordHao.BF_BiaoBenHua, 2, base.owner);
            }
            base.OnSucceedAttack(target);
        }
    }
}
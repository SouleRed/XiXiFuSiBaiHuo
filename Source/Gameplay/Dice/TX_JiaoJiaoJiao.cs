using BaseMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 裁缝之王
{
    public class DiceCardAbility_TX_JiaoJiaoJiao : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]若目标带有[标本化]则击杀目标并恢复体力上限5%的体力";
        public override void OnSucceedAttack()
        {
            BattleUnitModel target = base.card.target;
            if (target == null)
            {
                return;
            }
            BattleUnitBuf battleUnitBuf = target.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf x) => x.bufType == KeywordHao.BF_BiaoBenHua);
            if (battleUnitBuf != null)
            {
                target.Die(null, true);
                base.owner.RecoverHP((int)(owner.MaxHp * 0.05f));
            }                      
        }
    }
}
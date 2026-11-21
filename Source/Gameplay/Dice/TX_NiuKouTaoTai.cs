using BaseMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 裁缝之王
{
    public class DiceCardAbility_TX_NiuKouTaoTai : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]追加目标[针钉]层数的伤害";
        public override void OnSucceedAttack()
        {
            BattleUnitModel target = base.card.target;
            if (target != null)
            {
                BattleUnitBuf activatedBuf = target.bufListDetail.GetActivatedBuf(KeywordHao.BF_ZhenDing);
                if (activatedBuf != null)
                {
                    int stack = activatedBuf.stack;
                    if (stack > 0)
                    {
                        target.TakeDamage(stack, DamageType.Card_Ability, base.owner, KeywordBuf.None);
                    }
                }
            }
        }
    }
}
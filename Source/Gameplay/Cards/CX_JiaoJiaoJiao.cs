using BaseMod;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Y_LimbusBuff.Method;
using static UnityEngine.GraphicsBuffer;

namespace  裁缝之王
{
    public class DiceCardSelfAbility_CX_JiaoJiaoJiao : DiceCardSelfAbilityBase
    {
        private int add;
        public static string Desc = "[优先指定标本化的目标] [钉层数低于5的单位无法改变本书页的攻击目标] [使用时]目标每带有1层[针钉]使本书页造成的伤害+10%(至多50%)";
        public override void BeforeGiveDamage(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior?.card?.target;
            if (target != null)
            {
                int add = target.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing);
                if (add > 0 && behavior != null)
                {
                    behavior.ApplyDiceStatBonus(new DiceStatBonus
                    {
                        dmgRate = Math.Min(add * 10, 50)
                    });
                }
            }
        }
        public override bool IsTargetChangable(BattleUnitModel attacker)
        {
            BattleUnitModel target = base.card.target;
            int add = target.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing);
            if (add >= 5 && target != null)
            {
                return true;
            }
            return false;
        }
    }
}

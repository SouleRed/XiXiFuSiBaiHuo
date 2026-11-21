using 裁缝之王.Presentation.CustomCombat;
using LOR_DiceSystem;
using System;
using Y_LimbusBuff.Method;

namespace 裁缝之王
{
    public class DiceCardSelfAbility_CX_WeiShaJiu : DiceCardSelfAbilityBase
    {
        public static string Desc = "[攻击容量2] [使用时]目标每带有1层[针钉]使本书页造成的伤害+5%(至多25%)";
        public override void OnApplyCard()
        {
            CustomCombatTargeting.AssignTargets(card);
            base.OnApplyCard();
        }
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
                        dmgRate = Math.Min(add * 5, 25)
                    });
                }
            }
        }
        
       
    }
}

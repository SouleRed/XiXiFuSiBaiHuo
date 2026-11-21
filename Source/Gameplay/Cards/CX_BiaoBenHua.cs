using 裁缝之王.Presentation.CustomCombat;
using LOR_DiceSystem;
using System;
namespace 裁缝之王
{
    public class DiceCardSelfAbility_CX_BiaoBenHua : DiceCardSelfAbilityBase
    {
        public static string Desc = "[攻击容量5] [本书页不造成伤害与混乱伤害] [使用时]摧毁目标当前书页所有骰子";
        public override void OnApplyCard()
        {
            CustomCombatTargeting.AssignTargets(card);
            base.OnApplyCard();
        }

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior?.card?.target;
            if (target != null)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus
                {
                    dmgRate = -9999,
                    breakRate = -9999
                });

            }
                
        }

        public override void OnUseCard()
        {
            card.target?.currentDiceAction?.DestroyDice(DiceMatch.AllDice, DiceUITiming.Start);
        }
    }
}

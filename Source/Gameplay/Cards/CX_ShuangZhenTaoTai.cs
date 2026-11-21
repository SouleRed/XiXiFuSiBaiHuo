using 裁缝之王.Presentation.CustomCombat;
using System.Collections.Generic;
using System.Linq;
using LOR_DiceSystem;
using System;
using Y_LimbusBuff.Method;

namespace 裁缝之王
{
    public class DiceCardSelfAbility_CX_ShuangZhenTaoTai : DiceCardSelfAbilityBase
    {
        public static string Desc = "[攻击容量5] [使用时]除自身外每有一名友方角色则使本书页所有骰子造成的伤害+50%且威力+4 若场上不存在友方单位则摧毁本书页所有骰子 [攻击结束]移除所有针族并使自身下一幕获得移除的友方单位数的[强壮]";
        public override void OnApplyCard()
        {
            CustomCombatTargeting.AssignTargets(card);
            base.OnApplyCard();
        }
        private List<BattleUnitModel> AlivePins(List<BattleUnitModel> aliveList)
            => aliveList.Where(unit =>
                unit != null &&
                unit != this.owner &&
                unit.passiveDetail.HasPassive<PassiveAbility_7XiaoErJian_CFK>()).ToList();

        public override void OnUseCard()
        {
            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(owner.faction);
            int allies = aliveList.Count(unit => unit != owner);
            if (allies == 0)
            {
                card.RemoveAllDice();
                return;
            }

            Presentation.Actions.KingAllyPresentation.Capture(this.card, AlivePins(aliveList).ToArray());
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
            {
                power = 4 * allies,
                dmgRate = 50 * allies,
            });
        }

        public override void OnEndBattle()
        {
            if (this.owner == null)
            {
                base.OnEndBattle();
                return;
            }

            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(this.owner.faction);

            int aliveCount = aliveList.Count(unit => unit != null && unit != this.owner);
            this.owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Strength, aliveCount, this.owner);

            foreach (BattleUnitModel target in AlivePins(aliveList))
            {
                target.Die(null, true);
            }

            base.OnEndBattle();
        }
    }
}

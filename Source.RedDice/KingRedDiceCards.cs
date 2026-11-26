using LOR_DiceSystem;
using RedDice;
using System;
using System.Collections.Generic;
using System.Linq;
using 裁缝之王.Presentation.CustomCombat;

namespace 裁缝之王
{
    public class DiceCardSelfAbility_CX_WeiShaJiu : RedDiceCardSelfAbilityBase, ICustomRandomBarrage
    {
        public int AttackCapacity => 3;

        public override bool RedDice(bool RedDice)
        {
            return true;
        }

        public static string Desc = "[攻击容量3][广域乱射] [使用时]目标每带有1层[针钉]使本书页造成的伤害+5%(至多25%)";

        public override void BeforeGiveDamage(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior?.card?.target;
            if (target == null) return;
            int add = target.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing);
            if (add > 0)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = Math.Min(add * 5, 25) });
            }
        }
    }

    public class DiceCardSelfAbility_CX_20YiShang : RedDiceCardSelfAbilityBase, ICustomAttackCapacity
    {
        public int AttackCapacity => 5;

        public override bool RedDice(bool RedDice)
        {
            return true;
        }

        public static string Desc = "[攻击容量5] [使用时]目标每带有1层[针钉]使本书页造成的伤害+10%(至多50%)";

        public override void BeforeGiveDamage(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior?.card?.target;
            if (target == null) return;
            int add = target.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing);
            if (add > 0)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = Math.Min(add * 10, 50) });
            }
        }
    }

    public class DiceCardSelfAbility_CX_ShuangZhenTaoTai : RedDiceCardSelfAbilityBase, ICustomAttackCapacity
    {
        public int AttackCapacity => 5;

        public override bool RedDice(bool RedDice)
        {
            return true;
        }

        public static string Desc = "[攻击容量5] [使用时]除自身外每有一名友方角色则使本书页所有骰子造成的伤害+50%且威力+4 若场上不存在友方单位则摧毁本书页所有骰子 [攻击结束]移除所有针族并使自身下一幕获得移除的友方单位数的[强壮]";

        private List<BattleUnitModel> AlivePins(List<BattleUnitModel> aliveList)
            => aliveList.Where(unit =>
                unit != null &&
                unit != owner &&
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

            Presentation.Actions.KingAllyPresentation.Capture(card, AlivePins(aliveList).ToArray());
            card.ApplyDiceStatBonus(DiceMatch.AllDice, new DiceStatBonus
            {
                power = 4 * allies,
                dmgRate = 50 * allies,
            });
        }

        public override void OnEndBattle()
        {
            if (owner == null)
            {
                base.OnEndBattle();
                return;
            }

            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(owner.faction);
            int aliveCount = aliveList.Count(unit => unit != null && unit != owner);
            owner.bufListDetail.AddKeywordBufByCard(KeywordBuf.Strength, aliveCount, owner);

            foreach (BattleUnitModel target in AlivePins(aliveList))
            {
                target.Die(null, true);
            }

            base.OnEndBattle();
        }
    }

    public class DiceCardSelfAbility_CX_JiaoJiaoJiao : RedDiceCardSelfAbilityBase
    {
        public override bool RedDice(bool RedDice)
        {
            return true;
        }

        public static string Desc = "[优先指定标本化的目标] [钉层数低于5的单位无法改变本书页的攻击目标] [使用时]目标每带有1层[针钉]使本书页造成的伤害+10%(至多50%)";

        public override void BeforeGiveDamage(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior?.card?.target;
            if (target == null) return;
            int add = target.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing);
            if (add > 0)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = Math.Min(add * 10, 50) });
            }
        }

        public override bool IsTargetChangable(BattleUnitModel attacker)
        {
            BattleUnitModel target = card?.target;
            return target != null && target.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing) >= 5;
        }
    }

    public class DiceCardSelfAbility_CX_BiaoBenHua : RedDiceCardSelfAbilityBase, ICustomAttackCapacity
    {
        public int AttackCapacity => 5;

        public override bool RedDice(bool RedDice)
        {
            return true;
        }

        public static string Desc = "[攻击容量5] [本书页不造成伤害与混乱伤害] [使用时]摧毁目标当前书页所有骰子";

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior?.card?.target;
            if (target == null) return;

            behavior.ApplyDiceStatBonus(new DiceStatBonus { dmgRate = -9999, breakRate = -9999 });
        }

        public override void OnUseCard()
        {
            card.target?.currentDiceAction?.DestroyDice(DiceMatch.AllDice, DiceUITiming.Start);
        }
    }
}

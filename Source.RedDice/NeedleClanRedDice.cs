using RedDice;

namespace 裁缝之王
{
    public class DiceCardAbility_TX_2Liu_X : RedDiceCardAbilityBase
    {
        public override bool RedDice(bool RedDice)
        {
            return true;
        }

        public static string Desc = "[命中时]下一幕对目标2层[流血]";

        public override void OnSucceedAttack()
        {
            BattleUnitModel target = card.target;
            if (target == null) return;

            target.bufListDetail.AddKeywordBufByCard(KeywordBuf.Bleeding, 2, owner);
        }
    }

    public class DiceCardAbility_TX_XiaoDaZhao : RedDiceCardAbilityBase
    {
        public static string Desc = "[命中时]重复投掷本骰子 [重复投掷命中时]追加目标[流血]层数的伤害并施加1层[针钉]";

        private int count;

        public override bool RedDice(bool RedDice)
        {
            return true;
        }

        public override void OnSucceedAttack()
        {
            owner.battleCardResultLog.SetSucceedAtkEvent(SucceedAtkEvent);
            if (count >= 1)
            {
                BattleUnitModel target = card.target;
                int stack = target?.bufListDetail.GetActivatedBuf(KeywordBuf.Bleeding)?.stack ?? 0;
                if (stack > 0)
                {
                    target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordHao.BF_ZhenDing, 1, owner);
                    target.TakeDamage(stack, DamageType.Card_Ability, owner, KeywordBuf.None);
                }
                return;
            }

            if (behavior.DiceVanillaValue > behavior.GetDiceMin())
            {
                ActivateBonusAttackDice();
                count++;
            }
        }

        private void SucceedAtkEvent()
        {
            foreach (BattleUnitModel unit in BattleObjectManager.instance.GetAliveList(owner.faction))
            {
                SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfile(
                    unit, unit.faction, unit.hp, unit.breakDetail.breakGauge, null);
            }
        }
    }
}

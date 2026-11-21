using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace 裁缝之王
{
    public class DiceCardAbility_TX_XiaoDaZhao : DiceCardAbilityBase
    {
        public static string Desc = "[命中时]重复投掷本骰子 [重复投掷命中时]追加目标[流血]层数的伤害";
        public override void OnSucceedAttack()
        {
            base.owner.battleCardResultLog.SetSucceedAtkEvent(new BattleCardBehaviourResult.BehaviourEvent(this.SucceedAtkEvent));
            if (this.count >= 1)
            {
                BattleUnitModel target = base.card.target;
                if (target != null)
                {
                    BattleUnitBuf activatedBuf = target.bufListDetail.GetActivatedBuf(KeywordBuf.Bleeding);
                    if (activatedBuf != null)
                    {
                        int stack = activatedBuf.stack;
                        if (stack > 0)
                        {
                            target.TakeDamage(stack, DamageType.Card_Ability, base.owner, KeywordBuf.None);
                        }
                    }
                }
                return;
            }
            if (this.behavior.DiceVanillaValue > this.behavior.GetDiceMin())
            {
                base.ActivateBonusAttackDice();
                this.count++;
            }
        }
        private void SucceedAtkEvent()
        {
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList(base.owner.faction))
            {
                SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.UpdateCharacterProfile(battleUnitModel, battleUnitModel.faction, battleUnitModel.hp, battleUnitModel.breakDetail.breakGauge, null);
            }
        }

        private const int _REPEAT = 2;

        private int count;

        private List<GameObject> _effectList;
    }
}
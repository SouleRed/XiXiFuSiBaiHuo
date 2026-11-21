using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LOR_DiceSystem;
using System.Threading.Tasks;

namespace 裁缝之王
{
    public class PassiveAbility_6FengRen_CFK : PassiveAbilityBase
    {
        public override void OnSucceedAttack(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior.card.target;
            if (behavior.Detail == BehaviourDetail.Penetrate && RandomUtil.valueForProb < 0.33f)
            {
                target.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordHao.BF_ZhenDing, 1, this.owner);
            }
            base.OnSucceedAttack(behavior);
        }
    }
}

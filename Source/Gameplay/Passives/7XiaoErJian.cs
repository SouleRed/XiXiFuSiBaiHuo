using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LOR_DiceSystem;
using BaseMod;
using System.Threading.Tasks;

namespace 裁缝之王
{
    public class PassiveAbility_7XiaoErJian_CFK : PassiveAbilityBase
    {
        public override void OnSucceedAttack(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior.card.target;
            BattleUnitBuf_BF_ZhenDing testBuf = target.bufListDetail.FindBuf<BattleUnitBuf_BF_ZhenDing>(BufReadyType.ThisRound);
            if (testBuf != null)
            {
                target.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Bleeding, 1, this.owner);
            }
            base.OnSucceedAttack(behavior);
        }
    }
}

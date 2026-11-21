using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 裁缝之王
{
    public class PassiveAbility_4CaiFengKai_CFK : PassiveAbilityBase
    {
        public override void OnRoundStart()
        {
            foreach (BattleUnitModel battleUnitModel in BattleObjectManager.instance.GetAliveList_random((this.owner.faction == Faction.Enemy) ? Faction.Player : Faction.Enemy, 2))
            {
                battleUnitModel.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordHao.BF_CiSheZ, 1, this.owner);
            }
        }
    }
}

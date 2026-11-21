using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Y_LimbusBuff.Method;
using Y_LimbusBuff.Quote;
using static UnityEngine.UI.GridLayoutGroup;

namespace 裁缝之王
{
    public class PassiveAbility_0LiZhi_CFK : Y_PassiveAbilityBase
    {
        public override void OnSanityLost()
        {
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Weak, 1, base.owner);
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Protection, 2, base.owner);
        }

        public override void OnSanityLostForMax()
        {
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Weak, 2, base.owner);
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.DmgUp, 4, base.owner);
        }

        public override bool LockSanityLost()
        {
            return true;
        }

        public override bool SwitchSanityLost()
        {
            return true;
        }

        public override bool SanitySystem()
        {
            return true;
        }

    }
}

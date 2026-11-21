using BaseMod;
using LOR_DiceSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Y_LimbusBuff.Method;
using static UnityEngine.GraphicsBuffer;

namespace  裁缝之王
{
    public class DiceCardSelfAbility_CX_NiuKouTaoTai : DiceCardSelfAbilityBase
    {
        private int add;
        public static string Desc = "[拼点失败]使目标的[针钉]层数-1";

        public override void OnLoseParrying()
        {
            BattlePlayingCardDataInUnitModel card = base.card;
            BattleUnitModel battleUnitModel = (card != null) ? card.target : null;
            if (battleUnitModel != null)
            {
                BattleUnitBuf_BF_ZhenDing battleUnitBuf_BF_ZhenDing = battleUnitModel.bufListDetail.GetActivatedBuf(KeywordHao.BF_ZhenDing) as BattleUnitBuf_BF_ZhenDing;
                if (battleUnitBuf_BF_ZhenDing != null && battleUnitBuf_BF_ZhenDing.stack > 0)
                {
                    battleUnitBuf_BF_ZhenDing.XiaoStack(1);
                }
            }
            base.OnLoseParrying();
        }
        
       
    }
}

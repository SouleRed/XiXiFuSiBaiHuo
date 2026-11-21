using BaseMod;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Y_LimbusBuff.Method;
using static UnityEngine.GraphicsBuffer;

namespace  裁缝之王
{
    public class DiceCardSelfAbility_CX_ZhaoHuan : DiceCardSelfAbilityBase
    {
        private int add;
        public static string Desc = "[战斗开始]这一幕结束时召唤3名友方单位";
        public override void OnStartBattle()
        {
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordHao.BF_Zhaohuan, 1, base.owner);
            base.OnStartBattle();
            Presentation.Actions.KingSummonPresentation.Register(this.card);
        }
       
    }
}

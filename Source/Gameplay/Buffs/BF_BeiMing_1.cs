using AutoKeywordUtil;
using BaseMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Y_LimbusBuff.Buff.OptimizeBuff;
using LOR_XML;
using Y_LimbusBuff.Method;
using static UnityEngine.GraphicsBuffer;
using LOR_DiceSystem;

namespace 裁缝之王
{
    internal class BattleUnitBuf_BF_BeiMing_1 : BattleUnitBuf, IAutoKeywordBuf
    {
        public string KeywordBufName => KeywordHao.BF_BeiMing_1Name;
        public override KeywordBuf bufType
        {
            get
            {
                return KeywordHao.BF_BeiMing_1;
            }
        }

        public override string keywordId
        {
            get
            {
                return "BF_BeiMing_1";
            }
        }

        public override string keywordIconId
        {
            get
            {
                return "BF_BeiMing_1";
            }
        }


        public override bool independentBufIcon
        {
            get
            {
                return false;
            }
        }

        public override void Init(BattleUnitModel owner)
        {
            base.Init(owner);
            Presentation.Effects.KingBuffVisuals.Track(owner);
        }

        public BattleUnitBuf_BF_BeiMing_1()
        {
            this.stack = 0;
            this.add = 0;
        }

        public override void OnRoundEnd()
        {
            //SingletonBehavior<BattleManagerUI>.Instance.ui_unitListInfoSummary.DisplayDlg(CaiFengKing_Initializer.GetBattleDlg(DialogType.DEATH, "CaiFengKing_1"),
                //base._owner, false, MentalState.Positive);
            YinXiao.Play("10SV-092", 1f, loop: false);
            this.stack = 0;
            this.Destroy();
        }
        public override bool IsImmune(KeywordBuf buf)
        {
            return buf == KeywordBuf.NullifyPower;
        }

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                max = 2
            });
        }

        public static int GetBuf(BattleUnitModel model)
        {
            BattleUnitBuf_BF_BeiMing_1 battleUnitBuf_BF_BeiMing_1 = model.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf x) => x is BattleUnitBuf_BF_BeiMing_1) as BattleUnitBuf_BF_BeiMing_1;
            bool flag = battleUnitBuf_BF_BeiMing_1 == null;
            bool flag2 = flag;
            int result;
            if (flag2)
            {
                result = 0;
            }
            else
            {
                result = battleUnitBuf_BF_BeiMing_1.stack;
            }
            return result;
        }

        public void Add(int add)
        {
            this.stack += add;
            int num = 9999;
            bool flag = this.stack >= num;
            if (flag)
            {
                this.stack = num;
            }
            bool flag2 = this.stack <= 0;
            if (flag2)
            {
                this.stack = 0;
            }
        }

        public override void OnAddBuf(int addedStack)
        {
            if (this.stack > 1)
            {
                this.stack = 1;
            }
        }

        public void SetMax()
        {
            this.stack = 1;
        }

        public void XiaoStack(int add)
        {
            this.stack -= add;
            if (this.stack <= 0)
            {
                this.Destroy();
            }
        }

        private BattleUnitModel _attacker;

        private int add;
    }
}

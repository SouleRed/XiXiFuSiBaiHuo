using AutoKeywordUtil;
using BaseMod;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Y_LimbusBuff.Buff.OptimizeBuff;
using Y_LimbusBuff.Method;
using static UnityEngine.GraphicsBuffer;

namespace 裁缝之王
{
    internal class BattleUnitBuf_BF_Zhaohuan : BattleUnitBuf, IAutoKeywordBuf
    {
        public string KeywordBufName => KeywordHao.BF_ZhaohuanName;
        public override KeywordBuf bufType
        {
            get
            {
                return KeywordHao.BF_Zhaohuan;
            }
        }

        public override string keywordId
        {
            get
            {
                return "BF_Zhaohuan";
            }
        }

        public override string keywordIconId
        {
            get
            {
                return "BF_Zhaohuan";
            }
        }


        public override bool independentBufIcon
        {
            get
            {
                return false;
            }
        }

        public BattleUnitBuf_BF_Zhaohuan()
        {
            this.stack = 0;
            this.add = 0;
        }
        public override void OnRoundEndTheLast()
        {
            this.Destroy();
            base.OnRoundEndTheLast();
        }

        public static int GetBuf(BattleUnitModel model)
        {
            BattleUnitBuf_BF_Zhaohuan battleUnitBuf_BF_Zhaohuan = model.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf x) => x is BattleUnitBuf_BF_Zhaohuan) as BattleUnitBuf_BF_Zhaohuan;
            bool flag = battleUnitBuf_BF_Zhaohuan == null;
            bool flag2 = flag;
            int result;
            if (flag2)
            {
                result = 0;
            }
            else
            {
                result = battleUnitBuf_BF_Zhaohuan.stack;
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

        private bool yes;
    }
}

using AutoKeywordUtil;
using BaseMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Y_LimbusBuff.Buff.OptimizeBuff;
using Y_LimbusBuff.Method;
using static UnityEngine.GraphicsBuffer;
using LOR_DiceSystem;

namespace 裁缝之王
{
    internal class BattleUnitBuf_BF_BeiMing_2 : BattleUnitBuf, IAutoKeywordBuf
    {
        public string KeywordBufName => KeywordHao.BF_BeiMing_2Name;
        public override KeywordBuf bufType
        {
            get
            {
                return KeywordHao.BF_BeiMing_2;
            }
        }

        public override string keywordId
        {
            get
            {
                return "BF_BeiMing_2";
            }
        }

        public override string keywordIconId
        {
            get
            {
                return "BF_BeiMing_2";
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

        public BattleUnitBuf_BF_BeiMing_2()
        {
            this.stack = 0;
            this.add = 0;
        }

        public override void OnRoundEnd()
        {
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
                max = 3
            });
        }

        public static int GetBuf(BattleUnitModel model)
        {
            BattleUnitBuf_BF_BeiMing_2 battleUnitBuf_BF_BeiMing_2 = model.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf x) => x is BattleUnitBuf_BF_BeiMing_2) as BattleUnitBuf_BF_BeiMing_2;
            bool flag = battleUnitBuf_BF_BeiMing_2 == null;
            bool flag2 = flag;
            int result;
            if (flag2)
            {
                result = 0;
            }
            else
            {
                result = battleUnitBuf_BF_BeiMing_2.stack;
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

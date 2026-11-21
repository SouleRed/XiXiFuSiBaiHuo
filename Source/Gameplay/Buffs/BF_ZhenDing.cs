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
    internal class BattleUnitBuf_BF_ZhenDing : BattleUnitBuf, IAutoKeywordBuf
    {
        public string KeywordBufName => KeywordHao.BF_ZhenDingName ;
        public override KeywordBuf bufType
        {
            get
            {
                return KeywordHao.BF_ZhenDing;
            }
        }

        public override string keywordId
        {
            get
            {
                return "BF_ZhenDing";
            }
        }

        public override string keywordIconId
        {
            get
            {
                return "BF_ZhenDing";
            }
        }


        public override bool independentBufIcon
        {
            get
            {
                return false;
            }
        }

        public BattleUnitBuf_BF_ZhenDing()
        {
            this.stack = 0;
            this.add = 0;
        }

        public override void OnRoundEnd()
        {
            _owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Bleeding, stack, base._owner);
            this.stack--;
            if (this.stack <= 0)
            {
                this.Destroy();
            }
        }

        public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
        {
            if (atkDice.Detail == BehaviourDetail.Penetrate)
            {
                _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Bleeding, 1, base._owner);
                this.stack--;
                if (this.stack <= 0)
                {
                    this.Destroy();
                }
            }
            base.OnTakeDamageByAttack(atkDice, dmg);
        }

        public static int GetBuf(BattleUnitModel model)
        {
            BattleUnitBuf_BF_ZhenDing battleUnitBuf_BF_ZhenDing = model.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf x) => x is BattleUnitBuf_BF_ZhenDing) as BattleUnitBuf_BF_ZhenDing;
            bool flag = battleUnitBuf_BF_ZhenDing == null;
            bool flag2 = flag;
            int result;
            if (flag2)
            {
                result = 0;
            }
            else
            {
                result = battleUnitBuf_BF_ZhenDing.stack;
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
            if (this.stack > 99)
            {
                this.stack = 99;
            }
        }

        public void SetMax()
        {
            this.stack = 99;
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

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
    internal class BattleUnitBuf_BF_BiaoBenHua : BattleUnitBuf, IAutoKeywordBuf
    {
        public string KeywordBufName => KeywordHao.BF_BiaoBenHuaName;
        public override KeywordBuf bufType
        {
            get
            {
                return KeywordHao.BF_BiaoBenHua;
            }
        }

        public override string keywordId
        {
            get
            {
                return "BF_BiaoBenHua";
            }
        }

        public override string keywordIconId
        {
            get
            {
                return "BF_BiaoBenHua";
            }
        }


        public override bool independentBufIcon
        {
            get
            {
                return false;
            }
        }

        public BattleUnitBuf_BF_BiaoBenHua()
        {
            this.stack = 0;
            this.add = 0;
        }

        public override void OnRoundEnd()
        {
            int num = (int)((float)this._owner.MaxHp * 0.2f);
            _owner.TakeDamage(num, DamageType.Passive, this._owner, KeywordBuf.None);
            this.stack--;
            if (this.stack <= 0)
            {
                this.QingAddZhen();
                this.Destroy();
            }
        }
        private void QingAddZhen()
        {
            BattleUnitBuf_BF_ZhenDing battleUnitBuf_BF_ZhenDing = base._owner.bufListDetail.GetActivatedBuf(KeywordHao.BF_ZhenDing) as BattleUnitBuf_BF_ZhenDing;
            if (battleUnitBuf_BF_ZhenDing != null && battleUnitBuf_BF_ZhenDing.stack > 0)
            {
                battleUnitBuf_BF_ZhenDing.XiaoStack(stack+1);
            }
        }

        public override int SpeedDiceBreakedAdder()
        {
            return 10;
        }

        public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
        {

                _owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Bleeding, 3, base._owner);

            base.OnTakeDamageByAttack(atkDice, dmg);
        }

        public static int GetBuf(BattleUnitModel model)
        {
            BattleUnitBuf_BF_BiaoBenHua battleUnitBuf_BF_BiaoBenHua = model.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf x) => x is BattleUnitBuf_BF_BiaoBenHua) as BattleUnitBuf_BF_BiaoBenHua;
            bool flag = battleUnitBuf_BF_BiaoBenHua == null;
            bool flag2 = flag;
            int result;
            if (flag2)
            {
                result = 0;
            }
            else
            {
                result = battleUnitBuf_BF_BiaoBenHua.stack;
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
            this.stack = 2;
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

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
    internal class BattleUnitBuf_BF_CiSheZ : BattleUnitBuf, IAutoKeywordBuf
    {
        public string KeywordBufName => KeywordHao.BF_CiSheZName ;
        public override KeywordBuf bufType
        {
            get
            {
                return KeywordHao.BF_CiSheZ;
            }
        }

        public override string keywordId
        {
            get
            {
                return "BF_CiSheZ";
            }
        }

        public override string keywordIconId
        {
            get
            {
                return "BF_CiSheZ";
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

        public BattleUnitBuf_BF_CiSheZ()
        {
            this.stack = 0;
            this.add = 0;
            yes = false;
        }
        public override void OnRollDice(BattleDiceBehavior behavior)
        {
            if (base.IsAttackDice(behavior.Detail))
            {
                yes = true;
            }
            base.OnRollDice(behavior);
        }
        public override void OnRoundEnd()
        {
            if (yes)
            {
                _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordHao.BF_ZhenDing, 3, base._owner);
            }
            else
            {
                this.BanAddZhen();
            }
            this.Destroy();
            
        }
        private void BanAddZhen()
        {
            BattleUnitBuf_BF_ZhenDing battleUnitBuf_BF_ZhenDing = base._owner.bufListDetail.GetActivatedBuf(KeywordHao.BF_ZhenDing) as BattleUnitBuf_BF_ZhenDing;
            if (battleUnitBuf_BF_ZhenDing != null && battleUnitBuf_BF_ZhenDing.stack > 0)
            {
                battleUnitBuf_BF_ZhenDing.XiaoStack((stack + 1) / 2);
            }
        }

        public static int GetBuf(BattleUnitModel model)
        {
            BattleUnitBuf_BF_CiSheZ battleUnitBuf_BF_CiSheZ = model.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf x) => x is BattleUnitBuf_BF_CiSheZ) as BattleUnitBuf_BF_CiSheZ;
            bool flag = battleUnitBuf_BF_CiSheZ == null;
            bool flag2 = flag;
            int result;
            if (flag2)
            {
                result = 0;
            }
            else
            {
                result = battleUnitBuf_BF_CiSheZ.stack;
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

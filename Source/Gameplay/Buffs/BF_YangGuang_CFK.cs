using AutoKeywordUtil;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace 裁缝之王
{
    internal class BattleUnitBuf_BF_YangGuang_CFK : BattleUnitBuf, IAutoKeywordBuf
    {
        public string KeywordBufName => KeywordHao.BF_YangGuang_CFKName ;
        public override KeywordBuf bufType
        {
            get
            {
                return KeywordHao.BF_YangGuang_CFK;
            }
        }

        public override string keywordId
        {
            get
            {
                return "BF_YangGuang_CFK";
            }
        }

        public override string keywordIconId
        {
            get
            {
                return "BF_YangGuang_CFK";
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

        public BattleUnitBuf_BF_YangGuang_CFK()
        {
            this.stack = 0;
            this.add = 0;
        }

        public override void OnRoundEnd()
        {
            this.stack = 0;
        }

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior?.card?.target;
            if (target != null)
            {
                int add = target.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing);
                if (add > 0 && behavior != null)
                {
                    behavior.ApplyDiceStatBonus(new DiceStatBonus
                    {
                        max = Math.Min(add / 3, 99)
                    });
                }
            }
        }

        public static int GetBuf(BattleUnitModel model)
        {
            BattleUnitBuf_BF_YangGuang_CFK battleUnitBuf_BF_YangGuang_CFK = model.bufListDetail.GetActivatedBufList().Find((BattleUnitBuf x) => x is BattleUnitBuf_BF_YangGuang_CFK) as BattleUnitBuf_BF_YangGuang_CFK;
            bool flag = battleUnitBuf_BF_YangGuang_CFK == null;
            bool flag2 = flag;
            int result;
            if (flag2)
            {
                result = 0;
            }
            else
            {
                result = battleUnitBuf_BF_YangGuang_CFK.stack;
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

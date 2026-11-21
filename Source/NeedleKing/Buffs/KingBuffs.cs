using System;
using AutoKeywordUtil;

namespace 裁缝之王
{
    internal class BattleUnitBuf_BF_BeiMing_1 : BattleUnitBuf, IAutoKeywordBuf
    {
        public string KeywordBufName => KeywordHao.BF_BeiMing_1Name;
        public override KeywordBuf bufType => KeywordHao.BF_BeiMing_1;
        public override string keywordId => "BF_BeiMing_1";
        public override string keywordIconId => "BF_BeiMing_1";
        public override bool independentBufIcon => false;

        public override void Init(BattleUnitModel owner)
        {
            base.Init(owner);
            Presentation.Effects.KingBuffVisuals.Track(owner);
        }

        public BattleUnitBuf_BF_BeiMing_1()
        {
            stack = 0;
        }

        public override void OnRoundEnd()
        {
            Presentation.Actions.ActionAudio.Play("10SV-092");
            stack = 0;
            Destroy();
        }

        public override bool IsImmune(KeywordBuf buf) => buf == KeywordBuf.NullifyPower;

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { max = 2 });
        }

        public static int GetBuf(BattleUnitModel model)
        {
            var buf = model.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_BF_BeiMing_1) as BattleUnitBuf_BF_BeiMing_1;
            return buf?.stack ?? 0;
        }

        public void Add(int add) => stack = Math.Max(0, Math.Min(9999, stack + add));

        public override void OnAddBuf(int addedStack)
        {
            if (stack > 1) stack = 1;
        }

        public void SetMax() => stack = 1;

        public void XiaoStack(int add)
        {
            stack -= add;
            if (stack <= 0) Destroy();
        }
    }

    internal class BattleUnitBuf_BF_BeiMing_2 : BattleUnitBuf, IAutoKeywordBuf
    {
        public string KeywordBufName => KeywordHao.BF_BeiMing_2Name;
        public override KeywordBuf bufType => KeywordHao.BF_BeiMing_2;
        public override string keywordId => "BF_BeiMing_2";
        public override string keywordIconId => "BF_BeiMing_2";
        public override bool independentBufIcon => false;

        public override void Init(BattleUnitModel owner)
        {
            base.Init(owner);
            Presentation.Effects.KingBuffVisuals.Track(owner);
        }

        public BattleUnitBuf_BF_BeiMing_2()
        {
            stack = 0;
        }

        public override void OnRoundEnd()
        {
            stack = 0;
            Destroy();
        }

        public override bool IsImmune(KeywordBuf buf) => buf == KeywordBuf.NullifyPower;

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            behavior.ApplyDiceStatBonus(new DiceStatBonus { max = 3 });
        }

        public static int GetBuf(BattleUnitModel model)
        {
            var buf = model.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_BF_BeiMing_2) as BattleUnitBuf_BF_BeiMing_2;
            return buf?.stack ?? 0;
        }

        public void Add(int add) => stack = Math.Max(0, Math.Min(9999, stack + add));

        public override void OnAddBuf(int addedStack)
        {
            if (stack > 1) stack = 1;
        }

        public void SetMax() => stack = 1;

        public void XiaoStack(int add)
        {
            stack -= add;
            if (stack <= 0) Destroy();
        }
    }

    internal class BattleUnitBuf_BF_BiaoBenHua : BattleUnitBuf, IAutoKeywordBuf
    {
        public string KeywordBufName => KeywordHao.BF_BiaoBenHuaName;
        public override KeywordBuf bufType => KeywordHao.BF_BiaoBenHua;
        public override string keywordId => "BF_BiaoBenHua";
        public override string keywordIconId => "BF_BiaoBenHua";
        public override bool independentBufIcon => false;

        public BattleUnitBuf_BF_BiaoBenHua()
        {
            stack = 0;
        }

        public override void OnRoundEnd()
        {
            int dmg = (int)(_owner.MaxHp * 0.2f);
            _owner.TakeDamage(dmg, DamageType.Passive, _owner, KeywordBuf.None);
            stack--;
            if (stack <= 0)
            {
                QingAddZhen();
                Destroy();
            }
        }

        private void QingAddZhen()
        {
            if (_owner.bufListDetail.GetActivatedBuf(KeywordHao.BF_ZhenDing) is BattleUnitBuf_BF_ZhenDing zhenDing && zhenDing.stack > 0)
            {
                zhenDing.XiaoStack(stack + 1);
            }
        }

        public override int SpeedDiceBreakedAdder() => 10;

        public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
        {
            _owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Bleeding, 3, _owner);
            base.OnTakeDamageByAttack(atkDice, dmg);
        }

        public static int GetBuf(BattleUnitModel model)
        {
            var buf = model.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_BF_BiaoBenHua) as BattleUnitBuf_BF_BiaoBenHua;
            return buf?.stack ?? 0;
        }

        public void Add(int add) => stack = Math.Max(0, Math.Min(9999, stack + add));

        public override void OnAddBuf(int addedStack)
        {
            if (stack > 99) stack = 99;
        }

        public void SetMax() => stack = 2;

        public void XiaoStack(int add)
        {
            stack -= add;
            if (stack <= 0) Destroy();
        }
    }

    internal class BattleUnitBuf_BF_YangGuang_CFK : BattleUnitBuf, IAutoKeywordBuf
    {
        public string KeywordBufName => KeywordHao.BF_YangGuang_CFKName;
        public override KeywordBuf bufType => KeywordHao.BF_YangGuang_CFK;
        public override string keywordId => "BF_YangGuang_CFK";
        public override string keywordIconId => "BF_YangGuang_CFK";
        public override bool independentBufIcon => false;

        public override void Init(BattleUnitModel owner)
        {
            base.Init(owner);
            Presentation.Effects.KingBuffVisuals.Track(owner);
        }

        public BattleUnitBuf_BF_YangGuang_CFK()
        {
            stack = 0;
        }

        public override void OnRoundEnd()
        {
            stack = 0;
        }

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior?.card?.target;
            if (target == null) return;
            int add = target.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing);
            if (add > 0)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus { max = Math.Min(add / 3, 99) });
            }
        }

        public static int GetBuf(BattleUnitModel model)
        {
            var buf = model.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_BF_YangGuang_CFK) as BattleUnitBuf_BF_YangGuang_CFK;
            return buf?.stack ?? 0;
        }

        public void Add(int add) => stack = Math.Max(0, Math.Min(9999, stack + add));

        public override void OnAddBuf(int addedStack)
        {
            if (stack > 1) stack = 1;
        }

        public void SetMax() => stack = 1;

        public void XiaoStack(int add)
        {
            stack -= add;
            if (stack <= 0) Destroy();
        }
    }

    internal class BattleUnitBuf_BF_Zhaohuan : BattleUnitBuf, IAutoKeywordBuf
    {
        public string KeywordBufName => KeywordHao.BF_ZhaohuanName;
        public override KeywordBuf bufType => KeywordHao.BF_Zhaohuan;
        public override string keywordId => "BF_Zhaohuan";
        public override string keywordIconId => "BF_Zhaohuan";
        public override bool independentBufIcon => false;

        public BattleUnitBuf_BF_Zhaohuan()
        {
            stack = 0;
        }

        public override void OnRoundEndTheLast()
        {
            Destroy();
            base.OnRoundEndTheLast();
        }

        public static int GetBuf(BattleUnitModel model)
        {
            var buf = model.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_BF_Zhaohuan) as BattleUnitBuf_BF_Zhaohuan;
            return buf?.stack ?? 0;
        }

        public void Add(int add) => stack = Math.Max(0, Math.Min(9999, stack + add));

        public override void OnAddBuf(int addedStack)
        {
            if (stack > 1) stack = 1;
        }

        public void SetMax() => stack = 1;

        public void XiaoStack(int add)
        {
            stack -= add;
            if (stack <= 0) Destroy();
        }
    }

    internal class BattleUnitBuf_BF_ZhuiJi : BattleUnitBuf, IAutoKeywordBuf
    {
        public string KeywordBufName => KeywordHao.BF_ZhuiJiName;
        public override KeywordBuf bufType => KeywordHao.BF_ZhuiJi;
        public override string keywordId => "BF_ZhuiJi";
        public override string keywordIconId => "BF_ZhuiJi";
        public override bool independentBufIcon => false;

        public BattleUnitBuf_BF_ZhuiJi()
        {
            stack = 0;
        }

        public static int GetBuf(BattleUnitModel model)
        {
            var buf = model.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_BF_ZhuiJi) as BattleUnitBuf_BF_ZhuiJi;
            return buf?.stack ?? 0;
        }

        public void Add(int add) => stack = Math.Max(0, Math.Min(9999, stack + add));

        public override void OnAddBuf(int addedStack)
        {
            if (stack > 1) stack = 1;
        }

        public void SetMax() => stack = 1;

        public void XiaoStack(int add)
        {
            stack -= add;
            if (stack <= 0) Destroy();
        }
    }
}

using System;
using AutoKeywordUtil;
using BaseMod;
using LOR_DiceSystem;

namespace 裁缝之王
{
    public static class KeywordHao
    {
        public static KeywordBuf BF_BeiMing_1 { get; } = Tools.MakeEnum<KeywordBuf>("BF_BeiMing_1");
        public const string BF_BeiMing_1Name = "BF_BeiMing_1";

        public static KeywordBuf BF_BeiMing_2 { get; } = Tools.MakeEnum<KeywordBuf>("BF_BeiMing_2");
        public const string BF_BeiMing_2Name = "BF_BeiMing_2";

        public static KeywordBuf BF_BiaoBenHua { get; } = Tools.MakeEnum<KeywordBuf>("BF_BiaoBenHua");
        public const string BF_BiaoBenHuaName = "BF_BiaoBenHua";

        public static KeywordBuf BF_ZhenDing { get; } = Tools.MakeEnum<KeywordBuf>("BF_ZhenDing");
        public const string BF_ZhenDingName = "BF_ZhenDing";

        public static KeywordBuf BF_CiSheZ { get; } = Tools.MakeEnum<KeywordBuf>("BF_CiSheZ");
        public const string BF_CiSheZName = "BF_CiSheZ";

        public static KeywordBuf BF_YangGuang_CFK { get; } = Tools.MakeEnum<KeywordBuf>("BF_YangGuang_CFK");
        public const string BF_YangGuang_CFKName = "BF_YangGuang_CFK";

        public static KeywordBuf BF_Zhaohuan { get; } = Tools.MakeEnum<KeywordBuf>("BF_Zhaohuan");
        public const string BF_ZhaohuanName = "BF_Zhaohuan";

        public static KeywordBuf BF_ZhuiJi { get; } = Tools.MakeEnum<KeywordBuf>("BF_ZhuiJi");
        public const string BF_ZhuiJiName = "BF_ZhuiJi";
    }

    internal class BattleUnitBuf_BF_ZhenDing : BattleUnitBuf, IAutoKeywordBuf
    {
        public string KeywordBufName => KeywordHao.BF_ZhenDingName;
        public override KeywordBuf bufType => KeywordHao.BF_ZhenDing;
        public override string keywordId => "BF_ZhenDing";
        public override string keywordIconId => "BF_ZhenDing";
        public override bool independentBufIcon => false;

        public BattleUnitBuf_BF_ZhenDing()
        {
            stack = 0;
        }

        public override void OnRoundEnd()
        {
            _owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Bleeding, stack, _owner);
            stack--;
            if (stack <= 0) Destroy();
        }

        public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
        {
            if (atkDice.Detail == BehaviourDetail.Penetrate)
            {
                _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Bleeding, 1, _owner);
                stack--;
                if (stack <= 0) Destroy();
            }
            base.OnTakeDamageByAttack(atkDice, dmg);
        }

        public static int GetBuf(BattleUnitModel model)
        {
            var buf = model.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_BF_ZhenDing) as BattleUnitBuf_BF_ZhenDing;
            return buf?.stack ?? 0;
        }

        public void Add(int add) => stack = Math.Max(0, Math.Min(9999, stack + add));

        public override void OnAddBuf(int addedStack)
        {
            if (stack > 99) stack = 99;
        }

        public void SetMax() => stack = 99;

        public void XiaoStack(int add)
        {
            stack -= add;
            if (stack <= 0) Destroy();
        }
    }

    internal class BattleUnitBuf_BF_CiSheZ : BattleUnitBuf, IAutoKeywordBuf
    {
        public string KeywordBufName => KeywordHao.BF_CiSheZName;
        public override KeywordBuf bufType => KeywordHao.BF_CiSheZ;
        public override string keywordId => "BF_CiSheZ";
        public override string keywordIconId => "BF_CiSheZ";
        public override bool independentBufIcon => false;

        private bool yes;

        public override void Init(BattleUnitModel owner)
        {
            base.Init(owner);
            Presentation.Effects.KingBuffVisuals.Track(owner);
        }

        public BattleUnitBuf_BF_CiSheZ()
        {
            stack = 0;
            yes = false;
        }

        public override void OnRollDice(BattleDiceBehavior behavior)
        {
            if (IsAttackDice(behavior.Detail)) yes = true;
            base.OnRollDice(behavior);
        }

        public override void OnRoundEnd()
        {
            if (yes)
            {
                _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordHao.BF_ZhenDing, 3, _owner);
            }
            else
            {
                BanAddZhen();
            }
            Destroy();
        }

        private void BanAddZhen()
        {
            if (_owner.bufListDetail.GetActivatedBuf(KeywordHao.BF_ZhenDing) is BattleUnitBuf_BF_ZhenDing zhenDing && zhenDing.stack > 0)
            {
                zhenDing.XiaoStack((stack + 1) / 2);
            }
        }

        public static int GetBuf(BattleUnitModel model)
        {
            var buf = model.bufListDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_BF_CiSheZ) as BattleUnitBuf_BF_CiSheZ;
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

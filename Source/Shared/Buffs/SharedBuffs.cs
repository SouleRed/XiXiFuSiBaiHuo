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

    internal abstract class NeedleModBuf : BattleUnitBuf, IAutoKeywordBuf
    {
        public abstract string KeywordBufName { get; }
        public override string keywordId => KeywordBufName;
        public override string keywordIconId => KeywordBufName;
        public override bool independentBufIcon => false;

        protected virtual int MaxStack => 1;
        protected virtual int MaxSetValue => 1;
        protected virtual bool TrackVisuals => false;

        protected NeedleModBuf() => stack = 0;

        public override void Init(BattleUnitModel owner)
        {
            base.Init(owner);
            if (TrackVisuals) Presentation.Effects.KingBuffVisuals.Track(owner);
        }

        public void Add(int add) => stack = Math.Max(0, Math.Min(9999, stack + add));

        public override void OnAddBuf(int addedStack)
        {
            if (stack > MaxStack) stack = MaxStack;
        }

        public void SetMax() => stack = MaxSetValue;

        public void XiaoStack(int add)
        {
            stack -= add;
            if (stack <= 0) Destroy();
        }

        public static int GetBuf<T>(BattleUnitModel model) where T : BattleUnitBuf
            => (model?.bufListDetail.GetActivatedBufList().Find(x => x is T) as T)?.stack ?? 0;
    }

    internal class BattleUnitBuf_BF_ZhenDing : NeedleModBuf
    {
        public override string KeywordBufName => KeywordHao.BF_ZhenDingName;
        public override KeywordBuf bufType => KeywordHao.BF_ZhenDing;
        protected override int MaxStack => 99;
        protected override int MaxSetValue => 99;

        public override void OnRoundEnd()
        {
            _owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Bleeding, stack, _owner);
            XiaoStack(1);
        }

        public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
        {
            if (atkDice.Detail == BehaviourDetail.Penetrate)
            {
                _owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Bleeding, 1, _owner);
                XiaoStack(1);
            }
            base.OnTakeDamageByAttack(atkDice, dmg);
        }
    }

    internal class BattleUnitBuf_BF_CiSheZ : NeedleModBuf
    {
        public override string KeywordBufName => KeywordHao.BF_CiSheZName;
        public override KeywordBuf bufType => KeywordHao.BF_CiSheZ;
        protected override bool TrackVisuals => true;

        private bool yes;

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
            else if (_owner.bufListDetail.GetActivatedBuf(KeywordHao.BF_ZhenDing) is BattleUnitBuf_BF_ZhenDing zhenDing && zhenDing.stack > 0)
            {
                zhenDing.XiaoStack((stack + 1) / 2);
            }
            Destroy();
        }
    }
}

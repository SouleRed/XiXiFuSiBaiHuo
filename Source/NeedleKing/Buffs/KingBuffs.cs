using System;

namespace 裁缝之王
{
    internal class BattleUnitBuf_BF_BeiMing_1 : NeedleModBuf
    {
        public override string KeywordBufName => KeywordHao.BF_BeiMing_1Name;
        public override KeywordBuf bufType => KeywordHao.BF_BeiMing_1;
        protected override bool TrackVisuals => true;

        public override void OnRoundEnd()
        {
            Presentation.Actions.ActionAudio.PlayVoice("10SV-092", 2);
            stack = 0;
            Destroy();
        }

        public override bool IsImmune(KeywordBuf buf) => buf == KeywordBuf.NullifyPower;

        public override void BeforeRollDice(BattleDiceBehavior behavior)
            => behavior.ApplyDiceStatBonus(new DiceStatBonus { max = 2 });
    }

    internal class BattleUnitBuf_BF_BeiMing_2 : NeedleModBuf
    {
        public override string KeywordBufName => KeywordHao.BF_BeiMing_2Name;
        public override KeywordBuf bufType => KeywordHao.BF_BeiMing_2;
        protected override bool TrackVisuals => true;

        public override void OnRoundEnd()
        {
            stack = 0;
            Destroy();
        }

        public override bool IsImmune(KeywordBuf buf) => buf == KeywordBuf.NullifyPower;

        public override void BeforeRollDice(BattleDiceBehavior behavior)
            => behavior.ApplyDiceStatBonus(new DiceStatBonus { max = 3 });
    }

    internal class BattleUnitBuf_BF_BiaoBenHua : NeedleModBuf
    {
        public override string KeywordBufName => KeywordHao.BF_BiaoBenHuaName;
        public override KeywordBuf bufType => KeywordHao.BF_BiaoBenHua;
        protected override int MaxStack => 99;
        protected override int MaxSetValue => 2;

        public override void OnRoundEnd()
        {
            _owner.TakeDamage((int)(_owner.MaxHp * 0.2f), DamageType.Passive, _owner, KeywordBuf.None);
            stack--;
            if (stack <= 0)
            {
                if (_owner.bufListDetail.GetActivatedBuf(KeywordHao.BF_ZhenDing) is BattleUnitBuf_BF_ZhenDing zhenDing && zhenDing.stack > 0)
                {
                    zhenDing.XiaoStack(stack + 1);
                }
                Destroy();
            }
        }

        public override int SpeedDiceBreakedAdder() => 10;

        public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
        {
            _owner.bufListDetail.AddKeywordBufByEtc(KeywordBuf.Bleeding, 3, _owner);
            base.OnTakeDamageByAttack(atkDice, dmg);
        }
    }

    internal class BattleUnitBuf_BF_YangGuang_CFK : NeedleModBuf
    {
        public override string KeywordBufName => KeywordHao.BF_YangGuang_CFKName;
        public override KeywordBuf bufType => KeywordHao.BF_YangGuang_CFK;
        protected override bool TrackVisuals => true;

        public override void OnRoundEnd() => stack = 0;

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior?.card?.target;
            if (target == null) return;
            int add = target.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing);
            if (add > 0)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus { power = Math.Min(add / 3, 99) });
            }
        }
    }

    internal class BattleUnitBuf_BF_Zhaohuan : NeedleModBuf
    {
        public override string KeywordBufName => KeywordHao.BF_ZhaohuanName;
        public override KeywordBuf bufType => KeywordHao.BF_Zhaohuan;

        public override void OnRoundEndTheLast()
        {
            Destroy();
            base.OnRoundEndTheLast();
        }
    }

    internal class BattleUnitBuf_BF_ZhuiJi : NeedleModBuf
    {
        public override string KeywordBufName => KeywordHao.BF_ZhuiJiName;
        public override KeywordBuf bufType => KeywordHao.BF_ZhuiJi;
    }
}

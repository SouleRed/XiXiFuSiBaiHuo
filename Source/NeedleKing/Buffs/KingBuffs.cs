using System;

namespace 裁缝之王
{
    internal abstract class BeiMingBuffBase : NeedleModBuf
    {
        protected override bool TrackVisuals => true;
        protected abstract int PowerBonus { get; }

        public override void Init(BattleUnitModel owner)
        {
            base.Init(owner);
            ReleaseBreak();
        }

        public override void OnBreakState() => ReleaseBreak();

        public override bool IsImmune(KeywordBuf buf) => buf == KeywordBuf.NullifyPower;

        public override void BeforeRollDice(BattleDiceBehavior behavior)
            => behavior.ApplyDiceStatBonus(new DiceStatBonus { power = PowerBonus });

        public override void OnRoundEnd()
        {
            stack = 0;
            Destroy();
        }

        internal static bool HasActive(BattleUnitModel unit) => unit?.bufListDetail != null &&
            (unit.bufListDetail.GetActivatedBuf(KeywordHao.BF_BeiMing_1) != null ||
             unit.bufListDetail.GetActivatedBuf(KeywordHao.BF_BeiMing_2) != null);

        private void ReleaseBreak()
        {
            if (_owner == null) return;
            var detail = _owner.breakDetail;
            if (!_owner.IsBreakLifeZero() && !detail.nextTurnBreak && _owner.turnState != BattleUnitTurnState.BREAK) return;

            detail.RecoverBreakLife(_owner.MaxBreakLife, ignore: true);
            detail.nextTurnBreak = false;
            detail.ResetGauge();
            if (_owner.turnState == BattleUnitTurnState.BREAK) _owner.turnState = BattleUnitTurnState.WAIT_CARD;
            _owner.OnReleaseBreak();

            if (Singleton<StageController>.Instance.IsLogState())
            {
                _owner.battleCardResultLog?.SetBreakState(false);
                _owner.battleCardResultLog?.SetCurrentBreakGauge(detail.breakGauge);
            }
            else
            {
                SingletonBehavior<BattleManagerUI>.Instance?.ui_unitListInfoSummary?.UpdateCharacterProfile(_owner, _owner.faction, _owner.hp, detail.breakGauge);
            }
        }
    }

    internal class BattleUnitBuf_BF_BeiMing_1 : BeiMingBuffBase
    {
        public override string KeywordBufName => KeywordHao.BF_BeiMing_1Name;
        public override KeywordBuf bufType => KeywordHao.BF_BeiMing_1;
        protected override int PowerBonus => 2;

        public override void OnRoundEnd()
        {
            Presentation.Actions.ActionAudio.PlayVoice("10SV-092", 2);
            base.OnRoundEnd();
        }
    }

    internal class BattleUnitBuf_BF_BeiMing_2 : BeiMingBuffBase
    {
        public override string KeywordBufName => KeywordHao.BF_BeiMing_2Name;
        public override KeywordBuf bufType => KeywordHao.BF_BeiMing_2;
        protected override int PowerBonus => 3;

        public override void OnRoundEnd() { }
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

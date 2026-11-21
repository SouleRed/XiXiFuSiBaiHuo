using BaseMod;

namespace 裁缝之王
{
    public class PassiveAbility_7XiaoErJian_CFK : PassiveAbilityBase
    {
        internal const int PinHeight = 150;

        public override void OnWaveStart()
        {
            base.OnWaveStart();
            owner?.view?.ChangeHeight(PinHeight);
        }

        public override void OnSucceedAttack(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior.card.target;
            BattleUnitBuf_BF_ZhenDing testBuf = target.bufListDetail.FindBuf<BattleUnitBuf_BF_ZhenDing>(BufReadyType.ThisRound);
            if (testBuf != null)
            {
                target.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Bleeding, 1, owner);
            }
            base.OnSucceedAttack(behavior);
        }
    }
}

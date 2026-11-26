using System;
using System.Collections.Generic;
using 裁缝之王.Shared.Interop;

namespace 裁缝之王
{
    public class PassiveAbility_8LRH_WarmLight_CFK : PassiveAbilityBase
    {
        public override void OnWaveStart()
        {
            if (!LeRougeInterop.IsHeathcliffKing(owner))
            {
                owner?.passiveDetail?.DestroyPassive(this);
                return;
            }

            if (owner.bufListDetail.GetActivatedBuf(KeywordHao.BF_YangGuang_LRH) == null)
            {
                owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordHao.BF_YangGuang_LRH, 1, owner);
            }
        }

        public override void OnRoundStart()
        {
            if (!LeRougeInterop.IsHeathcliffKing(owner))
            {
                owner?.passiveDetail?.DestroyPassive(this);
                return;
            }

            List<BattleUnitModel> opponents = BattleObjectManager.instance?.GetAliveList_opponent(owner.faction);
            if (opponents == null || opponents.Count == 0) return;

            List<BattleUnitModel> pool = new(opponents);
            int count = Math.Min(2, pool.Count);
            for (int i = 0; i < count; i++)
            {
                BattleUnitModel chosen = RandomUtil.SelectOne(pool);
                pool.Remove(chosen);
                chosen.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordHao.BF_CiSheZ_LRH, 1, owner);
            }
        }

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            if (behavior == null || !LeRougeInterop.IsHeathcliffKing(owner)) return;

            int round = Singleton<StageController>.Instance?.RoundTurn ?? 0;
            if (round <= 0 || round % 3 != 0) return;
            if (!LeRougeInterop.HasCry(owner)) return;

            int sharpNails = LeRougeInterop.GetSharpNails(owner);
            int extraDmgRate = 10 + (sharpNails / 10) * 5;
            behavior.ApplyDiceStatBonus(new DiceStatBonus
            {
                power = 1,
                dmgRate = extraDmgRate,
            });
        }
    }

    internal class BattleUnitBuf_BF_YangGuang_LRH : NeedleModBuf
    {
        public override string KeywordBufName => KeywordHao.BF_YangGuang_LRHName;
        public override string keywordIconId => KeywordHao.BF_YangGuang_CFKName;
        public override KeywordBuf bufType => KeywordHao.BF_YangGuang_LRH;
        protected override bool TrackVisuals => true;

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior?.card?.target;
            if (target == null) return;

            int total = LeRougeInterop.GetPinStack(target) + LeRougeInterop.GetAbjectStack(target);
            int bonus = Math.Min(3, total / 3);
            if (bonus > 0)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus { power = bonus });
            }
        }
    }

    internal class BattleUnitBuf_BF_CiSheZ_LRH : NeedleModBuf
    {
        public override string KeywordBufName => KeywordHao.BF_CiSheZ_LRHName;
        public override string keywordIconId => KeywordHao.BF_CiSheZName;
        public override KeywordBuf bufType => KeywordHao.BF_CiSheZ_LRH;
        public override BufPositiveType positiveType => BufPositiveType.Negative;
        protected override bool TrackVisuals => true;

        public override double ChangeDamage(BattleUnitModel attacker, double damage) => damage * 1.05;

        public override void OnRoundEnd()
        {
            if (_owner != null && !_owner.IsDead())
            {
                LeRougeInterop.GainPin(_owner, 2);
            }
            Destroy();
        }
    }
}

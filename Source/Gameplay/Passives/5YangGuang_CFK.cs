using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Y_LimbusBuff.Buff.OptimizeBuff;
using Y_LimbusBuff.Method;

namespace 裁缝之王
{
    public class PassiveAbility_5YangGuang_CFK : PassiveAbilityBase
    {
        public override void OnWaveStart()
        {
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordHao.BF_YangGuang_CFK, 1, this.owner);
            if (this.owner.faction == Faction.Player)
            {
                return;
            }
            Singleton<StageController>.Instance.GetStageModel().SetCurrentMapInfo(0);
            int emotionTotalCoinNumber = Singleton<StageController>.Instance.GetCurrentStageFloorModel().team.emotionTotalCoinNumber;
            Singleton<StageController>.Instance.GetCurrentWaveModel().team.emotionTotalBonus = emotionTotalCoinNumber + 1;
            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(Faction.Player);
            foreach (BattleUnitModel battleUnitModel in aliveList)
            {
                battleUnitModel.bufListDetail.AddBuf(new PassiveAbility_5YangGuang_CFK.BattleUnitBuf_battle123
                {
                    hpAdder = 50,
                    breakGageAdder = 30
                });
                battleUnitModel.SetHp(battleUnitModel.MaxHp);
                battleUnitModel.breakDetail.breakGauge = battleUnitModel.breakDetail.GetDefaultBreakGauge();
            }
        }

        public override void OnRoundStart()
        {
            var ownerSanity = owner.bufListDetail.GetActivatedBuf(Y_KeywordBuf.KeywordBufs.Sanity) as BattleUnitBuf_Sanity;
            if (ownerSanity != null)
                ownerSanity.OnSanityUpdate(20);
        }



        public class BattleUnitBuf_battle123 : BattleUnitBuf
        {

            public BattleUnitBuf_battle123()
            {
                this.stack = 0;
            }

            public override StatBonus GetStatBonus()
            {
                return new StatBonus
                {
                    hpAdder = this.hpAdder,
                    breakGageAdder = this.breakGageAdder
                };
            }

            public int hpAdder;

            public int breakGageAdder;
        }
    }
}

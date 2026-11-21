using BaseMod;
using LOR_DiceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UI;
using LOR_XML;
using Y_LimbusBuff.Buff.OptimizeBuff;
using Y_LimbusBuff.Method;
namespace 裁缝之王
{
    public class PassiveAbility_3DuShagTS_CFK : PassiveAbilityBase
    {

        private int _TZ;
        private static int _BHMC;
        private static int _JD;
        private bool _transformed;
        private int _dmgReduction;
        public static int BHMC
        {
            get { return _BHMC; }
            private set { _BHMC = value; } // 如果不希望外部类修改，可以设置为 private
        }

        public static int JD
        {
            get { return _JD; }
            set { _JD = value; } // 同样，限制外部修改
        }

        internal const int KingHeight = 370;

        public override void OnWaveStart()
        {
            owner?.view?.ChangeHeight(KingHeight);
            Presentation.Actions.ActionAudio.Play("10SV-090");
            _BHMC = 0;
            _TZ = 0;
            _JD = 1;
            if (this.owner.hp <= (float)this.owner.MaxHp * 0.65f && _JD == 1)
            {
                _JD = 2;
                this.ZhuanJie();
            }
        }
        public override void OnRoundEnd()
        {
            BattleUnitBuf_BF_Zhaohuan testBuf = owner.bufListDetail.FindBuf<BattleUnitBuf_BF_Zhaohuan>(BufReadyType.ThisRound);
            if (testBuf != null)
            {
                ZhaoHuan();
            }
        }
        public override void OnRoundEndTheLast()
        {
            if (this.owner.hp <= (float)this.owner.MaxHp * 0.65f && _JD == 1)
            {
                _JD = 2;
                this.ZhuanJie();
            }
            base.OnRoundEnd();
        }
        public override bool BeforeTakeDamage(BattleUnitModel attacker, int dmg)
        {
            _dmgReduction = 0;
            if (!this._transformed && this.owner.hp - (float)dmg <= (float)this.owner.MaxHp * 0.65f && _JD == 1)
            {
                _transformed = true;
                _dmgReduction = (int)((float)this.owner.MaxHp * 0.65f - (this.owner.hp - (float)dmg));
            }
            return base.BeforeTakeDamage(attacker, dmg);
        }

        public override int GetDamageReductionAll()
        {
            int result;
            if (this.owner.hp <= (float)this.owner.MaxHp * 0.65f && _JD == 1)
            {
                result = 9999;
            }
            else
            {
                result = this._dmgReduction;
            }
            return result;
        }

        private static readonly int[] SummonUnitIds = { 2, 2, 3 };
        private static readonly MethodInfo CreateLibrarianUnitMethod = AccessTools.Method(
            typeof(StageController), "CreateLibrarianUnit",
            new[] { typeof(SephirahType), typeof(UnitBattleDataModel), typeof(int) });

        private void ZhaoHuan()
        {
            Presentation.Actions.ActionAudio.Play("10SV-096");
            SummonPins();
        }

        private void SummonPins()
        {
            var stage = Singleton<StageController>.Instance;
            var faction = owner.faction;
            var alive = BattleObjectManager.instance.GetAliveList(faction);
            int aliveCount = alive.Count;
            if (aliveCount >= 5) return;

            var occupied = new HashSet<int>();
            foreach (var unit in alive)
                occupied.Add(unit.index);

            int nextSlot = 0;
            bool anyAdded = false;
            foreach (int id in SummonUnitIds)
            {
                if (aliveCount >= 5) break;
                while (nextSlot < 5 && occupied.Contains(nextSlot)) nextSlot++;
                if (nextSlot >= 5) break;

                var unitId = new LorId("CaiFengZhiKing", id);
                BattleUnitModel summoned;
                if (faction == Faction.Enemy)
                {
                    summoned = stage.AddNewUnit(faction, unitId, nextSlot, 110);
                }
                else
                {
                    var data = UnitBattleDataModel.CreateUnitBattleDataByEnemyUnitId(stage.GetStageModel(), unitId);
                    data.unitData.customizeData.height = 110;
                    BattleObjectManager.instance.UnregisterUnitByIndex(faction, nextSlot);
                    summoned = (BattleUnitModel)CreateLibrarianUnitMethod.Invoke(stage,
                        new object[] { stage.GetCurrentStageFloorModel().Sephirah, data, nextSlot });
                }
                if (summoned == null) break;
                summoned.SetDeadSceneBlock(false);
                if (faction == Faction.Player) summoned.allyCardDetail.DrawCards(4);
                occupied.Add(nextSlot);
                aliveCount++;
                nextSlot++;
                anyAdded = true;
            }

            if (!anyAdded) return;
            int num = 0;
            foreach (var unit in BattleObjectManager.instance.GetList())
                SingletonBehavior<UICharacterRenderer>.Instance.SetCharacter(unit.UnitData.unitData, num++, true, false);
            BattleObjectManager.instance.InitUI();
        }
        private void BeiMing()
        {
            if (this.owner.IsBreakLifeZero())
            {
                this.owner.breakDetail.RecoverBreakLife(this.owner.MaxBreakLife, false);
                this.owner.breakDetail.nextTurnBreak = false;
                this.owner.breakDetail.RecoverBreak(this.owner.breakDetail.GetDefaultBreakGauge());
            }
            if (JD == 1)
            {
                Presentation.Actions.ActionAudio.Play("10SV-091");
                owner.bufListDetail.AddKeywordBufByEtc(KeywordHao.BF_BeiMing_1, 1, this.owner);
            }
            else if (JD >= 2)
            {
                owner.bufListDetail.AddKeywordBufByEtc(KeywordHao.BF_BeiMing_2, 2, this.owner);
            }
        }
        public override void OnDie()
        {
            Presentation.Actions.ActionAudio.Play("10SV-099");
            base.OnDie();
        }

        private void ZhuanJie()
        {
            SummonPins();
            Presentation.Actions.ActionAudio.Play("10SV-093");
            this.SetSp(30);
            this.owner.bufListDetail.RemoveBufAll(BufPositiveType.Negative);
            if (this.owner.IsBreakLifeZero())
            {
                this.owner.breakDetail.RecoverBreakLife(this.owner.MaxBreakLife, false);
                this.owner.breakDetail.nextTurnBreak = false;
                this.owner.breakDetail.RecoverBreak(this.owner.breakDetail.GetDefaultBreakGauge());
            }
            this.owner.breakDetail.RecoverBreak(500);
            _BHMC = 0;
        }

        public override bool OnBreakGageZero()
        {
            BattleUnitBuf_BF_BeiMing_1 testBuf = owner.bufListDetail.FindBuf<BattleUnitBuf_BF_BeiMing_1>(BufReadyType.ThisRound);
            BattleUnitBuf_BF_BeiMing_2 testBuf2 = owner.bufListDetail.FindBuf<BattleUnitBuf_BF_BeiMing_2>(BufReadyType.ThisRound);
            if (testBuf != null || testBuf2 != null)
            {
                return true;
            }
            return false;
        }

        private void SetSp(int add)
        {
            BattleUnitBuf_Sanity battleUnitBuf_Sanity = owner.bufListDetail.GetActivatedBuf(Y_KeywordBuf.KeywordBufs.Sanity) as BattleUnitBuf_Sanity;
            if (battleUnitBuf_Sanity != null)
            {
                battleUnitBuf_Sanity.ResetSanity();
                battleUnitBuf_Sanity.OnSanityUpdate(add);
            }
        }

        public override int SpeedDiceNumAdder()
        {
            int num = this._TZ;
            int num2 = this.owner.emotionDetail.SpeedDiceNumAdder();
            if (num2 > 0)
            {
                num -= num2;
            }
            return num;
        }


        public override void OnRoundStart()
        {
            if (this.owner.faction != Faction.Player)
            {
                int emotionTotalCoinNumber = Singleton<StageController>.Instance.GetCurrentStageFloorModel().team.emotionTotalCoinNumber;
                Singleton<StageController>.Instance.GetCurrentWaveModel().team.emotionTotalBonus = emotionTotalCoinNumber + 1;
            }
            _BHMC++;
            this.owner.allyCardDetail.ExhaustAllCards();
            if (_JD == 1)
            {
                if (_BHMC == 1)
                {
                    this._TZ = 5;
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 3), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 2), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 1), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 1), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 1), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 5), false);
                }
                else if (_BHMC == 2)
                {
                    this._TZ = 4;
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 4), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 3), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 6), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 6), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 6), false);
                }
                else if (_BHMC == 3)
                {
                    BeiMing();
                    _BHMC = 0;
                    this._TZ = 4;
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 7), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 4), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 2), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 8), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 8), false);
                }
            }
            else if (_JD == 2)
            {
                BeiMing();
                if (_BHMC == 1)
                {
                    this._TZ = 5;
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 3), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 3), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 1), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 1), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 2), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 2), false);
                }
                else if (_BHMC == 2)
                {
                    _BHMC = 0;
                    this._TZ = 5;
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 4), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 3), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 1), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 2), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 8), false);
                    this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 8), false);
                }
            }
            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList((this.owner.faction == Faction.Player) ? Faction.Enemy : Faction.Player);
            int ZhenDing = 0;
            int BiaoBen = 0;
            foreach (BattleUnitModel battleUnitModel in aliveList)
            {
                if (battleUnitModel == null) continue;
                ZhenDing += battleUnitModel.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing);
                BiaoBen += battleUnitModel.bufListDetail.GetKewordBufStack(KeywordHao.BF_BiaoBenHua);
            }
            if (ZhenDing >= 5)
            {
                this._TZ++;
                this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 10), false);
            }
            if (BiaoBen > 0)
            {
                this._TZ++;
                this.owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 9), false);
            }
            foreach (BattleDiceCardModel battleDiceCardModel in this.owner.allyCardDetail.GetHand())
            {
                battleDiceCardModel.SetCostToZero(true);
            }
        }

        public override BattleUnitModel ChangeAttackTarget(BattleDiceCardModel card, int idx)
        {
            // 1. 空检查
            if (card == null || card.owner == null)
                return base.ChangeAttackTarget(card, idx);

            // 2. 只对指定卡生效
            LorId cardId = card.GetID();
            bool isSpecialCard =
                cardId.Equals(new LorId("CaiFengZhiKing", 9));

            if (!isSpecialCard)
                return base.ChangeAttackTarget(card, idx);

            // 3. 获取玩家存活单位并筛选带有 BF_BiaoBenHua 的单位
            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(Faction.Player);
            List<BattleUnitModel> candidates = new List<BattleUnitModel>();

            foreach (BattleUnitModel u in aliveList)
            {
                if (u == null) continue;
                if (u.bufListDetail == null) continue;

                if (u.bufListDetail.GetActivatedBufList().Exists((BattleUnitBuf x) => x.bufType == KeywordHao.BF_BiaoBenHua))
                    candidates.Add(u);
            }
            // 5. 否则，如有候选目标则随机选取并返回
            if (candidates.Count > 0)
            {
                return candidates[UnityEngine.Random.Range(0, candidates.Count)];
            }

            if (card.GetID() == new LorId("CaiFengZhiKing", 6))
            {
                List<BattleUnitModel> aliveList2 = BattleObjectManager.instance.GetAliveList(this.owner.faction);
                aliveList2.Remove(this.owner);
                aliveList2.RemoveAll((BattleUnitModel x) => !x.IsTargetable(this.owner));
                if (aliveList2.Count > 0)
                {
                    return RandomUtil.SelectOne<BattleUnitModel>(aliveList2);
                }
            }

            return base.ChangeAttackTarget(card, idx);
        }
    }
    
}

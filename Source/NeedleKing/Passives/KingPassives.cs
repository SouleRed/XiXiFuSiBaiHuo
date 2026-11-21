using System.Collections.Generic;
using System.Reflection;
using BaseMod;
using HarmonyLib;
using LOR_DiceSystem;
using UI;
using Y_LimbusBuff.Buff.OptimizeBuff;
using Y_LimbusBuff.Method;
using Y_LimbusBuff.Quote;

namespace 裁缝之王
{
    public class PassiveAbility_0LiZhi_CFK : Y_PassiveAbilityBase
    {
        public override void OnSanityLost()
        {
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Weak, 1, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Protection, 2, owner);
        }

        public override void OnSanityLostForMax()
        {
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.Weak, 2, owner);
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordBuf.DmgUp, 4, owner);
        }

        public override bool LockSanityLost() => true;
        public override bool SwitchSanityLost() => true;
        public override bool SanitySystem() => true;
    }

    public class PassiveAbility_1CaiFengK_CFK : PassiveAbilityBase
    {
        public override bool IsImmune(KeywordBuf buf)
            => buf == KeywordBuf.Stun || buf == KeywordBuf.SealKeyword || buf == KeywordBuf.Seal;

        public override float DmgFactor(int dmg, DamageType type = DamageType.ETC, KeywordBuf keyword = KeywordBuf.None)
        {
            if (type == DamageType.Buf && !owner.IsBreakLifeZero()) return 0.5f;
            return base.DmgFactor(dmg, type, keyword);
        }

        public override double ChangeDamage(BattleUnitModel attacker, double dmg)
        {
            if (dmg > 20 && !owner.IsBreakLifeZero()) return 30 + (dmg - 20) * 0.4;
            return base.ChangeDamage(attacker, dmg);
        }

        public override void OnRoundStart()
        {
            KangXingSet();
            base.OnRoundStart();
        }

        private void KangXingSet()
        {
            owner.bufListDetail.RemoveBufAll(typeof(EmotionCardAbility_freischutz3.BattleUnitBuf_Emotion_Fruischutz_Flame));
            owner.Book.SetResistHP(BehaviourDetail.Slash, AtkResist.Endure);
            owner.Book.SetResistHP(BehaviourDetail.Penetrate, AtkResist.Normal);
            owner.Book.SetResistHP(BehaviourDetail.Hit, AtkResist.Resist);
            owner.Book.SetResistBP(BehaviourDetail.Slash, AtkResist.Resist);
            owner.Book.SetResistBP(BehaviourDetail.Penetrate, AtkResist.Endure);
            owner.Book.SetResistBP(BehaviourDetail.Hit, AtkResist.Normal);
        }

        public override bool BeforeTakeDamage(BattleUnitModel attacker, int dmg)
        {
            KangXingSet();
            if (BattleObjectManager.instance.GetAliveList(owner.faction).Exists(x => x != owner))
            {
                dmg /= 2;
            }
            return base.BeforeTakeDamage(attacker, dmg);
        }
    }

    public class PassiveAbility_2ShouGongF_CFK : PassiveAbilityBase
    {
        public override BattleUnitModel ChangeAttackTarget(BattleDiceCardModel card, int idx)
        {
            if (card == null || card.owner == null) return base.ChangeAttackTarget(card, idx);

            if (card.GetID() == new LorId("CaiFengZhiKing", 6))
            {
                List<BattleUnitModel> allies = BattleObjectManager.instance.GetAliveList(owner.faction);
                allies.Remove(owner);
                allies.RemoveAll(x => !x.IsTargetable(owner));
                if (allies.Count > 0) return RandomUtil.SelectOne(allies);
            }

            return base.ChangeAttackTarget(card, idx);
        }

        public override void BeforeRollDice(BattleDiceBehavior behavior)
        {
            int allyCount = 0;
            foreach (BattleUnitModel unit in BattleObjectManager.instance.GetAliveList(owner.faction))
            {
                if (unit != null && unit != owner) allyCount++;
            }
            if (allyCount > 0)
            {
                behavior.ApplyDiceStatBonus(new DiceStatBonus { power = allyCount });
            }
        }
    }

    public class PassiveAbility_3DuShagTS_CFK : PassiveAbilityBase
    {
        private int _TZ;
        private static int _BHMC;
        private static int _JD;
        private bool _transformed;
        private int _dmgReduction;

        public static int BHMC
        {
            get => _BHMC;
            private set => _BHMC = value;
        }

        public static int JD
        {
            get => _JD;
            set => _JD = value;
        }

        internal const int KingHeight = 370;

        public override void OnWaveStart()
        {
            owner?.view?.ChangeHeight(KingHeight);
            Presentation.Actions.ActionAudio.Play("10SV-090");
            _BHMC = 0;
            _TZ = 0;
            _JD = 1;
            if (owner.hp <= owner.MaxHp * 0.65f && _JD == 1)
            {
                _JD = 2;
                ZhuanJie();
            }
        }

        public override void OnRoundEnd()
        {
            BattleUnitBuf_BF_Zhaohuan testBuf = owner.bufListDetail.FindBuf<BattleUnitBuf_BF_Zhaohuan>(BufReadyType.ThisRound);
            if (testBuf != null) ZhaoHuan();
        }

        public override void OnRoundEndTheLast()
        {
            if (owner.hp <= owner.MaxHp * 0.65f && _JD == 1)
            {
                _JD = 2;
                ZhuanJie();
            }
            base.OnRoundEnd();
        }

        public override bool BeforeTakeDamage(BattleUnitModel attacker, int dmg)
        {
            _dmgReduction = 0;
            if (!_transformed && owner.hp - dmg <= owner.MaxHp * 0.65f && _JD == 1)
            {
                _transformed = true;
                _dmgReduction = (int)(owner.MaxHp * 0.65f - (owner.hp - dmg));
            }
            return base.BeforeTakeDamage(attacker, dmg);
        }

        public override int GetDamageReductionAll()
        {
            if (owner.hp <= owner.MaxHp * 0.65f && _JD == 1) return 9999;
            return _dmgReduction;
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
            foreach (var unit in alive) occupied.Add(unit.index);

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
            if (owner.IsBreakLifeZero())
            {
                owner.breakDetail.RecoverBreakLife(owner.MaxBreakLife, false);
                owner.breakDetail.nextTurnBreak = false;
                owner.breakDetail.RecoverBreak(owner.breakDetail.GetDefaultBreakGauge());
            }
            if (JD == 1)
            {
                Presentation.Actions.ActionAudio.Play("10SV-091");
                owner.bufListDetail.AddKeywordBufByEtc(KeywordHao.BF_BeiMing_1, 1, owner);
            }
            else if (JD >= 2)
            {
                owner.bufListDetail.AddKeywordBufByEtc(KeywordHao.BF_BeiMing_2, 2, owner);
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
            SetSp(30);
            owner.bufListDetail.RemoveBufAll(BufPositiveType.Negative);
            if (owner.IsBreakLifeZero())
            {
                owner.breakDetail.RecoverBreakLife(owner.MaxBreakLife, false);
                owner.breakDetail.nextTurnBreak = false;
                owner.breakDetail.RecoverBreak(owner.breakDetail.GetDefaultBreakGauge());
            }
            owner.breakDetail.RecoverBreak(500);
            _BHMC = 0;
        }

        public override bool OnBreakGageZero()
        {
            BattleUnitBuf_BF_BeiMing_1 testBuf = owner.bufListDetail.FindBuf<BattleUnitBuf_BF_BeiMing_1>(BufReadyType.ThisRound);
            BattleUnitBuf_BF_BeiMing_2 testBuf2 = owner.bufListDetail.FindBuf<BattleUnitBuf_BF_BeiMing_2>(BufReadyType.ThisRound);
            return testBuf != null || testBuf2 != null;
        }

        private void SetSp(int add)
        {
            if (owner.bufListDetail.GetActivatedBuf(Y_KeywordBuf.KeywordBufs.Sanity) is BattleUnitBuf_Sanity sanity)
            {
                sanity.ResetSanity();
                sanity.OnSanityUpdate(add);
            }
        }

        public override int SpeedDiceNumAdder()
        {
            int num = _TZ;
            int num2 = owner.emotionDetail.SpeedDiceNumAdder();
            if (num2 > 0) num -= num2;
            return num;
        }

        public override void OnRoundStart()
        {
            if (owner.faction != Faction.Player)
            {
                int emotionTotalCoinNumber = Singleton<StageController>.Instance.GetCurrentStageFloorModel().team.emotionTotalCoinNumber;
                Singleton<StageController>.Instance.GetCurrentWaveModel().team.emotionTotalBonus = emotionTotalCoinNumber + 1;
            }
            _BHMC++;
            owner.allyCardDetail.ExhaustAllCards();
            if (_JD == 1)
            {
                if (_BHMC == 1)
                {
                    _TZ = 5;
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 3), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 2), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 1), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 1), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 1), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 5), false);
                }
                else if (_BHMC == 2)
                {
                    _TZ = 4;
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 4), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 3), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 6), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 6), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 6), false);
                }
                else if (_BHMC == 3)
                {
                    BeiMing();
                    _BHMC = 0;
                    _TZ = 4;
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 7), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 4), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 2), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 8), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 8), false);
                }
            }
            else if (_JD == 2)
            {
                BeiMing();
                if (_BHMC == 1)
                {
                    _TZ = 5;
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 3), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 3), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 1), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 1), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 2), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 2), false);
                }
                else if (_BHMC == 2)
                {
                    _BHMC = 0;
                    _TZ = 5;
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 4), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 3), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 1), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 2), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 8), false);
                    owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 8), false);
                }
            }

            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(owner.faction == Faction.Player ? Faction.Enemy : Faction.Player);
            int zhenDing = 0;
            int biaoBen = 0;
            foreach (BattleUnitModel unit in aliveList)
            {
                if (unit == null) continue;
                zhenDing += unit.bufListDetail.GetKewordBufStack(KeywordHao.BF_ZhenDing);
                biaoBen += unit.bufListDetail.GetKewordBufStack(KeywordHao.BF_BiaoBenHua);
            }
            if (zhenDing >= 5)
            {
                _TZ++;
                owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 10), false);
            }
            if (biaoBen > 0)
            {
                _TZ++;
                owner.allyCardDetail.AddNewCard(new LorId("CaiFengZhiKing", 9), false);
            }
            foreach (BattleDiceCardModel card in owner.allyCardDetail.GetHand())
            {
                card.SetCostToZero(true);
            }
        }

        public override BattleUnitModel ChangeAttackTarget(BattleDiceCardModel card, int idx)
        {
            if (card == null || card.owner == null) return base.ChangeAttackTarget(card, idx);

            LorId cardId = card.GetID();
            if (!cardId.Equals(new LorId("CaiFengZhiKing", 9))) return base.ChangeAttackTarget(card, idx);

            List<BattleUnitModel> candidates = new List<BattleUnitModel>();
            foreach (BattleUnitModel u in BattleObjectManager.instance.GetAliveList(Faction.Player))
            {
                if (u?.bufListDetail == null) continue;
                if (u.bufListDetail.GetActivatedBufList().Exists(x => x.bufType == KeywordHao.BF_BiaoBenHua))
                    candidates.Add(u);
            }
            if (candidates.Count > 0)
                return candidates[UnityEngine.Random.Range(0, candidates.Count)];

            if (cardId == new LorId("CaiFengZhiKing", 6))
            {
                List<BattleUnitModel> allies = BattleObjectManager.instance.GetAliveList(owner.faction);
                allies.Remove(owner);
                allies.RemoveAll(x => !x.IsTargetable(owner));
                if (allies.Count > 0) return RandomUtil.SelectOne(allies);
            }

            return base.ChangeAttackTarget(card, idx);
        }
    }

    public class PassiveAbility_4CaiFengKai_CFK : PassiveAbilityBase
    {
        public override void OnRoundStart()
        {
            foreach (BattleUnitModel unit in BattleObjectManager.instance.GetAliveList_random(owner.faction == Faction.Enemy ? Faction.Player : Faction.Enemy, 2))
            {
                unit.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordHao.BF_CiSheZ, 1, owner);
            }
        }
    }

    public class PassiveAbility_5YangGuang_CFK : PassiveAbilityBase
    {
        public override void OnWaveStart()
        {
            owner.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordHao.BF_YangGuang_CFK, 1, owner);
            if (owner.faction == Faction.Player) return;

            Singleton<StageController>.Instance.GetStageModel().SetCurrentMapInfo(0);
            int emotionTotalCoinNumber = Singleton<StageController>.Instance.GetCurrentStageFloorModel().team.emotionTotalCoinNumber;
            Singleton<StageController>.Instance.GetCurrentWaveModel().team.emotionTotalBonus = emotionTotalCoinNumber + 1;
            foreach (BattleUnitModel unit in BattleObjectManager.instance.GetAliveList(Faction.Player))
            {
                unit.bufListDetail.AddBuf(new BattleUnitBuf_battle123
                {
                    hpAdder = 50,
                    breakGageAdder = 30
                });
                unit.SetHp(unit.MaxHp);
                unit.breakDetail.breakGauge = unit.breakDetail.GetDefaultBreakGauge();
            }
        }

        public override void OnRoundStart()
        {
            if (owner.bufListDetail.GetActivatedBuf(Y_KeywordBuf.KeywordBufs.Sanity) is BattleUnitBuf_Sanity ownerSanity)
            {
                ownerSanity.OnSanityUpdate(20);
            }
        }

        public class BattleUnitBuf_battle123 : BattleUnitBuf
        {
            public BattleUnitBuf_battle123()
            {
                stack = 0;
            }

            public override StatBonus GetStatBonus() => new()
            {
                hpAdder = hpAdder,
                breakGageAdder = breakGageAdder
            };

            public int hpAdder;
            public int breakGageAdder;
        }
    }

    public class PassiveAbility_6FengRen_CFK : PassiveAbilityBase
    {
        public override void OnSucceedAttack(BattleDiceBehavior behavior)
        {
            BattleUnitModel target = behavior.card.target;
            if (behavior.Detail == BehaviourDetail.Penetrate && RandomUtil.valueForProb < 0.33f)
            {
                target.bufListDetail.AddKeywordBufThisRoundByEtc(KeywordHao.BF_ZhenDing, 1, owner);
            }
            base.OnSucceedAttack(behavior);
        }
    }
}

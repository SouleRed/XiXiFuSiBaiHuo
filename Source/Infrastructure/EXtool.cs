
using EnumExtenderV2;
using LOR_DiceSystem;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace 裁缝之王
{
    public static class ExtraTools
    {
        public static BattleDiceCardModel GetCard(LorId cardId, BattleUnitModel owner)
        {
            DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(cardId, false);
            bool flag = cardItem == null;
            BattleDiceCardModel result;
            if (flag)
            {
                result = null;
            }
            else
            {
                BattleDiceCardModel battleDiceCardModel = BattleDiceCardModel.CreatePlayingCard(cardItem);
                battleDiceCardModel.owner = owner;
                result = battleDiceCardModel;
            }
            return result;
        }

        public static void AddCardAttack(BattleUnitModel owner, BattleUnitModel target, LorId cardid)
        {
            BattleDiceCardModel card = ExtraTools.GetCard(cardid, owner);
            BattlePlayingCardDataInUnitModel battlePlayingCardDataInUnitModel = new BattlePlayingCardDataInUnitModel();
            battlePlayingCardDataInUnitModel.card = card;
            battlePlayingCardDataInUnitModel.owner = owner;
            battlePlayingCardDataInUnitModel.speedDiceResultValue = 9999;
            battlePlayingCardDataInUnitModel.target = target;
            battlePlayingCardDataInUnitModel.subTargets = new List<BattlePlayingCardDataInUnitModel.SubTarget>();
            battlePlayingCardDataInUnitModel.earlyTarget = target;
            battlePlayingCardDataInUnitModel.earlyTargetOrder = -1;
            battlePlayingCardDataInUnitModel.cardAbility = card.CreateDiceCardSelfAbilityScript();
            bool flag = battlePlayingCardDataInUnitModel.cardAbility != null;
            if (flag)
            {
                battlePlayingCardDataInUnitModel.cardAbility.card = battlePlayingCardDataInUnitModel;
                battlePlayingCardDataInUnitModel.cardAbility.OnApplyCard();
            }
            battlePlayingCardDataInUnitModel.ResetCardQueue();
            Singleton<StageController>.Instance.AddAllCardListInBattle(battlePlayingCardDataInUnitModel, target, -1);
            bool flag2 = battlePlayingCardDataInUnitModel.card.GetSpec().Ranged == CardRange.FarArea || battlePlayingCardDataInUnitModel.card.GetSpec().Ranged == CardRange.FarAreaEach;
            if (flag2)
            {
                List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList((owner.faction == Faction.Player) ? Faction.Enemy : Faction.Player);
                battlePlayingCardDataInUnitModel.subTargets = new List<BattlePlayingCardDataInUnitModel.SubTarget>();
                foreach (BattleUnitModel battleUnitModel in aliveList)
                {
                    bool flag3 = battleUnitModel != battlePlayingCardDataInUnitModel.target && battleUnitModel.IsTargetable(owner) && !battleUnitModel.IsDead();
                    if (flag3)
                    {
                        BattlePlayingCardDataInUnitModel.SubTarget subTarget = new BattlePlayingCardDataInUnitModel.SubTarget();
                        subTarget.targetSlotOrder = 0;
                        subTarget.target = battleUnitModel;
                        battlePlayingCardDataInUnitModel.subTargets.Add(subTarget);
                    }
                }
            }
        }

        public static TEnum AddEnum<TEnum>(string name) where TEnum : struct, Enum
        {
            bool flag = string.IsNullOrEmpty(name);
            TEnum result;
            if (flag)
            {
                result = default(TEnum);
            }
            else
            {
                TEnum tenum;
                bool flag2 = EnumExtender.TryGetValueOf<TEnum>(name, out tenum);
                if (flag2)
                {
                    result = tenum;
                }
                else
                {
                    bool flag3 = EnumExtender.TryFindUnnamedValue<TEnum>(new TEnum?(default(TEnum)), null, false, out tenum) && EnumExtender.TryAddName<TEnum>(name, tenum, false);
                    if (flag3)
                    {
                        result = tenum;
                    }
                    else
                    {
                        result = default(TEnum);
                    }
                }
            }
            return result;
        }

        public static LorId MakeLorId(int id)
        {
            return new LorId("Gustab.DonQuixote", id);
        }

        public static bool IsAttackDice(BehaviourDetail detail)
        {
            return detail >= BehaviourDetail.Slash || detail <= BehaviourDetail.Hit;
        }

        public static bool IsDefenseDice(BehaviourDetail detail)
        {
            return detail == BehaviourDetail.Guard || detail == BehaviourDetail.Evasion;
        }

        public static void SetStageData(string name, object value)
        {
            Singleton<StageController>.Instance.GetStageModel().SetStageStorgeData(name, value);
        }

        public static bool GetStageData<TValue>(string name, out TValue value)
        {
            return Singleton<StageController>.Instance.GetStageModel().GetStageStorageData<TValue>(name, out value);
        }

        public static void RemovePassive(BattleUnitModel unit, LorId lorId)
        {
            foreach (PassiveAbilityBase passiveAbilityBase in unit.passiveDetail.PassiveList)
            {
                bool flag = passiveAbilityBase.id == lorId;
                if (flag)
                {
                    passiveAbilityBase.destroyed = true;
                }
            }
        }

        public static bool HasPassive(BattleUnitModel unit, LorId lorId)
        {
            PassiveXmlInfo data = Singleton<PassiveXmlList>.Instance.GetData(lorId);
            foreach (PassiveAbilityBase passiveAbilityBase in unit.passiveDetail.PassiveList)
            {
                bool flag = passiveAbilityBase.id == lorId;
                if (flag)
                {
                    return true;
                }
                bool flag2 = data == null;
                if (!flag2)
                {
                    bool flag3 = passiveAbilityBase.GetType().Name == "PassiveAbility_" + data.script;
                    if (flag3)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}


using LOR_DiceSystem;
using NAudio.Wave;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using 裁缝之王.Source.Shared.Tool;

namespace 裁缝之王.Shared.Tool;

public static class BattleTools
{
    public static void SetStageData(string name, object value)//拉取战斗bgm用
    {
        Singleton<StageController>.Instance.GetStageModel().SetStageStorgeData(name, value);
    }

    public static bool GetStageData<TValue>(string name, out TValue value)//拉取战斗bgm用
    {
        return Singleton<StageController>.Instance.GetStageModel().GetStageStorageData<TValue>(name, out value);
    }
    public static bool TryGetAudioClip(string path, out AudioClip clip)//拉取战斗bgm用
    {
        bool flag = File.Exists(path);
        if (flag)
        {
            bool flag2 = path.EndsWith(".mp3");
            if (flag2)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                using (Mp3FileReader mp3FileReader = new Mp3FileReader(path))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        WaveFileWriter.WriteWavFileToStream(memoryStream, mp3FileReader);
                        WAV wav = new WAV(memoryStream.ToArray());
                        clip = AudioClip.Create(fileNameWithoutExtension, wav.SampleCount, 1, wav.Frequency, false);
                        clip.SetData(wav.LeftChannel, 0);
                        return true;
                    }
                }
            }
            bool flag3 = path.EndsWith(".wav");
            if (flag3)
            {
                string fileNameWithoutExtension2 = Path.GetFileNameWithoutExtension(path);
                WAV wav2 = new WAV(File.ReadAllBytes(path));
                clip = AudioClip.Create(fileNameWithoutExtension2, wav2.SampleCount, 1, wav2.Frequency, false);
                clip.SetData(wav2.LeftChannel, 0);
                return true;
            }
        }
        clip = null;
        return false;
    }

    public static BattleDiceCardModel GetCard(LorId cardId, BattleUnitModel owner)
    {
        DiceCardXmlInfo cardItem = ItemXmlDataList.instance.GetCardItem(cardId, false);
        if (cardItem == null) return null;

        BattleDiceCardModel card = BattleDiceCardModel.CreatePlayingCard(cardItem);
        card.owner = owner;
        return card;
    }

    public static void AddCardAttack(BattleUnitModel owner, BattleUnitModel target, LorId cardId)
    {
        BattleDiceCardModel card = GetCard(cardId, owner);
        if (card == null) return;

        BattlePlayingCardDataInUnitModel playingCard = new()
        {
            card = card,
            owner = owner,
            speedDiceResultValue = 9999,
            target = target,
            subTargets = new List<BattlePlayingCardDataInUnitModel.SubTarget>(),
            earlyTarget = target,
            earlyTargetOrder = -1,
            cardAbility = card.CreateDiceCardSelfAbilityScript()
        };
        if (playingCard.cardAbility != null)
        {
            playingCard.cardAbility.card = playingCard;
            playingCard.cardAbility.OnApplyCard();
        }
        playingCard.ResetCardQueue();
        Singleton<StageController>.Instance.AddAllCardListInBattle(playingCard, target, -1);

        CardRange ranged = playingCard.card.GetSpec().Ranged;
        if (ranged is CardRange.FarArea or CardRange.FarAreaEach)
        {
            List<BattleUnitModel> aliveList = BattleObjectManager.instance.GetAliveList(owner.faction == Faction.Player ? Faction.Enemy : Faction.Player);
            foreach (BattleUnitModel unit in aliveList)
            {
                if (unit != playingCard.target && unit.IsTargetable(owner) && !unit.IsDead())
                {
                    playingCard.subTargets.Add(new BattlePlayingCardDataInUnitModel.SubTarget
                    {
                        targetSlotOrder = 0,
                        target = unit
                    });
                }
            }
        }
    }
}

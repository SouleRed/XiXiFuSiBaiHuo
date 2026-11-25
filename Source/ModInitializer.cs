using AutoKeywordUtil;
using HarmonyLib;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Y_LimbusBuff.Method;
using 裁缝之王.Presentation.CustomCombat;
using 裁缝之王.Shared.Interop;

namespace 裁缝之王;

public class CaiFengKing_Initializer : global::ModInitializer
{
    public const string ModPackage = "CaiFengKing";
    public static string packageId = "CaiFengKing";
    public static string _packageId = "CaiFengKing";
    public static string currentID = "CaiFengKing";
    public static string PackageId => _packageId;

    public static string path = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
    public static readonly Dictionary<string, AudioClip> AudioClips = new(StringComparer.OrdinalIgnoreCase);

    public static LorId GetLorId(int id) => new(_packageId, id);

    public override void OnInitializeMod()
    {
        NativeKingSkin.LoadSkins(path);
        NativeStageMap.LoadMaps(path);
        RedDiceInterop.Initialize();
        CustomCombatRuntime.Register();
        AddAudioClips(new DirectoryInfo(Path.Combine(path, "BGM")));
        YInitializer.ActivateMods.Add("CaiFengKing");
        AutoKeywordUtils.RegisterKeywordsFromAssembly(Assembly.GetExecutingAssembly());

        Harmony harmony = new(_packageId);
        foreach (Type type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
        {
            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception e)
            {
                Debug.LogError($"[裁缝之王][Harmony] {type.FullName}: 注册失败 {e}");
            }
        }
    }

    public static string GetBattleDlg(LOR_XML.DialogType dlgType, string character)
    {
        LOR_XML.BattleDialogCharacter characterData = Singleton<BattleDialogXmlList>.Instance.GetCharacterData("CaiFengKing", character);
        if (characterData == null) return string.Empty;

        foreach (LOR_XML.BattleDialogType dialogType in characterData.dialogTypeList)
        {
            if (dialogType.dialogType == dlgType && dialogType.dialogList.Count > 0)
                return dialogType.dialogList[UnityEngine.Random.Range(0, dialogType.dialogList.Count)].dialogContent;
        }
        return string.Empty;
    }

    private static void AddAudioClips(DirectoryInfo dir)
    {
        if (dir == null || !dir.Exists) return;
        foreach (DirectoryInfo sub in dir.GetDirectories()) AddAudioClips(sub);
        foreach (FileInfo f in dir.GetFiles())
        {
            string ext = f.Extension.ToLowerInvariant();
            if (ext != ".wav" && ext != ".mp3") continue;
            try
            {
                string key = Path.GetFileNameWithoutExtension(f.Name);
                AudioClip clip = ext == ".mp3" ? LoadMp3Clip(f.FullName) : LoadWavClip(f.FullName);
                if (clip) AudioClips[key] = clip;
            }
            catch (Exception e)
            {
                Debug.LogError($"[裁缝之王] 音频加载失败：{f.FullName}\n{e}");
            }
        }
    }

    private static AudioClip LoadMp3Clip(string mp3Path)
    {
        string tempWav = mp3Path + ".wav";
        try
        {
            using (var mp3 = new Mp3FileReader(mp3Path))
                WaveFileWriter.CreateWaveFile(tempWav, mp3);
            return LoadWavClip(tempWav);
        }
        finally
        {
            if (File.Exists(tempWav)) File.Delete(tempWav);
        }
    }

    private static AudioClip LoadWavClip(string wavPath)
    {
        var w = new WavData(File.ReadAllBytes(wavPath));
        if (w.LeftChannel == null || w.SampleCount <= 0 || w.ChannelCount <= 0) return null;

        float[] interleaved;
        if (w.ChannelCount == 1)
        {
            interleaved = w.LeftChannel;
        }
        else
        {
            interleaved = new float[w.SampleCount * 2];
            for (int i = 0; i < w.SampleCount; i++)
            {
                interleaved[i * 2] = w.LeftChannel[i];
                interleaved[i * 2 + 1] = w.RightChannel[i];
            }
        }

        AudioClip clip = AudioClip.Create(Path.GetFileNameWithoutExtension(wavPath), w.SampleCount, w.ChannelCount, w.Frequency, false);
        clip.SetData(interleaved, 0);
        return clip;
    }

    private sealed class WavData
    {
        internal readonly float[] LeftChannel;
        internal readonly float[] RightChannel;
        internal readonly int ChannelCount;
        internal readonly int SampleCount;
        internal readonly int Frequency;

        internal WavData(byte[] wav)
        {
            ChannelCount = wav[22];
            Frequency = wav[24] | (wav[25] << 8) | (wav[26] << 16) | (wav[27] << 24);
            int pos = 12;
            while (pos + 8 <= wav.Length && (wav[pos] != 'd' || wav[pos + 1] != 'a' || wav[pos + 2] != 't' || wav[pos + 3] != 'a'))
            {
                int chunkSize = wav[pos + 4] | (wav[pos + 5] << 8) | (wav[pos + 6] << 16) | (wav[pos + 7] << 24);
                pos += 8 + chunkSize;
            }
            pos += 8;
            SampleCount = Math.Max(0, (wav.Length - pos) / 2);
            if (ChannelCount == 2) SampleCount /= 2;
            LeftChannel = new float[SampleCount];
            RightChannel = ChannelCount == 2 ? new float[SampleCount] : null;
            for (int i = 0; i < SampleCount && pos + 1 < wav.Length; i++)
            {
                LeftChannel[i] = (short)((wav[pos + 1] << 8) | wav[pos]) / 32768f;
                pos += 2;
                if (ChannelCount == 2 && pos + 1 < wav.Length)
                {
                    RightChannel[i] = (short)((wav[pos + 1] << 8) | wav[pos]) / 32768f;
                    pos += 2;
                }
            }
        }
    }
}

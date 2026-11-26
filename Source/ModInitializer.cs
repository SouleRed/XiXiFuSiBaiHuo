using AutoKeywordUtil;
using HarmonyLib;
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
            if (!f.Extension.Equals(".wav", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                string key = Path.GetFileNameWithoutExtension(f.Name);
                AudioClip clip = LoadWavClip(f.FullName);
                if (clip) AudioClips[key] = clip;
            }
            catch (Exception e)
            {
                Debug.LogError($"[裁缝之王] 音频加载失败：{f.FullName}\n{e}");
            }
        }
    }

    private static AudioClip LoadWavClip(string wavPath)
    {
        byte[] wav = File.ReadAllBytes(wavPath);
        if (wav.Length < 44) return null;

        int channels = wav[22] | (wav[23] << 8);
        int frequency = wav[24] | (wav[25] << 8) | (wav[26] << 16) | (wav[27] << 24);
        if (channels <= 0 || frequency <= 0) return null;

        int pos = 12;
        int dataSize = 0;
        while (pos + 8 <= wav.Length)
        {
            int chunkSize = wav[pos + 4] | (wav[pos + 5] << 8) | (wav[pos + 6] << 16) | (wav[pos + 7] << 24);
            if (wav[pos] == 'd' && wav[pos + 1] == 'a' && wav[pos + 2] == 't' && wav[pos + 3] == 'a')
            {
                pos += 8;
                dataSize = Math.Min(Math.Max(0, chunkSize), wav.Length - pos);
                break;
            }
            pos += 8 + chunkSize + (chunkSize & 1);
        }

        int totalSamples = dataSize / 2;
        int sampleFrames = totalSamples / channels;
        if (sampleFrames <= 0) return null;

        float[] data = new float[sampleFrames * channels];
        for (int i = 0; i < data.Length; i++, pos += 2)
            data[i] = (short)((wav[pos + 1] << 8) | wav[pos]) / 32768f;

        AudioClip clip = AudioClip.Create(Path.GetFileNameWithoutExtension(wavPath), sampleFrames, channels, frequency, false);
        clip.SetData(data, 0);
        return clip;
    }
}

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
        var selfAssembly = Assembly.GetExecutingAssembly();
        LoadDeferredRedDiceAbilities(selfAssembly, path);
        NativeKingSkin.LoadSkins(path);
        NativeStageMap.LoadMaps(path);
        RedDiceInterop.Initialize();
        CustomCombatRuntime.Register();
        AddAudioClips(new DirectoryInfo(Path.Combine(path, "BGM")));
        YInitializer.ActivateMods.Add("CaiFengKing");
        AutoKeywordUtils.RegisterKeywordsFromAssembly(selfAssembly);

        Harmony harmony = new(_packageId);
        foreach (Type type in AccessTools.GetTypesFromAssembly(selfAssembly))
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

    private static bool _resolverInstalled;
    private static Assembly _selfAssemblyCache;
    private static Assembly _redDiceAssemblyCache;

    // 依赖 RedDice 的书页/骰子脚本独立打包在 Assemblies\Deferred\XiXiFuSiBaiHuo.RedDice.dll，
    // 避开阶段 1 原版扫描时因 MOD 加载顺序导致的 Mono 引用缺失缓存；
    // OnInitializeMod 执行时所有前置 MOD 均已加载完毕，挂好 AssemblyResolve 后再加载并去重注册。
    private static void LoadDeferredRedDiceAbilities(Assembly selfAssembly, string assembliesDir)
    {
        string deferredPath = Path.Combine(assembliesDir, "Deferred", "XiXiFuSiBaiHuo.RedDice.dll");
        if (!File.Exists(deferredPath))
        {
            Debug.LogWarning($"[裁缝之王][RedDice] 未找到延迟程序集：{deferredPath}");
            return;
        }

        Assembly redDiceAssembly = null;
        foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (string.Equals(loaded.GetName().Name, "RedDice", StringComparison.OrdinalIgnoreCase))
            {
                redDiceAssembly = loaded;
                break;
            }
        }

        if (redDiceAssembly == null)
        {
            Debug.LogWarning("[裁缝之王][RedDice] 未检测到 RedDice 程序集，跳过红骰书页与骰子脚本加载。");
            return;
        }

        _selfAssemblyCache = selfAssembly;
        _redDiceAssemblyCache = redDiceAssembly;
        if (!_resolverInstalled)
        {
            _resolverInstalled = true;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveDeferredDependency;
        }

        Assembly deferredAssembly;
        try
        {
            deferredAssembly = Assembly.LoadFile(deferredPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[裁缝之王][RedDice] 延迟程序集加载失败：{deferredPath}\n{e}");
            return;
        }

        var manager = Singleton<AssemblyManager>.Instance;
        if (manager == null) return;

        const string selfPrefix = "DiceCardSelfAbility_";
        const string dicePrefix = "DiceCardAbility_";
        int selfCount = 0;
        int diceCount = 0;
        foreach (Type type in AccessTools.GetTypesFromAssembly(deferredAssembly))
        {
            if (type == null || type.IsAbstract) continue;
            if (type.IsSubclassOf(typeof(DiceCardSelfAbilityBase)) && type.Name.StartsWith(selfPrefix, StringComparison.Ordinal))
            {
                string key = type.Name.Substring(selfPrefix.Length);
                if (!manager._diceCardSelfAbilityDict._dict.ContainsKey(key))
                {
                    manager._diceCardSelfAbilityDict.Add(key, type);
                    selfCount++;
                }
            }
            else if (type.IsSubclassOf(typeof(DiceCardAbilityBase)) && type.Name.StartsWith(dicePrefix, StringComparison.Ordinal))
            {
                string key = type.Name.Substring(dicePrefix.Length);
                if (!manager._diceCardAbilityDict._dict.ContainsKey(key))
                {
                    manager._diceCardAbilityDict.Add(key, type);
                    diceCount++;
                }
            }
        }

        AutoKeywordUtils.RegisterKeywordsFromAssembly(deferredAssembly);
        Debug.Log($"[裁缝之王][RedDice] 已延迟注册红骰脚本：书页={selfCount}, 骰子={diceCount}");
    }

    private static Assembly ResolveDeferredDependency(object sender, ResolveEventArgs args)
    {
        if (string.IsNullOrEmpty(args?.Name)) return null;
        string shortName = new AssemblyName(args.Name).Name;
        if (string.Equals(shortName, "RedDice", StringComparison.OrdinalIgnoreCase))
            return _redDiceAssemblyCache;
        if (string.Equals(shortName, _selfAssemblyCache?.GetName().Name, StringComparison.OrdinalIgnoreCase))
            return _selfAssemblyCache;
        return null;
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

using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace 裁缝之王.Shared.Interop;

/// <summary>
/// 与《红与黑》(LeRougeEtLeNoir) 的运行时弱耦合联动：
/// 不在编译期硬引用 LeRougeEtLeNoir.dll，在 OnInitializeMod 时按已加载程序集绑定委托并挂载悲鸣保活补丁。
/// </summary>
internal static class LeRougeInterop
{
    private static Type _kingPassiveType;
    private static Type _weakCryType;
    private static Type _malignantCryType;
    private static Type _complaintsPassiveType;

    private static Func<BattleUnitModel, BattleUnitModel> _findSinclairAlly;
    private static Func<BattleUnitModel, BattleUnitBuf> _findWeakCry;
    private static Func<BattleUnitModel, BattleUnitBuf> _gainWeakCry;
    private static Func<BattleUnitModel, BattleUnitBuf> _findMalignantCry;
    private static Action<BattleUnitModel, int> _gainMalignantCry;
    private static Func<BattleUnitModel, int> _getSharpNails;
    private static Func<BattleUnitModel, BattleUnitBuf> _findPin;
    private static Action<BattleUnitModel, int> _gainPin;
    private static Func<BattleUnitModel, int> _getAbject;

    private static FieldInfo _weakCryHealsField;
    private static FieldInfo _malignantCryHealsField;

    internal static bool Available { get; private set; }

    internal static void Initialize(Harmony harmony)
    {
        Available = false;

        Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, "LeRougeEtLeNoir", StringComparison.OrdinalIgnoreCase));
        if (assembly == null) return;

        const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        _kingPassiveType = assembly.GetType("LeRougeEtLeNoir.LeRouge.KingOfTailorsHeathcliff.Passives.PassiveAbility_LRH_King", false);
        _complaintsPassiveType = assembly.GetType("LeRougeEtLeNoir.LeRouge.KingOfTailorsHeathcliff.Passives.PassiveAbility_LRH_Complaints", false);
        _weakCryType = assembly.GetType("LeRougeEtLeNoir.LeRouge.KingOfTailorsHeathcliff.Buffs.BattleUnitBuf_LRH_WeakCry", false);
        _malignantCryType = assembly.GetType("LeRougeEtLeNoir.LeRouge.KingOfTailorsHeathcliff.Buffs.BattleUnitBuf_LRH_MalignantCry", false);
        Type sharpNailsType = assembly.GetType("LeRougeEtLeNoir.LeRouge.KingOfTailorsHeathcliff.Buffs.BattleUnitBuf_LRH_SharpNails", false);
        Type pinType = assembly.GetType("LeRougeEtLeNoir.LeRouge.KingOfTailorsHeathcliff.Buffs.BattleUnitBuf_LRH_Pin", false);
        Type abjectType = assembly.GetType("LeRougeEtLeNoir.LeRouge.RedGodSinclair.Buffs.BattleUnitBuf_LRS_Abject", false);
        Type heathcliffPagesType = assembly.GetType("LeRougeEtLeNoir.LeRouge.KingOfTailorsHeathcliff.Cards.HeathcliffPages", false);

        if (_kingPassiveType == null || _complaintsPassiveType == null || _weakCryType == null
            || _malignantCryType == null || sharpNailsType == null || pinType == null || heathcliffPagesType == null)
        {
            Debug.LogWarning("[裁缝之王][红与黑联动] 目标类型不完整，跳过联动初始化。");
            return;
        }

        _findSinclairAlly = BindFunc<BattleUnitModel, BattleUnitModel>(heathcliffPagesType, "FindSinclairAlly", AnyStatic);
        _findWeakCry = BindBufFunc(_weakCryType, "Find", AnyStatic);
        _gainWeakCry = BindBufFunc(_weakCryType, "GainBuf", AnyStatic);
        _findMalignantCry = BindBufFunc(_malignantCryType, "Find", AnyStatic);
        _gainMalignantCry = BindAction<BattleUnitModel, int>(_malignantCryType, "GainBuf", AnyStatic);
        _getSharpNails = BindFunc<BattleUnitModel, int>(sharpNailsType, "GetBuf", AnyStatic);
        _findPin = BindBufFunc(pinType, "Find", AnyStatic);
        _gainPin = BindAction<BattleUnitModel, int>(pinType, "GainBuf", AnyStatic);
        _getAbject = abjectType != null ? BindFunc<BattleUnitModel, int>(abjectType, "GetBuf", AnyStatic) : null;

        _weakCryHealsField = _weakCryType.GetField("_heals", AnyInstance);
        _malignantCryHealsField = _malignantCryType.GetField("_heals", AnyInstance);

        if (_findSinclairAlly == null || _findWeakCry == null || _gainWeakCry == null
            || _findMalignantCry == null || _gainMalignantCry == null || _getSharpNails == null
            || _findPin == null || _gainPin == null)
        {
            Debug.LogWarning("[裁缝之王][红与黑联动] 部分核心方法绑定失败，跳过联动初始化。");
            return;
        }

        Available = true;
        PatchCryWithoutSinclair(harmony, AnyInstance);
        Debug.Log("[裁缝之王][红与黑联动] 已绑定红与黑裁缝之王希斯克里夫联动。");
    }

    internal static bool IsHeathcliffKing(BattleUnitModel unit)
    {
        if (!Available || unit?.passiveDetail == null || _kingPassiveType == null) return false;
        foreach (PassiveAbilityBase passive in unit.passiveDetail.PassiveList)
        {
            if (passive != null && !passive.destroyed && _kingPassiveType.IsInstanceOfType(passive))
                return true;
        }
        return false;
    }

    internal static bool HasWarmLightPassive(BattleUnitModel unit)
    {
        if (unit?.passiveDetail == null) return false;
        foreach (PassiveAbilityBase passive in unit.passiveDetail.PassiveList)
        {
            if (passive is PassiveAbility_8LRH_WarmLight_CFK && !passive.destroyed)
                return true;
        }
        return false;
    }

    internal static bool HasCry(BattleUnitModel unit) => GetCryTier(unit) > 0;

    internal static int GetCryTier(BattleUnitModel unit)
    {
        if (!Available || unit == null) return 0;
        if (_findMalignantCry(unit) != null) return 2;
        if (_findWeakCry(unit) != null) return 1;
        return 0;
    }

    internal static int GetSharpNails(BattleUnitModel unit)
        => Available && unit != null ? _getSharpNails(unit) : 0;

    internal static int GetPinStack(BattleUnitModel unit)
        => Available && unit != null ? (_findPin(unit)?.stack ?? 0) : 0;

    internal static int GetAbjectStack(BattleUnitModel unit)
        => Available && unit != null && _getAbject != null ? _getAbject(unit) : 0;

    internal static void GainPin(BattleUnitModel unit, int amount)
    {
        if (Available && unit != null && amount > 0) _gainPin(unit, amount);
    }

    private static void PatchCryWithoutSinclair(Harmony harmony, BindingFlags instanceFlags)
    {
        if (harmony == null) return;
        try
        {
            MethodInfo weakStart = _weakCryType.GetMethod("OnRoundStart", instanceFlags);
            MethodInfo malignantStart = _malignantCryType.GetMethod("OnRoundStart", instanceFlags);
            MethodInfo complaintsStart = _complaintsPassiveType.GetMethod("OnRoundStart", instanceFlags);

            if (weakStart != null)
                harmony.Patch(weakStart, prefix: new HarmonyMethod(typeof(LeRougeInterop), nameof(WeakCry_OnRoundStart_Prefix)));
            if (malignantStart != null)
                harmony.Patch(malignantStart, prefix: new HarmonyMethod(typeof(LeRougeInterop), nameof(MalignantCry_OnRoundStart_Prefix)));
            if (complaintsStart != null)
                harmony.Patch(complaintsStart, prefix: new HarmonyMethod(typeof(LeRougeInterop), nameof(Complaints_OnRoundStart_Prefix)));
        }
        catch (Exception e)
        {
            Debug.LogError($"[裁缝之王][红与黑联动] 悲鸣联动补丁注册失败：{e}");
        }
    }

    private static bool WeakCry_OnRoundStart_Prefix(BattleUnitBuf __instance)
    {
        BattleUnitModel owner = __instance?._owner;
        if (owner == null || !HasWarmLightPassive(owner) || !IsHeathcliffKing(owner)) return true;
        if (_findSinclairAlly(owner) != null) return true;

        _weakCryHealsField?.SetValue(__instance, 0);
        return false;
    }

    private static bool MalignantCry_OnRoundStart_Prefix(BattleUnitBuf __instance)
    {
        BattleUnitModel owner = __instance?._owner;
        if (owner == null || !HasWarmLightPassive(owner) || !IsHeathcliffKing(owner)) return true;
        if (_findSinclairAlly(owner) != null) return true;

        _malignantCryHealsField?.SetValue(__instance, 0);
        return false;
    }

    private static bool Complaints_OnRoundStart_Prefix(PassiveAbilityBase __instance)
    {
        BattleUnitModel owner = __instance?.owner;
        if (owner == null || !HasWarmLightPassive(owner) || !IsHeathcliffKing(owner)) return true;
        if (_findSinclairAlly(owner) != null) return true;

        if (_findMalignantCry(owner) != null) return false;
        BattleUnitBuf cry = _gainWeakCry(owner);
        if (cry == null || cry.stack < 5) return false;

        _gainMalignantCry(owner, cry.stack);
        cry.Destroy();
        return false;
    }

    private static Func<T1, TResult> BindFunc<T1, TResult>(Type type, string methodName, BindingFlags flags)
    {
        MethodInfo method = type?.GetMethod(methodName, flags, null, new[] { typeof(T1) }, null);
        return method != null && method.ReturnType == typeof(TResult)
            ? (Func<T1, TResult>)Delegate.CreateDelegate(typeof(Func<T1, TResult>), method)
            : null;
    }

    private static Func<BattleUnitModel, BattleUnitBuf> BindBufFunc(Type type, string methodName, BindingFlags flags)
    {
        MethodInfo method = type?.GetMethod(methodName, flags, null, new[] { typeof(BattleUnitModel) }, null);
        if (method == null || !typeof(BattleUnitBuf).IsAssignableFrom(method.ReturnType)) return null;
        return unit => (BattleUnitBuf)method.Invoke(null, new object[] { unit });
    }

    private static Action<T1, T2> BindAction<T1, T2>(Type type, string methodName, BindingFlags flags)
    {
        MethodInfo method = type?.GetMethod(methodName, flags, null, new[] { typeof(T1), typeof(T2) }, null);
        return method != null && method.ReturnType == typeof(void)
            ? (Action<T1, T2>)Delegate.CreateDelegate(typeof(Action<T1, T2>), method)
            : null;
    }
}

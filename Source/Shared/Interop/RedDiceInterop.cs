using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace 裁缝之王.Shared.Interop;

/// <summary>启动期检测可选联动 MOD，绑定强类型委托以避免战斗期反射开销。</summary>
internal static class RedDiceInterop
{
    internal static Action<BattleParryingManager.ParryingTeam, BattleParryingManager.ParryingTeam> CheckParryingEnd { get; private set; }

    internal static void Initialize()
    {
        CheckParryingEnd = null;
        Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == "RedDice");
        if (assembly == null) return;

        Type initializer = assembly.GetType("RedDice.RedDiceInitializer", false);
        MethodInfo method = initializer?.GetMethod(
            "BattleParryingManager_CheckParryingEnd_Pre",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(BattleParryingManager.ParryingTeam), typeof(BattleParryingManager.ParryingTeam) },
            null);

        if (method != null && method.ReturnType == typeof(void))
        {
            CheckParryingEnd = (Action<BattleParryingManager.ParryingTeam, BattleParryingManager.ParryingTeam>)Delegate.CreateDelegate(
                typeof(Action<BattleParryingManager.ParryingTeam, BattleParryingManager.ParryingTeam>),
                method);
            return;
        }

        Debug.LogWarning("[裁缝之王][联动] RedDice.BattleParryingManager_CheckParryingEnd_Pre 不存在或签名已改变，跳过此兼容补丁。");
    }
}

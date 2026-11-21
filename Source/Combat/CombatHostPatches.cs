using HarmonyLib;

namespace 裁缝之王.Presentation.CustomCombat;

internal static class CombatHostPatches
{
    internal static void Install(Harmony harmony)
    {
        foreach (var patch in typeof(CombatHostPatches).GetNestedTypes(System.Reflection.BindingFlags.NonPublic))
            harmony.CreateClassProcessor(patch).Patch();
    }

    [HarmonyPatch(typeof(RencounterManager), "get_CurRencounterState")]
    private static class State
    {
        [HarmonyPostfix]
        private static void Postfix(ref RencounterManager.RencounterState __result)
        {
            if (CustomCombatRuntime.IsRunning) __result = CustomCombatRuntime.Playback.CurRencounterState;
        }
    }

    [HarmonyPatch(typeof(RencounterManager), "get_curMovingSmoothTime")]
    private static class SmoothTime
    {
        [HarmonyPostfix]
        private static void Postfix(ref float __result)
        {
            if (CustomCombatRuntime.IsRunning) __result = CustomCombatRuntime.Playback.curMovingSmoothTime;
        }
    }

    [HarmonyPatch(typeof(RencounterManager), nameof(RencounterManager.Pause))]
    private static class Pause
    {
        [HarmonyPostfix]
        private static void Postfix(bool b)
        {
            if (CustomCombatRuntime.IsRunning) CustomCombatRuntime.Playback.Pause(b);
        }
    }

    [HarmonyPatch(typeof(BattleParryingManager), nameof(BattleParryingManager.GetCurrentWinner))]
    private static class Winner
    {
        [HarmonyPostfix]
        private static void Postfix(ref BattleUnitModel __result)
        {
            if (CustomCombatRuntime.Active) __result = CustomCombatRuntime.CurrentWinner;
        }
    }

    [HarmonyPatch(typeof(BattleOneSidePlayManager), nameof(BattleOneSidePlayManager.GetCurrentAttacker))]
    private static class Attacker
    {
        [HarmonyPostfix]
        private static void Postfix(ref BattleUnitModel __result)
        {
            if (CustomCombatRuntime.Active) __result = CustomCombatRuntime.CurrentAttacker;
        }
    }
}

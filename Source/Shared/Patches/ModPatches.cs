using HarmonyLib;
using UI;
using UnityEngine;
using 裁缝之王.Presentation.Actions;
using 裁缝之王.Presentation.CustomCombat;
using 裁缝之王.Presentation.Effects;

namespace 裁缝之王.Shared.Patches;

public static class ModPatches
{
    #region 自定义演出与拼点托管

    [HarmonyPatch(typeof(RencounterManager), "get_CurRencounterState")]
    public static class RencounterManager_CurRencounterState
    {
        [HarmonyPostfix]
        private static void Postfix(ref RencounterManager.RencounterState __result)
        {
            if (CustomCombatRuntime.IsRunning) __result = CustomCombatRuntime.Playback.CurRencounterState;
        }
    }

    [HarmonyPatch(typeof(RencounterManager), "get_curMovingSmoothTime")]
    public static class RencounterManager_CurMovingSmoothTime
    {
        [HarmonyPostfix]
        private static void Postfix(ref float __result)
        {
            if (CustomCombatRuntime.IsRunning) __result = CustomCombatRuntime.Playback.curMovingSmoothTime;
        }
    }

    [HarmonyPatch(typeof(RencounterManager), nameof(RencounterManager.Pause))]
    public static class RencounterManager_Pause
    {
        [HarmonyPostfix]
        private static void Postfix(bool b)
        {
            if (CustomCombatRuntime.IsRunning) CustomCombatRuntime.Playback.Pause(b);
        }
    }

    [HarmonyPatch(typeof(BattleParryingManager), nameof(BattleParryingManager.GetCurrentWinner))]
    public static class BattleParryingManager_GetCurrentWinner
    {
        [HarmonyPostfix]
        private static void Postfix(ref BattleUnitModel __result)
        {
            if (CustomCombatRuntime.Active) __result = CustomCombatRuntime.CurrentWinner;
        }
    }

    [HarmonyPatch(typeof(BattleOneSidePlayManager), nameof(BattleOneSidePlayManager.GetCurrentAttacker))]
    public static class BattleOneSidePlayManager_GetCurrentAttacker
    {
        [HarmonyPostfix]
        private static void Postfix(ref BattleUnitModel __result)
        {
            if (CustomCombatRuntime.Active) __result = CustomCombatRuntime.CurrentAttacker;
        }
    }

    #endregion

    #region 原生皮肤与身高

    [HarmonyPatch(typeof(BattleUnitView), nameof(BattleUnitView.ChangeHeight))]
    public static class BattleUnitView_ChangeHeight
    {
        [HarmonyPrepare]
        private static bool Prepare() => NativeKingSkin.HasKingSkin;

        [HarmonyPrefix]
        private static void Prefix(BattleUnitView __instance, BattleUnitView.SkinInfo ____skinInfo, ref int height)
            => NativeKingSkin.TryOverrideHeight(__instance, ____skinInfo, ref height);
    }

    [HarmonyPatch(typeof(BookXmlInfo), nameof(BookXmlInfo.GetThumbSprite))]
    public static class BookXmlInfo_GetThumbSprite
    {
        [HarmonyPrepare]
        private static bool Prepare() => NativeKingSkin.HasAnySkin;

        [HarmonyPostfix]
        private static void Postfix(BookXmlInfo __instance, ref Sprite __result)
            => NativeKingSkin.UsePrefabThumbnail(__instance, ref __result);
    }

    [HarmonyPatch(typeof(BookModel), nameof(BookModel.GetThumbSprite))]
    public static class BookModel_GetThumbSprite
    {
        [HarmonyPrepare]
        private static bool Prepare() => NativeKingSkin.HasAnySkin;

        [HarmonyPostfix]
        private static void Postfix(BookModel __instance, ref Sprite __result)
            => NativeKingSkin.UsePrefabThumbnail(__instance.ClassInfo, ref __result);
    }

    [HarmonyPatch(typeof(SdCharacterUtil), nameof(SdCharacterUtil.CreateSkin))]
    public static class SdCharacterUtil_CreateSkin
    {
        [HarmonyPrepare]
        private static bool Prepare() => NativeKingSkin.HasAnySkin;

        [HarmonyPrefix]
        private static bool Prefix(UnitDataModel unit, Faction faction, Transform characterRoot, ref CharacterAppearance __result)
            => NativeKingSkin.TryCreateBattleSkin(unit, faction, characterRoot, ref __result);
    }

    [HarmonyPatch(typeof(UICharacterRenderer), nameof(UICharacterRenderer.SetCharacter))]
    public static class UICharacterRenderer_SetCharacter
    {
        [HarmonyPrepare]
        private static bool Prepare() => NativeKingSkin.HasAnySkin;

        [HarmonyPrefix]
        private static bool Prefix(UICharacterRenderer __instance, UnitDataModel unit, int index, ref bool forcelyReload)
            => NativeKingSkin.TrySetPreviewSkin(__instance, unit, index, ref forcelyReload);
    }

    #endregion

    #region 裁缝之王动作连段与慢动作控制

    [HarmonyPatch(typeof(RencounterManager), "CheckMovingStateBeforeRoll")]
    public static class RencounterManager_CheckMovingStateBeforeRoll
    {
        [HarmonyPrepare]
        private static bool Prepare() => NativeKingSkin.HasKingSkin;

        [HarmonyPrefix]
        private static bool Prefix(RencounterManager __instance, BattleUnitView ____enemy, BattleUnitView ____librarian,
            BattleCardBehaviourResult ____currentEnemyBehaviourResult, BattleCardBehaviourResult ____currentLibrarianBehaviourResult)
        {
            KingContinuation.RestoreDelay();
            return !KingContinuation.Prepare(____enemy, ____librarian, ____currentEnemyBehaviourResult, ____currentLibrarianBehaviourResult,
                __instance.playAuto && !__instance.forbidAutoPlay, __instance._bNoDelay, __instance.SetNodelay);
        }
    }

    [HarmonyPatch(typeof(RencounterManager), nameof(RencounterManager.EndRencounterForcely))]
    public static class RencounterManager_EndRencounterForcely
    {
        [HarmonyPrepare]
        private static bool Prepare() => NativeKingSkin.HasKingSkin;

        [HarmonyPrefix]
        private static void Prefix(RencounterManager __instance)
            => RencounterEffects.CancelActive(__instance, stopAllEffects: true);
    }

    [HarmonyPatch(typeof(TimeManager), nameof(TimeManager.SlowMotion))]
    public static class TimeManager_SlowMotion
    {
        [HarmonyPrepare]
        private static bool Prepare() => NativeKingSkin.HasKingSkin;

        [HarmonyPrefix]
        private static bool Prefix()
        {
            if (CustomCombatRuntime.IsRunning)
            {
                var custom = CustomCombatRuntime.Playback;
                return !custom._actionAfterBehaviourIsRunning
                    || (!KingBehaviourAction.IsNativeWinningAction(custom._librarian, custom._currentLibrarianBehaviourResult)
                        && !KingBehaviourAction.IsNativeWinningAction(custom._enemy, custom._currentEnemyBehaviourResult));
            }

            var manager = SingletonBehavior<RencounterManager>.Instance;
            if (!manager || !manager.IsRunning || !manager._actionAfterBehaviourIsRunning) return true;
            return !KingBehaviourAction.IsNativeWinningAction(manager._librarian, manager._currentLibrarianBehaviourResult)
                && !KingBehaviourAction.IsNativeWinningAction(manager._enemy, manager._currentEnemyBehaviourResult);
        }
    }

    #endregion
}

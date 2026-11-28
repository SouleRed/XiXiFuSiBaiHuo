using HarmonyLib;
using LOR_BattleUnit_UI;
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

    private const float DiceActionUiOffsetX = 1.059f;
    private const float DiceActionUiOffsetY = 0.398f;

    private static void ApplyDiceActionUiOffset(BattleUnitView view, int height = 0)
    {
        var ui = view?.diceActionUI;
        if (ui == null || !NativeKingSkin.IsNative(view)) return;

        float followerScale = view.camRotationFollower != null ? Mathf.Abs(view.camRotationFollower.transform.localScale.y) : 0f;
        float scale = height > 0
            ? 170f / height
            : (followerScale > Mathf.Epsilon ? 1f / followerScale : 1f);
        float facingSign = view.model?.direction == Direction.RIGHT ? -1f : 1f;
        ui.transform.localPosition = new Vector3(0f, 1.913f, 0f) + new Vector3(DiceActionUiOffsetX * facingSign, DiceActionUiOffsetY, 0f) * scale;
    }

    [HarmonyPatch(typeof(BattleUnitView), nameof(BattleUnitView.ChangeHeight))]
    public static class BattleUnitView_ChangeHeight
    {
        [HarmonyPrepare]
        private static bool Prepare() => NativeKingSkin.HasKingSkin;

        [HarmonyPrefix]
        private static void Prefix(BattleUnitView __instance, BattleUnitView.SkinInfo ____skinInfo, ref int height)
            => NativeKingSkin.TryOverrideHeight(__instance, ____skinInfo, ref height);

        [HarmonyPostfix]
        private static void Postfix(BattleUnitView __instance, int height)
            => ApplyDiceActionUiOffset(__instance, height);
    }

    [HarmonyPatch(typeof(BattleUnitDiceActionUI), "Update")]
    public static class BattleUnitDiceActionUI_MirrorOffset
    {
        [HarmonyPrepare]
        private static bool Prepare() => NativeKingSkin.HasKingSkin;

        [HarmonyPostfix]
        private static void Postfix(BattleUnitDiceActionUI __instance)
        {
            if (__instance == null || !__instance.rootCanvas.enabled) return;
            ApplyDiceActionUiOffset(__instance._unitView);
        }
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

    #region 攻击容量我方多目标选牌交互

    [HarmonyPatch(typeof(StageController), nameof(StageController.StartBattle))]
    public static class StageController_StartBattle
    {
        [HarmonyPrefix]
        public static void Prefix() => CustomCombatTargeting.Reset();
    }

    [HarmonyPatch(typeof(StageController), nameof(StageController.EndBattle))]
    public static class StageController_EndBattle
    {
        [HarmonyPrefix]
        public static void Prefix() => CustomCombatTargeting.Reset();
    }

    [HarmonyPatch(typeof(BattlePlayingCardSlotDetail), nameof(BattlePlayingCardSlotDetail.AddCard))]
    public static class BattlePlayingCardSlotDetail_AddCard
    {
        [HarmonyPostfix]
        public static void Postfix(BattlePlayingCardSlotDetail __instance, BattleDiceCardModel card)
            => CustomCombatTargeting.FillAutomatic(__instance, card);
    }

    [HarmonyPatch(typeof(BattleAllyCardDetail), nameof(BattleAllyCardDetail.PlayTurnAutoForPlayer))]
    public static class BattleAllyCardDetail_PlayTurnAutoForPlayer
    {
        [HarmonyPostfix]
        public static void Postfix(BattleUnitModel ____self, int idx)
            => CustomCombatTargeting.FillPlayerAuto(____self, idx);
    }

    [HarmonyPatch(typeof(BattleUnitCardsInHandUI), nameof(BattleUnitCardsInHandUI.ApplySelectedCard))]
    public static class BattleUnitCardsInHandUI_ApplySelectedCard
    {
        [HarmonyPrefix]
        public static bool Prefix(BattleUnitCardsInHandUI __instance, BattleUnitModel target, int targetSlot)
            => !CustomCombatTargeting.Apply(__instance, target, targetSlot);
    }

    [HarmonyPatch(typeof(BattleUnitCardsInHandUI), "Update")]
    public static class BattleUnitCardsInHandUI_Update
    {
        [HarmonyPrefix]
        public static bool Prefix(BattleUnitCardsInHandUI __instance) => CustomCombatTargeting.Update(__instance);
    }

    [HarmonyPatch(typeof(SpeedDiceUI), nameof(SpeedDiceUI.OnSubmit))]
    public static class SpeedDiceUI_OnSubmit
    {
        [HarmonyPrefix]
        public static bool Prefix(UnityEngine.EventSystems.BaseEventData data)
        {
            if (UIControlManager.GetInpuTypeOf(data) != InputType.RightClick) return true;
            return !CustomCombatTargeting.HandleRightClick();
        }
    }

    [HarmonyPatch(typeof(BattleUnitCardsInHandUI), nameof(BattleUnitCardsInHandUI.Deactivate))]
    public static class BattleUnitCardsInHandUI_Deactivate
    {
        [HarmonyPostfix]
        public static void Postfix(BattleUnitCardsInHandUI __instance) => CustomCombatTargeting.OnDeactivate(__instance);
    }

    [HarmonyPatch(typeof(BattleDiceCardUI), "ShowDetail")]
    public static class BattleDiceCardUI_ShowDetail
    {
        [HarmonyPrefix]
        public static bool Prefix(BattleDiceCardUI __instance) => !CustomCombatTargeting.Blocks(__instance);
    }

    [HarmonyPatch(typeof(BattleDiceCardUI), "OnClick")]
    public static class BattleDiceCardUI_OnClick
    {
        [HarmonyPrefix]
        public static bool Prefix(BattleDiceCardUI __instance) => !CustomCombatTargeting.Blocks(__instance);
    }

    [HarmonyPatch(typeof(BattleDiceCardUI), "OnPdSubmit")]
    public static class BattleDiceCardUI_OnPdSubmit
    {
        [HarmonyPrefix]
        public static bool Prefix(BattleDiceCardUI __instance) => !CustomCombatTargeting.Blocks(__instance);
    }

    #endregion
}

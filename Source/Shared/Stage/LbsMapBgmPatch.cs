using System;
using HarmonyLib;
using UnityEngine;

namespace 裁缝之王
{
    /// <summary>
    /// LbsMap 插件生成的地图会从模板地图继承 <see cref="MapManager.mapBgm"/>，
    /// 导致战斗 BGM 被原版默认曲目覆盖。这里做三件事：
    /// 1. 地图创建时就套用本 MOD 的 BGM，让原版 OnStageStart 一开始播放的就是它；
    /// 2. 任意一次敌方主题切换都强制指向该曲目（情绪等级变化等情况）；
    /// 3. 切图后同步地图自身的 mapBgm，避免后续原版逻辑再拿回默认曲目。
    ///
    /// BGM 的查找规则见 <see cref="BgmProvider"/>。
    /// 所有入口都做了异常兜底：补丁里抛异常会打断原版流程（甚至崩游戏），
    /// 所以这里宁可静默退回原版行为，只在日志里留痕。
    /// </summary>
    public static class LbsMapBgmPatch
    {
        /// <summary>插件生成的地图对象名固定带这个后缀，用名字判断比依赖管理器类型更稳。</summary>
        private const string LbsMapMarker = "[LbsMap]";

        private const string LogTag = "[裁缝之王][BGM] ";

        [HarmonyPatch(typeof(BattleSceneRoot), nameof(BattleSceneRoot.InitInvitationMap))]
        public static class BattleSceneRoot_InitInvitationMap
        {
            [HarmonyPostfix]
            private static void Postfix(MapManager m)
            {
                try
                {
                    if (!IsLbsMap(m)) return;
                    Debug.Log(LogTag + "创建地图对象=" + m.name);

                    AudioClip clip = BgmProvider.Resolve(m);
                    if (clip == null) return;

                    // 地图创建阶段就写好，原版 OnStageStart 会用 mapBgm[0] 作为
                    // 友方（正在播放的）音轨，开场即是我们自己的曲子。
                    m.mapBgm = new[] { clip, clip, clip };
                    BgmProvider.Publish(clip);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(LogTag + "设置地图 BGM 失败，已回退原版行为：" + e);
                }
            }
        }

        [HarmonyPatch(typeof(BattleSoundManager), nameof(BattleSoundManager.ChangeEnemyTheme))]
        public static class BattleSoundManager_ChangeEnemyTheme
        {
            [HarmonyPrefix]
            private static bool Prefix(BattleSoundManager __instance, ref int idx)
            {
                try
                {
                    if (__instance == null) return true;
                    if (!TryGetTheme(out AudioClip[] theme)) return true;

                    // 只把数据对齐，不执行原方法：
                    // 原方法会 ChangeCurrentTheme(false) 把音轨切到敌方通道，
                    // 正在播放的友方音轨会被暂停、曲子从头重来（听起来就是“被覆盖”）。
                    __instance.SetEnemyTheme(theme);
                    idx = 0;
                    return false;
                }
                catch (Exception e)
                {
                    Debug.LogWarning(LogTag + "拦截敌方主题切换失败：" + e);
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(BattleSceneRoot), nameof(BattleSceneRoot.ChangeToSpecialMap))]
        public static class BattleSceneRoot_ChangeToSpecialMap
        {
            [HarmonyPostfix]
            private static void Postfix(BattleSceneRoot __instance)
            {
                try
                {
                    if (__instance == null) return;
                    if (!TryGetTheme(out AudioClip[] theme)) return;

                    MapManager map = __instance.currentMapObject;
                    if (map == null) return;
                    map.mapBgm = theme;
                }
                catch (Exception e)
                {
                    Debug.LogWarning(LogTag + "切图后同步 BGM 失败：" + e);
                }
            }
        }

        private static bool IsLbsMap(MapManager map)
        {
            return map != null
                && !string.IsNullOrEmpty(map.name)
                && map.name.IndexOf(LbsMapMarker, StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// 本 MOD 的曲目素材 RMS 偏低（实测约 32%，原版约 40%+），听感偏轻。
        /// 这里只对“正在播放的确实是本 MOD 曲目”的情况做增益，最终由 BgmProvider.BgmGain 控制。
        /// </summary>
        [HarmonyPatch(typeof(BattleSoundManager), "UpdateVolume")]
        public static class BattleSoundManager_UpdateVolume
        {
            [HarmonyPostfix]
            private static void Postfix(BattleSoundManager __instance)
            {
                try
                {
                    if (__instance == null) return;

                    AudioSource source = __instance.CurrentPlayingTheme;
                    if (source == null || source.clip == null) return;
                    if (!BgmProvider.TryGetPublished(out AudioClip clip) || clip == null) return;
                    if (source.clip != clip) return;

                    source.volume = Mathf.Clamp(source.volume * BgmProvider.BgmGain, 0f, 1f);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(LogTag + "调整 BGM 音量失败：" + e);
                }
            }
        }

        private static bool TryGetTheme(out AudioClip[] theme)
        {
            theme = null;

            BattleSceneRoot scene = SingletonBehavior<BattleSceneRoot>.Instance;
            MapManager map = scene ? scene.currentMapObject : null;
            if (!IsLbsMap(map)) return false;

            AudioClip clip = BgmProvider.Resolve(map);
            if (clip == null) return false;

            theme = new[] { clip, clip, clip };
            return true;
        }
    }
}

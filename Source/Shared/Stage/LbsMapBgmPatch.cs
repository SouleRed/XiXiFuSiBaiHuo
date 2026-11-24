using System;
using System.IO;
using HarmonyLib;
using UnityEngine;
using 裁缝之王.Shared.Tool;

namespace 裁缝之王
{
    /// <summary>
    /// LbsMap 插件生成的地图会从模板地图继承 <see cref="MapManager.mapBgm"/>，
    /// 导致战斗 BGM 被原版默认曲目覆盖。这里做三件事：
    /// 1. 地图创建时就套用本 MOD 的 BGM，让原版 OnStageStart 一开始播放的就是它；
    /// 2. 任意一次敌方主题切换都强制指向该曲目（情绪等级变化等情况）；
    /// 3. 切图后同步地图自身的 mapBgm，避免后续原版逻辑再拿回默认曲目。
    ///
    /// 所有入口都做了异常兜底：补丁里抛异常会打断原版流程（甚至崩游戏），
    /// 所以这里宁可静默退回原版行为，只在日志里留痕。
    /// </summary>
    public static class LbsMapBgmPatch
    {
        /// <summary>插件生成的地图管理器类型名（避免直接引用 LbsMapPlugin.dll）。</summary>
        private const string LbsMapManagerTypeName = "LbsAssetMapManager";

        private const string BgmStageKey = "CFK_BGM";

        private const string LogTag = "[裁缝之王][BGM] ";

        private static AudioClip cachedBgm;
        private static bool loadFailed;

        [HarmonyPatch(typeof(BattleSceneRoot), nameof(BattleSceneRoot.InitInvitationMap))]
        public static class BattleSceneRoot_InitInvitationMap
        {
            [HarmonyPostfix]
            private static void Postfix(MapManager m)
            {
                try
                {
                    if (m == null) return;
                    if (m.GetType().Name != LbsMapManagerTypeName) return;

                    AudioClip clip = GetBgmClip();
                    if (clip == null) return;

                    // 地图创建阶段就写好，原版 OnStageStart 会用 mapBgm[0] 作为
                    // 友方（正在播放的）音轨，开场即是我们自己的曲子。
                    m.mapBgm = new[] { clip, clip, clip };
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
            private static void Prefix(BattleSoundManager __instance, ref int idx)
            {
                try
                {
                    if (__instance == null) return;
                    if (!TryGetTheme(out AudioClip[] theme)) return;

                    __instance.SetEnemyTheme(theme);
                    idx = 0;
                }
                catch (Exception e)
                {
                    Debug.LogWarning(LogTag + "拦截敌方主题切换失败：" + e);
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

        private static bool TryGetTheme(out AudioClip[] theme)
        {
            theme = null;

            BattleSceneRoot scene = SingletonBehavior<BattleSceneRoot>.Instance;
            MapManager map = scene ? scene.currentMapObject : null;
            if (map == null) return false;
            if (map.GetType().Name != LbsMapManagerTypeName) return false;

            AudioClip clip = GetBgmClip();
            if (clip == null) return false;

            theme = new[] { clip, clip, clip };
            return true;
        }

        /// <summary>
        /// 加载并缓存本 MOD 的战斗 BGM，同时写回关卡数据供被动逻辑复用，避免重复解码。
        /// 任何一步失败都返回 null，让原版继续用自己的曲目，而不是抛出异常。
        /// </summary>
        private static AudioClip GetBgmClip()
        {
            if (cachedBgm != null) return cachedBgm;
            if (loadFailed) return null;

            try
            {
                if (BattleTools.GetStageData(BgmStageKey, out AudioClip stored) && stored != null)
                {
                    cachedBgm = stored;
                    Debug.Log(LogTag + "复用关卡数据中的 BGM");
                    return cachedBgm;
                }
            }
            catch
            {
                // 关卡模型尚未就绪时忽略，继续走文件加载
            }

            try
            {
                string assemblyPath = CaiFengKing_Initializer.path;
                if (string.IsNullOrEmpty(assemblyPath))
                {
                    loadFailed = true;
                    Debug.LogWarning(LogTag + "程序集目录未知，跳过 BGM 加载");
                    return null;
                }

                string path = Path.Combine(assemblyPath, "BGM", "BGM1.wav");
                if (!File.Exists(path))
                {
                    loadFailed = true;
                    Debug.LogWarning(LogTag + "找不到 BGM 文件：" + path);
                    return null;
                }

                if (!BattleTools.TryGetAudioClip(path, out AudioClip clip) || clip == null)
                {
                    loadFailed = true;
                    Debug.LogWarning(LogTag + "BGM 解码失败：" + path);
                    return null;
                }

                cachedBgm = clip;
                Debug.Log(LogTag + "BGM 加载完成：" + path);

                try
                {
                    BattleTools.SetStageData(BgmStageKey, clip);
                }
                catch
                {
                    // 写入关卡数据失败不影响播放
                }

                return cachedBgm;
            }
            catch (Exception e)
            {
                loadFailed = true;
                Debug.LogWarning(LogTag + "BGM 加载异常，已回退原版曲目：" + e);
                return null;
            }
        }
    }
}

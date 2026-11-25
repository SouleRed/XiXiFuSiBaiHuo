using System;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace 裁缝之王
{
    /// <summary>
    /// 原生图书馆地图 AssetBundle 载入与注册：
    /// 贴近原版 StageController.InitializeMap() 流程——关卡在 StageInfo.xml 中填写标准
    /// &lt;MapInfo&gt;Filature_Map&lt;/MapInfo&gt;，原版调用 Util.LoadPrefab("InvitationMaps/InvitationMap_Filature_Map", parent)
    /// 时直接从 MOD 的原生 LZ4 AB 包实例化带原版 MapManager 脚本的预制件，随后完全交由原版
    /// BattleSceneRoot.InitInvitationMap / ChangeToSpecialMap 接管生命周期与镜头。
    /// </summary>
    internal static class NativeStageMap
    {
        internal const string FilatureMapId = "Filature_Map";
        private const string InvitationPrefabPrefix = "InvitationMaps/InvitationMap_";
        private const string LogTag = "[裁缝之王][NativeMap] ";

        private sealed class MapEntry
        {
            internal readonly string MapId;
            internal readonly string BundleFile;
            internal readonly string AssetAddress;
            internal AssetBundle Bundle;
            internal GameObject Prefab;

            internal MapEntry(string mapId, string bundleFile, string assetAddress)
            {
                MapId = mapId;
                BundleFile = bundleFile;
                AssetAddress = assetAddress;
            }
        }

        private static readonly MapEntry[] Entries =
        {
            new MapEntry(FilatureMapId, "filature_map.ab", "Filature_Map")
        };

        internal static void LoadMaps(string assembliesRoot)
        {
            if (string.IsNullOrEmpty(assembliesRoot)) return;
            foreach (MapEntry entry in Entries)
            {
                if (entry.Prefab != null) continue;
                try
                {
                    string abPath = Path.Combine(assembliesRoot, "AB", entry.BundleFile);
                    if (!File.Exists(abPath))
                    {
                        Debug.LogWarning(LogTag + "未找到原生地图资产包：" + abPath);
                        continue;
                    }

                    entry.Bundle = AssetBundle.LoadFromFile(abPath);
                    if (entry.Bundle == null)
                    {
                        Debug.LogError(LogTag + "无法加载原生地图资产包：" + abPath);
                        continue;
                    }

                    GameObject prefab = entry.Bundle.LoadAsset<GameObject>(entry.AssetAddress);
                    if (prefab == null || prefab.GetComponent<MapManager>() == null)
                    {
                        Debug.LogError(LogTag + "地图预制件缺失或未挂载原生 MapManager：" + entry.AssetAddress);
                        entry.Bundle.Unload(true);
                        entry.Bundle = null;
                        continue;
                    }

                    entry.Prefab = prefab;
                    MapManager mm = prefab.GetComponent<MapManager>();
                    int bgmCount = mm != null && mm.mapBgm != null ? mm.mapBgm.Length : 0;
                    Debug.Log(LogTag + "已载入原生地图：" + entry.MapId + " (" + entry.BundleFile + ", mapBgm=" + bgmCount + ")");
                }
                catch (Exception e)
                {
                    Debug.LogError(LogTag + "载入原生地图异常 (" + entry.MapId + ")：" + e);
                }
            }
        }

        /// <summary>
        /// 从已注册原生地图预制件的 <see cref="MapManager.mapBgm"/> 中直接读取内置 BGM，
        /// 供角色独立换曲功能复用，避免重复维护外置音频文件与运行时解码。
        /// </summary>
        internal static bool TryGetDefaultBgm(out AudioClip clip, string mapId = FilatureMapId)
        {
            clip = null;
            try
            {
                MapEntry entry = Array.Find(Entries, e => string.Equals(e.MapId, mapId, StringComparison.OrdinalIgnoreCase));
                if (entry == null) return false;
                if (entry.Prefab == null)
                    LoadMaps(CaiFengKing_Initializer.path);
                if (entry.Prefab == null) return false;

                MapManager mm = entry.Prefab.GetComponent<MapManager>();
                if (mm == null || mm.mapBgm == null) return false;
                foreach (AudioClip item in mm.mapBgm)
                {
                    if (item != null)
                    {
                        clip = item;
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogTag + "读取原生地图内置 BGM 失败：" + e.Message);
            }
            return false;
        }

        /// <summary>
        /// 采用原版 Boss 战（哭泣之子、残响乐团、殷红迷雾等）的标准做法：
        /// 固定敌方总情感币判定端口（emotionTotalBonus = 我方总情感币 + 1），
        /// 使 <see cref="BattleSoundManager.CheckTheme"/> 始终走敌方主题轨，
        /// 不会因敌我情感等级反超而切回楼层默认 BGM。
        /// </summary>
        internal static void LockEnemyThemePort()
        {
            try
            {
                StageController stage = Singleton<StageController>.Instance;
                if (stage == null) return;
                StageLibraryFloorModel floor = stage.GetCurrentStageFloorModel();
                StageWaveModel wave = stage.GetCurrentWaveModel();
                if (floor == null || floor.team == null || wave == null || wave.team == null) return;

                wave.team.emotionTotalBonus = floor.team.emotionTotalCoinNumber + 1;
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogTag + "固定敌方情感 BGM 端口失败：" + e.Message);
            }
        }

        internal static bool IsRegisteredMap(MapManager map)
        {
            if (map == null || string.IsNullOrEmpty(map.name)) return false;
            foreach (MapEntry entry in Entries)
            {
                if (map.name.IndexOf(entry.MapId, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static bool TryInstantiateInvitationMap(string path, Transform parent, out GameObject instance)
        {
            instance = null;
            if (!path.StartsWith(InvitationPrefabPrefix, StringComparison.Ordinal)) return false;

            string mapId = path.Substring(InvitationPrefabPrefix.Length);
            MapEntry entry = Array.Find(Entries, e => string.Equals(e.MapId, mapId, StringComparison.OrdinalIgnoreCase));
            if (entry == null) return false;

            if (entry.Prefab == null)
                LoadMaps(CaiFengKing_Initializer.path);
            if (entry.Prefab == null) return false;

            instance = parent != null
                ? UnityEngine.Object.Instantiate(entry.Prefab, parent, false)
                : UnityEngine.Object.Instantiate(entry.Prefab);
            instance.name = "InvitationMap_" + entry.MapId;
            Debug.Log(LogTag + "已按原版流程实例化接待地图：" + instance.name);
            return true;
        }

        [HarmonyPatch(typeof(Util), nameof(Util.LoadPrefab), new[] { typeof(string), typeof(Transform) })]
        public static class Util_LoadPrefab_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(string path, Transform parent, ref GameObject __result)
            {
                try
                {
                    if (string.IsNullOrEmpty(path)) return true;
                    if (!TryInstantiateInvitationMap(path, parent, out GameObject instance)) return true;
                    __result = instance;
                    return false;
                }
                catch (Exception e)
                {
                    Debug.LogError(LogTag + "拦截 Util.LoadPrefab 失败，已回退原版：" + e);
                    return true;
                }
            }
        }
    }
}

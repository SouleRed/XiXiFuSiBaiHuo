using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using 裁缝之王.Shared.Tool;

namespace 裁缝之王
{
    /// <summary>
    /// 战斗 BGM 解析器：按优先级找文件，按路径缓存。
    /// <list type="number">
    /// <item>地图目录：<c>Assemblies\LbsMap\&lt;地图名&gt;\BGM.wav</c>（或 .mp3）</item>
    /// <item>关卡专用：<c>Assemblies\BGM\CFK_&lt;关卡id&gt;.wav</c>（或 .mp3）</item>
    /// <item>兜底：<c>Assemblies\BGM\BGM1.wav</c></item>
    /// </list>
    /// 任何一步失败都返回 null（由调用方回退原版曲目），不会抛异常。
    /// </summary>
    internal static class BgmProvider
    {
        internal const string StageKey = "CFK_BGM";

        /// <summary>本 MOD 曲目的额外增益（1 = 不放大）。素材 RMS 偏低时调大可改善听感，最终会夹在 1 以内。</summary>
        internal const float BgmGain = 1.3f;

        private const string LogTag = "[裁缝之王][BGM] ";

        /// <summary>LbsMap 插件给地图 GameObject 起的名字格式：InvitationMap_&lt;地图名&gt; [LbsMap]</summary>
        private const string MapNamePrefix = "InvitationMap_";
        private const string MapNameSuffix = " [LbsMap]";

        private const string FallbackFile = "BGM1";

        private static readonly Dictionary<string, AudioClip> Cache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> Failed = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>解析某个地图应当使用的 BGM。</summary>
        internal static AudioClip Resolve(MapManager map)
        {
            string modRoot = CaiFengKing_Initializer.path;
            if (string.IsNullOrEmpty(modRoot)) return null;

            // CaiFengKing_Initializer.path 本身就是 ...\Assemblies 目录，不要再拼一层
            string lbsRoot = Path.Combine(modRoot, "LbsMap");
            string objectName = map != null ? map.name : null;
            string parsedName = MapNameOf(map);

            // 1) 地图目录自带 BGM：对象名精确解析
            AudioClip clip = TryMapFolder(lbsRoot, parsedName);
            if (clip != null)
            {
                Debug.Log(LogTag + "使用地图目录 BGM：" + parsedName);
                return clip;
            }

            // 1b) 兜底：用对象名做“包含”匹配，按目录名长度从长到短，避免 CFK 抢在 CFK_Test 前面
            string matched = FindMapFolderByContains(lbsRoot, objectName);
            if (!string.IsNullOrEmpty(matched))
            {
                clip = TryMapFolder(lbsRoot, matched);
                if (clip != null)
                {
                    Debug.Log(LogTag + "使用地图目录 BGM（按对象名匹配）：" + matched);
                    return clip;
                }
            }

            // 1b) 同一关卡里可能有多张地图（模板 LbsMap + 用户地图），
            //     已经解析过的那首直接复用，避免模板地图把兜底曲塞回来。
            if (TryGetPublished(out AudioClip published))
            {
                Debug.Log(LogTag + "沿用同关卡已确定的 BGM");
                return published;
            }

            // 2) 按关卡 id 指定：BGM\CFK_<id>.wav
            int stageId = CurrentStageId();
            if (stageId > 0)
            {
                clip = LoadStem(Path.Combine(modRoot, "BGM", "CFK_" + stageId));
                if (clip != null)
                {
                    Debug.Log(LogTag + "使用关卡专用 BGM：CFK_" + stageId);
                    return clip;
                }
            }

            // 3) 兜底
            Debug.Log(LogTag + "地图对象=\"" + objectName + "\" 解析名=" + (parsedName ?? "(null)")
                + " 关卡id=" + stageId + "，未匹配到专属 BGM，使用兜底 " + FallbackFile);
            return LoadStem(Path.Combine(modRoot, "BGM", FallbackFile));
        }

        private static AudioClip TryMapFolder(string lbsRoot, string mapName)
        {
            if (string.IsNullOrEmpty(mapName)) return null;
            return LoadStem(Path.Combine(lbsRoot, mapName, "BGM"));
        }

        /// <summary>用地图对象名去匹配 LbsMap 下的目录名（包含关系，优先长名）。</summary>。。
        private static string FindMapFolderByContains(string lbsRoot, string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return null;

            try
            {
                if (!Directory.Exists(lbsRoot)) return null;

                string[] dirs = Directory.GetDirectories(lbsRoot);
                Array.Sort(dirs, (a, b) => Path.GetFileName(b).Length.CompareTo(Path.GetFileName(a).Length));

                foreach (string dir in dirs)
                {
                    string name = Path.GetFileName(dir);
                    if (objectName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) return name;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogTag + "扫描地图目录失败：" + e.Message);
            }

            return null;
        }

        /// <summary>解析当前战斗场景正在使用的地图（可能为空）。</summary>
        internal static AudioClip ResolveForCurrentMap()
        {
            try
            {
                BattleSceneRoot scene = SingletonBehavior<BattleSceneRoot>.Instance;
                return scene ? Resolve(scene.currentMapObject) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogTag + "解析当前地图 BGM 失败：" + e.Message);
                return null;
            }
        }

        /// <summary>写回关卡数据，供被动/ego 卡读取，避免重复解码。</summary>
        internal static void Publish(AudioClip clip)
        {
            if (clip == null) return;
            try
            {
                BattleTools.SetStageData(StageKey, clip);
            }
            catch (Exception e)
            {
                Debug.LogWarning(LogTag + "写入关卡数据失败：" + e.Message);
            }
        }

        internal static bool TryGetPublished(out AudioClip clip)
        {
            clip = null;
            try
            {
                return BattleTools.GetStageData(StageKey, out clip) && clip != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>从地图对象名里取出插件用的地图名，取不到返回 null。</summary>
        internal static string MapNameOf(MapManager map)
        {
            if (map == null) return null;

            string name = map.name;
            if (string.IsNullOrEmpty(name)) return null;

            if (name.StartsWith(MapNamePrefix, StringComparison.Ordinal))
            {
                name = name.Substring(MapNamePrefix.Length);
            }

            int suffixIndex = name.IndexOf(MapNameSuffix, StringComparison.Ordinal);
            if (suffixIndex >= 0)
            {
                name = name.Substring(0, suffixIndex);
            }

            name = name.Trim();
            return name.Length == 0 ? null : name;
        }

        private static int CurrentStageId()
        {
            try
            {
                StageModel model = Singleton<StageController>.Instance?.GetStageModel();
                StageClassInfo info = model?.ClassInfo;
                if (info == null) return 0;
                return info.id.id;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>依次尝试同名 .wav / .mp3；已加载过的直接返回缓存。</summary>
        private static AudioClip LoadStem(string stemWithoutExtension)
        {
            return LoadFile(stemWithoutExtension + ".wav") ?? LoadFile(stemWithoutExtension + ".mp3");
        }

        private static AudioClip LoadFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            if (Cache.TryGetValue(path, out AudioClip cached) && cached != null) return cached;
            if (Failed.Contains(path)) return null;
            if (!File.Exists(path)) return null;

            try
            {
                if (!BattleTools.TryGetAudioClip(path, out AudioClip clip) || clip == null)
                {
                    Failed.Add(path);
                    Debug.LogWarning(LogTag + "解码失败：" + path);
                    return null;
                }

                Cache[path] = clip;
                Debug.Log(LogTag + "加载完成：" + path);
                return clip;
            }
            catch (Exception e)
            {
                Failed.Add(path);
                Debug.LogWarning(LogTag + "加载异常 " + path + "：" + e.Message);
                return null;
            }
        }
    }
}

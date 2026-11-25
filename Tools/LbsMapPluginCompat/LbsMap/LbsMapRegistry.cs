using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Mod;
using UnityEngine;
using Object = UnityEngine.Object;
using Workshop;

namespace LbsMap;

public static class LbsMapRegistry
{
	public sealed class InitializeMapsState
	{
		internal StageClassInfo Info;

		internal List<string> Original;

		internal readonly List<KeyValuePair<int, string>> Removed = new List<KeyValuePair<int, string>>();

		internal void Restore()
		{
			if (Info != null)
			{
				RestoreInto(Original);
				if (Info.mapInfo == null)
				{
					Info.mapInfo = Original;
				}
				if (Info.mapInfo != Original)
				{
					RestoreInto(Info.mapInfo);
				}
			}
		}

		private void RestoreInto(List<string> list)
		{
			if (list == null)
			{
				return;
			}
			foreach (KeyValuePair<int, string> item in Removed)
			{
				if (!list.Contains(item.Value))
				{
					list.Insert(Math.Min(item.Key, list.Count), item.Value);
				}
			}
		}
	}

	private static readonly Dictionary<string, LbsMapConfig> configs = new Dictionary<string, LbsMapConfig>(StringComparer.Ordinal);

	private static readonly ConditionalWeakTable<StageClassInfo, List<string>> injected = new ConditionalWeakTable<StageClassInfo, List<string>>();

	private static bool installed;

	private static readonly FieldInfo AddedMaps = AccessTools.Field(typeof(BattleSceneRoot), "_addedMapList");

	public static void Install(Harmony harmony)
	{
		if (installed)
		{
			return;
		}
		if (harmony == null)
		{
			throw new ArgumentNullException("harmony");
		}
		LbsMapInitializationGuard.TryInitialize(() =>
		{
			try
			{
				ReloadConfiguration();
				harmony.Patch(AccessTools.Method(typeof(StageModel), "Init"), new HarmonyMethod(typeof(LbsMapRegistry), "BeforeStageInit"), new HarmonyMethod(typeof(LbsMapRegistry), "AfterStageInit"));
				harmony.Patch(AccessTools.Method(typeof(StageController), "InitializeMap"), new HarmonyMethod(typeof(LbsMapRegistry), "BeforeInitializeMaps"), new HarmonyMethod(typeof(LbsMapRegistry), "AfterInitializeMaps"), null, new HarmonyMethod(typeof(LbsMapRegistry), "FinalizeInitializeMaps"), null);
				harmony.Patch(AccessTools.Method(typeof(BattleSceneRoot), "ChangeToSpecialMap"), new HarmonyMethod(typeof(LbsMapRegistry), "BeforeChangeToSpecialMap"));
				installed = true;
			}
			catch
			{
				RemoveOwnPatches(harmony, typeof(StageModel), "Init", "BeforeStageInit", "AfterStageInit");
				RemoveOwnPatches(harmony, typeof(StageController), "InitializeMap", "BeforeInitializeMaps", "AfterInitializeMaps", "FinalizeInitializeMaps");
				RemoveOwnPatches(harmony, typeof(BattleSceneRoot), "ChangeToSpecialMap", "BeforeChangeToSpecialMap");
				throw;
			}
		}, () => Harmony.HasAnyPatches("LOR.LbsMapPlugin"));
	}

	private static void RemoveOwnPatches(Harmony harmony, Type type, string method, params string[] patches)
	{
		MethodInfo original = AccessTools.Method(type, method);
		foreach (string name in patches)
		{
			harmony.Unpatch(original, AccessTools.Method(typeof(LbsMapRegistry), name));
		}
	}

	public static void ReloadConfiguration()
	{
		configs.Clear();
		foreach (ModContentInfo item in from m in Singleton<ModContentManager>.Instance.GetAllMods()
			where m.activated
			select m)
		{
			ReadMod(item);
		}
	}

	private static void ReadMod(ModContentInfo mod)
	{
		string uniqueId = mod.invInfo.workshopInfo.uniqueId;
		try
		{
			FileInfo stageFile = mod.invInfo.fileInfo.stageFile;
			string stageFile2 = LbsMapConfig.ResolveStageFile(mod.dirInfo.FullName, stageFile?.relativePath);
			configs[uniqueId] = LbsMapConfig.Read(mod.dirInfo.FullName, stageFile2);
		}
		catch (Exception innerException)
		{
			LbsMapInitializer.WriteError(new InvalidOperationException("读取 Mod '" + uniqueId + "' 的 MapAdd 失败", innerException));
		}
	}

	private static LbsMapConfig GetConfig(StageClassInfo info)
	{
		if (info == null || string.IsNullOrEmpty(info.workshopID))
		{
			return null;
		}
		if (configs.TryGetValue(info.workshopID, out var value))
		{
			return value;
		}
		ModContentInfo val = Singleton<ModContentManager>.Instance.GetAllMods().Find((ModContentInfo m) => m.activated && m.invInfo.workshopInfo.uniqueId == info.workshopID);
		if (val == null)
		{
			return null;
		}
		ReadMod(val);
		return configs.TryGetValue(info.workshopID, out value) ? value : null;
	}

	private static List<LbsMapDeclaration> Declarations(StageClassInfo info)
	{
		LbsMapConfig config = GetConfig(info);
		List<LbsMapDeclaration> value;
		return (config != null && config.Stages.TryGetValue(info._id, out value)) ? value : new List<LbsMapDeclaration>();
	}

	public static void BeforeStageInit(StageClassInfo __0)
	{
		if (__0 == null || !injected.TryGetValue(__0, out var value))
		{
			return;
		}
		if (__0.mapInfo != null)
		{
			foreach (string item in value)
			{
				int num = __0.mapInfo.LastIndexOf(item);
				if (num >= 0)
				{
					__0.mapInfo.RemoveAt(num);
				}
			}
		}
		injected.Remove(__0);
	}

	public static void AfterStageInit(StageModel __instance)
	{
		try
		{
			AppendNames(__instance.ClassInfo);
		}
		catch (Exception error)
		{
			LbsMapInitializer.WriteError(error);
		}
	}

	private static void AppendNames(StageClassInfo info)
	{
		if (info == null || injected.TryGetValue(info, out var _))
		{
			return;
		}
		List<LbsMapDeclaration> list = Declarations(info);
		if (list.Count == 0)
		{
			return;
		}
		if (info.mapInfo == null)
		{
			info.mapInfo = new List<string>();
		}
		List<string> list2 = new List<string>();
		foreach (LbsMapDeclaration item in list)
		{
			if (!info.mapInfo.Contains(item.Name))
			{
				info.mapInfo.Add(item.Name);
				list2.Add(item.Name);
			}
		}
		injected.Add(info, list2);
	}

	public static void BeforeInitializeMaps(StageController __instance, out InitializeMapsState __state)
	{
		__state = new InitializeMapsState();
		try
		{
			StageModel stageModel = __instance.GetStageModel();
			if (stageModel == null)
			{
				return;
			}
			List<LbsMapDeclaration> list = Declarations(stageModel.ClassInfo);
			if (list.Count == 0)
			{
				return;
			}
			AppendNames(stageModel.ClassInfo);
			__state.Info = stageModel.ClassInfo;
			__state.Original = stageModel.ClassInfo.mapInfo;
			HashSet<string> hashSet = new HashSet<string>(list.Select((LbsMapDeclaration d) => d.Name), StringComparer.Ordinal);
			for (int num = 0; num < __state.Original.Count; num++)
			{
				if (hashSet.Contains(__state.Original[num]))
				{
					__state.Removed.Add(new KeyValuePair<int, string>(num, __state.Original[num]));
				}
			}
			__state.Original.RemoveAll(hashSet.Contains);
		}
		catch (Exception error)
		{
			__state.Restore();
			LbsMapInitializer.WriteError(error);
		}
	}

	public static void AfterInitializeMaps(StageController __instance, InitializeMapsState __state)
	{
		if (__state == null || __state.Info == null)
		{
			return;
		}
		__state.Restore();
		try
		{
			BattleSceneRoot instance = SingletonBehavior<BattleSceneRoot>.Instance;
			if (UnityNull.IsAlive((Object)(object)instance))
			{
				RegisterForStage(instance, __state.Info);
			}
		}
		catch (Exception error)
		{
			LbsMapInitializer.WriteError(error);
		}
	}

	public static Exception FinalizeInitializeMaps(Exception __exception, InitializeMapsState __state)
	{
		__state?.Restore();
		return __exception;
	}

	public static void RegisterForStage(BattleSceneRoot scene, StageClassInfo info)
	{
		LbsMapConfig config = GetConfig(info);
		if (!UnityNull.IsAlive((Object)(object)scene) || config == null)
		{
			return;
		}
		foreach (LbsMapDeclaration item in Declarations(info))
		{
			MapManager val = null;
			try
			{
				if (!UnityNull.IsAlive((Object)(object)FindRegistered(scene, item.Name)))
				{
					val = LbsMapFactory.Create(((Component)scene).transform, scene.currentMapObject, config, item);
					scene.InitInvitationMap(val);
					((Component)val).gameObject.SetActive(true);
				}
			}
			catch (Exception innerException)
			{
				if (UnityNull.IsAlive((Object)(object)val))
				{
					if (AddedMaps.GetValue(scene) is List<MapManager> list)
					{
						list.Remove(val);
					}
					LbsMapResources component = ((Component)val).GetComponent<LbsMapResources>();
					if (UnityNull.IsAlive((Object)(object)component))
					{
						component.Release();
					}
					((Component)val).gameObject.SetActive(false);
					Object.Destroy((Object)(object)((Component)val).gameObject);
				}
				LbsMapInitializer.WriteError(new InvalidOperationException("MapAdd '" + item.Name + "' 导入失败；Mod=" + info.workshopID, innerException));
			}
		}
	}

	public static MapManager FindRegistered(BattleSceneRoot scene, string name)
	{
		return (!(AddedMaps.GetValue(scene) is List<MapManager> list)) ? null : list.Find((MapManager m) => UnityNull.IsAlive((Object)(object)m) && ((Object)m).name == LbsMapFactory.NativeName(name));
	}

	public static void BeforeChangeToSpecialMap(BattleSceneRoot __instance, ref string __0)
	{
		if (!string.IsNullOrEmpty(__0))
		{
			MapManager val = FindRegistered(__instance, __0);
			if (UnityNull.IsAlive((Object)(object)val))
			{
				__0 = ((Object)val).name;
			}
		}
	}
}

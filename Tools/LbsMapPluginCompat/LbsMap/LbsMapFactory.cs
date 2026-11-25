using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LbsMap;

public static class LbsMapFactory
{
	private static readonly Dictionary<string, AssetBundle> bundles = new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, AssetBundle> bundleContent = new Dictionary<string, AssetBundle>(StringComparer.Ordinal);

	internal static string NativeName(string name)
	{
		return "InvitationMap_" + name + " [LbsMap]";
	}

	internal static AssetBundle LoadBundle(string path)
	{
		path = Path.GetFullPath(path);
		if (bundles.TryGetValue(path, out var value) && UnityNull.IsAlive((Object)(object)value))
		{
			return value;
		}
		string key;
		using (FileStream inputStream = File.OpenRead(path))
		{
			using SHA256 sHA = SHA256.Create();
			key = BitConverter.ToString(sHA.ComputeHash(inputStream));
		}
		if (!bundleContent.TryGetValue(key, out value) || !UnityNull.IsAlive((Object)(object)value))
		{
			value = AssetBundle.LoadFromFile(path);
			if (!UnityNull.IsAlive((Object)(object)value))
			{
				throw new InvalidDataException("AssetBundle 加载失败：" + path);
			}
			bundleContent[key] = value;
		}
		bundles[path] = value;
		return value;
	}

	public static MapManager Create(Transform parent, MapManager template, LbsMapConfig config, LbsMapDeclaration declaration)
	{
		return Create(parent, template, config, declaration, null);
	}

	public static MapManager Create(Transform parent, MapManager template, LbsMapConfig config, LbsMapDeclaration declaration, AssetBundle loadedBundle)
	{
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected Obj, but got Unknown
		string text = (UnityNull.IsAlive((Object)(object)loadedBundle) ? null : config.ResolveAsset(declaration.Name));
		bool flag = text != null && Directory.Exists(text);
		AssetBundle bundle = (UnityNull.IsAlive((Object)(object)loadedBundle) ? loadedBundle : LoadBundle(flag ? Path.Combine(LbsMapInitializer.path, "LbsMap", "LbsMap.ab") : text));
		GameObject val = SelectPrefab(bundle, flag ? new LbsMapDeclaration
		{
			Name = "LbsMap",
			Prefab = ""
		} : declaration);
		GameObject val2 = null;
		try
		{
			val2 = new GameObject(NativeName(declaration.Name));
			val2.SetActive(false);
			val2.transform.SetParent(parent, false);
			GameObject val3 = Object.Instantiate<GameObject>(val, val2.transform, false);
			MapManager val4 = val3.GetComponent<MapManager>();
			if (UnityNull.IsAlive((Object)(object)val4))
			{
				val3.SetActive(false);
				val3.transform.SetParent(parent, false);
				Object.Destroy((Object)(object)val2);
				val2 = val3;
				((Object)val2).name = NativeName(declaration.Name);
			}
			else
			{
				val4 = (MapManager)(object)val2.AddComponent<LbsAssetMapManager>();
				((LbsAssetMapManager)(object)val4).Configure(val3, template);
			}
			LbsMapResources lbsMapResources = val2.AddComponent<LbsMapResources>();
			lbsMapResources.Key = Path.GetFullPath(config.ModRoot) + "|" + declaration.Name;
			if (flag)
			{
				ReplaceImage(val3, "tr_quad_floor_lv0", "quad_floor", LbsMapConfig.FindImage(text, "Board"), lbsMapResources);
				ReplaceImage(val3, "tr_m_back_lv0", "quad", LbsMapConfig.FindImage(text, "BG"), lbsMapResources);
			}
			LbsMapLayers.Apply(val4);
			return val4;
		}
		catch
		{
			if (UnityNull.IsAlive((Object)(object)val2))
			{
				LbsMapResources component = val2.GetComponent<LbsMapResources>();
				if (UnityNull.IsAlive((Object)(object)component))
				{
					component.Release();
				}
				val2.SetActive(false);
				Object.Destroy((Object)(object)val2);
			}
			throw;
		}
	}

	private static GameObject SelectPrefab(AssetBundle bundle, LbsMapDeclaration declaration)
	{
		string text = (string.IsNullOrEmpty(declaration.Prefab) ? declaration.Name : declaration.Prefab);
		GameObject val = bundle.LoadAsset<GameObject>(text);
		if (UnityNull.IsAlive((Object)(object)val))
		{
			return val;
		}
		if (!string.IsNullOrEmpty(declaration.Prefab))
		{
			throw new InvalidDataException("AB 中找不到指定 Prefab：" + text);
		}
		GameObject[] array = bundle.LoadAllAssets<GameObject>();
		GameObject[] array2 = array.Where((GameObject p) => UnityNull.IsAlive((Object)(object)p.GetComponent<MapManager>())).ToArray();
		if (array2.Length == 1)
		{
			return array2[0];
		}
		if (array.Length == 1)
		{
			return array[0];
		}
		throw new InvalidDataException("AB 中存在多个地图预设，请在 MapAdd 上用 Prefab 属性指定完整路径");
	}

	private static void ReplaceImage(GameObject visuals, string pivot, string leaf, string imagePath, LbsMapResources resources)
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected Obj, but got Unknown
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Expected Obj, but got Unknown
		Transform[] array = (from t in visuals.GetComponentsInChildren<Transform>(true)
			where ((Object)t).name == leaf && UnityNull.IsAlive((Object)(object)t.parent) && ((Object)t.parent).name == pivot
			select t).ToArray();
		if (array.Length != 1)
		{
			throw new InvalidDataException("LbsMap 模板需要唯一的 " + pivot + "/" + leaf);
		}
		Renderer component = ((Component)array[0]).GetComponent<Renderer>();
		if (!UnityNull.IsAlive((Object)(object)component))
		{
			throw new InvalidDataException("模板节点缺少 Renderer：" + pivot + "/" + leaf);
		}
		Texture2D val = resources.Own<Texture2D>(new Texture2D(2, 2, (TextureFormat)4, true));
		((Object)val).name = Path.GetFileName(imagePath);
		if (!ImageConversion.LoadImage(val, File.ReadAllBytes(imagePath), false))
		{
			throw new InvalidDataException("图片解码失败：" + imagePath);
		}
		((Texture)val).wrapMode = (TextureWrapMode)1;
		((Texture)val).filterMode = (FilterMode)1;
		Material[] sharedMaterials = component.sharedMaterials;
		if (sharedMaterials.Length == 0)
		{
			throw new InvalidDataException("模板节点缺少材质：" + leaf);
		}
		for (int num = 0; num < sharedMaterials.Length; num++)
		{
			if (!UnityNull.IsAlive((Object)(object)sharedMaterials[num]) || !sharedMaterials[num].HasProperty("_MainTex"))
			{
				throw new InvalidDataException("模板材质缺少 _MainTex：" + leaf);
			}
			Material val2 = resources.Own<Material>(new Material(sharedMaterials[num]));
			((Object)val2).name = ((Object)sharedMaterials[num]).name + "_" + ((Object)val).name;
			val2.SetTexture("_MainTex", (Texture)(object)val);
			sharedMaterials[num] = val2;
		}
		component.sharedMaterials = sharedMaterials;
	}
}

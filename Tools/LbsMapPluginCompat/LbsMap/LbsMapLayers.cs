using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LbsMap;

internal static class LbsMapLayers
{
	internal static int Classify(string name, int inherited, int floor, int background)
	{
		string text = name.ToLowerInvariant();
		if (text.Contains("floor") || (text.Contains("ground") && !text.Contains("background")) || text.Contains("board") || text.Contains("地板") || text.Contains("地面"))
		{
			return floor;
		}
		if (text == "bg" || text.StartsWith("bg_") || text.EndsWith("_bg") || text.StartsWith("tr_m_back") || text.StartsWith("back_") || text.Contains("background") || text.Contains("背景"))
		{
			return background;
		}
		return inherited;
	}

	internal static void Apply(MapManager manager)
	{
		int num = LayerMask.NameToLayer("Default");
		int num2 = LayerMask.NameToLayer("Background_Back");
		if (num < 0 || num2 < 0)
		{
			throw new InvalidOperationException("地图图层 Default / Background_Back 不存在");
		}
		ApplyNode(((Component)manager).transform, num, num, num2, manager.backgroundRoot);
	}

	private static void ApplyNode(Transform node, int inherited, int floor, int background, GameObject backgroundRoot)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected Obj, but got Unknown
		int num = (((Object)(object)((Component)node).gameObject == (Object)(object)backgroundRoot) ? background : Classify(((Object)node).name, inherited, floor, background));
		((Component)node).gameObject.layer = num;
		foreach (Transform item in node)
		{
			Transform node2 = item;
			ApplyNode(node2, num, floor, background, backgroundRoot);
		}
	}
}

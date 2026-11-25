using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LbsMap;

public sealed class LbsMapInitializer : ModInitializer
{
	internal const string HarmonyId = "LOR.LbsMapPlugin";

	public static readonly string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

	public override void OnInitializeMod()
	{
		base.OnInitializeMod();
		try
		{
			LbsMapRegistry.Install(new Harmony("LOR.LbsMapPlugin"));
		}
		catch (Exception error)
		{
			WriteError(error);
		}
	}

	internal static void WriteError(Exception error)
	{
		Debug.LogError((object)("[MapAdd] " + error));
		try
		{
			File.AppendAllText(Path.Combine(path, "LbsMap-error.txt"), error?.ToString() + Environment.NewLine);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}
}

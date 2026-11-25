using System;

namespace LbsMap;

internal static class LbsMapInitializationGuard
{
	internal const string StateKey = "LOR.LbsMapPlugin.Initialization";

	internal static bool TryInitialize(Action initialize, Func<bool> alreadyInstalled)
	{
		if (initialize == null)
		{
			throw new ArgumentNullException("initialize");
		}
		lock (string.Intern("LOR.LbsMapPlugin.Initialization"))
		{
			AppDomain currentDomain = AppDomain.CurrentDomain;
			if (currentDomain.GetData("LOR.LbsMapPlugin.Initialization") != null)
			{
				return false;
			}
			if (alreadyInstalled != null && alreadyInstalled())
			{
				currentDomain.SetData("LOR.LbsMapPlugin.Initialization", "initialized");
				return false;
			}
			currentDomain.SetData("LOR.LbsMapPlugin.Initialization", "initializing");
			try
			{
				initialize();
				currentDomain.SetData("LOR.LbsMapPlugin.Initialization", "initialized");
				return true;
			}
			catch
			{
				currentDomain.SetData("LOR.LbsMapPlugin.Initialization", null);
				throw;
			}
		}
	}
}

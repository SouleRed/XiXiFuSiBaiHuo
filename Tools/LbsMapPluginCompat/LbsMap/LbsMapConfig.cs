using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace LbsMap;

public sealed class LbsMapConfig
{
	public readonly Dictionary<int, List<LbsMapDeclaration>> Stages = new Dictionary<int, List<LbsMapDeclaration>>();

	public string ModRoot { get; private set; }

	public static LbsMapConfig Read(string modRoot, string stageFile)
	{
		LbsMapConfig lbsMapConfig = new LbsMapConfig
		{
			ModRoot = Path.GetFullPath(modRoot)
		};
		if (!File.Exists(stageFile))
		{
			return lbsMapConfig;
		}
		XmlDocument xmlDocument = new XmlDocument
		{
			XmlResolver = null
		};
		using (XmlReader reader = XmlReader.Create(stageFile, new XmlReaderSettings
		{
			DtdProcessing = DtdProcessing.Prohibit,
			XmlResolver = null
		}))
		{
			xmlDocument.Load(reader);
		}
		foreach (XmlNode item in xmlDocument.SelectNodes("/StageXmlRoot/Stage"))
		{
			if (item.Attributes["id"] == null || !int.TryParse(item.Attributes["id"].Value, out var result))
			{
				continue;
			}
			if (!lbsMapConfig.Stages.TryGetValue(result, out var value))
			{
				value = (lbsMapConfig.Stages[result] = new List<LbsMapDeclaration>());
			}
			foreach (XmlNode item2 in item.SelectNodes("MapAdd"))
			{
				string name = item2.InnerText.Trim();
				if (name.Length != 0 && !value.Exists((LbsMapDeclaration x) => string.Equals(x.Name, name, StringComparison.Ordinal)))
				{
					ValidateName(name);
					value.Add(new LbsMapDeclaration
					{
						Name = name,
						Prefab = ((item2.Attributes["Prefab"] == null) ? "" : item2.Attributes["Prefab"].Value.Trim())
					});
				}
			}
		}
		return lbsMapConfig;
	}

	internal static void ValidateName(string name)
	{
		if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.EndsWith(".", StringComparison.Ordinal) || name.EndsWith(" ", StringComparison.Ordinal))
		{
			throw new InvalidDataException("MapAdd 必须是 LbsMap 下的文件名或目录名：" + name);
		}
	}

	public string ResolveAsset(string name)
	{
		ValidateName(name);
		string text = Path.Combine(ModRoot, "Assemblies", "LbsMap", name);
		if (File.Exists(text))
		{
			return text;
		}
		if (File.Exists(text + ".ab"))
		{
			return text + ".ab";
		}
		if (Directory.Exists(text))
		{
			return text;
		}
		throw new FileNotFoundException("MapAdd 找不到对应资源：" + text + "（或 .ab）");
	}

	internal static string ResolveStageFile(string modRoot, string relativePath)
	{
		string text = Path.GetFullPath(modRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string path;
		if (string.IsNullOrWhiteSpace(relativePath))
		{
			// 兼容 BaseMod 的 StaticInfo 目录布局：优先 Data\StageInfo.xml（原插件约定），
			// 找不到时回落到 StaticInfo\StageInfo\StageInfo.xml。
			path = Path.Combine("Data", "StageInfo.xml");
			if (!File.Exists(Path.Combine(text, path)))
			{
				string fallback = Path.Combine("StaticInfo", "StageInfo", "StageInfo.xml");
				if (File.Exists(Path.Combine(text, fallback)))
				{
					path = fallback;
				}
			}
		}
		else
		{
			path = relativePath.TrimStart('\\', '/');
		}
		string fullPath = Path.GetFullPath(Path.Combine(text, path));
		if (!fullPath.StartsWith(text + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidDataException("StageInfo.xml 路径超出所属 Mod：" + relativePath);
		}
		return fullPath;
	}

	internal static string FindImage(string directory, string stem)
	{
		string[] array = (from p in Directory.GetFiles(directory)
			where string.Equals(Path.GetFileNameWithoutExtension(p), stem, StringComparison.OrdinalIgnoreCase) && new string[3] { ".png", ".jpg", ".jpeg" }.Contains(Path.GetExtension(p).ToLowerInvariant())
			select p).ToArray();
		if (array.Length != 1)
		{
			throw new InvalidDataException(directory + " 需要唯一的 " + stem + ".png/.jpg/.jpeg");
		}
		return array[0];
	}
}

using System.Collections.Generic;
using UnityEngine;

namespace LbsMap;

public sealed class LbsMapResources : MonoBehaviour
{
	public string Key;

	private readonly List<Object> owned = new List<Object>();

	public T Own<T>(T value) where T : Object
	{
		owned.Add((Object)(object)value);
		return value;
	}

	public void Release()
	{
		foreach (Object item in owned)
		{
			if (UnityNull.IsAlive(item))
			{
				Object.Destroy(item);
			}
		}
		owned.Clear();
	}

	private void OnDestroy()
	{
		Release();
	}
}

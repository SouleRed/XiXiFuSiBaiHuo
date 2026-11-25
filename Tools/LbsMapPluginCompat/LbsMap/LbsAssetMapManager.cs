using System.Reflection;
using UnityEngine;

namespace LbsMap;

public sealed class LbsAssetMapManager : MapManager
{
	public void Configure(GameObject visuals, MapManager template)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		base.mapSize = (MapSize)((!UnityNull.IsAlive((Object)(object)template)) ? 2 : ((int)template.mapSize));
		base.sephirahType = (SephirahType)(UnityNull.IsAlive((Object)(object)template) ? ((int)template.sephirahType) : 0);
		base.sephirahColor = (UnityNull.IsAlive((Object)(object)template) ? template.sephirahColor : Color.black);
		base.mapBgm = ((UnityNull.IsAlive((Object)(object)template) && template.mapBgm != null) ? template.mapBgm : new AudioClip[0]);
		BattleSoundManager val = Object.FindObjectOfType<BattleSoundManager>();
		AudioClip[] array = (UnityNull.IsAlive((Object)(object)val) ? (typeof(BattleSoundManager).GetField("enemyThemeSound", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(val) as AudioClip[]) : null);
		if (array != null && array.Length != 0)
		{
			base.mapBgm = (AudioClip[])array.Clone();
		}
		base.scratchPrefabs = ((UnityNull.IsAlive((Object)(object)template) && template.scratchPrefabs != null) ? template.scratchPrefabs : new GameObject[0]);
		base.wallCratersPrefabs = ((UnityNull.IsAlive((Object)(object)template) && template.wallCratersPrefabs != null) ? template.wallCratersPrefabs : new GameObject[0]);
		typeof(MapManager).GetField("_roots", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(this, new GameObject[1] { visuals });
		((Component)this).transform.localScale = Vector3.one * 3f;
		Collider[] componentsInChildren = visuals.GetComponentsInChildren<Collider>(true);
		foreach (Collider val2 in componentsInChildren)
		{
			val2.enabled = false;
		}
		((Component)this).gameObject.AddComponent<LbsMapCamera>().Configure((MapManager)(object)this);
	}

	public override void EnableMap(bool b)
	{
		if (b)
		{
			((Component)this).gameObject.SetActive(true);
		}
		base.EnableMap(b);
	}

	// 原 DLL 里是 IL 允许的“扩大访问级别的 override”（public override 覆盖 protected），
	// C# 编译器不接受，这里收窄为 protected；Unity 仍然按名字调用，行为不变。
	protected override void LateUpdate()
	{
	}
}

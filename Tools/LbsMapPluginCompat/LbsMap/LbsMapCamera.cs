using UnityEngine;

namespace LbsMap;

[DefaultExecutionOrder(30000)]
public sealed class LbsMapCamera : MonoBehaviour
{
	private MapManager owner;

	private BattleCamManager cameras;

	private BattleSceneRoot scene;

	private Camera background;

	public void Configure(MapManager map)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected Obj, but got Unknown
		owner = map;
		GameObject val = new GameObject("LbsMap background camera");
		val.transform.SetParent(((Component)this).transform, false);
		background = val.AddComponent<Camera>();
		((Behaviour)background).enabled = false;
		val.AddComponent<LbsMapCameraResources>();
	}

	private void LateUpdate()
	{
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		if (!UnityNull.IsAlive((Object)(object)owner) || !UnityNull.IsAlive((Object)(object)background))
		{
			return;
		}
		if (!UnityNull.IsAlive((Object)(object)scene))
		{
			scene = Object.FindObjectOfType<BattleSceneRoot>();
		}
		if (!UnityNull.IsAlive((Object)(object)cameras))
		{
			cameras = Object.FindObjectOfType<BattleCamManager>();
		}
		if (!owner.isEnabled || !((Component)owner).gameObject.activeInHierarchy || !UnityNull.IsAlive((Object)(object)scene) || !((Object)(object)scene.currentMapObject == (Object)(object)owner) || !UnityNull.IsAlive((Object)(object)cameras) || !UnityNull.IsAlive((Object)(object)cameras.mainCam) || !UnityNull.IsAlive((Object)(object)cameras.bgCam))
		{
			((Behaviour)background).enabled = false;
			return;
		}
		Camera mainCam = cameras.mainCam;
		Camera bgCam = cameras.bgCam;
		background.CopyFrom(bgCam);
		background.targetTexture = null;
		background.orthographic = false;
		background.allowHDR = true;
		background.clearFlags = (CameraClearFlags)2;
		background.backgroundColor = Color.black;
		background.nearClipPlane = 0.1f;
		background.farClipPlane = 2000f;
		background.cullingMask = 1 << LayerMask.NameToLayer("Background_Back");
		((Component)background).transform.position = ((Component)mainCam).transform.position;
		((Component)background).transform.rotation = ((Component)mainCam).transform.rotation;
		background.depth = bgCam.depth + 0.2f;
		background.aspect = mainCam.aspect;
		float num = Mathf.Max(25f, Vector3.Dot(-((Component)mainCam).transform.position, ((Component)mainCam).transform.forward));
		background.fieldOfView = (mainCam.orthographic ? Mathf.Clamp(2f * Mathf.Atan(mainCam.orthographicSize / num) * 57.29578f, 5f, 110f) : mainCam.fieldOfView);
		((Behaviour)background).enabled = true;
		LbsMapCameraResources lbsMapCameraResources = ((Component)mainCam).GetComponent<LbsMapCameraResources>();
		if (!UnityNull.IsAlive((Object)(object)lbsMapCameraResources))
		{
			lbsMapCameraResources = ((Component)mainCam).gameObject.AddComponent<LbsMapCameraResources>();
		}
		lbsMapCameraResources.FollowActiveMap = true;
	}

	private void OnDisable()
	{
		if (UnityNull.IsAlive((Object)(object)background))
		{
			((Behaviour)background).enabled = false;
		}
	}
}

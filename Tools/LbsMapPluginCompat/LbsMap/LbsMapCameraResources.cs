using UnityEngine;
using UnityEngine.Rendering;

namespace LbsMap;

[DefaultExecutionOrder(30001)]
[RequireComponent(typeof(Camera))]
public sealed class LbsMapCameraResources : MonoBehaviour
{
	public bool FollowActiveMap;

	private Camera target;

	private BattleSceneRoot scene;

	private CommandBuffer capture;

	private CommandBuffer release;

	private void OnEnable()
	{
		target = ((Component)this).GetComponent<Camera>();
	}

	private void LateUpdate()
	{
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		bool flag = UnityNull.IsAlive((Object)(object)target) && ((Behaviour)target).enabled;
		if (FollowActiveMap)
		{
			if (!UnityNull.IsAlive((Object)(object)scene))
			{
				scene = Object.FindObjectOfType<BattleSceneRoot>();
			}
			MapManager val = (UnityNull.IsAlive((Object)(object)scene) ? scene.currentMapObject : null);
			flag = flag && UnityNull.IsAlive((Object)(object)val) && val.isEnabled && ((Component)val).gameObject.activeInHierarchy && UnityNull.IsAlive((Object)(object)((Component)val).GetComponent<LbsMapCamera>());
		}
		if (flag)
		{
			Camera val2 = target;
			val2.depthTextureMode = (val2.depthTextureMode | (DepthTextureMode)1);
		}
		if (flag && capture == null)
		{
			Install();
		}
		else if (!flag && capture != null)
		{
			Remove();
		}
	}

	private void Install()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected Obj, but got Unknown
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected Obj, but got Unknown
		Camera val = target;
		val.depthTextureMode = (val.depthTextureMode | (DepthTextureMode)1);
		int num = Shader.PropertyToID("_LbsMapOpaque_" + ((Object)this).GetInstanceID());
		capture = new CommandBuffer
		{
			name = "LbsMap opaque background"
		};
		capture.GetTemporaryRT(num, -1, -1, 0, (FilterMode)1, (RenderTextureFormat)2);
		capture.Blit(new RenderTargetIdentifier((BuiltinRenderTextureType)2), new RenderTargetIdentifier(num));
		capture.SetGlobalTexture("_GrabbedTexture", new RenderTargetIdentifier(num));
		release = new CommandBuffer
		{
			name = "LbsMap release background"
		};
		release.ReleaseTemporaryRT(num);
		target.AddCommandBuffer((CameraEvent)16, capture);
		target.AddCommandBuffer((CameraEvent)20, release);
	}

	private void Remove()
	{
		if (UnityNull.IsAlive((Object)(object)target) && capture != null)
		{
			target.RemoveCommandBuffer((CameraEvent)16, capture);
		}
		if (UnityNull.IsAlive((Object)(object)target) && release != null)
		{
			target.RemoveCommandBuffer((CameraEvent)20, release);
		}
		if (capture != null)
		{
			capture.Release();
		}
		if (release != null)
		{
			release.Release();
		}
		capture = (release = null);
	}

	private void OnDisable()
	{
		Remove();
	}

	private void OnDestroy()
	{
		Remove();
	}
}

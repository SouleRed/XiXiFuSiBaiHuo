using UnityEngine;
using System.Collections;
namespace 裁缝之王.Presentation.Camera;

// 晚于原版基础跟随和根节点旋转；只求值，不在此阶段修改显示摄像机。
[DefaultExecutionOrder(32000)]
public sealed class TimelineCameraDriver : MonoBehaviour
{
    private void OnEnable() => StartCoroutine(RestoreAfterRendering());
    private IEnumerator RestoreAfterRendering()
    {
        var endOfFrame = new WaitForEndOfFrame();
        while (true) { yield return endOfFrame; TimelineActionCamera.RestoreVisualFrame(); }
    }
    private void LateUpdate()
    {
        TimelineActionCamera.PrepareFrame();
    }
    private void OnDisable() { StopAllCoroutines(); TimelineActionCamera.Clear(); }
    private void OnDestroy() => TimelineActionCamera.Clear();
}

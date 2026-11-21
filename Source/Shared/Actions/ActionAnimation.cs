using UnityEngine;

namespace 裁缝之王.Presentation.Actions;

internal static class ActionAnimation
{
    internal static float ClipTime(float elapsed, float duration, float clipIn, float speed)
        => clipIn + Mathf.Clamp(elapsed, 0f, duration) * speed;

    internal static bool IsInClipWindow(float elapsed, float duration, float preRoll, float postRoll, float tolerance, bool hold = false)
        => elapsed >= -preRoll - tolerance && (hold || elapsed <= duration + postRoll + tolerance);

    private static float ClampSampleTime(float time, float length) => Mathf.Min(length, time + .0000001f);

    // 挂点曲线有独立的生效窗口。片段结束后，在窗口内保持最后一个采样值。
    internal static bool TrySampleCurveTime(float sourceTime, float activeDuration, float curveDuration, out float sampleTime)
    {
        sampleTime = ClampSampleTime(sourceTime, curveDuration);
        return IsInClipWindow(sourceTime, activeDuration, 0f, 0f, 0f);
    }

    internal static void SampleInitialPose(AnimationClip clip, GameObject root) => SampleClip(clip, root, 0f);

    private static void SampleClip(AnimationClip clip, GameObject root, float time)
    {
        if (!clip.legacy && !root.GetComponent<Animator>())
        {
            var animator = root.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        clip.SampleAnimation(root, time);
    }

    internal static void SampleTrack(AnimationClip clip, Transform root, float clipTime)
        => SampleClip(clip, root.gameObject, ClampSampleTime(clipTime, clip.length));

    internal static void SampleInstance(AnimationClip clip, Transform root, float clipTime)
    {
        Vector3 position = root.localPosition, scale = root.localScale;
        Quaternion rotation = root.localRotation;
        SampleTrack(clip, root, clipTime);
        root.localPosition = position;
        root.localRotation = rotation;
        root.localScale = scale;
    }
}

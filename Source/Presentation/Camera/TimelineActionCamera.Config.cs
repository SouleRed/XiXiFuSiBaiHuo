using System;
using System.Collections.Generic;
using System.Linq;
using Soule.MotionCamera;
using Clip = Soule.MotionCamera.MotionCameraSequence;

namespace 裁缝之王.Presentation.Camera;

internal sealed partial class TimelineActionCamera
{
    internal static class Clips
    {
        private static readonly Dictionary<string, Dictionary<string, Clip>> Libraries = new(StringComparer.Ordinal);

        internal static Clip Get(string bundleKey, string id)
        {
            if (!Libraries.TryGetValue(bundleKey, out var clips))
            {
                var library = NativeKingSkin.BundleFor(bundleKey).LoadAsset<MotionCameraLibrary>("motion_cameras.asset");
                if (!library) throw new InvalidOperationException("角色资产包缺少镜头配置：" + bundleKey);
                clips = library.sequences.ToDictionary(sequence => sequence.id, StringComparer.Ordinal);
                Libraries.Add(bundleKey, clips);
            }

            if (clips.TryGetValue(id, out var clip)) return clip;
            throw new KeyNotFoundException($"角色资产包 {bundleKey} 缺少镜头序列：{id}");
        }
    }
}

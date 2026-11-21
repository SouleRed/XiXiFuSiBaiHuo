using System.Collections.Generic;
using UnityEngine;

namespace 裁缝之王.Presentation.Actions;

internal static class RencounterFocus
{
    // 切层当帧提亮，保留皮肤染色，并停止原有的颜色过渡。
    internal static void Highlight(CharacterAppearance appearance, Dictionary<SpriteRenderer, Color> previousColors = null)
    {
        if (appearance._imgColorRoutine != null)
        {
            appearance.StopCoroutine(appearance._imgColorRoutine);
            appearance._imgColorRoutine = null;
        }

        foreach (var motion in appearance._characterMotionDic.Values)
        {
            foreach (var sprite in motion.motionSpriteSet)
            {
                var renderer = sprite.sprRenderer;
                if (previousColors != null && !previousColors.ContainsKey(renderer)) previousColors.Add(renderer, renderer.color);
                renderer.color = sprite.sprType == CharacterAppearanceType.Skin ? appearance._curSkinColor : Color.white;
            }
        }

        appearance._curSpriteColor = Color.white;
    }
}

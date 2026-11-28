using System.Collections.Generic;
using UnityEngine;

namespace 裁缝之王.Presentation.Actions;

internal static class RencounterFocus
{
    internal static void Apply(BattleUnitView first, BattleUnitView second)
    {
        Highlight(first.charAppearance);
        Highlight(second.charAppearance);
        foreach (var unit in BattleObjectManager.instance.GetAliveList())
            if (unit != first.model && unit != second.model) unit.view.charAppearance.ChangeColor(Color.grey);
    }

    // 切层当帧提亮，保留皮肤染色，并停止原有的颜色过渡。
    internal static void Highlight(CharacterAppearance appearance, Dictionary<SpriteRenderer, Color> previousColors = null)
        => SetColor(appearance, Color.white, previousColors);

    internal static void Dim(CharacterAppearance appearance, Dictionary<SpriteRenderer, Color> previousColors = null)
        => SetColor(appearance, Color.grey, previousColors);

    private static void SetColor(CharacterAppearance appearance, Color color, Dictionary<SpriteRenderer, Color> previousColors)
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
                renderer.color = sprite.sprType == CharacterAppearanceType.Skin ? appearance._curSkinColor * color : color;
            }
        }

        appearance._curSpriteColor = color;
    }
}

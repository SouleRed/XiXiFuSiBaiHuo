using UnityEngine;
using 裁缝之王.Presentation.Effects;

namespace 裁缝之王.Presentation.Actions;

public sealed class KingParticle : ModParticleEffect
{
    internal KingEffect Cue;
    internal AssetBundle ActionBundle;
    protected override AssetBundle EffectBundle => ActionBundle;
    protected override string PrefabName => Cue.Prefab;
    protected override BattleUnitView WorldOrigin => Cue.Placement < 0 ? null : victim;
    protected override bool OnGround => Cue.Placement == 1;
    protected override void ConfigureInstance()
    {
        Instance.transform.localPosition = Cue.Placement < 0 ? Cue.Offset : Vector3.zero;
        Instance.transform.localRotation = Quaternion.identity;
        Instance.transform.localScale = Cue.Placement < 0 ? Vector3.one + Cue.ExtraScale
            : Vector3.Scale(Instance.transform.localScale, Vector3.Scale(Vector3.one + Cue.ExtraScale, Cue.ScaleCorrection));
        if (Cue.Placement == 2) ConfigureTimelinePosition(victim, center: true, follow: true, initialGround: false);
    }
}

using UnityEngine;

namespace 裁缝之王.Presentation.Actions;

internal enum NeedleClanPose
{
    S1Ready, S1Windup, S1Thrust, S2Ready, S2Thrust, S3Ready, S3Windup, S3Thrust
}

internal readonly struct NeedleClanSkin
{
    internal const string Pierce = "001_fx_pierce.prefab";
    internal const string PierceFollowUp = "002_fx_pierce (1).prefab";
    internal const string Dash = "003_fx_dash.prefab";
    internal const string PierceCombo = "004_fx_pierce (2).prefab";
    internal static readonly Vector3 BackStep = new(-1f, 0f, 0f);

    internal bool IsWeft { get; }
    internal string BundleKey { get; }
    internal AssetBundle Bundle { get; }
    private readonly string root;

    internal NeedleClanSkin(BattleUnitView actor)
    {
        int id = NativeKingSkin.NeedleId(actor);
        IsWeft = id == 1499;
        BundleKey = $"needleclan{id}_skin";
        Bundle = NativeKingSkin.BundleFor(BundleKey);
        root = $"assets/针族{id}还原";
    }

    internal KingPose Pose(float time, NeedleClanPose pose)
        => new(time, ActionDetail.Standing, root + "/角色/sprites/" + (IsWeft ? WeftSprites : WarpSprites)[(int)pose]);

    internal CoinTimeline.Cue Effect(KingPlayback play, float time, string prefab, float activation)
        => play.EffectAt(new KingEffect(time, root + "/特效/prefabs/动作/" + prefab,
            -1, activation, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one));

    private static readonly string[] WarpSprites =
    {
        "cab-9814cbea16055c6c36352d2595849c20__4347611321496831591.png",
        "cab-9814cbea16055c6c36352d2595849c20__109655537710481466.png",
        "cab-9814cbea16055c6c36352d2595849c20__-6136868848597552158.png",
        "cab-9814cbea16055c6c36352d2595849c20__2586550158242821412.png",
        "cab-9814cbea16055c6c36352d2595849c20__5471594271417655721.png",
        "cab-9814cbea16055c6c36352d2595849c20__1508051928259539833.png",
        "cab-9814cbea16055c6c36352d2595849c20__2395088452308935558.png",
        "cab-9814cbea16055c6c36352d2595849c20__2972853769452513749.png",
    };

    private static readonly string[] WeftSprites =
    {
        "cab-9814cbea16055c6c36352d2595849c20__-1723007548125854194.png",
        "cab-9814cbea16055c6c36352d2595849c20__-5978181537264503335.png",
        "cab-9814cbea16055c6c36352d2595849c20__23146987465728320.png",
        "cab-9814cbea16055c6c36352d2595849c20__-2810091037772313697.png",
        "cab-9814cbea16055c6c36352d2595849c20__-1247482312993337507.png",
        "cab-9814cbea16055c6c36352d2595849c20__8327730203807127810.png",
        "cab-9814cbea16055c6c36352d2595849c20__-2317044922151451943.png",
        "cab-9814cbea16055c6c36352d2595849c20__3268087927700489120.png",
    };
}

using 裁缝之王.Presentation.Camera;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace 裁缝之王.Presentation.Actions;

public sealed class BehaviourAction_CFK_S7_Coin1 : KingBehaviourAction
{
    protected override List<RencounterManager.MovingAction> BuildAction(
        ref RencounterManager.ActionAfterBehaviour self, ref RencounterManager.ActionAfterBehaviour opponent)
    {
        var poses = new KingPose[]
        {
            new(0f, ActionDetail.Slash2),
            new(0.13333334f, ActionDetail.Default, "assets/裁缝之王敌方1496还原/角色/sprites/skill1_3_-2548473754123588126.png"),
            new(0.25f, ActionDetail.Guard),
            new(0.33333334f, ActionDetail.S4),
            new(1.9333333f, ActionDetail.S7),
            new(2f, ActionDetail.S3),
            new(2.01666668f, ActionDetail.Slash),
        };
        var play = new KingPlayback(ref self, ref opponent, "S7", poses, withAllies: true);
        const float feedback = 2.2666667f;
        float end = 3.55f;
        var timeline = play.CreateTimeline(play.Sample, completionSourceTime: end);
        var ready = opponent.data.actionDetail;
        timeline.Section(feedback, ActionDetail.Slash2, ready, new[]
        {
            play.MoveAt(new KingMove(0f, 0f, 1, false, true, true, 4, false, Vector3.zero, 8f, false, false)),
            CoinTimeline.PlayVoice(0f, "10SV-095"),
            CoinTimeline.PlaySound(0f, "magicwood_4_1-1_e9fd2782", 1f),
            CoinTimeline.PlaySound(0.16666667f, "human_rush_746aa24b", 1f),
            play.MoveAt(new KingMove(0.21666667f, 0.066f, 1, false, false, true, 4, false, new Vector3(1f,0f,0f), 0f, false, false)),
            play.EffectAt(new KingEffect(0.33333334f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/019_fx_s17_ready_char.prefab", -1, 1.5833334f, 0f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            CoinTimeline.PlaySound(1.9166666f, "human_rush_746aa24b", 1f),
            play.EffectAt(new KingEffect(2f, "assets/裁缝之王敌方1496还原/特效/prefabs/动作/001_fx_mon_cp10_needle_king_slash.prefab", -1, 0.36666667f, 1f, false, false, Vector3.zero, Vector3.zero, Vector3.one)),
            CoinTimeline.PlaySound(2.1666667f, "magicwood_7_1-3_8f46d4ed", 1f),
            CoinTimeline.PlaySound(2.2166667f, "magicwood_7_1-3_8f46d4ed", 1f),
            play.HitAt(2.2666667f, 0f, secondary: true),
        });
        timeline.Section(end, ActionDetail.Slash, ActionDetail.Damaged, new[]
        {
            CoinTimeline.PlaySound(2.3333333f, "magicwood_7_1-3_8f46d4ed", 1f),
            CoinTimeline.PlaySound(2.3666666f, "magicwood_7_1-3_8f46d4ed", 1f),
            CoinTimeline.PlaySound(2.5f, "magicwood_7_1-3_8f46d4ed", 1f),
        }, feedback: true);
        var camera = TimelineActionCamera.Clips.Get(KingPlayback.BundleKey, "NeedleKingEnemy1496_S7_Coin1");
        return play.BindAction(timeline, camera, ref self, ref opponent);
    }
}

internal sealed class KingAllyPresentation
{
    private static readonly ConditionalWeakTable<BattlePlayingCardDataInUnitModel, BattleUnitModel[]> Participants = new();
    private const string EffectRoot = "assets/裁缝之王敌方1496还原/特效/prefabs/动作/";
    private const string PinSpriteRoot = "assets/裁缝之王敌方1496还原/角色/sprites/敌方针族/";
    private const float SourceSkinPivotScale = .5f;
    private const float RotationCenterScale = .8f;
    private static readonly Vector3 PinCenterLocal = new(0f, .695f, 0f);

    private sealed class PinFx
    {
        public BattleUnitView source;
        public Transform root;
        public SpriteRenderer renderer;
        public Vector3 start;
        public KingParticle lift;
    }

    private readonly BattleUnitView actor, target;
    private readonly KingPlayback playback;
    private readonly BattleUnitView[] sourcePins;
    private readonly List<PinFx> pins = new();
    private Sprite warpDamaged, weftDamaged;
    private Vector3 center;
    private bool spawned, gathering, collided;

    internal static void Capture(BattlePlayingCardDataInUnitModel card, BattleUnitModel[] pins)
    {
        Participants.Remove(card);
        Participants.Add(card, pins);
    }

    internal KingAllyPresentation(BattleUnitView actor, BattleUnitView target, BattlePlayingCardDataInUnitModel card, KingPlayback playback)
    {
        this.actor = actor;
        this.target = target;
        this.playback = playback;
        sourcePins = card != null && Participants.TryGetValue(card, out var recorded)
            ? recorded.Where(unit => unit?.view && unit.view.charAppearance).Select(unit => unit.view).ToArray()
            : System.Array.Empty<BattleUnitView>();
    }

    private static bool IsWeftPin(BattleUnitView view, int index)
    {
        var bookId = view.model?.Book?.BookId;
        if (bookId != null && bookId.packageId == KingBehaviourAction.PackageId)
        {
            if (bookId.id == 2) return false;
            if (bookId.id == 3) return true;
        }
        var res = view.charAppearance ? view.charAppearance.resourceName : null;
        if (!string.IsNullOrEmpty(res))
        {
            if (res.IndexOf("Wei", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (res.IndexOf("Jing", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
        }
        return index % 2 != 0;
    }

    private static Sprite LoadPinSprite(string file)
        => NativeKingSkin.LoadedBundle.LoadAssetWithSubAssets<Sprite>(PinSpriteRoot + file).Single();

    internal void Sample(float time)
    {
        const float liftAt = .25f, gatherAt = 2.1666667f, collisionAt = 2.2666667f;
        if (sourcePins.Length == 0 || time < liftAt) return;

        var actorRenderer = actor.charAppearance.transform.Find("Custom_Standing/Customize_Renderer")?.GetComponent<SpriteRenderer>()
            ?? actor.charAppearance.transform.Find("Custom_Default/Customize_Renderer")?.GetComponent<SpriteRenderer>();
        Quaternion camRot = Quaternion.Euler(SingletonBehavior<BattleCamManager>.Instance.RotationX, 0f, 0f);
        float units = ActionMotion.Scale(actor);
        float appearanceScale = units * RotationCenterScale;

        if (!spawned)
        {
            spawned = true;
            int effectLayer = LayerMask.NameToLayer("Effect");
            warpDamaged ??= LoadPinSprite("a_受击.png");
            weftDamaged ??= LoadPinSprite("b_受击.png");

            for (int i = 0; i < sourcePins.Length; i++)
            {
                var src = sourcePins[i];
                if (!src) continue;
                Vector3 startPos = src.WorldPosition;
                if (src.charAppearance) src.charAppearance.gameObject.SetActive(false);

                var rootGo = new GameObject($"[裁缝之王] S7针族_{i + 1}");
                rootGo.transform.position = startPos;
                rootGo.layer = effectLayer;

                var visualGo = new GameObject("外观", typeof(SpriteRenderer));
                visualGo.transform.SetParent(rootGo.transform, false);
                visualGo.transform.localScale = Vector3.one * (SourceSkinPivotScale * appearanceScale);
                visualGo.transform.rotation = camRot;
                visualGo.layer = effectLayer;

                var sr = visualGo.GetComponent<SpriteRenderer>();
                sr.sprite = IsWeftPin(src, i) ? weftDamaged : warpDamaged;
                if (actorRenderer)
                {
                    sr.sortingLayerID = actorRenderer.sortingLayerID;
                    sr.sortingOrder = actorRenderer.sortingOrder;
                }
                float facing = src.model.direction == Direction.LEFT ? -1f : 1f;
                sr.flipX = facing >= 0f;

                var liftFx = playback.Spawn(new KingEffect(liftAt, EffectRoot + "029_fx_s7_ready.prefab", 1, 3f, 0f, false, false,
                    Vector3.zero, Vector3.zero, Vector3.one), actor);
                if (liftFx)
                {
                    liftFx.transform.SetParent(rootGo.transform, false);
                    liftFx.transform.position = visualGo.transform.TransformPoint(PinCenterLocal);
                    liftFx.transform.rotation = camRot;
                    liftFx.transform.localScale = Vector3.one * appearanceScale;
                }

                pins.Add(new PinFx
                {
                    source = src,
                    root = rootGo.transform,
                    renderer = sr,
                    start = startPos,
                    lift = liftFx,
                });
            }
        }

        if (time >= gatherAt && !gathering)
        {
            gathering = true;
            center = target.WorldPosition;
        }
        float lift = ActionMotion.EvaluateEase(6, time - liftAt, null);
        float gather = ActionMotion.EvaluateEase(6, (time - gatherAt) / .1f, null);
        for (int i = 0; i < pins.Count; i++)
        {
            var pin = pins[i];
            if (!pin.root) continue;
            var airborne = pin.start + Vector3.up * (units * lift);
            float angle = Mathf.PI + i * (2f * Mathf.PI / pins.Count);
            var ring = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (.5f * units);
            pin.root.position = gathering ? Vector3.LerpUnclamped(airborne, center + ring, gather) : airborne;
            pin.renderer.transform.rotation = camRot;
            float facing = gathering ? center.x - pin.start.x : (pin.source && pin.source.model.direction == Direction.LEFT ? -1f : 1f);
            pin.renderer.flipX = facing >= 0f;
            if (pin.lift)
            {
                pin.lift.transform.position = pin.renderer.transform.TransformPoint(PinCenterLocal);
                pin.lift.transform.rotation = camRot;
                pin.lift.transform.localScale = Vector3.one * appearanceScale;
            }
        }
        if (collided || time < collisionAt) return;
        collided = true;
        foreach (var pin in pins) if (pin.renderer) pin.renderer.enabled = false;
        playback.Spawn(new KingEffect(collisionAt, EffectRoot + "030_fx_s7_explosion.prefab", 1, 3f, 0f, false, false,
            Vector3.zero, Vector3.zero, Vector3.one), target);
    }

    internal void Finish()
    {
        for (int i = 0; i < pins.Count; i++)
        {
            var pin = pins[i];
            if (pin.lift && pin.lift.transform.parent == pin.root)
                pin.lift.transform.SetParent(null, true);
            if (pin.root) Object.Destroy(pin.root.gameObject);
            if (pin.source && pin.source.charAppearance && !pin.source.model.IsDead())
                pin.source.charAppearance.gameObject.SetActive(true);
        }
        pins.Clear();
    }
}

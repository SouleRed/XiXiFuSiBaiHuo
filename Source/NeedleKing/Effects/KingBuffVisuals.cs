using UnityEngine;

namespace 裁缝之王.Presentation.Effects;

public sealed class KingBuffVisuals : MonoBehaviour
{
    private const string StatusFolder = "assets/裁缝之王敌方1496还原/特效/prefabs/状态/";
    // 原版阳光由 SetEffect_Label 使用源身高的四分之一进行定标。
    private const float SunlightHeightFactor = .25f;
    private const float CocoonFieldScale = 2.86f;
    private const float CocoonFieldOffscreenX = 22f;
    private const float CocoonFieldHeight = 16.4f;
    private static readonly Vector3 CocoonRippleChildOffset = new(-28.44f, 4.84f, 0f);

    private readonly BuffVisual needle = new("035_fx_mon_cp10_needleking_buff_needleexpected.prefab");
    private readonly BuffVisual sunlight = new("039_fx_mon_cp10_goldenledder1.prefab");
    private readonly BuffVisual cocoon = new("031_fx_mon_cp10_needleking_buff_howling_hear.prefab");
    private readonly BuffVisual cocoonField1 = new("033_fx_mon_cp10_needleking_buff_cocooncrying_2.prefab");
    private readonly BuffVisual cocoonField2 = new("034_fx_mon_cp10_needleking_buff_cocooncrying_3.prefab");
    private static KingBuffVisuals leftFieldOwner, rightFieldOwner;
    private BattleUnitView view;
    private CharacterAppearance appearance;
    private int activeHowlingTier;

    internal static void Track(BattleUnitModel owner)
    {
        var unitView = owner?.view;
        if (!unitView) return;
        var visuals = unitView.GetComponent<KingBuffVisuals>();
        if (!visuals) visuals = unitView.gameObject.AddComponent<KingBuffVisuals>();
        visuals.view = unitView;
    }

    private void LateUpdate()
    {
        var model = view ? view.model : null;
        if (model == null || model.IsDead() || model.IsExtinction() || !view.charAppearance)
        {
            Clear();
            return;
        }

        if (appearance != view.charAppearance)
        {
            Clear();
            appearance = view.charAppearance;
        }

        bool marked = false, golden = false, hasKingWarmLight = false, hasLrhWarmLight = false;
        int howlingTier = 0;
        foreach (var buf in model.bufListDetail.GetActivatedBufList())
        {
            if (buf == null || buf.IsDestroyed()) continue;
            if (buf is BattleUnitBuf_BF_YangGuang_CFK)
            {
                golden = true;
                hasKingWarmLight = true;
            }
            else if (buf is BattleUnitBuf_BF_YangGuang_LRH)
            {
                golden = true;
                hasLrhWarmLight = true;
            }
            else if (buf.stack <= 0) continue;
            else if (buf is BattleUnitBuf_BF_CiSheZ || buf is BattleUnitBuf_BF_CiSheZ_LRH) marked = true;
            else if (buf is BattleUnitBuf_BF_BeiMing_2) howlingTier = 2;
            else if (buf is BattleUnitBuf_BF_BeiMing_1 && howlingTier < 1) howlingTier = 1;
        }
        if (hasLrhWarmLight && PassiveAbility_8LRH_WarmLight_CFK.IsCryEnhancedRound())
        {
            int lrhCryTier = Shared.Interop.LeRougeInterop.GetCryTier(model);
            if (lrhCryTier > howlingTier) howlingTier = lrhCryTier;
        }

        // 统一挂在 Appearance 节点下，将源特效从贴图单位转回源单位。
        // 联动红派希斯克里夫按常规角色身高（170）与裁缝之王固定身高（370）之比在原缩放基础上叠加等比缩放。
        float sourceUnits = 1f / Actions.ActionMotion.SourceVisualScale;
        float personalScale = hasLrhWarmLight && !hasKingWarmLight
            ? 170f / PassiveAbility_3DuShagTS_CFK.KingHeight
            : 1f;
        var parent = appearance.transform;
        var rotation = Quaternion.Euler(0f, 180f, 0f);
        int layer = LayerMask.NameToLayer(appearance.GetLayerName() ?? "");
        if (layer < 0) layer = appearance.gameObject.layer;

        needle.Sync(marked, parent, rotation, sourceUnits, layer);
        sunlight.Sync(golden, parent, rotation, sourceUnits * NativeKingSkin.SunlightSourceHeight * SunlightHeightFactor * personalScale, layer);
        cocoon.Sync(howlingTier > 0, parent, rotation, 1f * personalScale, layer);

        activeHowlingTier = howlingTier;
        var fieldParent = SingletonBehavior<BattleSceneRoot>.Instance ? SingletonBehavior<BattleSceneRoot>.Instance.transform : null;
        if (howlingTier <= 0 || !fieldParent)
        {
            ReleaseFieldSlots();
            cocoonField1.Clear();
            cocoonField2.Clear();
            return;
        }

        bool onRight = model.faction == Faction.Enemy;
        ref var slot = ref (onRight ? ref rightFieldOwner : ref leftFieldOwner);
        if (!slot || !slot.isActiveAndEnabled || slot.activeHowlingTier <= 0 || howlingTier > slot.activeHowlingTier)
            slot = this;

        var activeField = howlingTier == 2 ? cocoonField2 : cocoonField1;
        (howlingTier == 2 ? cocoonField1 : cocoonField2).Clear();
        if (slot != this)
        {
            activeField.Clear();
            return;
        }

        Quaternion rot = onRight ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        if (!activeField.Sync(true, fieldParent, rot, CocoonFieldScale) || !activeField.Instance) return;

        var map = SingletonBehavior<HexagonalMapManager>.Instance;
        float left = map ? map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.LEFT) : -56f;
        float right = map ? map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.RIGHT) : 56f;
        float up = map ? map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.UP) : 34.64f;
        float down = map ? map.GetCurrentBorderPos(HexagonalMapManager.WallDirection.DOWN) : -51.96f;
        Vector3 desiredCenter = new(onRight ? right + CocoonFieldOffscreenX : left - CocoonFieldOffscreenX, CocoonFieldHeight, (up + down) * .5f);
        activeField.Instance.transform.position = desiredCenter - (rot * CocoonRippleChildOffset) * CocoonFieldScale;
    }

    private void ReleaseFieldSlots()
    {
        if (leftFieldOwner == this) leftFieldOwner = null;
        if (rightFieldOwner == this) rightFieldOwner = null;
    }

    private void Clear()
    {
        activeHowlingTier = 0;
        ReleaseFieldSlots();
        needle.Clear();
        sunlight.Clear();
        cocoon.Clear();
        cocoonField1.Clear();
        cocoonField2.Clear();
        appearance = null;
    }

    private void OnDisable() => Clear();

    private sealed class BuffVisual
    {
        private readonly string asset;
        private GameObject prefab, instance;
        private bool loadAttempted;
        private Transform[] layerNodes;
        private int currentLayer = -1;

        internal BuffVisual(string file) => asset = StatusFolder + file;

        internal GameObject Instance => instance;

        internal bool Sync(bool visible, Transform parent, Quaternion rotation, float scale, int hostLayer = -1)
        {
            if (!visible)
            {
                Clear();
                return false;
            }

            bool created = false;
            if (!instance)
            {
                if (!LoadPrefab()) return false;
                instance = Instantiate(prefab, parent, false);
                instance.transform.localRotation = rotation * prefab.transform.localRotation;
                instance.transform.localScale = prefab.transform.localScale * scale;
                if (hostLayer >= 0) layerNodes = instance.GetComponentsInChildren<Transform>(true);
                instance.SetActive(true);
                var particles = instance.GetComponent<ParticleSystem>();
                if (particles) particles.Play(true);
                created = true;
            }

            if (layerNodes == null || currentLayer == hostLayer) return created;
            foreach (var node in layerNodes)
                if (node) node.gameObject.layer = hostLayer;
            currentLayer = hostLayer;
            return created;
        }

        private bool LoadPrefab()
        {
            if (prefab) return true;
            var bundle = NativeKingSkin.LoadedBundle;
            if (!bundle || loadAttempted) return false;
            loadAttempted = true;
            prefab = bundle.LoadAsset<GameObject>(asset);
            if (!prefab) Debug.LogWarning("[裁缝之王] 缺少 BUFF 特效资产：" + asset);
            return prefab;
        }

        internal void Clear()
        {
            if (instance)
            {
                instance.SetActive(false);
                Destroy(instance);
            }
            instance = null;
            layerNodes = null;
            currentLayer = -1;
        }
    }
}

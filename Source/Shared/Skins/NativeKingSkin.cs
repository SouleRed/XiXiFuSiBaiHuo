using System;
using System.IO;
using UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace 裁缝之王
{
    internal static class NativeKingSkin
    {
        private const string PackageId = "CaiFengZhiKing";
        private const string NativePrefix = PackageId + "/Native/";
        private static readonly SkinAsset King = new("CaiFengKing", "needlekingenemy1496_skin",
            "assets/裁缝之王敌方1496还原/图书馆皮肤/prefabs/appearance_needlekingenemy1496.prefab");
        private static readonly SkinAsset[] Skins =
        {
            King,
            new("ZhenZuJing", "needleclan1498_skin",
                "assets/针族1498还原/图书馆皮肤/prefabs/appearance_needleclan1498.prefab"),
            new("ZhenZuWei", "needleclan1499_skin",
                "assets/针族1499还原/图书馆皮肤/prefabs/appearance_needleclan1499.prefab")
        };
        private static bool registered;

        private sealed class SkinAsset
        {
            internal readonly string Id, BundleName, Address;
            internal AssetBundle Bundle;
            internal GameObject Prefab;
            internal string ResourceName => NativePrefix + Id;

            internal SkinAsset(string id, string bundleName, string address)
            {
                Id = id;
                BundleName = bundleName;
                Address = address;
            }

            internal void Load(string assemblyDirectory)
            {
                string path = Path.Combine(assemblyDirectory, "AB", BundleName);
                try
                {
                    Bundle = AssetBundle.LoadFromFile(path);
                    if (!Bundle) throw new IOException("无法打开资源包：" + path);
                    Prefab = Bundle.LoadAsset<GameObject>(Address);
                    if (!Prefab || !Prefab.GetComponent<CharacterAppearance>() || !Prefab.GetComponent<CharacterSound>())
                        throw new InvalidDataException("皮肤缺少原生组件：" + Address);
                }
                catch (Exception exception)
                {
                    Prefab = null;
                    if (Bundle) Bundle.Unload(true);
                    Bundle = null;
                    Debug.LogError("[裁缝之王] 注册原生皮肤失败：" + Id + " " + exception);
                }
            }
        }

        internal static AssetBundle LoadedBundle => King.Bundle;

        internal static int NeedleId(BattleUnitView view)
        {
            string resource = view && view.charAppearance ? view.charAppearance.resourceName : null;
            if (resource == Skins[1].ResourceName) return 1498;
            if (resource == Skins[2].ResourceName) return 1499;
            return 0;
        }

        internal static AssetBundle BundleFor(string key)
            => Array.Find(Skins, skin => skin.BundleName == key)?.Bundle;
        internal static float SunlightSourceHeight { get; private set; }
        internal static bool IsNative(BattleUnitView view)
            => view && view.charAppearance && view.charAppearance.resourceName == King.ResourceName;

        internal static bool HasAnySkin => Array.Exists(Skins, skin => skin.Prefab);
        internal static bool HasKingSkin => King.Prefab;

        internal static void LoadSkins(string assemblyDirectory)
        {
            if (registered) return;
            foreach (var skin in Skins) skin.Load(assemblyDirectory);

            if (!HasAnySkin) return;
            registered = true;
            if (King.Prefab)
            {
                var height = King.Prefab.transform.Find("LimbusFxRoot/[Transform]CharacterHeightPoint");
                SunlightSourceHeight = height ? height.localPosition.y : 0f;
            }
        }

        internal static void TryOverrideHeight(BattleUnitView view, BattleUnitView.SkinInfo skinInfo, ref int height)
        {
            if (skinInfo?.state is BattleUnitView.SkinState.EGO or BattleUnitView.SkinState.Creature) return;
            var passives = view.model?.passiveDetail?.PassiveList;
            if (passives == null) return;
            foreach (var p in passives)
            {
                if (p is PassiveAbility_3DuShagTS_CFK)
                {
                    height = PassiveAbility_3DuShagTS_CFK.KingHeight;
                    return;
                }
            }
        }

        private static SkinAsset Resolve(UnitDataModel unit)
        {
            if (unit == null) return null;
            if (!string.IsNullOrEmpty(unit.workshopSkin))
                return Array.Find(Skins, skin => skin.Prefab && unit.workshopSkin == PackageId + "/" + skin.Id);
            return Resolve(unit.CustomBookItem?.ClassInfo);
        }

        private static SkinAsset Resolve(BookXmlInfo book)
        {
            if (book?.id?.packageId != PackageId) return null;
            string id = book.GetCharacterSkin();
            return Array.Find(Skins, skin => skin.Prefab && skin.Id == id);
        }

        internal static void UsePrefabThumbnail(BookXmlInfo book, ref Sprite result)
        {
            var skin = Resolve(book);
            if (skin == null) return;
            var renderer = skin.Prefab.transform.Find("Custom_Default/Customize_Renderer")?.GetComponent<SpriteRenderer>();
            if (renderer && renderer.sprite) result = renderer.sprite;
        }

        private static CharacterAppearance Create(UnitDataModel unit, SkinAsset skin, GameObject instance, string layer)
        {
            var appearance = instance.GetComponent<CharacterAppearance>();
            appearance.Initialize(skin.ResourceName);
            appearance.InitCustomData(unit.customizeData, unit.defaultBook.GetBookClassInfoId());
            appearance.InitGiftDataAll(unit.giftInventory.GetEquippedList());
            appearance.ChangeMotion(ActionDetail.Standing);
            appearance.ChangeLayer(layer);
            return appearance;
        }

        internal static bool TryCreateBattleSkin(UnitDataModel unit, Faction faction, Transform characterRoot, ref CharacterAppearance result)
        {
            var skin = Resolve(unit);
            if (skin == null) return true;
            GameObject instance = null;
            try
            {
                instance = Object.Instantiate(skin.Prefab, characterRoot, false);
                var sound = instance.GetComponent<CharacterSound>();
                if (!sound._soundPrefab) sound._soundPrefab = SingletonBehavior<BattleSoundManager>.Instance?.effectSoundPrefab;
                var appearance = Create(unit, skin, instance, "Character");
                appearance.SetLibrarianOnlySprites(faction);
                result = appearance;
                return false;
            }
            catch (Exception exception)
            {
                if (instance) Object.Destroy(instance);
                Debug.LogError("[裁缝之王] 创建战斗皮肤失败：" + exception);
                return true;
            }
        }

        internal static bool TrySetPreviewSkin(UICharacterRenderer renderer, UnitDataModel unit, int index, ref bool forcelyReload)
        {
            if (unit == null || index < 0 || index >= renderer.characterList.Count) return true;
            var slot = renderer.characterList[index];
            string currentResource = slot.unitAppearance ? slot.unitAppearance.resourceName : null;
            bool wasNative = currentResource?.StartsWith(NativePrefix, StringComparison.Ordinal) == true;
            var skin = Resolve(unit);
            if (skin == null)
            {
                if (wasNative) forcelyReload = true;
                return true;
            }
            unit.textureIndex = index;
            if (!forcelyReload && currentResource == skin.ResourceName && slot.unitModel == unit) return false;

            GameObject instance = null;
            try
            {
                instance = Object.Instantiate(skin.Prefab, renderer.characterRoot, false);
                instance.transform.localPosition = new Vector3(10 * index, -2f, 10f);
                var appearance = Create(unit, skin, instance, "CharacterAppearance_UI");
                if (slot.unitAppearance)
                {
                    if (!wasNative) Singleton<AssetBundleManagerRemake>.Instance.ReleaseSdObject(slot.unitAppearance.resourceName);
                    slot.unitAppearance.gameObject.SetActive(false);
                    Object.Destroy(slot.unitAppearance.gameObject);
                }
                if (!string.IsNullOrEmpty(slot.resName) && (!slot.unitAppearance || slot.resName != slot.unitAppearance.resourceName))
                    Singleton<AssetBundleManagerRemake>.Instance.ReleaseSdObject(slot.resName);
                slot.unitModel = unit;
                slot.unitAppearance = appearance;
                slot.resName = "";
            }
            catch (Exception exception)
            {
                if (instance) Object.Destroy(instance);
                Debug.LogError("[裁缝之王] 创建皮肤预览失败：" + exception);
                forcelyReload = true;
                return true;
            }
            renderer.Redraw(index);
            return false;
        }
    }
}

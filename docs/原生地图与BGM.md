# 原生地图与 BGM（当前方案）

本项目的地图从 MOD 自带的原生 LZ4 AssetBundle 实例化，**不依赖任何第三方地图插件**。
旧的 LbsMap 插件链路（`LbsMapPlugin.dll`、`Assemblies\LbsMap\*`、`CFK_Full.ab`）已废弃并移除。

## 结构

```
MOD根目录\Assemblies\
├ AB\
│   ├ filature_map.ab              地图 AB（含预制体与内置 mapBgm）
│   ├ needleclan1498_skin          皮肤
│   ├ needleclan1499_skin
│   └ needlekingenemy1496_skin
└ XiXiFuSiBaiHuo.dll               含 NativeStageMap
```

## 加载路径

`Source\Shared\Stage\NativeStageMap.cs`：

- `FilatureMapId = "Filature_Map"`
- AB 目录：`<Assemblies>\AB\<BundleFile>`（`filature_map.ab`）
- 资源地址：`InvitationMaps/InvitationMap_Filature_Map`（与原版 `Util.LoadPrefab` 的路径一致）
- `MapEntry(FilatureMapId, "filature_map.ab", "Filature_Map")`

流程：

1. `LoadMaps(assembliesRoot)` 逐个 `AssetBundle.LoadFromFile`，`LoadAsset<GameObject>` 缓存到 `entry.Prefab`
2. StageInfo 里写 `<MapInfo>Filature_Map</MapInfo>`，原版调用 `Util.LoadPrefab("InvitationMaps/InvitationMap_Filature_Map", parent)` 时由我们改道到缓存预制体
3. 预制体自带原版 `MapManager` 脚本，之后的相机、换场、BGM 全部交给原版

## BGM

- 曲目随 AB 内置（`mapBgm` 索引），不再需要 `Assemblies\BGM\*.wav`，也不再走 `BgmProvider`
- 日志中 `mapBgm=3` 即从 AB 读取到的曲目索引

## 验证

进战斗后查 `Player.log`：

```
[裁缝之王][NativeMap] 已载入原生地图：Filature_Map (filature_map.ab, mapBgm=3)
[裁缝之王][NativeMap] 已按原版流程实例化接待地图：InvitationMap_Filature_Map
```

- `未找到原生地图资产包` → `Assemblies\AB\filature_map.ab` 缺少，检查部署
- `无法加载原生地图资产包` → AB 与当前 Unity 版本不匹配（本项目为 2019.3.15f1）
- 加载到预制体但画面黑 → AB 内的图层/贴图引用缺失，需重新打包 AB

## 构建与部署

```powershell
.\BuildAndDeploy.ps1 -BuildOnly   # 只编译
.\BuildAndDeploy.ps1              # 编译 + 整体替换创意工坊目录（游戏需关闭）
```

- 部署目标是**创意工坊目录**：`<Steam>\steamapps\workshop\content\1256670\XiXiFuSiBaiHuo`
- 脚本会清理游戏根 `LibraryOfRuina_Data\Mods` 下的同名副本（移入回收站），避免两处并存
- 游戏实际加载的是创意工坊目录，日志 `load : ...\workshop\content\1256670\XiXiFuSiBaiHuo\Assemblies\XiXiFuSiBaiHuo.dll` 可确认

## 边界

- 不再引入第三方地图插件；新增地图按同一模式加 `MapEntry` 与对应 AB
- 地图 AB 需与游戏同为 Unity 2019.3.15f1，且为 LZ4 压缩
- 本方案不含动态粒子调参入口：粒子属于 AB 内容，改动需回到 AB 制作侧

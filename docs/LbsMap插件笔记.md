# LbsMap 地图插件笔记

插件：`MOD根目录\Assemblies\LbsMapPlugin.dll`，模板：`MOD根目录\Assemblies\LbsMap\LbsMap.ab`
本项目的 DLL 已被改造过（源码在 `Tools\LbsMapPluginCompat`），详见第 6 节。

## 1. 两种模式

| 模式 | `<MapAdd>` 指向 | 用到的图 |
|---|---|---|
| 目录模式（我们在用） | `Assemblies\LbsMap\<名字>` 目录 | 目录里的 `BG.png` + `Board.png` |
| ab 模式 | `Assemblies\LbsMap\<名字>.ab` | ab 里的预制体（可用 `Prefab="路径"` 指定） |

目录模式的实际流程：加载 `LbsMap.ab` 模板预制体 → 把模板里两个节点的贴图换成你的图：

```
tr_m_back_lv0  / quad         ← BG.png      （远景背景）
tr_quad_floor_lv0 / quad_floor ← Board.png  （地板）
```

`<MapAdd>` 的值必须与目录/文件名一致，否则 `LbsMap-error.txt` 里会写「MapAdd 找不到对应资源」。

## 2. ⚠️ 一条 Stage 只能写一条 MapAdd（踩过的坑）

插件会为**每一条** `<MapAdd>` 各生成一张地图并注册进场景，而游戏最终只显示其中一张。
实测：同时写 `<MapAdd>LbsMap</MapAdd>` 和 `<MapAdd>CFK</MapAdd>` 时，显示的是 **`LbsMap` 模板地图**（模板自带美术），
你改 `CFK` 目录里的 BG/Board 完全不生效——看起来就像"换图没反应"。

**正确写法**（StageInfo）：

```xml
<Stage id="1">
  <FloorNum>1</FloorNum>
  <MapAdd>CFK</MapAdd>      <!-- 只写你想要的那张 -->
  ...
```

不需要再写 `<MapAdd>LbsMap</MapAdd>`：模板 ab 是按固定路径直接加载的，不依赖这条登记。

## 3. 素材规格

| 文件 | 建议尺寸 | 说明 |
|---|---|---|
| `BG.png` | **2000×552** | 模板自带 BG 就是 2000×552；用 2000×551 也能显示，差 1px 无所谓 |
| `Board.png` | **2000×678** | 与边狱 battlemap 的 `floor` 贴图规格一致，可直接用解包产物 |

- 支持 `.png/.jpg/.jpeg`，按文件名（不含扩展名）匹配，**必须唯一**（同目录下不能有两个 `BG.*`）
- 尺寸比例不对会被拉伸：BG 约 3.63:1、Board 约 2.95:1

## 4. 地图对象与命名

插件生成的 GameObject 名固定为 `InvitationMap_<MapAdd名> [LbsMap]`，
本项目的 BGM 补丁就是靠 `[LbsMap]` 标记来识别插件地图的。

## 5. 排查清单

1. **图不生效** → 先看 StageInfo 是不是写了两条 `<MapAdd>`（第 2 节），这是最常见原因
2. 看 `MOD根目录\Assemblies\LbsMap-error.txt`（插件把异常写这里；不存在=没抛异常）
3. `Player.log` 搜 `[MapAdd]` / `LbsMap`
4. 确认资源路径 `Assemblies\LbsMap\<名字>\BG.png` 存在，且与 `<MapAdd>` 完全一致

## 6. 本项目的插件改造（Tools\LbsMapPluginCompat）

改造原因：插件默认读 `<mod>\Data\StageInfo.xml`，而 BaseMod 的标准布局是 `StaticInfo\StageInfo\StageInfo.xml`。

改动只有一个方法 `LbsMapConfig.ResolveStageFile`：优先 `Data\StageInfo.xml`，不存在则回落 `StaticInfo\StageInfo\StageInfo.xml`。
因此本项目**不需要**额外的 `Data\StageInfo.xml`。

反编译重编译时的两个坑（已修）：

1. `((父类)this).虚方法()` 会被 C# 编成 `callvirt` → 无限递归崩游戏，必须写 `base.`
2. Harmony/反射按参数名匹配时以游戏实际参数名为准

## 7. 与 BGM 的配合

- 地图创建时（`BattleSceneRoot.InitInvitationMap` 后缀）把 BGM 写进 `mapBgm`，原版开局就会播
- 一个关卡里若有多张地图，解析时先查"同关卡已发布的 BGM"，保证所有地图用同一首
- 详见 AGENTS.md 第 5 节与 `BgmProvider.cs`

## 8. 亮度/灯光还原的上限

LbsMap 目录模式**只有两张平面贴图**，没有灯光、没有后处理，所以和游戏内实机亮度必然有差距。
两条路线：

| 路线 | 效果 | 代价 |
|---|---|---|
| A. 把亮度烘进贴图（当前做法） | 接近，但光是静态的 | 低，Pillow 调亮度/对比即可 |
| B. 整张预制体 + `.ab`（作者示范） | 最接近，含动态灯光/材质 | 高：Unity6 解包 → Unity2019 重建 → 打 ab（LimbusUnpackingEffects 模块1/2 的活） |

作者示范在 `workshop\1256670\3559072562`（关卡 213510010「黑派经理」），用的是**另一套入口**：

```xml
<KnxMapAdd Prefab="LeNoir_GreatHall_Phase0">lenoir_brandmanager_background</KnxMapAdd>
```

`KnxMapAdd` 由该 MOD 的 `Leonard.dll` 提供，资源是
`Assemblies\AssetBundle\NewTx\lenoir_brandmanager_background.ab`（23MB 完整预制体），所以能还原灯光。
**这不是 LbsMap 的写法**，两套不能混用。

当前我们的程度：BG 用 Pillow 提亮（1.9× + 轻微对比），Board 提亮 1.35×，已明显接近但仍是静态光。
要完全一致就得走 B 路线。

## 9. 自己打包 ab 地图的正确姿势（重要）

插件渲染背景的方式：`LbsMapCamera.LateUpdate` 新建一个相机，**复制游戏战斗相机**（位置/旋转/FOV，透视），
且 `cullingMask` 只渲染 `Background_Back` 这一层（模板里是 layer 13）。地板则走主相机（Default 层）。

所以自建 prefab 必须满足：

1. 背景节点放在 **layer 13**（`Background_Back`），并按模板的坐标摆放
2. 地板节点放在 **layer 0**（Default）
3. 直接**克隆 `LbsMap.ab` 里的 `LbsMap` 预制体**再换贴图，最稳妥——手搭的坐标/缩放一定对不上，
   因为背景是从透视相机看的，尺寸和位置与正交直觉差很远

模板实测坐标（`LbsMap.ab`）：

```
LbsMap
├ BG_Pivot                       layer 13  z = 16
│ └ Background_MasterTransform   layer 13
│   ├ tr_m_back_lv0              layer 13  y = 8.29, scale 3
│   │   └ quad                   layer 13  scale (20, 5.52)   ← BG 图
│   └ back_drak_SharpGroup       layer 13  左右上下的暗角遮罩
└ Board_Pivot                    layer 0
  ├ BattleGround                 layer 0   BoxCollider
  ├ Floor_MasterTransform
  │ └ tr_quad_floor_lv0          layer 0   z = -4, scale (3, 6, 3)
  │     └ quad_floor             layer 0   scale (20, 6.78)   ← 地板图
  └ Floor_FrontDark              layer 0   底部暗角
```

做法：编辑器里 `AssetBundle.LoadFromFile` 读模板 → 实例化 → 换 `quad`/`quad_floor` 材质 →
需要纵深就复制 `tr_m_back_lv0`（z 往前挪 0.6 / 1.2 / 1.8）→ 存成自己的 prefab → 打包。
脚本见 `Assets\Editor\MapBuilder.cs`（工程 `D:\APP\UnityProjects\CFKMapBuild`）。
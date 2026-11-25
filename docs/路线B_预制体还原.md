# 路线 B：预制体级地图还原（LbsMap ab 模式）

目标：不再用两张平面贴图，而是把**完整层级**（多层背景 + 地板 + 发光层）打进 `.ab`，
交给 LbsMap 的 ab 模式加载，得到接近游戏实机的层次与亮度。

> 作者示范（`workshop\1256670\3559072562`，关卡 213510010）用的是另一套 `KnxMapAdd` + 23MB 预制体 ab。
> 我们不走他那套，改走 **LbsMap 的 ab 模式**，最终效果等价，但入口统一在本项目用的插件上。

## 前置条件（当前缺这一项）

- **Unity 2019.3.15f1**（与《废墟图书馆》一致）+ Unity Hub
- 本机目前**没有任何 Unity 编辑器**，安装需要 Unity 账号登录（几个 GB）
- 其余都已就绪（见下）

## 已准备好的素材

| 内容 | 路径 |
|---|---|
| 分层贴图（BG 5 层 + 地板 + 遮罩） | `C:\Users\MECHREVO\LimbusWork\out\filature\*.png` |
| 完整层级规格（44 个节点的位置/缩放/旋转/材质/贴图） | `C:\Users\MECHREVO\LimbusWork\out\filature\filature_spec.json` |
| 源 bundle（边狱 Unity6） | Unity 缓存 `*/07c51ccc91d4dce9e26b46565468ee38/__data` |
| 解包方法 | `docs\Limbus解包记录.md` |

`filature_spec.json` 里每个节点带 `path/position/scale/rotation/components/renderer.materials[].textures[]`，
重建时照着摆即可；关键节点：

```
BG_Pivot/Background_MasterTransform/tr_m_obj01..05/quad     ← 背景层（由近到远 z=0..9）
Board_Pivot/Floor_MasterTransform/tr_quad_floor/quad_floor  ← 地板
```

## 打包与接入步骤

1. **建工程**：Unity 2019.3.15f1，3D 模板（内置渲染管线，不要 URP）
2. **导图**：把分层 PNG 拖进工程，Texture Type 用 `Default`（不要 Sprite），
   Compression 选 `None`，Max Size 至少 2048，避免二次压缩
3. **搭预制体**：按 spec 摆 quad（1 单位 = 100 像素，直接照 `scale` 用），
   材质用内置 `Unlit/Transparent`（发光层可叠加 `Particles/Additive` 近似光晕）
4. **命名要点**：层节点名保持含 `background`/`floor`/`quad` 等关键词，
   LbsMap 的 `LbsMapLayers` 靠名字把节点分类到背景层/地板层
5. **导出**：`AssetBundleBuild` 打包成 `CFK_Full.ab`，内部预制体名 `CFK_Full`
6. **放置**：`MOD根目录\Assemblies\LbsMap\CFK_Full.ab`
7. **登记**：StageInfo 改 ab 模式（**仍然只能写一条 MapAdd**）
   ```xml
   <MapAdd Prefab="CFK_Full">CFK_Full</MapAdd>
   ```
   （包内只有一个 prefab 时可省略 `Prefab` 属性）
8. **测试**：进关看层次；亮度不对就调材质颜色/发光层强度，不用重烘贴图

## 注意

- **Shader 必须用 2019 内置管线**：边狱是 Unity6 + URP，直接搬 shader 会紫/黑
- 没有贴图的节点（`back_endM`、`dark` 遮罩）用纯色材质即可
- LbsMap 检测到 prefab **自带 MapManager** 时会直接用它、不再加自己的背景相机；
  我们从零搭的 prefab 不带 MapManager，插件会自动补 `LbsAssetMapManager` + 背景相机
- 打包后 `.ab` 与游戏同版本（2019.3.15f1），不要用新版 Unity 导出

## 现状

- 阻塞在"没有 Unity 2019 编辑器"
- 在这之前游戏里用的是**烘焙亮度版**（`BG` ×1.9、`Board` ×1.35），见 `docs\LbsMap插件笔记.md` 第 8 节

## 关键发现（2026-11-25）：作者是怎么成的

作者的 ab（`lenoir_brandmanager_background.ab`，23MB）里带了 **8 个自己移植的 shader**：

```
LeNoirMap/Source0/Unity2019
LeNoirMap/Source1/Unity2019
...
LeNoirMap/InactiveGroundMaterial/Unity2019
```

而 `LbsMap.ab` 模板的 BG/地板材质用的**也是** `LeNoirMap/Source0/Unity2019`
（所以目录模式能正常显示——真正干活的是模板里那份移植好的 shader）。

### 这解释了我们的失败

1. 自己写 shader（`CFK/UnlitAlpha`）→ 能进包，但**模板的层级/材质用法不兼容**，实机要么洋红要么不显示
2. 克隆模板材质 → 材质引用的 shader 只在 `LbsMap.ab` 里，工程里没有这个 shader 资源，
   材质存不成 `.asset`，打包时贴图全丢（包只剩 13 KB）

### 要真正走通 ab 路线，前置是

**移植巴士的 shader 到 Unity2019**（工作流模块2：反编译并还原着色器 → `-Targets Unity2019`）。
拿到 shader 源码放进工程后，才能像作者那样：模板材质 + 自己的 shader + 多层 + 粒子。

在此之前，目录模式（BG.png/Board.png）是最稳的：亮度、辉光、粉尘都可以烘进图里。
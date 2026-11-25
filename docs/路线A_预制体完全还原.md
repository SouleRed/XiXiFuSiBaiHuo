# 路线 A：预制体级完全还原（LbsMap ab）

目标：把边狱巴士的战斗地图**整张预制体**重建到 Unity 2019，产出 `.ab`，
交给 LbsMap 的 ab 模式加载 —— 得到真 3D 视差、动态粒子、光照，和实机一致。

## 结论先行（2026-11-25 定的路线）

阿末（示例作者）的 `lenoir_brandmanager_background.ab` 里有 8 个 `LeNoirMap/Source0..7/Unity2019` shader，
而**巴士的 bundle 里没有 shader 源码**（`Shader.m_Script` 全是 0 长度）。
所以 `Source0..7` 是**用工作流模块2 把编译后的 shader 反编译成 HLSL、再重建到 Unity2019** 得到的。

我们要走的就是这条路。工作量主要在 shader，几何/粒子/灯光参数都已经拿到。

## 已确认的事实

### 地图用到的 shader（Filature 预设，CAB `s8_a1c10p1_battlemap_...`）

| shader | 用途 |
|---|---|
| `Fx_Team_BG/Fx_Grp_BG_Mesh_Simple_Shader` | 绝大多数层：`m_obj01~05`、`m_floor`、`m_map_dark`、`m_map_graduationTip` |
| `Fx_Team_BG/Fx_Grp_BG_Mesh_Shader` | `m_back_omentum`、`m_floor_curtain02` |
| `Fx_Team_Cp10/FX_RPG_Cp10_Light1_Blend` | 光斑 `FX_M_RPG_Cp10_BG_Light1`（加法） |
| `Fx_Team/Fx_Grp_Compact_Shader` | 粒子（粉尘） |

### 已备好的输入

| 内容 | 位置 |
|---|---|
| 44 个节点位置/缩放/旋转/材质引用 | `C:\Users\MECHREVO\LimbusWork\unity_payload\manifest.json` |
| 各层贴图（按 PathID 导出） | `C:\Users\MECHREVO\LimbusWork\out\filature\*.png` |
| 粒子贴图 + 参数（生命 5s、发射 5/s、尺寸 0.5、颜色白→透明） | `Fx_T_Shape_Dot_01_hw` 256×256 |
| 灯光参数 | `Light` 组件的 `m_Color`/`m_Intensity`/`m_Range` 可从 bundle 读 |
| 源 bundle | Unity 缓存 `*/07c51ccc91d4dce9e26b46565468ee38/__data` |
| Unity 2019.3.15f1 编辑器 | `D:\APP\Unity\2019.3.15f1\Editor\Unity.exe` |
| 工程 | `D:\APP\UnityProjects\CFKMapBuild` |

### LbsMap 对 prefab 的要求（踩过的坑）

1. 背景节点在 **layer 13（`Background_Back`）**，地板在 **layer 0**
2. 参考模板坐标：`BG_Pivot` z=16、背景层 y=8.29 / scale 3、地板 `tr_quad_floor_lv0` z=-4 / **X 轴 90° 旋转** / scale (3,6,3)
3. 材质必须落成 `.asset`，否则打包时贴图全丢（包只剩 13KB）
4. 同名材质要被多个节点共用时必须**复用同一个资源**，不能重建（否则引用被删 → 洋红）
5. 内置 shader 不可靠；shader 必须作为工程资源进包

## 执行步骤

### 1. 装模块2 工具链

```powershell
cd "C:\Users\MECHREVO\OneDrive\dll文件\LimbusUnpackingEffects\2.反编译并还原着色器"
.\Run.ps1 -Setup
```

会建 `.runtime`、装 Python 依赖、从 GitHub 下 HLSLDecompiler release 并校验哈希。

### 2. 盘点 + 反编译

```powershell
.\Run.ps1 -Config <target.json> -RunDirectory E:\LimbusWork\map\shader -Stage inspect
.\Run.ps1 -Config <target.json> -RunDirectory E:\LimbusWork\map\shader -Stage all
```

`target.json` 里固定 CAB 与 PathID：上面 4 个 shader。

### 3. 生成 Unity2019 外壳

```powershell
.\Run.ps1 -Config <target.json> -RunDirectory E:\LimbusWork\map\shader -Stage scaffold -Targets Unity2019
```

### 4. 构建 Unity2019 资产

```powershell
.\Run-Assets.ps1 -Route Unity2019 -UnityEditor "D:\APP\Unity\2019.3.15f1\Editor\Unity.exe" -Project <工程> -Job <job> -Log <log>
```

### 5. 重建 prefab 并打包

- 用 `manifest.json` 的 44 个节点建层级（网格 = 内置 Quad）
- 材质用第 3~4 步产出的 shader
- 加粒子（参数见上）与灯光
- `MapBuilder` 打 `CFK_Full.ab`

### 6. 接入

```xml
<MapAdd Prefab="CFK_Full">CFK_Full</MapAdd>
```

## 与已验证的退路（保留着）

- **目录模式**：`BG.png` 2000×552 + `Board.png` 2000×678，静态但稳
- **改模板 ab**：复制 `LbsMap.ab` → 用 UnityPy 替换两张贴图（`92162c3b…` BG / `e92dfef7…` 地板）→
  包内预制体仍叫 `LbsMap`，StageInfo 写 `<MapAdd Prefab="LbsMap">CFK_Full</MapAdd>`。
  这套能保留模板全部渲染特性，但无法加层/粒子。

## 进度

- [ ] 模块2 Setup
- [ ] 4 个 shader 反编译
- [ ] Unity2019 外壳
- [ ] prefab 重建
- [ ] 粒子/灯光
- [ ] 打包与实机验证

## 模块2 实测结果（2026-11-25 01:26）

工具链装好了：`.runtime`（Python 3.12 + UnityPy 1.25.3）+ `tools\HLSLDecompiler\cmd_Decompiler.exe`
（sha256 `E63026E9…5A9C`，与脚本内期望值一致）。

配置要点（踩过的坑）：
- `shader_path_id` 必须是**十进制字符串**，不能写成 JSON 数字
- `unity_version` 是**源包**的版本 → 填 `6000.3.12f1`；目标版本由 `-Targets Unity2019` 决定
- `decompiler` 指向 `cmd_Decompiler.exe`（release zip 里解出来是 `HLSLDecompiler\cmd_Decompiler.exe`，要多拷一层）

`Fx_Team_BG/Fx_Grp_BG_Mesh_Simple_Shader`（PathID `2670115154157785094`）反编译成功，产出在
`C:\Users\MECHREVO\LimbusWork\map\shader\run_bg_simple\programs\`（vertex/fragment 的 hlsl、asm、ir、reflection）。

### 结论：这些 shader 比想象中简单

**Fragment 实际逻辑**（`fragment.named.hlsl`）：

```hlsl
float4 c = t0.Sample(s0_s, uv);          // 主贴图
float3 k = sRGB(_Main_Color.xyz);        // 常量色做了 linear→sRGB
o0.xyz = c.xyz * k;
o0.w = 1;                                // 输出不透明
```

**Vertex**：标准 `world → viewProj` 变换（cb1 = 物体矩阵，cb0[79..82] = UNITY_MATRIX_VP）。

也就是说：`Fx_Grp_BG_Mesh_Simple_Shader` = **贴图 × 常量色（带 sRGB 校正）+ 不透明输出**，
混合方式由材质 Pass 的 Blend 状态决定（alpha / additive）。

这解释了为什么我们自己的 `CFK/UnlitAlpha` / `CFK/Additive` 能直接渲染出层次——
两者数学上几乎等价。剩下要补的只有 sRGB 校正与“不透明层”这两种模式。

### 因此路线 A 的落点

不需要走完 `scaffold`/`Run-Assets` 的长流程，直接用等价 shader：

| 用途 | shader | 关键点 |
|---|---|---|
| 墙面/道具层 | `CFK/MapLayer` | `tex * srgb(_Color)`，`o.w = 1`，按材质 Blend 决定透明 |
| 光斑 | `CFK/Additive` | `Blend One One`，已有 |
| 粒子 | `CFK/Additive`（顶点色） | 已有 |

然后按 `manifest.json` 的 44 个节点重建 prefab（真实变换），加粒子与光斑。
## ★ 关键结构规律（2026-11-25 验证通过，务必照做）

阿末的 ab 里，**背景图、地板、道具、粒子分属三套层级**，各自由不同相机渲染，这是"3D 层正确"的根本：

```
Root
├ BG_Pivot (layer 13 = Background_Back)      z = 16
│   └ Background_MasterTransform (layer 13)
│       └ tr_m_back_lv0..3 (layer 13)  y=8.29  scale=3  → quad 20×5.52
└ Board_Pivot (layer 0)
    ├ BattleGround                BoxCollider
    ├ Floor_MasterTransform
    │   └ tr_quad_floor_lv0..3    z=-4  scale=(3,6,3)  rot 90°X  → quad_floor 20×6.78
    └ Foreground_MasterTransform  ★ layer 0 ★
        ├ tr_m_floor_arch   z=15.89
        ├ tr_m_floor_obj03  z=15.05
        ├ tr_m_floor_obj02  z=14.30
        ├ tr_tableGroup     z=12.83
        ├ tr_m_floor_pole   z=13.14
        ├ tr_m_floor_obj01  z=12.24
        ├ tr_m_floor_fog    z=12.05
        └ Particle1 / Bubble1     ← 粒子也在这里
```

### 三条硬规律

1. **背景图只放 layer 13**（`Background_Back`），由插件新建的背景相机渲染
2. **地板放 layer 0**，`z=-4`、`scale=(3,6,3)`、**X 轴 90° 旋转**
3. **所有道具（obj01/02/03、拱门、柱子、雾）和粒子放 `Foreground_MasterTransform`，layer 0**，z 在 12~16 之间、scale 3

踩过的坑：把道具/粒子放进 layer 13 会出现"被地板压住"、"粒子只在中间一小块"——
因为背景相机是**后画**的独立 pass，和主相机的地板没有正确的深度关系。
放进 Foreground（layer 0）后，主相机按"地板 → 前景道具 → 粒子"的顺序画，一切正常。

### 当前实现（`Assets\Editor\MapBuilder.cs`）

- 背景墙面：`BG_Pivot`(layer 13) + `tr_m_back_lv0`(y=8.29, scale 3, quad 20×5.52)，贴图按 `KWall=1.70` 放大
- 地板：`tr_quad_floor_lv0`(z=-4, rot 90°X, scale 3/6/3)，材质 `renderQueue=2900` 先画
- 前景：`Foreground_MasterTransform`(layer 0) 挂 `obj03`(z=15.05) 与 `obj01`(z=12.24)，scale ×3
- 粒子：`FX_Dust` 挂前景层，`shape.scale=(110,55,8)`，`noise` 噪声场 + 横向风 `vel.x=0.22`

### 粒子规格（参考作者 ab 的 152 个系统）

作者的系统：生命 5~6s、初速 0、尺寸 0.5/0.9/4、速率 1~10/s、速度模块近 0、颜色与尺寸曲线都开。
我们在此基础上加噪声场，让灰尘"随风飘"：`noise.strength=0.35`、`frequency=0.12`、`scrollSpeed=0.08`。
## 当前参数与进度（2026-11-25 02:45）

已跑通：解包 → 重建 prefab → 打包 → 接入 LbsMap → **等价相机预览自检**。

### 参数快照（`Assets\Editor\MapBuilder.cs`）

| 参数 | 值 | 说明 |
|---|---|---|
| `KWall` | 1.95 | 背景墙面放大系数 |
| `BgYOffset` | 6.5 | 墙面上移，消顶部黑边 |
| 墙面 tint | (0.58, 0.66, 0.60) | sRGB 已提亮，tint 压到 1 以下 |
| 地板 tint | (0.26, 0.32, 0.29) | renderQueue 2900 |
| 光晕强度 | 0.24 | 7 个加法面片，11×8，y=9.2 |
| 薄雾 | 0.0 | 先关掉，避免荧光绿 |
| 地面光池 | 0.12 | 18×11，贴地 |
| 中央装置 | scale 2.6，z=12.24，y=5.72 | 约背景高度 40% |
| 织布机 | 左右两半贴图，x=±13.5，z=15.05，22.9×6.55 | 脚踩地板 |
| 粒子 | 42/s，140×70×10 盒，0.2~0.5 尺寸，噪声场 | 见模块8 粒子文档 |
| 黑底 | 120×28，z=26 | 挡层间缝隙 |

### 预览命令

```powershell
& "D:\APP\Unity\2019.3.15f1\Editor\Unity.exe" -batchmode -quit `
  -projectPath "D:\APP\UnityProjects\CFKMapBuild" `
  -executeMethod MapPreview.CaptureFromCLI
```

相机按模板几何反推：`z = 16 - 30 = -14`、`FOV ≈ 58.7°`、`y = 8.29`，取景与游戏一致。

### 还差的（下一轮可直接从参数改）

1. 参考图的窗口是**竖向长条 + 强光晕**，我们的窗带偏"横向分段"
2. 整体仍略偏亮，可继续降 `tint` 到 0.5 左右
3. 地板可以再加一层绿色光池，位置对准中央装置

### 备用产物

`C:\Users\MECHREVO\LimbusWork\out\final\`
- `BG.png` / `Board.png`：烘焙版平面贴图（目录模式可用）
- `CFK_Full.ab`：模板 ab 换贴图版（保留模板全部渲染特性，无 3D 层）
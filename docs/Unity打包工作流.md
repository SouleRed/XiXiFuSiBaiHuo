# Unity 2019 打包工作流（给图书馆做 .ab）

本机已装好 Unity，用来把边狱解包出来的层级重建为《废墟图书馆》可用的 `.ab`（Unity 2019.3.15f1）。

## 环境

| 项 | 值 |
|---|---|
| 编辑器 | `D:\APP\Unity\2019.3.15f1\Editor\Unity.exe`（2019.3.15.5898046，与游戏同版） |
| 许可证 | `C:\ProgramData\Unity\Unity_lic.ulf`（已激活；Hub 登录**不够**，2019 需要单独激活） |
| 工程 | `D:\APP\UnityProjects\CFKMapBuild` |
| 构建入口 | `Assets\Editor\MapBuilder.cs` 的 `MapBuilder.BuildFromCLI` |
| 输入 | `Assets\MapData\manifest.json` + `Assets\MapData\tex\<PathID>.png` |
| 输出 | `AssetBundles\cfk_full.ab` → 拷成 `Assemblies\LbsMap\CFK_Full.ab` |

## 一键构建

```powershell
$u="D:\APP\Unity\2019.3.15f1\Editor\Unity.exe"
$proj="D:\APP\UnityProjects\CFKMapBuild"
& $u -batchmode -nographics -quit -projectPath $proj `
      -executeMethod MapBuilder.BuildFromCLI -logFile "$proj\build.log"
```

## 素材从哪来

1. 用 UnityPy 从边狱 bundle 遍历地图预设，导出每层贴图（**按 PathID 命名**，同名贴图很多）
2. 生成 `manifest.json`：每层的 `path / position / scale / rotation / 材质 / 贴图文件`
3. 素材准备脚本：`C:\Users\MECHREVO\LimbusWork\prepare_unity_assets.py`

## 踩过的坑

1. **材质必须存成 `.asset`**：`new Material(...)` 只存在于内存，打包时材质和贴图都会被丢掉（包会只有几 KB、没有 Texture2D）。正确做法：`AssetDatabase.CreateAsset(mat, path)` → 再 `LoadAssetAtPath` → 改字段后 `EditorUtility.SetDirty` → `AssetDatabase.SaveAssets()` **在打包之前**
2. **AssetBundle 名字会被 Unity 强制小写**：`CFK_Full.ab` 实际产物是 `cfk_full.ab`。Windows 文件系统不区分大小写，LbsMap 按 `CFK_Full.ab` 查找仍能找到
3. 贴图导入设置要在打包前设：`Uncompressed`、`maxTextureSize 4096`、`mipmap off`、`alphaIsTransparency`
4. Shader 用 2019 内置管线（`Unlit/Transparent`）；边狱的 URP shader 一律不能用
5. 批处理要 `-nographics`，否则可能因为没有显示设备失败
6. 首次构建 Unity 会自动 import 一堆 Package，耗时 1–3 分钟属正常
7. **内置 shader 不可靠**：`Shader.Find("Unlit/Transparent")` 打出来的包里 shader 是空的（`m_ParsedForm.m_Name` 为空），进游戏全是洋红。
   正确做法：工程里放自己的 `.shader` 资源（`Assets/MapData/CFKUnlitAlpha.shader`，`Shader "CFK/UnlitAlpha"`），材质引用它，包内会带上已编译的 shader
8. 透明层设置：`Blend SrcAlpha OneMinusSrcAlpha`、`ZWrite Off`、`Cull Off`、`Queue=Transparent`；
   各层靠 transform 的 z 值排序（z 越大越远）

## 接入 LbsMap（ab 模式）

```xml
<MapAdd Prefab="CFK_Full">CFK_Full</MapAdd>
```

- 仍然**只能写一条 MapAdd**
- 包内预制体名要与 MapAdd 名一致（或在 `Prefab` 属性里写包内完整名）
- LbsMap 检测到 prefab 没有 MapManager 时会自己补 `LbsAssetMapManager` + 背景相机；
  `LbsMapLayers` 按节点名把层归类（含 `floor/ground/board` → 地板，`bg/back/background` → 背景），所以**节点名不要乱改**

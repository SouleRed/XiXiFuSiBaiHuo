# AGENTS.md — 裁缝之王（XiXiFuSiBaiHuo）工程规范

## 1. 项目标识与依赖

- **目标平台**：《废墟图书馆》(Library of Ruina) MOD，C# 10 / .NET Framework 4.8，目标 Unity **2019.3.15f1**
- **核心标识**（注意区分，切勿混用）：
  - 程序集名 / 工坊部署目录名：`XiXiFuSiBaiHuo`
  - 代码内 `PackageId`（`LorId`、对话、激活列表等）：`CaiFengKing`（定义于 [ModInitializer.cs](Source/ModInitializer.cs)）
  - `StageModInfo.xml` Workshop ID：`CaiFengZhiKing`（游戏内显示名：`裁缝之王`）
- **主要依赖**（见 [XiXiFuSiBaiHuo.csproj](XiXiFuSiBaiHuo.csproj)）：`Assembly-CSharp`、`BaseMod`、`0Harmony`、`10EnumExtender`、`AutoKeywordUtil`、`Y_LimbusBuff`、`Soule.MotionCamera.Runtime`、`Soule.CombatEntry`、`RedDice`（可选联动）

## 2. 构建与部署

```powershell
.\BuildAndDeploy.ps1              # 编译 + 整体同步到创意工坊目录（需先关闭游戏）
.\BuildAndDeploy.ps1 -BuildOnly   # 仅编译并更新仓库 MOD根目录\Assemblies\XiXiFuSiBaiHuo.dll
.\BuildAndDeploy.ps1 -WorkshopRoot "<SteamLibrary>\steamapps\workshop\content\1256670"
```

- 脚本自动探测 Steam 注册表与 `libraryfolders.vdf` 定位 `1256670\XiXiFuSiBaiHuo`，采用临时目录整体原子替换并校验 DLL SHA256（忽略 0 线程僵尸进程），同时自动将游戏根目录 `LibraryOfRuina_Data\Mods` 下残留副本移入回收站以防双份加载。
- **交付约定**：只允许通过 `.\BuildAndDeploy.ps1` 整体同步 `MOD根目录`，**禁止单独拷贝 DLL 到创意工坊或游戏 `Mods` 目录**。游戏运行时仅更新仓库并注明待关闭游戏后执行部署脚本。

## 3. 目录与核心子系统

- `Source\`：C# 源码；`MOD根目录\`：最终分发目录；`必要程序集\`：编译期引用；`Tools\`：本地工具与反编译目录（git 忽略）；`docs\`：专题文档与样例归档。
- `MOD根目录\Assemblies\`：主 DLL + `AB\`（原生皮肤与地图 AssetBundle）+ `BGM\`（39 个 16-bit PCM `.wav` 角色语音/音效，由 `CaiFengKing_Initializer.AddAudioClips` 直接解码载入 `AudioClips`）。
- `MOD根目录\StaticInfo\StageInfo\StageInfo.xml`：关卡配置（不再使用 `Data\StageInfo.xml`）。
- **原生地图加载**（[NativeStageMap.cs](Source/Shared/Stage/NativeStageMap.cs)）：
  - 不依赖第三方地图插件；地图在 Unity 2019.3.15f1 制作并直接挂载原生 `MapManager`（导出 LZ4 AB 时将编辑器别名程序集回写为 `Assembly-CSharp.dll`），交付为 `MOD根目录\Assemblies\AB\filature_map.ab`。
  - 关卡配置使用标准 `<MapInfo>Filature_Map</MapInfo>`；MOD 初始化预载 AB，单点 Hook `Util.LoadPrefab` 命中 `"InvitationMaps/InvitationMap_Filature_Map"` 时实例化，后续初始化、镜头、切图与销毁均由原版 `BattleSceneRoot` 接管。
- **BGM 双轨控制**：
  - **地图内置 BGM**：序列化在 `Filature_Map.prefab` 的 `MapManager.mapBgm[0..2]` 中，随 `filature_map.ab` 分发，切图时由原版自动调用 `SetEnemyTheme`。
  - **固定敌方主题端口**：在 `OnWaveStart` / `OnRoundStart` 调用 `NativeStageMap.LockEnemyThemePort()`，防止情感等级波动时切回楼层默认 BGM。
  - **角色独立切曲**：`DiceCardSelfAbility_CX_CFK_BGM`（个人 EGO 书页 ID `19` 及玩家方二阶段 `ZhuanJie()`）复用 `NativeStageMap.TryGetDefaultBgm()`，可在任意地图独立覆盖 BGM 且不篡改原地图 `mapBgm`。
- **路径注意**：`CaiFengKing_Initializer.path` 已指向 `...\Assemblies` 目录，拼接子路径时不要重复附加 `"Assemblies"`。

## 4. 编码与工程约束

1. **Harmony 补丁**：参数名必须与目标方法签名完全一致（否则静默失效）；补丁方法体必须有 `try/catch` 防护，避免异常打断原版战斗流程。
2. **继承调用**：禁止将反编译风格的 `((父类)this).虚方法()` 直接落盘（会编译为 `callvirt` 引发无限递归），必须使用 `base.`。
3. **`.csproj` 文件列表**：项目为非 SDK 风格工程，使用 `Source\**\*.cs` 通配符收录源码；如为兼容 VS IntelliSense 显式添加 `<Compile Include>`，必须同步将其加入通配符的 `Exclude` 属性，防止重复编译（CS2002）。
4. **编码与游戏文本标点**：源码与 XML 统一使用 UTF-8 编码；**游戏内本地化与风味文本 XML** 禁止使用全角中文标点（`，。！？：；、""`），一律转为半角 `, . ! ? : ; ... "`。
5. **删除策略**：清理仓库外或游戏目录内的临时文件、备份或废弃副本时，统一移入 Windows 回收站，不做永久删除。

## 5. 书页风味文本与预览（LS_PreviewUtil）

- 插件位于 `MOD根目录\Assemblies\1FrameworkAssemblies\LS_PreviewUtil.dll`；配置在 `MOD根目录\CardPreviewData\*.xml`，文本定义在 `MOD根目录\Localize\cn\etc\FlavorTexts.xml`（前缀 `ui_cfk_`）。
- 完整格式规范与样例见 [docs/CardPreview.md](docs/CardPreview.md)、[docs/LS_PreviewUtil使用说明.md](docs/LS_PreviewUtil使用说明.md) 及 `docs/CardPreview.*.xml`。
- **快速录入约定**（当需求给出 `idN，文本` 或 `idN，金色色号，文本` 时）：
  - 文本 ID 固定为 `ui_cfk_flavor_<书页ID>`；`<Flavor>` 节点值写文本 ID，不写正文；
  - 默认色号 `#D2A679`，指定“金色色号”时用 `#FFD700`；其余属性（`B/I/U/S`）保持不变；
  - 文本正文按第 4 节规则将全角中文标点转为半角；修改后需完全重启游戏生效。

## 6. 验证与排错

- **编译验证**：`.\BuildAndDeploy.ps1 -BuildOnly`。
- **静态检查**：修改 Harmony 补丁或程序集引用后，可用工坊内的 `Mono.Cecil.dll`（按本机 Steam 库定位 `workshop\content\1256670\2745822929\Assemblies\Mono.Cecil.dll`）检查编译后 DLL 的引用、补丁参数名、异常处理器及是否存在自递归。
- **运行日志**：`%LOCALAPPDATA%Low\Project Moon\LibraryOfRuina\Player.log`，按关键字检索（勿全文读取）：
  - `[裁缝之王]`、`[裁缝之王][NativeMap]`、`[裁缝之王][BGM]`、`[裁缝之王][Harmony]`、`[裁缝之王][联动]`、`LS_PreviewUtil`
- **原生崩溃**：Unity 原生崩溃会导致 `Player.log` 截断，需结合 Windows「事件查看器 → 应用程序」中的出错模块排查。

# AGENTS.md — 裁缝之王工程速查

> 给 AI 助手 / 新协作者的固定上下文。**先读这份，再动代码**；不要靠翻历史对话还原现状。

## 1. 项目概览

- 《废墟图书馆》(Library of Ruina) MOD，C# / .NET Framework 4.8，目标 Unity **2019.3.15f1**
- 程序集名 `XiXiFuSiBaiHuo`，包 ID `CaiFengZhiKing`，游戏内 mod 文件夹名 `裁缝之王`
- 依赖：BaseMod、0Harmony、RedDice（可选联动）、1Framework / Soule 系列运行时

## 2. 构建与部署

```powershell
.\BuildAndDeploy.ps1              # 编译 + 完整同步到创意工坊目录（部署前需关闭游戏）
.\BuildAndDeploy.ps1 -BuildOnly   # 只编译并更新仓库 MOD根目录\Assemblies\XiXiFuSiBaiHuo.dll
.\BuildAndDeploy.ps1 -WorkshopRoot "D:\path\to\steamapps\workshop\content\1256670"
```

- 目标布局：统一部署到自动探测的 Steam 创意工坊目录 `<SteamLibrary>\steamapps\workshop\content\1256670\XiXiFuSiBaiHuo`
- 脚本会自动探测 Steam 注册表与 `libraryfolders.vdf` 定位 `1256670` 工坊目录，采用临时目录整体原子替换并校验 DLL SHA256，同时忽略 0 线程僵尸进程
- 部署完成后会自动检查并清理游戏根目录 `LibraryOfRuina_Data\Mods` 下残余的本 MOD 副本（移入回收站），防止双份加载报错
- 产物：`bin\Debug\XiXiFuSiBaiHuo.dll`，同时拷贝到 `MOD根目录\Assemblies\XiXiFuSiBaiHuo.dll`
- **交付铁律**：要么完整运行 `.\BuildAndDeploy.ps1`（连同 `MOD根目录` 下全部 DLL、AB 资产、XML 等整体同步），要么等用户自行执行；**不要只单独拷贝 DLL 到创意工坊或游戏 `Mods` 目录**
  - 游戏在运行时部署会被脚本拦住，此时先只改仓库，回复里注明"待游戏关闭后执行 `.\BuildAndDeploy.ps1`"

## 3. 目录要点

- `Source\` 源码；`MOD根目录\` 是最终部署内容
- `MOD根目录\Assemblies\`：dll + `AB\`（皮肤与原生地图资源）+ `BGM\`（角色语音等音频）
- `MOD根目录\StaticInfo\StageInfo\StageInfo.xml`：关卡定义（**不再用** `Data\StageInfo.xml`）
- `Tools\`：反编译工程与备份，git 未跟踪，`bin/obj` 已被忽略

## 4. 原生地图加载（NativeStageMap）

- **不依赖第三方地图插件**（已移除 `LbsMapPlugin.dll` 与 `Assemblies\LbsMap\`）。
- 地图资产在 Unity 2019.3.15f1（`d:\TOOL\Unity Projects\LOB特效`）中制作并直接挂载原生 `MapManager`（导出 LZ4 AB 时将 `LoRGame-CSharpX.dll` 脚本身份回写为 `Assembly-CSharp.dll`），部署在 `MOD根目录\Assemblies\AB\filature_map.ab`。
- 关卡配置直接走原版标准 `<MapInfo>Filature_Map</MapInfo>`（`StageInfo.xml`）。
- 注册与加载入口：[Source\Shared\Stage\NativeStageMap.cs](Source/Shared/Stage/NativeStageMap.cs)：
  - MOD 初始化时通过 `NativeStageMap.LoadMaps(path)` 预载 `AB\filature_map.ab`；
  - 仅单点 Hook `Util.LoadPrefab(string path, Transform parent)`，命中 `"InvitationMaps/InvitationMap_Filature_Map"` 时实例化原生预制件，后续初始化、镜头、切图与销毁全部由原版 `BattleSceneRoot` 接管。

## 5. BGM 控制（原生 MapManager.mapBgm + 独立角色切曲）

- **接待地图 BGM**：直接序列化在 `Filature_Map.prefab` 的 `MapManager.mapBgm[0..2]` 中（Vorbis 压缩 + `normalize: 1` 标准化响度），随 `filature_map.ab` 一并分发，切图时由原版 `BattleSceneRoot.ChangeToSpecialMap` 自动调用 `SetEnemyTheme(currentMapObject.mapBgm)`。
- **固定 BGM 判定端口**：参照原版 Boss 战（哭泣之子 / 残响乐团 / 殷红迷雾），在 `OnWaveStart` 与 `OnRoundStart` 调用 `NativeStageMap.LockEnemyThemePort()`（设置 `waveModel.team.emotionTotalBonus = floorModel.team.emotionTotalCoinNumber + 1`），防止敌我情感等级波动时切回楼层默认 BGM。
- **裁缝之王独立换曲功能**：`DiceCardSelfAbility_CX_CFK_BGM`（个人 EGO 书页 ID `19` 及玩家方二阶段 `ZhuanJie()`）直接复用 `NativeStageMap.TryGetDefaultBgm()`，按原版殷红迷雾模式调用 `LockEnemyThemePort()` + `SetEnemyTheme` + `ChangeEnemyTheme(0)`，可在任意其他地图中独立覆盖 BGM 且不篡改原地图 `mapBgm`。
- ⚠️ `CaiFengKing_Initializer.path` **已经是 `...\Assemblies` 目录**，不要再拼一层 `"Assemblies"`。

## 6. 已知坑（都实际踩过）

1. **Harmony 参数名必须与目标方法参数名一致**（例如 `ChangeEnemyTheme(int idx)`），否则注册失败，游戏不崩但补丁静默失效
2. **补丁方法体必须 try/catch**：异常会插进原版流程，可能直接崩游戏
3. **反编译代码里 `((父类)this).虚方法()` 会编成 `callvirt`** → 无限递归崩游戏；必须改写成 `base.`
4. csproj 用 `Source\**\*.cs` 通配符，不要再对同一文件加显式 `<Compile Include>`（会 CS2002）
5. 源码一律 UTF-8；不要用 shell 命令重写含中文的源文件（会乱码）
6. Unity 原生崩溃时 Player.log 会丢尾巴；崩溃要看「事件查看器 → 应用程序」的 `出错模块`
7. 新建 `.cs` 文件时，除了通配符还要在 csproj 里**显式加 `<Compile Include>`**（并从通配符 `Exclude`），否则 VS 的 IntelliSense 认不到，会一直报 CS0103（命令行编译却是通过的）
8. **文本里不能用中文标点**（，。！？：；、"")，只能用半角 `, . ! ? : ;`——本地化与风味文本都适用；写文案后要扫一遍全角标点

## 7. 验证手段

- **地图预览（不必进游戏）**：`Unity.exe -batchmode -quit -projectPath D:\APP\UnityProjects\CFKMapBuild -executeMethod MapPreview.CaptureFromCLI`，
  输出 `Preview\preview.png`；相机按 LbsMap 的双相机模型还原（主相机正交俯视 orthoSize≈15 / pitch≈19°，
  背景相机 fov=2·atan(orthoSize/25)≈62°），取景与游戏一致
- 编译：`.\BuildAndDeploy.ps1 -BuildOnly`
- 静态检查：用 Cecil（`D:\APP\Steam\steamapps\workshop\content\1256670\2745822929\Assemblies\Mono.Cecil.dll`）读部署后的 dll，核对补丁签名、参数名、异常处理器、自递归
- 运行日志：`%LOCALAPPDATA%Low\Project Moon\LibraryOfRuina\Player.log`
  - 关键标签：`[裁缝之王]`、`[裁缝之王][BGM]`、`[MapAdd]`
  - **只 grep 关键字 + 行号，不要全文读**
- 仓库/游戏目录一致性：比对两个目录下同名文件的 MD5

## 8. 给 AI 的工作方式（省 token）

- 开工先读本文件 + `git status`，不要重读历史对话
- 诊断先 `grep` 日志关键字定位行号，再 `read` 带 offset/limit 读附近片段
- 不要为了"确认"去全量扫二进制；需要时用 Cecil 定向查类型/方法
- 改完必须给出：改了哪些文件、编译结果、静态检查结论、回滚方式
- 交付前不要跑无关的重扫；一次脚本做完多项检查
- **随手记录**：每解决一个问题/踩到一个坑/摸索出一套流程，立刻落到文档里，别等下次重查——
  - 规则、坑、约定、当前状态 → 本文件（AGENTS.md）
  - 某个主题的完整操作流程 → `docs\<主题>.md`（本文件只留一行指针）
  - 外部给的说明/样例 → 复制进 `docs\` 归档（Downloads 里的东西会丢）
  - 回复里说一句"已记到哪"，让用户知道下次能直接复用

## 9. 当前状态

- 地图：主关（Stage id=1）已切换为原生地图 `<MapInfo>Filature_Map</MapInfo>`（`Assemblies\AB\filature_map.ab`，内置 BGM），旧版 LbsMap 插件与测试关（id=2）已清理
- 未提交改动：见 `git status`

## 10. 书页风味文本 / 预览（LS_PreviewUtil）

插件：`MOD根目录\Assemblies\1FrameworkAssemblies\LS_PreviewUtil.dll`（已装）。

- 数据文件：`MOD根目录\CardPreviewData\*.xml`（任意文件名、可子文件夹递归；已有 `CardPreviewData.xml`，内含书页 3-10 的风味文本）
  - 根节点 `CardPreviewXmlRoot`，每张书页一个 `<Card ID="书页ID">`，至少要有一个 `Preview` 或 `Flavor`
  - `<Preview>书页ID</Preview>` 多个 = 多页（左右方向键翻页）；`Pid` **不继承** Card 的 `Pid`——原版必须显式 `Pid="@origin"`
  - `<Flavor B/I/U/S/Color>文本ID</Flavor>` 一行一条；**值必须是文本 ID，不是正文**
  - `Battle` 默认 true；`false` 只关闭"战斗中查看手牌"的预览
- 文本 ID 定义在 `MOD根目录\Localize\cn\etc\*.xml`（BaseMod 读取 `localize/text` 写进 `TextDataModel.textDic`；插件本身不加载文本），统一前缀 `ui_cfk_`，目前放在 `FlavorTexts.xml`
- 改完 CardPreviewData 或文本必须**完全重启游戏**才生效；排错搜 Player.log 的 `LS_PreviewUtil`、`Error while load preview datas`
- 官方说明归档：`docs\LS_PreviewUtil使用说明.md`；细节：`docs\CardPreview.md`；作者样例：`docs\CardPreview.example.xml`、`docs\CardPreview.sample.xml`
- 录入约定（用户会这样提需求）：`idN，文本` 或 `idN，金色色号，文本`
  - 文本 ID 固定为 `ui_cfk_flavor_<书页ID>`
  - 默认颜色 `#D2A679`（浅棕）；写了"金色色号"的用 `#FFD700`
  - 原样其余属性（B/I/U/S）不动
  - 中文标点一律转半角：`，→,` `。→.` `！→!` `？→?` `：→:` `；→;` `、→,` `。。。→...` `""→"`
- 详细说明与作者示例：`docs\CardPreview.md`、`docs\CardPreview.example.xml`、`docs\CardPreview.sample.xml`

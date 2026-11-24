# AGENTS.md — 裁缝之王工程速查

> 给 AI 助手 / 新协作者的固定上下文。**先读这份，再动代码**；不要靠翻历史对话还原现状。

## 1. 项目概览

- 《废墟图书馆》(Library of Ruina) MOD，C# / .NET Framework 4.8，目标 Unity **2019.3.15f1**
- 程序集名 `XiXiFuSiBaiHuo`，包 ID `CaiFengZhiKing`，游戏内 mod 文件夹名 `裁缝之王`
- 依赖：BaseMod、0Harmony、RedDice（可选联动）、1Framework / Soule 系列运行时

## 2. 构建与部署

```powershell
.\Deploy-Local.ps1              # 编译 + 同步到游戏目录（部署前需关闭游戏）
.\Deploy-Local.ps1 -BuildOnly   # 只编译
```

- 游戏目录：`D:\APP\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\裁缝之王`
- 部署脚本自动探测 Steam 库；`Tools\LbsMapPluginCompat\LbsMapPlugin.csproj` 里的 `GameManaged` 是硬编码路径
- 产物：`bin\Debug\XiXiFuSiBaiHuo.dll`，同时拷贝到 `MOD根目录\Assemblies\`
- 协作者用的 `BuildAndDeploy.ps1` 指向 `E:\GAME`（本机不存在，部署到创意工坊布局），**本机不要用**
- **交付铁律：只要改了 `MOD根目录` 下的任何东西（XML / 图片 / 音频 / dll），都必须同步到游戏目录**，并在回复里明确说"已同步"。用户不希望自己手动复制。
  - 改代码或引用变更 → `.\Deploy-Local.ps1`（会重编 + 全量同步）
  - 只改 XML/资源 → 直接 `Copy-Item` 到游戏目录即可（不必重编）
  - 游戏在运行时部署会被脚本拦住，此时先只改仓库，回复里注明"待游戏关闭后同步"

## 3. 目录要点

- `Source\` 源码；`MOD根目录\` 是最终部署内容
- `MOD根目录\Assemblies\`：dll + `AB\`（皮肤资源）+ `BGM\`（音频）+ `LbsMap\`（地图资源）
- `MOD根目录\StaticInfo\StageInfo\StageInfo.xml`：关卡定义（**不再用** `Data\StageInfo.xml`）
- `Tools\`：反编译工程与备份，git 未跟踪，`bin/obj` 已被忽略

## 4. 地图（LbsMap 插件）

- 一张地图 = `Assemblies\LbsMap\<名字>\{BG.png, Board.png}` + StageInfo 里一行 `<MapAdd>名字</MapAdd>`
- 模板固定为 `Assemblies\LbsMap\LbsMap.ab`，不要改
- 图片比例：`BG` 2000×551、`Board` 2000×678，否则被拉伸
- 插件 DLL 已被我们改造过（改为读 BaseMod 的 `StaticInfo` 布局），源码在 `Tools\LbsMapPluginCompat`
- 插件报错写入 `Assemblies\LbsMap-error.txt`
- 切图时机：原版在首个回合开始（`StageController.CheckMapChange`）才把插件地图设为当前地图，比 BGM 晚一拍；
  `KingPassives.OnWaveStart`（接待站分支）里已手动提前调用 `BattleSceneRoot.ChangeToSpecialMap(mapName, false, false)`，让地图与 BGM 同时出现；后续回合开始的检查会因"已是当前地图"直接跳过

## 5. BGM 解析（BgmProvider）

优先级：

1. `Assemblies\LbsMap\<地图名>\BGM.wav`（或 `.mp3`）——复制地图文件夹即连音乐一起走
2. `Assemblies\BGM\CFK_<关卡id>.wav`
3. `Assemblies\BGM\BGM1.wav`（兜底）

- ⚠️ `CaiFengKing_Initializer.path` **已经是 `...\Assemblies` 目录**，不要再拼一层 `"Assemblies"`
- ⚠️ 一个关卡里会有**两张地图**（`LbsMap` 模板 + 用户地图），它们都会走 BGM 解析：
  必须让同关卡所有地图统一到同一首（解析时先查已发布到关卡数据的 BGM），否则模板地图会用兜底曲把音乐"盖掉"
- ⚠️ 不要在补丁里让原版 `ChangeEnemyTheme` 继续执行：它会切到敌方音轨并让曲子从头开始；现在实现为"只对齐数据 + 跳过原方法"
- 播放走友方音轨（`ChangeAllyTheme`）；走敌人音轨会出现"秒开秒停"的爆音
- 素材响度偏低时调 `BgmProvider.BgmGain`（默认 1.3，最终 clamp 到 1.0）；只作用于本 MOD 曲目
- 关键文件：`Source\Shared\Tool\BgmProvider.cs`、`Source\Shared\Stage\LbsMapBgmPatch.cs`

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

- 编译：`.\Deploy-Local.ps1 -BuildOnly`
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

## 9. 当前状态

- 地图：`CFK`（主关）、`CFK_Test`（测试关，验证"地图自带 BGM"）；测试关 id=2，测完可整段删除
- 未提交改动：见 `git status`；`Tools\` 与 `Assemblies\LbsMap\` 仍是未跟踪状态

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

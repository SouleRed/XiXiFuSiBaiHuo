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

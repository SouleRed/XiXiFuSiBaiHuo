# 仓库工作约定

## 标识与布局

- **目标**：《废墟图书馆》MOD（C# 10 / .NET Framework 4.8 / Unity 2019.3.15f1）。
- **区分三套标识**：程序集与工坊目录为 `XiXiFuSiBaiHuo`；代码内 `PackageId`（`LorId`、对话、激活列表）为 `CaiFengKing`；`StageModInfo.xml` Workshop ID 为 `CaiFengZhiKing`。
- **目录职责**：`Source\` 为源码；`MOD根目录\` 为最终分发目录；关卡配置使用 `MOD根目录\StaticInfo\StageInfo\StageInfo.xml`（非 `Data\`）；`CaiFengKing_Initializer.path` 已指向 `...\Assemblies`，拼接子路径时不要重复附加 `Assemblies`。

## 构建、部署与仓库边界

- **构建**：`.\BuildAndDeploy.ps1 -BuildOnly`（编译并更新 `MOD根目录\Assemblies\XiXiFuSiBaiHuo.dll`）。
- **部署**：仅在用户明确要求且游戏已关闭时执行 `.\BuildAndDeploy.ps1` 整体同步；禁止单独拷贝 DLL 到创意工坊或游戏 `Mods` 目录，未经请求不自动部署。
- **仓库卫生**：禁止将临时解包、反编译输出、日志或额外备份写入本仓库；清理临时文件或废弃副本时移入 Windows 回收站，不做永久删除。
- **测试边界**：未经用户明确要求，不主动创建测试关卡/测试桩或执行游戏测试。

## 编码与资产约束

- **原生地图与 BGM**：地图通过 `NativeStageMap` 加载原生 `MapManager` AB（`Assemblies\AB\filature_map.ab`）及内置 `mapBgm`，不引入第三方地图插件。
  流程、验证日志与排错见 [docs/原生地图与BGM.md](docs/原生地图与BGM.md)；部署目标是创意工坊目录 `workshop\content\1256670\XiXiFuSiBaiHuo`，脚本会清掉游戏根 `Mods` 下的同名副本。
- **Harmony 补丁**：参数名必须与目标方法签名完全一致；常规补丁通过前置条件与空值守卫保证安全，**禁止无脑 `try/catch` 吞异常**；仅在外部资产/联动边界或具备明确回退路径（如失败回退原版）的拦截点按需捕获并完整记录异常。
- **继承虚调用**：禁止直接落盘反编译风格的 `((父类)this).虚方法()`，必须使用 `base.`。
- **`.csproj` 收录**：使用 `Source\**\*.cs` 通配符；若显式添加 `<Compile Include>`，必须同步加入通配符 `Exclude`，避免 CS2002。
- **接待图标**：以作者示例 mod（3616379979）为准，写在 `Data\StageLine\StoryIconSlot.xml`（`IconSlot`：Name/Stage/Locate/Position），
  图标素材放 `Data\StageLine\ArtWork\`；`Data\XmlInfos\StoryIcon`（MapIconSlot）是章节地图体系，与本章不同。
  字段说明见 [docs/章节地图与接待图标.md](docs/章节地图与接待图标.md)。
- **接待邀请函书籍**：`<Book>` 引用原版书页必须写 `Pid="@origin"`，否则会被当成本模组的书、退化成"棍木"占位；
  引用本模组的书写包名（`Pid="CaiFengKing"`）。详见 [docs/章节地图与接待图标.md](docs/章节地图与接待图标.md)。
- **术语来源**：边狱巴士原生系统（攻击容量、广域乱射等）用边狱官方译名；其余用图书馆官方译名（幕=막、混乱=흐트러짐、强壮=힘）。详见 `LimbusUnpackingEffects\9.边狱巴士与图书馆本地化翻译\` 的术语对照表。
- **多语言**：本地化源在 `Assemblies\Localize\{cn,en,kr}\`；同一份还要镜像到根 `Localize\{cn,en,kr}\`（BaseMod 加载会走本地化字体处理，
  否则部分 UI 的韩文会显示成方块）。改了语言文件后：`robocopy MOD根目录\Assemblies\Localize MOD根目录\Localize /MIR`，然后把镜像目录里的 `.xml` 全部改名成 `.txt`（BaseMod 只解析 `.txt`）。
- **本地化与风味文本**：游戏内 XML 文本禁止全角中文标点（`，。！？：；、""`），统一转为半角；书页风味文本与预览规范及录入约定见 [docs/CardPreview.md](docs/CardPreview.md)。

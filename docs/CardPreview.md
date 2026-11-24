# 书页风味文本 / 预览（LS_PreviewUtil）

官方说明归档：[docs/LS_PreviewUtil使用说明.md](LS_PreviewUtil使用说明.md)
作者示例：`docs/CardPreview.example.xml`（字段说明）、`docs/CardPreview.sample.xml`（Clock_Ren 实例）、`docs/CardPreview.sample.etc.xml`（文本 ID 写法）

插件本体：`MOD根目录\Assemblies\1FrameworkAssemblies\LS_PreviewUtil.dll`（已装，不能再套子文件夹）

## 1. 数据文件位置

```
MOD根目录\CardPreviewData\*.xml      ← 任意文件名，可放子文件夹（递归读取）
```

- 与 `StageModInfo.xml` 同级，不要在 `Data\` 或 `StaticInfo\` 下
- 本项目现有：`CardPreviewData\CardPreviewData.xml`

## 2. 结构

```xml
<?xml version="1.0" encoding="utf-8"?>
<CardPreviewXmlRoot xmlns:xsd="http://www.w3.org/2001/XMLSchema"
                    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Version>1.1</Version>                 <!-- 插件不读取，保留即可 -->

  <Card ID="3">                          <!-- 被挂预览的书页 ID（必填，整数） -->
    <Pid>CaiFengZhiKing</Pid>            <!-- 可选；省略=本模组；@origin=原版；其他=对应 Workshop ID -->
    <Battle>false</Battle>               <!-- 可选，默认 true；false 只关闭“战斗中查看手牌”的预览 -->
    <Preview>5</Preview>                 <!-- 一页预览；多个 = 多页，游戏内左右方向键翻页 -->
    <Preview Pid="@origin">25625</Preview>
    <Flavor Color="#D2A679">ui_cfk_flavor_3</Flavor>
  </Card>
</CardPreviewXmlRoot>
```

要点：

- **至少写一个 `<Preview>` 或 `<Flavor>`**，否则该 Card 无效
- `<Preview>` 的 `Pid` **不继承** `Card` 的 `Pid`；给原版书页配原版预览必须显式 `Pid="@origin"`
- `<Preview>` 内容不存在时该页被跳过
- 只有“被查看的书页”需要 Card 数据；被预览的书页本身不需要
- 同一书页写多个 Card 会合并（任一 `Battle=true` 即显示），建议只写一个

## 3. 文本 ID 从哪来

`<Flavor>` 的内容是文本 ID，插件从 **`TextDataModel.textDic`** 读取，**自己不加载文本文件**。

本项目的文本由 BaseMod 的 `Localize\cn\etc\*.xml` 提供（BaseMod 用 `localize/text` 节点写入 textDic）：

```xml
<localize>
  <text id="ui_cfk_flavor_3">缝纫最重要的就是针数啊,针数!</text>
</localize>
```

- 文件名任意，目录必须是 `MOD根目录\Localize\cn\etc\`
- ID 不存在时该行显示为空（不会报错）

## 4. 排错

日志：`%LOCALAPPDATA%Low\Project Moon\LibraryOfRuina\Player.log`

| 搜索关键字 | 含义 |
|---|---|
| `LS_PreviewUtil` | 插件是否初始化 |
| `Error while load preview datas` | 该文件没被读取，查 XML 结构与数字 ID |
| `Fail to load assetbundle dependencies` / `Fail to load UI main assetbundle` | DLL 不完整，重新复制 |

- 有预览窗无卡面：`Preview` ID 无效 / `Pid` 写错（原版必须 `@origin`）/ 被预览模组未启用
- 有卡面无文字：`Flavor` 里写了正文而不是文本 ID / ID 不在 textDic / 语言不匹配
- 战斗中不显示：`Battle=false`，或只在查看手牌详情时显示
- **改了 CardPreviewData 或文本后必须完全重启游戏**（启动时读取一次）

## 5. 本项目约定（给 AI）

- 用户给 `idN，文本` 或 `idN，金色色号，文本` 时：写 `Card ID="N"` + `Flavor`，文本 ID = `ui_cfk_flavor_<N>`
- 默认颜色 `#D2A679`（浅棕），"金色"用 `#FFD700`；其余属性默认不写
- 中文标点必须转半角：`，→,` `。→.` `！→!` `？→?` `：→:` `；→;` `、→,` `。。。→...` `""→"`

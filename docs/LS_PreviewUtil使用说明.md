# LS_PreviewUtil 使用说明

LS_PreviewUtil 用于在查看书页时显示额外的卡面与风味文本。

节点结构以 Example.xml 为准。

## 一、安装

### 目录层级

- 你的模组/
  - StageModInfo.xml
  - Assemblies/
    - 0Harmony.dll
    - 1FrameworkLoader.dll
    - Mono.Cecil.dll
    - Mono.Cecil.Mdb.dll
    - Mono.Cecil.Pdb.dll
    - Mono.Cecil.Rocks.dll
    - MonoMod.RuntimeDetour.dll
    - MonoMod.Utils.dll
    - 1FrameworkAssemblies/
      - LS_PreviewUtil.dll
  - CardPreviewData/
    - 任意 .xml 文件

### 放置规则

- LS_PreviewUtil.dll 直接放在 Assemblies/1FrameworkAssemblies/ 下，不能再套子文件夹。
- 0Harmony.dll、1FrameworkLoader.dll、Mono.Cecil 的四个 DLL、MonoMod 的两个 DLL 放在 Assemblies/ 根目录。
- CardPreviewData 放在模组根目录，与 StageModInfo.xml 同级。
- CardPreviewData 内放 .xml 文件；可放子文件夹，插件会递归读取。

## 二、字段

### CardPreviewXmlRoot

- 根节点，名称固定。
- 子节点为任意数量的 Card。

### Card

- ID：必填属性，整数，表示要挂预览的书页 ID。
- Pid：可选子节点；省略或留空表示文件所在模组；@origin 表示原版；其他值为对应模组的 Workshop ID。Pid 与 ID 共同确定目标书页。
- Battle：可选子节点，布尔值，默认 true；false 只关闭战斗中查看手牌时的预览，不影响卡组、库存、战斗准备界面的详情预览。
- Preview：可选子节点，可多个；每个是一页预览，按出现顺序排列。
- Flavor：可选子节点，可多个；每行一条风味文本，按出现顺序换行。
- 至少写一个 Preview 或 Flavor。
- 只有被查看的书页需要 Card 数据；Preview 指向的书页不需要自己有 Card 数据。
- 同一书页（相同 Pid 和 ID）写多个 Card 时会合并；建议只写一个。

### Preview

- 元素内容是预览书页的数字 ID。
- Pid：可选属性；省略表示文件所在模组；@origin 表示原版；其他值为对应模组的 Workshop ID。
- Preview 的 Pid 不继承 Card 的 Pid；给原版书页配原版预览时必须显式写 Pid="@origin"。
- 多个 Preview 形成分页；游戏内用键盘的左右方向键翻页。
- ID 不存在时该项被跳过。

### Flavor

- 元素内容是文本 ID，不是正文。
- 文本从 TextDataModel.textDic 读取；插件不负责加载文本文件。该表由游戏或你使用的本地化加载方式填充。
- 文本 ID 不存在时该行显示为空。
- 可选属性：B 加粗、I 斜体、U 下划线、S 删除线，取值为 true 或 false；Color 为 #RRGGBB 或 #RRGGBBAA；省略时不启用。

### 其他

- Version：示例中可保留，插件不读取。

## 三、排错

### 日志

- 路径：%USERPROFILE%\AppData\LocalLow\Project Moon\LibraryOfRuina\Player.log
- 搜索 LS_PreviewUtil，确认插件是否初始化。
- 搜索 Error while load preview datas，出现时对应文件不会被读取，检查 XML 结构与数字 ID。
- 搜索 Fail to load assetbundle dependencies 或 Fail to load UI main assetbundle，出现时重新复制完整的 LS_PreviewUtil.dll。

### 没有预览

- LS_PreviewUtil.dll 不在 Assemblies/1FrameworkAssemblies/ 下，或又多套了一层文件夹。
- 缺少基础依赖文件。
- CardPreviewData 不在模组根目录，或文件不是 .xml。
- 被查看书页的 ID 与 Card 的 ID 不一致，或 Card 的 Pid 作用域不对。
- 该 Card 既没有有效 Preview，也没有 Flavor。

### 有预览窗，但没有卡面

- Preview 的内容不是有效书页 ID。
- Preview 的 Pid 写错；原版书页必须写 Pid="@origin"。
- 被预览书页所在模组未启用。

### 有卡面，但没有文字

- Flavor 里写的是正文，不是文本 ID。
- 该文本 ID 不在 TextDataModel.textDic 中。
- 文本没有被你的本地化加载方式写入，或游戏语言与文本所属语言不匹配。

### 战斗中不显示

- 该 Card 写了 Battle=false。
- 同一书页有多条 Card 时，任意一条 Battle 为 true 就会显示；检查重复条目。
- 该预览只在查看手牌详情时显示。

### 修改后不生效

- CardPreviewData 和文本只在启动时读取；修改后必须完全重启。

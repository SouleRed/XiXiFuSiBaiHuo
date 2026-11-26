# 反馈：ChapterMap / MapIconSlot 在“自建接待”模组上的兼容问题

## 环境

- 游戏：《废墟图书馆》1.1（Unity 2019.3.15f1）
- 插件：`LS_InvitationUtil.dll` **2.0.0.0**（2026-11-26 收到的修复版，12,982 KB，放在 `Assemblies\1FrameworkAssemblies\`）
- 模组形态：**BaseMod 布局**，接待/敌方/书页在 `MOD根目录\StaticInfo\...` 注册，不使用插件的 `Data\XmlInfos\StageInfo`
- 插件只用于：本地化（`Assemblies\Localize\cn\...`）、章节地图/接待图标

## 现象

配置 `ChapterMap` + `MapIconSlot` 后：

1. 邀请函界面出现**两个重叠的图标**：一个是接待图标，一个是剧情图标（本模组没有剧情）
2. 点击后**播放原版剧情**，并在剧情结束时抛异常：

```
IndexOutOfRangeException: Index was outside the bounds of the array.
  at UI.UIStoryProgressIconSlot.SetLevelUIColor (UnityEngine.Color c, System.Int32 level)
  at UI.UIStoryProgressIconSlot.<OnClickStoryButton>b__73_0 ()
  at StoryScene.StoryRoot.EndStory (System.Boolean forcely)
  at StoryScene.StoryManager.EndStory (System.Boolean forcely)
  at StoryScene.StoryManager.SkipAll ()
  at UI.UICustomSelectable.OnSubmit (UnityEngine.EventSystems.BaseEventData eventData)
```

3. 章节地图界面打开后**无法切回原版地图**（UI 卡在章节地图状态）

## 最小复现配置

`Data\XmlInfos\ChapterMap\ChapterMap.xml`

```xml
<ChapterMapXmlRoot>
  <Version>1.1</Version>
  <ChapterMap ID="1">
    <Name>CFK_ChapterMap</Name>
    <Chapter>7</Chapter>
    <Condition><Level>0</Level></Condition>
    <Grade Name="0">
      <Icon>CFK_ChapterIcon</Icon>
      <Title>ui_cfk_chapter_0</Title>
      <Condition><Level>0</Level></Condition>
      <Story>CFK_StoryIcon_0</Story>
      <RectSize>0.75</RectSize>
      <Position x="0" y="0" />
    </Grade>
  </ChapterMap>
</ChapterMapXmlRoot>
```

`Data\XmlInfos\StoryIcon\StoryIcon.xml`

```xml
<StoryIconRoot>
  <Version>1.1</Version>
  <MapIconSlot ID="1">
    <Name>CFK_StoryIcon_0</Name>
    <RequireUnlock>true</RequireUnlock>
    <Stage Pid="CaiFengKing">149601</Stage>
    <Position x="0" y="0" />
    <ChapterMap>1</ChapterMap>
    <Grade>0</Grade>
  </MapIconSlot>
</StoryIconRoot>
```

`StaticInfo\StageInfo\StageInfo.xml`（BaseMod 注册的接待）

```xml
<StageXmlRoot>
  <Stage id="149601">
    <FloorNum>1</FloorNum>
    <Chapter>7</Chapter>
    <Invitation Combine="BookValue"><Value>7</Value><Num>1</Num></Invitation>
    <Wave>
      <Formation>41</Formation>
      <AvailableUnit>5</AvailableUnit>
      <Unit>1</Unit>
      <FormationType>Default</FormationType>
      <emotionCardList/>
    </Wave>
    <MapInfo>Filature_Map</MapInfo>
    <Name>裁缝之王</Name>
    <StageType>Invitation</StageType>
    <Condition><Level>0</Level></Condition>
    <IsChapterLast>false</IsChapterLast>
    <invitationtype>false</invitationtype>
  </Stage>
</StageXmlRoot>
```

## 已排除的可能性

| 尝试 | 结果 |
|---|---|
| 删除 `StageInfo` 里全部 `<Story>` / `<StoryType>`（确认本模组无剧情） | 问题依旧 |
| 关卡 ID 从 `1` 改为 `149601`（避开与原版第 1 关撞号） | 问题依旧 |
| `Invitation` 从 `BookValue` 改为 `BookRecipe` + 指定书籍 | 问题依旧 |
| 移除 `ChapterMap`/`StoryIcon` 全部配置 | **恢复正常**（可进战斗、UI 正常） |

## 另一个已确认的类型定义问题（可能相关）

反编译 `LS_InvitationUtil.dll` 2.0.0.0：

```
LS_InvitationUtil.XML.Overrides.StageExtraCondition_Overrides
  NeedClearStages_Overrides   [XmlElement("Stage")]
  func                        [XmlElement("Func")]

（基类，Assembly-CSharp）
StageExtraCondition
  needClearStageList          [XmlElement("Stage")]
  needLevel                   [XmlElement("Level")]
```

派生类与基类都有 `XmlElement("Stage")`。此前 1.1/早期 2.0 会导致：

```
System.InvalidOperationException: There was an error reflecting type 'ChapterMapXmlRoot'
  → field 'condition' → type 'StageExtraCondition_Overrides'
  → field 'NeedClearStages_Overrides'
  → The XML element 'Stage' from namespace '' is already present in the current scope.
```

当前这版已能通过反序列化（日志无 `Error while load "ChapterMap"`），但如果这个重名仍在，建议把派生类的元素名改成唯一值（如 `NeedClearStage`），避免以后在别的序列化路径再次触发。

## 期望

1. 自建接待（BaseMod 注册、无 `<Story>`）不要生成剧情图标，也不要进入 `StoryManager` 播放路径
2. 章节地图界面应能返回原版地图（提供返回按钮或保证 ESC/取消可用）
3. 若接待的 `Chapter` 超出原版颜色表范围（本例为 7），`SetLevelUIColor` 不应越界崩溃

## 参考：日志位置

`%LOCALAPPDATA%Low\Project Moon\LibraryOfRuina\Player.log`

关键行（示例）：

```
LS_InvitationUtil : StoryIconUIManager initialize completed.
Uploading Crash Report
IndexOutOfRangeException: Index was outside the bounds of the array.
  at UI.UIStoryProgressIconSlot.SetLevelUIColor ...
```

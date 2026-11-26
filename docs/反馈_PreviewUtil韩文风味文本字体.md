# 反馈：LS_PreviewUtil 风味文本在韩文环境下显示为方块

## 环境

- 游戏：《废墟图书馆》1.1（Unity 2019.3.15f1），语言设为 **한국어**
- 插件：`LS_PreviewUtil.dll` **2.0.0.0**（放 `Assemblies\1FrameworkAssemblies\`）
- 文本来源：`Assemblies\Localize\kr\etc\*.xml`（LS_InvitationUtil 的本地化目录），内容是 `<localize><text id="...">…</text></localize>`
- `CardPreviewData` 里用 `<Flavor Color="#D2A679">ui_cfk_flavor_4</Flavor>` 引用文本 ID

## 现象

书页预览的**风味文本提示框**里，韩文全部显示为方块（缺字形），但**数字与半角标点正常**：

```
□□□□□□□□□□□' '□□□ 200□ □□□□ □□□□□□!'
```

同一条文本在文件里是：

```xml
<text id="ui_cfk_flavor_4">'아 이거 홀 밀라네제 아니네.' '번수가 200은 넘어야 셔츠 원단이죠.'</text>
```

即：文本**已被成功读取并写入 textDic**（否则不会是方块而是空白或键名），但**渲染用的字体没有韩文字形**。

对照观察：

- 同一个 MOD 的其他韩文文本（书页名、被动名、被动描述、卡牌效果）显示正常 —— 这些走游戏常规 UI 文本组件
- 只有风味文本（插件自己的预览面板）出现方块

## 已排除

| 尝试 | 结果 |
|---|---|
| 确认 textDic 里有该 ID | 有（数字 200 能渲染说明取到的是我们的文本） |
| 把 `etc\FlavorTexts.xml` 改存为带 BOM 的 UTF-8 | 仍是方块，排除"按 ANSI 读取导致乱码" |
| 换英文文本 | 正常（ASCII 字形存在），说明是字形覆盖问题而非编码问题 |

## 推测

插件预览面板里的 `TMP_Text` 使用了一个**固定的 TMP_FontAsset**（大概率为中文字体或仅含拉丁/中文字形），

- 插件内可见 `LocalizedFontSetter` / `SetLocalizedFont` 相关符号，但没有针对韩文（或按当前语言）切换字体
- 因此风味文本在 `kr`（可能还有 `jp`）环境下缺少字形回退

## 期望

1. 风味文本/预览文本按**当前游戏语言**选择字体（或使用游戏自带的本地化字体回退机制）
2. 或在说明里注明：风味文本仅支持中文/英文；非支持语言的 MOD 可自行提供字体

## 复现材料

- `CardPreviewData\CardPreviewData.xml`：

```xml
<Card ID="3">
  <Preview>3</Preview>
  <Flavor Color="#D2A679">ui_cfk_flavor_3</Flavor>
</Card>
```

- `Assemblies\Localize\kr\etc\FlavorTexts.xml`：

```xml
<localize>
  <text id="ui_cfk_flavor_3">재봉 땀수가 중요해 땀수</text>
</localize>
```

游戏语言设为 한국어，打开书页 3 的预览即可复现。

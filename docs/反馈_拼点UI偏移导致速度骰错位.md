# 反馈：拼点 UI 偏移补丁导致原生裁缝之王「速度骰」长期错位

## 环境

- 游戏：《废墟图书馆》1.1（Unity 2019.3.15f1）
- 插件：`LS_InvitationUtil.dll` 2.0.0.0 + `LS_PreviewUtil.dll` 1.0.1.0（`Assemblies\1FrameworkAssemblies\`）
- 相关提交：`be94474 修复：调整S10抓取高度与落地复位，新增拼点UI偏移与朝向镜像`
- 触发模组：裁缝之王（原生皮肤 `NativeKingSkin`，`KingHeight = 370`）

## 现象

原生裁缝之王登场后，头顶的**速度骰排列整体偏离头部中线**（向右约一个身位宽度），
在 S9 站位调整（`e13624c`）后更明显：王站在场地最左侧时，骰子排明显悬在头部右侧、贴近左上角的 HP 条。

- 只影响原生裁缝之王皮肤（`NativeKingSkin.IsNative == true` 的单位）
- 普通身高的罪人/司书不受影响
- 与拼点是否发生无关：**平时站立、未拼点时也已经偏移**

## 根因

`Source\Shared\Patches\ModPatches.cs`：

```csharp
private const float DiceActionUiOffsetX = 1.059f;
private const float DiceActionUiOffsetY = 0.398f;

private static void ApplyDiceActionUiOffset(BattleUnitView view, int height = 0)
{
    var ui = view?.diceActionUI;
    if (ui == null || !NativeKingSkin.IsNative(view)) return;

    float followerScale = view.camRotationFollower != null
        ? Mathf.Abs(view.camRotationFollower.transform.localScale.y) : 0f;
    float scale = height > 0
        ? 170f / height
        : (followerScale > Mathf.Epsilon ? 1f / followerScale : 1f);
    float facingSign = view.model?.direction == Direction.RIGHT ? -1f : 1f;

    ui.transform.localPosition = new Vector3(0f, 1.913f, 0f)
        + new Vector3(DiceActionUiOffsetX * facingSign, DiceActionUiOffsetY, 0f) * scale;
}

[HarmonyPatch(typeof(BattleUnitView), nameof(BattleUnitView.ChangeHeight))]
public static class BattleUnitView_ChangeHeight
{
    [HarmonyPostfix]
    private static void Postfix(BattleUnitView __instance, int height)
        => ApplyDiceActionUiOffset(__instance, height);
}

[HarmonyPatch(typeof(BattleUnitDiceActionUI), "Update")]
public static class BattleUnitDiceActionUI_MirrorOffset
{
    [HarmonyPostfix]
    private static void Postfix(BattleUnitDiceActionUI __instance)
    {
        if (__instance == null || !__instance.rootCanvas.enabled) return;
        ApplyDiceActionUiOffset(__instance._unitView);
    }
}
```

两点问题：

1. **每帧强制写 `localPosition`**：`BattleUnitDiceActionUI.Update` 的 Postfix 每帧执行，把偏移固化到所有状态（含未拼点的速度骰展示）。
2. **X 偏移按"拼点避让"设计，却作用于全部场景**：`DiceActionUiOffsetX = 1.059` 的目的是拼点时避开高大模型，
   但速度骰行与拼点骰行是同一个 `diceActionUI`，于是平时也被推歪。

## 数值验证

以 `KingHeight = 370` 代入：

```
scale   = 170 / 370            ≈ 0.4596
X 本地  = 1.059 × 0.4596       ≈ 0.487
视图缩放 = 370 / 170           ≈ 2.176
X 世界  = 0.487 × 2.176        ≈ 1.06
```

实测截图中骰子排中心相对头部中线的偏移量约等于一个身位宽，与 1.06 世界单位吻合。

## 原版行为（反编译 `Assembly-CSharp.dll` 确认）

`BattleUnitDiceActionUI`：

- 根节点 `transform.localPosition` **原版从不修改**
- 只使用子节点 `_objectRoot`，并在 `Start()` 中记录 `_objectRootOriginPos`，`InitDicesWithoutLog()` 里复位子节点
- 也就是说：**速度骰行的根位置完全由本补丁决定**，没有原版逻辑与之竞争或纠正

因此这不是"和原版打架"的抖动，而是补丁常量在非拼点场合不适用。

## 复现步骤

1. 使用原生裁缝之王皮肤进入接待战
2. 观察王头顶速度骰排：横向偏离头部中线（右侧约一个身位）
3. 调整 S9 站位后（`e13624c`）站在场地最左侧时最明显

## 建议修复方向

| 方案 | 说明 | 风险 |
|---|---|---|
| **A. 速度骰状态下 X 偏移归零** | 仅保留 Y 抬高；拼点时再按需横向避让 | 拼点时骰子可能压到高大模型 |
| **B. 只在拼点状态应用 X 偏移** | 平时 `X = 0`，拼点 `X = 1.059`（需要可靠的"是否拼点"判定） | 需要确认 `diceActionUI` 的拼点状态字段 |
| **C. 调小常量** | 例如 `DiceActionUiOffsetX = 0.4`，折中 | 需反复试数值 |

另外建议：

- **不要每帧写 `localPosition`**，改为在 `ChangeHeight` / 初始化时写入一次；或先记录原版初始值再叠加偏移，避免未来与其它 UI 插件冲突
- 若必须每帧校正，建议只在 `rootCanvas.enabled` 且确实处于拼点演出时执行

## 附件

- 现象截图：王头顶 6 颗速度骰（6/6/5/4/3/2）整体偏右，贴近左上角 HP 条
- 相关代码：`Source\Shared\Patches\ModPatches.cs`（`ApplyDiceActionUiOffset` 及两个 Harmony 补丁）

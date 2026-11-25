# 边狱巴士地图背景解包记录（供 LbsMap 使用）

工具来源：`C:\Users\MECHREVO\OneDrive\dll文件\LimbusUnpackingEffects`（模块1「Unity6 资产恢复」+ 模块4「通用资源」）。
本文记录一次完整实操（Canto10 西西弗百货 B2F · Filature），以后解包同类地图照此进行。

## 1. 关键路径

| 用途 | 路径 |
|---|---|
| 游戏本体 | `D:\APP\Steam\steamapps\common\Limbus Company` |
| Addressables 目录 | `LimbusCompany_Data\StreamingAssets\aa\catalog.bin` |
| **资源包缓存** | `%USERPROFILE%\AppData\LocalLow\Unity\ProjectMoon_LimbusCompany\<分组hash>\<bundle hash>\__data` |
| 工作目录 | `C:\Users\MECHREVO\LimbusWork`（venv 在 `.venv`，装 UnityPy 1.25.3） |

- 游戏**不把 bundle 存在安装目录**，运行时从 CDN 下载并以 Unity 缓存格式落在上面那个目录（`__data` 就是 bundle 本体）
- CDN 直连会被 Cloudflare 403，**必须走本地缓存**，不要试图下载
- Unity 版本：`6000.3.12f1`（从 `Player.log` 的 `Initialize engine version` 读）

## 2. 解包步骤

1. **确认关卡身份**：在 `LimbusCompany_Data\Lang\LLC_zh-CN\*.json` 里搜中文名/技能名，得到关卡或预设关键字。
   例：`BattleResultHint-a1c10p1.json` 里 `battleTip_11016_1` 的技能名对得上裁缝之王 → 属于 Canto10（a1c10）。
2. **定位地图预设**：BattleMap 预设类名形如 `BattleMapPreset_<区域>_<楼层>_<房间>`，
   Canto10 的在 catalog 里能搜到 `BattleMapPreset_Cp10RPG_B2F_Filature` 等（B2F 有 CoiledNest / Corridor / Filature）。
3. **定位所属 bundle**：把 catalog 里的 bundle 名（`*_assets_all_<32位hash>.bundle`）按名称筛出候选（如 `s8_a1c10p1_battlemap`），
   用 UnityPy 逐个打开缓存里的 `__data`，看哪个包里有该 GameObject。Filature 在
   `s8_a1c10p1_battlemap_assets_all_07c51ccc91d4dce9e26b46565468ee38`（51MB，cache 分组 `7191...` 类似结构）。
4. **解析包**：`UnityPy.config.FALLBACK_UNITY_VERSION = "6000.3.12f1"` 后再 `UnityPy.load(__data)`，
   否则报 `No valid Unity version found`（Unity6 的版本串是 `5.x.x`）。
5. **遍历预设层级**：从 GameObject `BattleMapPreset_*` 取 Transform，逐层读 `m_Children`，
   MeshRenderer → Material → `m_SavedProperties.m_TexEnvs` 拿 `_MainTex` 贴图 PathID。
6. **导出贴图**：`obj.read().image.save(...)`（Pillow）。注意同名贴图很多，**必须按 PathID 取**，不要按名字取。
7. **合成 LbsMap 素材**：
   - `Board.png` = `quad_floor` 引用的 `floor` 贴图，原生 **2000×678**，与 LbsMap 的 Board 规格完全一致，直接用
   - `BG.png` = 背景各层按 alt 裁边后叠在 2000×551 的画布上

## 3. Filature 地图的层结构（实测）

```
BattleMapPreset_Cp10RPG_B2F_Filature
├ BG_Pivot (0,0,11)
│ └ Background_MasterTransform
│   ├ back_endM             map_squareQM     256×256    纯黑底
│   ├ tr_m_obj05 / quad     obj05            831×138    远景立柱窗
│   ├ tr_m_obj04 / quad     obj04           1443×239    立柱窗
│   ├ tr_m_obj03 / quad     obj03           2000×445    缝纫机剪影
│   ├ tr_m_obj02 / quad     obj02           1904×315    立柱窗
│   ├ tr_m_obj01 / quad     obj01            503×440    布艺王座
│   └ back_drak_Sharp_*     map_graduationTipQM 256×256  暗角
└ Board_Pivot
  ├ BattleGround
  ├ Floor_MasterTransform
  │ └ tr_quad_floor / quad_floor   floor     2000×678   地面（= Board）
  ├ Foreground_MasterTransform
  └ Light1~3 / Burn1 / Particle1
```

`BG.png` 合成参数（Pillow，画布 2000×551，逐层 alpha>12 裁边后居中叠加，底黑）：

| 层 | 缩放 | 内容底部 y |
|---|---|---|
| obj05（远景立柱） | 1.00 | 320 |
| obj04（立柱） | 1.25 | 430 |
| obj02（立柱） | 1.60 | 505 |
| obj03（缝纫机） | 1.00 | 525 |
| obj01（王座） | 0.90 | 520 |

## 4. 成品去向

- 备份原图：`C:\Users\MECHREVO\LimbusWork\backup_cfk\*.orig`
- 仓库：`MOD根目录\Assemblies\LbsMap\CFK\{BG.png, Board.png}`
- 同步：游戏 `Mods\裁缝之王\Assemblies\LbsMap\CFK\`

## 5. 换别的图时

1. 换预设名重复第 2–4 步，找出包里 `BattleMapPreset_*` 对应的贴图
2. `floor` 直接做 Board；背景层按上表的思路（远→近、暗→亮）叠到 2000×551
3. 游戏里 `Player.log` 的 `Initialize engine version` 可能随版本变，改 `FALLBACK_UNITY_VERSION` 即可

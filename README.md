# XiXiFuSiBaiHuo（裁缝之王 MOD 工程）

《废墟图书馆》裁缝之王（敌方 Boss 1496 / 核心书页 / 经纬针族）MOD 源代码与资源包工程。

## 目录结构

- `Source/`：MOD C# 源码（引导、玩法被动/卡牌/BUFF、原生皮肤、演出动作、镜头与特效）。
- `MOD根目录/`：工坊 MOD 静态配置、本地化、音频与运行时依赖（编译产物 `XiXiFuSiBaiHuo.dll` 与 Unity `AB/*_skin` 由 `.gitignore` 忽略）。
- `XiXiFuSiBaiHuo.csproj` / `XiXiFuSiBaiHuo.slnx` / `packages.config`：工程文件。
- `BuildAndDeploy.ps1`：一键编译并部署到 Steam 工坊目录（支持 `-BuildOnly` 仅编译）。

## 构建与部署

```powershell
# 仅编译
.\BuildAndDeploy.ps1 -BuildOnly

# 编译并部署到工坊目录（需先关闭游戏）
.\BuildAndDeploy.ps1
```

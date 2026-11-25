# Unity MCP（可远程操作 Unity 工程）

协作方给的 `UnityMCP2019-中文导入包.unitypackage` 已装进我们的打包工程，现在可以脚本化操作编辑器。

## 装在哪

| 项 | 位置 |
|---|---|
| UPM 包 | `D:\APP\UnityProjects\CFKMapBuild\Packages\com.local.unity-mcp-2019` |
| 编辑器端点 | `D:\APP\UnityProjects\CFKMapBuild\Library\UnityMcp2019\endpoint.json` |
| 便携 Python | `<包>\Runtime~\python-win-x64\python.exe` |
| MCP 桥 | `<包>\Bridge~\unity_mcp_bridge.py` |

## 关键限制

- **编辑器端点只在非批处理模式下启动**：`EditorServer.Start()` 里有 `if (Application.isBatchMode) return;`，
  所以必须用 GUI 方式打开工程（`Unity.exe -projectPath <工程>`），批处理构建时没有端点
- 端点文件在每次重启后变化（随机端口 + 随机 token）

## 直接调用（不需要 MCP 客户端）

端点是一个本地 HTTP JSON-RPC：`POST http://127.0.0.1:<port>/rpc`，头带 `Authorization: Bearer <token>`。
用 PowerShell 就能调：

```powershell
$ep = Get-Content "D:\APP\UnityProjects\CFKMapBuild\Library\UnityMcp2019\endpoint.json" -Raw | ConvertFrom-Json
$body = @{ jsonrpc="2.0"; id=1; method="tools/call";
           params=@{ name="Unity_ManageScene"; arguments=@{ Action="GetHierarchy"; Depth=1 } } } |
        ConvertTo-Json -Depth 10 -Compress
Invoke-WebRequest -Uri "http://127.0.0.1:$($ep.port)/rpc" -Method Post -Body $body `
  -ContentType "application/json" -Headers @{ Authorization = "Bearer $($ep.token)" } -UseBasicParsing
```

`tools/list` 能拿到全部 54 个工具（`Unity_RunCommand` 可以直接编译执行 C# 片段，最有用）。

## 常用工具

| 工具 | 用途 |
|---|---|
| `Unity_RunCommand` | 执行任意 C#（要实现 `IRunCommand`/`IReadonlyRunCommand`，类名 `CommandScript`） |
| `Unity_ManageScene` | 建/载/存场景、取层级 |
| `Unity_ManageGameObject` | 增删改 GameObject 与组件 |
| `Unity_Camera_Capture` | 截场景视图（视觉验收用） |
| `Unity_GetConsoleLogs` | 读 Console，排错用 |

## 注意

- 工程被编辑器占用时，批处理构建会失败；构建前先关编辑器
- 这个包会往工程写 `.codex\config.toml`（给 Codex 用），对我们没影响，删不删都行

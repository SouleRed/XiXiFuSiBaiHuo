<#
.SYNOPSIS
    一键构建 + 上传/更新创意工坊。

.DESCRIPTION
    1) 调用 BuildAndDeploy.ps1 构建并同步到创意工坊目录
    2) 用 Tools\WorkshopUploader 通过 Steam 客户端上传 MOD根目录

    需要 Steam 客户端已启动并登录（不需要账号密码）。
    工坊物品 ID 首次填写后写入 workshop.config.json，后续直接 -Update 即可。

.EXAMPLE
    .\Upload-Workshop.ps1 -ChangeNote "修复 BGM 爆音"
    .\Upload-Workshop.ps1 -Check           # 只检查参数，不上传
    .\Upload-Workshop.ps1 -New -ChangeNote "首次发布"
#>
[CmdletBinding()]
param(
    [string]$ChangeNote,
    [string]$Title,
    [string]$Description,
    [string]$ItemId,
    [string]$Preview,
    [string[]]$Tags,
    [switch]$New,          # 创建新物品（忽略 config 里的 itemId）
    [string]$Lang,         # 本次标题/简介的语言（english/japanese/schinese/koreana）
    [switch]$PreserveMeta, # 只更新内容，标题/简介沿用服务器现有值
    [switch]$Info,         # 只读取并打印物品信息
    [switch]$UseConfigMeta, # 使用 config 里的标题/简介（首次发布或全量覆盖时用）
    [switch]$SkipBuild,    # 跳过构建，直接上传现有 MOD根目录
    [switch]$Check,        # 只做参数校验
    [switch]$Public,
    [switch]$Private
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$configPath = Join-Path $repoRoot 'workshop.config.json'
$uploader = Join-Path $repoRoot 'Tools\WorkshopUploader\bin\WorkshopUploader.exe'

if (!(Test-Path -LiteralPath $configPath)) { throw "找不到配置：$configPath" }
$config = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json

if (!$ItemId) { $ItemId = [string]$config.itemId }
if ($UseConfigMeta -or $New) {
    if (!$Title) { $Title = [string]$config.title }
    if (!$Description) { $Description = [string]$config.description }
}
if (!$Preview) { $Preview = [string]$config.preview }
if (!$Preview) { $Preview = 'MOD根目录\preview.jpg' }
if (!$Tags -or $Tags.Count -eq 0) { $Tags = @($config.tags) }
if (!$ChangeNote) { $ChangeNote = [string]$config.changeNote }
if (!$Lang) { $Lang = [string]$config.language }

$Preview = Join-Path $repoRoot $Preview
$content = Join-Path $repoRoot 'MOD根目录'

if (!$Check -and !$SkipBuild) {
    Write-Host '== 构建并同步 ==' -ForegroundColor Cyan
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'BuildAndDeploy.ps1')
    if ($LASTEXITCODE -ne 0) { throw '构建失败，已中止上传。' }
}

if (!(Test-Path -LiteralPath $uploader)) {
    Write-Host '== 编译上传器 ==' -ForegroundColor Cyan
    & dotnet msbuild (Join-Path $repoRoot 'Tools\WorkshopUploader\WorkshopUploader.csproj') /p:Configuration=Release /v:minimal
    if ($LASTEXITCODE -ne 0) { throw '上传器编译失败。' }
}

$args = @('--content', $content)
if ($Preview) { $args += @('--preview', $Preview) }
if ($Title) { $args += @('--title', $Title) }
if ($Description) { $args += @('--desc', $Description) }
if ($ChangeNote) { $args += @('--changenote', $ChangeNote) }
foreach ($t in $Tags) { if ($t) { $args += @('--tag', [string]$t) } }
if (!$New -and $ItemId) { $args += @('--id', $ItemId) }
if ($Public) { $args += '--public' }
if ($Private) { $args += '--private' }
if ($Check) { $args += '--check' }
if ($Info) { $args += '--info' }
if ($PreserveMeta -or (!$Title -and !$Description -and !$Info)) { $args += '--preserve-meta' }
if ($Lang) { $args += @('--lang', $Lang) }

Write-Host ('== 上传参数 ==' + "`n  content : $content`n  preview : $Preview`n  itemId  : " + $(if (!$New -and $ItemId) { $ItemId } else { '(新建)' })) -ForegroundColor Cyan
& $uploader @args
$code = $LASTEXITCODE
if ($code -ne 0) { throw "上传失败（退出码 $code）" }

if (!$Check -and !$ItemId -and !$New) {
    Write-Host '提示：首次上传后，把工坊物品 ID 填进 workshop.config.json 的 itemId，后续即可直接更新。' -ForegroundColor Yellow
}

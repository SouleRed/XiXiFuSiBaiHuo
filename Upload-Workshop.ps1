<#
.SYNOPSIS
    构建 + 用协助者的 SteamworkUploader CLI 发布/更新创意工坊。

.DESCRIPTION
    - 工具：C:\Users\MECHREVO\OneDrive\废墟图书馆mod工程文件\SteamworkUploader.exe
    - 草稿：workshop.draft.json（5 语言 title/description/changeLog）
    - 流程：BuildAndDeploy → configure → publish --confirm

.EXAMPLE
    .\Upload-Workshop.ps1 -ChangeNote "修复预览插件"
    .\Upload-Workshop.ps1 -DryRun                 # 只校验，不上传
    .\Upload-Workshop.ps1 -PageOnly -IncludeDescription   # 只更新页面文案
    .\Upload-Workshop.ps1 -SkipBuild -ChangeNote "仅资源"
#>
[CmdletBinding()]
param(
    [string]$Folder,
    [string]$ChangeNote,
    [switch]$SkipBuild,
    [switch]$DryRun,
    [switch]$PageOnly,
    [switch]$IncludeDescription,
    [ValidateSet('Keep','Private','FriendsOnly','Public')]
    [string]$Visibility = 'Keep',
    [switch]$Open,
    [switch]$Fetch
)

$ErrorActionPreference = 'Stop'
$repoRoot = $PSScriptRoot
$exe = 'C:\Users\MECHREVO\OneDrive\废墟图书馆mod工程文件\SteamworkUploader.exe'
$draftPath = Join-Path $repoRoot 'workshop.draft.json'

if (!$Folder) { $Folder = Join-Path $repoRoot 'MOD根目录' }
if (!(Test-Path -LiteralPath $folder)) { throw "MOD 目录不存在：$Folder" }
if (!(Test-Path -LiteralPath $exe)) { throw "找不到上传工具：$exe" }
if (!(Test-Path -LiteralPath $draftPath)) { throw "找不到草稿：$draftPath（可用 -Fetch 从工坊拉取）" }

[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

if (!$SkipBuild -and !$DryRun) {
    Write-Host '== 构建并同步 ==' -ForegroundColor Cyan
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'BuildAndDeploy.ps1')
    if ($LASTEXITCODE -ne 0) { throw '构建失败，已中止上传。' }
}

$draft = Get-Content -LiteralPath $draftPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ($Fetch) {
    Write-Host '== 从工坊拉取现有 5 语言文案 ==' -ForegroundColor Cyan
    $fetched = & $exe fetch --id $draft.workshopId --out $draftPath
    if ($LASTEXITCODE -ne 0) { throw 'fetch 失败' }
}

if ($ChangeNote) {
    foreach ($c in $draft.copies) { $c.changeLog = $ChangeNote }
    $draft | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $draftPath -Encoding UTF8
    Write-Host "== 更新说明：$ChangeNote（已写入 5 语言）" -ForegroundColor Cyan
}

Write-Host '== configure ==' -ForegroundColor Cyan
& $exe configure --folder $Folder --json $draftPath | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'configure 失败' }

Write-Host '== inspect ==' -ForegroundColor Cyan
& $exe inspect --folder $Folder | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'inspect 失败' }

$args = @('publish', '--folder', $Folder)
if ($DryRun) {
    $args += '--dry-run'
} else {
    $args += @('--confirm', '--visibility', $Visibility)
    if ($PageOnly) { $args += '--page-only' }
    if ($IncludeDescription) { $args += '--include-description' }
}
if ($Open) { & $exe open --folder $Folder | Out-Null }

Write-Host '== publish ==' -ForegroundColor Cyan
& $exe @args
$code = $LASTEXITCODE
if ($code -ne 0) { throw "publish 失败（退出码 $code）" }
Write-Host '完成。' -ForegroundColor Green

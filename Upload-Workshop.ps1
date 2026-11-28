<#
.SYNOPSIS
    构建 + 用协助者的 SteamworkUploader CLI 发布/更新创意工坊。

.DESCRIPTION
    - 工具：C:\Users\MECHREVO\OneDrive\废墟图书馆mod工程文件\SteamworkUploader.exe
    - 草稿：workshop.draft.json（5 语言 title/description，源文件）
    - 发布时只提交「中文」这一份 copy（更新说明也只写中文），其它语言的描述保持不动
    - 流程：BuildAndDeploy → configure(单语言) → inspect → publish --confirm

.EXAMPLE
    .\Upload-Workshop.ps1 -ChangeNote "修复预览插件"
    .\Upload-Workshop.ps1 -DryRun
    .\Upload-Workshop.ps1 -SkipBuild -ChangeNote "仅资源"
    .\Upload-Workshop.ps1 -IncludeDescription   # 5 语言描述全部重传（慎用）
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
$publishDraft = Join-Path $repoRoot '.publish.draft.json'

if (!$Folder) { $Folder = Join-Path $repoRoot 'MOD根目录' }
if (!(Test-Path -LiteralPath $Folder)) { throw "MOD 目录不存在：$Folder" }
if (!(Test-Path -LiteralPath $exe)) { throw "找不到上传工具：$exe" }
if (!(Test-Path -LiteralPath $draftPath)) { throw "找不到草稿：$draftPath" }

[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

if (!$SkipBuild -and !$DryRun) {
    Write-Host '== 构建并同步 ==' -ForegroundColor Cyan
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repoRoot 'BuildAndDeploy.ps1')
    if ($LASTEXITCODE -ne 0) { throw '构建失败，已中止上传。' }
}

$draft = Get-Content -LiteralPath $draftPath -Raw -Encoding UTF8 | ConvertFrom-Json

if ($Fetch) {
    Write-Host '== 从工坊拉取现有 5 语言文案 ==' -ForegroundColor Cyan
    & $exe fetch --id $draft.workshopId --out $draftPath | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'fetch 失败' }
    $draft = Get-Content -LiteralPath $draftPath -Raw -Encoding UTF8 | ConvertFrom-Json
}

# 只保留中文 copy：Steam 会对每个 copy 各生成一条更新公告，
# 多语言 changeLog 没有实际意义，反而刷出 5 条重复公告。
$copies = @($draft.copies | Where-Object { $_.language -eq 'schinese' })
if ($copies.Count -eq 0) { throw '草稿里缺少 schinese copy' }
if ($IncludeDescription) {
    $copies = @($draft.copies)
} else {
    foreach ($c in $copies) { $c.changeLog = if ($ChangeNote) { $ChangeNote } else { '' } }
}

$pub = [ordered]@{
    workshopId         = $draft.workshopId
    lastContentLanguage = 'schinese'
    pageOnly           = [bool]$PageOnly
    includeDescription = [bool]$IncludeDescription
    visibility         = $Visibility
    tags               = @($draft.tags)
    copies             = $copies
}
$pub | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $publishDraft -Encoding UTF8

if ($ChangeNote) { Write-Host "== 更新说明（仅中文）：$ChangeNote" -ForegroundColor Cyan }

Write-Host '== configure ==' -ForegroundColor Cyan
& $exe configure --folder $Folder --json $publishDraft | Out-Null
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

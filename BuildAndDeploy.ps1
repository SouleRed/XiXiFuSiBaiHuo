[CmdletBinding()]
param(
    [string]$WorkshopRoot = 'E:\GAME\STEAM\steamapps\workshop\content\1256670',
    [switch]$BuildOnly
)

$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSCommandPath)).TrimEnd('\')
$projectPath = Join-Path $repoRoot 'XiXiFuSiBaiHuo.csproj'
$dllSource = Join-Path $repoRoot 'bin\Debug\XiXiFuSiBaiHuo.dll'
$modSource = Join-Path $repoRoot 'MOD根目录'
$modTarget = Join-Path $WorkshopRoot 'XiXiFuSiBaiHuo'

Push-Location -LiteralPath $repoRoot
try {
    dotnet msbuild $projectPath /t:Build /p:Configuration=Debug /nologo /v:minimal /clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) { throw "编译失败，退出码：$LASTEXITCODE" }

    $legacyDll = Join-Path $modSource 'Assemblies\裁缝之王.dll'
    if (Test-Path -LiteralPath $legacyDll) { Remove-Item -LiteralPath $legacyDll -Force }
    Copy-Item -LiteralPath $dllSource -Destination (Join-Path $modSource 'Assemblies\XiXiFuSiBaiHuo.dll') -Force
    Write-Output "BUILD OK: $dllSource"

    if ($BuildOnly) { return }
    if (Get-Process LibraryOfRuina -ErrorAction SilentlyContinue) {
        throw 'LibraryOfRuina 正在运行，已跳过部署以免占用 DLL。'
    }
    if (!(Test-Path -LiteralPath $modTarget -PathType Container)) {
        New-Item -ItemType Directory -Path $modTarget -Force | Out-Null
    }
    Get-ChildItem -LiteralPath $modSource | Copy-Item -Destination $modTarget -Recurse -Force
    Write-Output "DEPLOY OK: $modTarget"
}
finally {
    Pop-Location
}

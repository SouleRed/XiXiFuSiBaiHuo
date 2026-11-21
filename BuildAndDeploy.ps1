[CmdletBinding()]
param(
    [string]$WorkshopRoot = 'E:\GAME\STEAM\steamapps\workshop\content\1256670',
    [switch]$BuildOnly
)

$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSCommandPath)).TrimEnd('\')
$projectPath = Join-Path $repoRoot '裁缝之王.csproj'
$dllSource = Join-Path $repoRoot 'bin\Debug\裁缝之王.dll'
$modSource = Join-Path $repoRoot 'MOD根目录'
$modTarget = Join-Path $WorkshopRoot '裁缝之王'

Push-Location -LiteralPath $repoRoot
try {
    dotnet msbuild $projectPath /t:Build /p:Configuration=Debug /nologo /v:minimal /clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) { throw "编译失败，退出码：$LASTEXITCODE" }

    Copy-Item -LiteralPath $dllSource -Destination (Join-Path $modSource 'Assemblies\裁缝之王.dll') -Force
    Write-Output "BUILD OK: $dllSource"

    if ($BuildOnly) { return }
    if (Get-Process LibraryOfRuina -ErrorAction SilentlyContinue) {
        throw 'LibraryOfRuina 正在运行，已跳过部署以免占用 DLL。'
    }
    if (!(Test-Path -LiteralPath $modTarget -PathType Container)) {
        New-Item -ItemType Directory -Path $modTarget -Force | Out-Null
    }
    Copy-Item -LiteralPath (Join-Path $modSource '*') -Destination $modTarget -Recurse -Force
    Write-Output "DEPLOY OK: $modTarget"
}
finally {
    Pop-Location
}

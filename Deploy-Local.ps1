#requires -Version 5.1
<#
    Local deploy helper for this machine.

    BuildAndDeploy.ps1 deploys into a Workshop-style folder (E:\GAME\...\workshop\...),
    which does not exist here. This machine loads the mod as a local mod from:
        <GameRoot>\LibraryOfRuina_Data\Mods\<folder>

    Usage:
        .\Deploy-Local.ps1                 # build + deploy
        .\Deploy-Local.ps1 -BuildOnly      # build + copy dll into MOD root only
        .\Deploy-Local.ps1 -ModsRoot "D:\path\to\LibraryOfRuina_Data\Mods"
#>
[CmdletBinding()]
param(
    [string]$ModsRoot,
    [string]$ModFolderName,
    [switch]$BuildOnly
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

$repoRoot    = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')
$projectPath = Join-Path $repoRoot 'XiXiFuSiBaiHuo.csproj'
$dllSource   = Join-Path $repoRoot 'bin\Debug\XiXiFuSiBaiHuo.dll'
$modSource   = Join-Path $repoRoot 'MOD根目录'

function Find-GameModsRoot {
    $steamRoots = New-Object System.Collections.Generic.List[string]

    $steamPath = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue).SteamPath
    if ($steamPath) { $steamRoots.Add($steamPath) }

    foreach ($root in @($steamRoots.ToArray())) {
        $vdf = Join-Path $root 'steamapps\libraryfolders.vdf'
        if (-not (Test-Path -LiteralPath $vdf)) { continue }
        foreach ($m in [regex]::Matches((Get-Content -LiteralPath $vdf -Raw), '"path"\s*"([^"]+)"')) {
            $p = $m.Groups[1].Value.Replace('\\', '\')
            if ($steamRoots -notcontains $p) { $steamRoots.Add($p) }
        }
    }

    # Last resort: the path this machine is known to use.
    if ($steamRoots -notcontains 'D:\APP\Steam') { $steamRoots.Add('D:\APP\Steam') }

    foreach ($root in $steamRoots) {
        $candidate = Join-Path $root 'steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods'
        if (Test-Path -LiteralPath $candidate -PathType Container) { return $candidate }
    }
    return $null
}

function Resolve-ModFolder([string]$modsRoot, [string]$preferred) {
    if ($preferred) { return (Join-Path $modsRoot $preferred) }
    foreach ($name in @('XiXiFuSiBaiHuo', '裁缝之王')) {
        $p = Join-Path $modsRoot $name
        if (Test-Path -LiteralPath $p -PathType Container) { return $p }
    }
    return (Join-Path $modsRoot 'XiXiFuSiBaiHuo')
}

if (-not (Test-Path -LiteralPath $projectPath)) { throw "Project not found: $projectPath" }
if (-not (Test-Path -LiteralPath $modSource))   { throw "MOD source folder not found: $modSource" }

Push-Location -LiteralPath $repoRoot
try {
    Write-Host "== Build ==" -ForegroundColor Cyan
    dotnet msbuild $projectPath /t:Build /p:Configuration=Debug /nologo /v:minimal /clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) { throw "Build failed, exit code: $LASTEXITCODE" }
    if (-not (Test-Path -LiteralPath $dllSource)) { throw "Built dll not found: $dllSource" }

    $legacyDll = Join-Path $modSource 'Assemblies\裁缝之王.dll'
    if (Test-Path -LiteralPath $legacyDll) { Remove-Item -LiteralPath $legacyDll -Force }

    $dllTarget = Join-Path $modSource 'Assemblies\XiXiFuSiBaiHuo.dll'
    Copy-Item -LiteralPath $dllSource -Destination $dllTarget -Force
    Write-Host "BUILD OK  -> $dllTarget" -ForegroundColor Green

    if ($BuildOnly) { exit 0 }

    if (Get-Process LibraryOfRuina -ErrorAction SilentlyContinue) {
        throw 'LibraryOfRuina is running. Close the game before deploying.'
    }

    if (-not $ModsRoot) {
        $ModsRoot = Find-GameModsRoot
        if (-not $ModsRoot) { throw 'Could not locate LibraryOfRuina_Data\Mods. Pass -ModsRoot explicitly.' }
    }

    $modTarget = Resolve-ModFolder $ModsRoot $ModFolderName
    if (-not (Test-Path -LiteralPath $modTarget -PathType Container)) {
        New-Item -ItemType Directory -Path $modTarget -Force | Out-Null
    }

    Write-Host "== Deploy -> $modTarget ==" -ForegroundColor Cyan
    Get-ChildItem -LiteralPath $modSource | Copy-Item -Destination $modTarget -Recurse -Force
    Write-Host "DEPLOY OK  -> $modTarget" -ForegroundColor Green
    exit 0
}
finally {
    Pop-Location
}

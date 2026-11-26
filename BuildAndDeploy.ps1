[CmdletBinding()]
param(
    [string]$WorkshopRoot,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$BuildOnly
)

$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSCommandPath)).TrimEnd('\')
$projectPath = Join-Path $repoRoot 'XiXiFuSiBaiHuo.csproj'
$redDiceProjectPath = Join-Path $repoRoot 'XiXiFuSiBaiHuo.RedDice.csproj'
$modSource = Join-Path $repoRoot 'MOD根目录'
$dllSource = Join-Path $repoRoot "bin\$Configuration\XiXiFuSiBaiHuo.dll"
$redDiceDllSource = Join-Path $repoRoot "bin\$Configuration\XiXiFuSiBaiHuo.RedDice.dll"
$modDirectoryName = 'XiXiFuSiBaiHuo'
$modPackageId = 'CaiFengZhiKing'

function Get-SteamLibraryRoots
{
    $steamRoots = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $libraryRoots = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

    function Add-ExistingRoot([Collections.Generic.HashSet[string]]$set, [string]$path)
    {
        if ([string]::IsNullOrWhiteSpace($path)) { return }
        $fullPath = [IO.Path]::GetFullPath($path.Replace('/', '\')).TrimEnd('\')
        if (Test-Path -LiteralPath $fullPath -PathType Container) { [void]$set.Add($fullPath) }
    }

    foreach ($registryLocation in @(
        @{ Path = 'HKCU:\Software\Valve\Steam'; Name = 'SteamPath' },
        @{ Path = 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam'; Name = 'InstallPath' },
        @{ Path = 'HKLM:\SOFTWARE\Valve\Steam'; Name = 'InstallPath' }
    ))
    {
        try { Add-ExistingRoot $steamRoots (Get-ItemPropertyValue -LiteralPath $registryLocation.Path -Name $registryLocation.Name -ErrorAction Stop) }
        catch { }
    }
    Add-ExistingRoot $steamRoots (Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Steam')
    Add-ExistingRoot $steamRoots (Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'Steam')

    foreach ($steamRoot in @($steamRoots))
    {
        Add-ExistingRoot $libraryRoots $steamRoot
        $libraryFile = Join-Path $steamRoot 'steamapps\libraryfolders.vdf'
        if (!(Test-Path -LiteralPath $libraryFile)) { continue }
        $content = Get-Content -LiteralPath $libraryFile -Raw
        foreach ($match in [regex]::Matches($content, '"path"\s+"(?<path>[^"]+)"'))
        {
            Add-ExistingRoot $libraryRoots ($match.Groups['path'].Value -replace '\\\\', '\')
        }
    }

    return @($libraryRoots)
}

function Resolve-LibraryOfRuinaWorkshopRoot([string]$overridePath, [string]$modDirName, [string[]]$libraryRoots)
{
    if (![string]::IsNullOrWhiteSpace($overridePath))
    {
        $resolved = [IO.Path]::GetFullPath($overridePath).TrimEnd('\')
        if ((Split-Path -Leaf $resolved) -ne '1256670' -or !(Test-Path -LiteralPath $resolved -PathType Container))
        {
            throw "-WorkshopRoot 必须指向已存在的 ...\steamapps\workshop\content\1256670：$resolved"
        }
        return $resolved
    }

    $candidates = @($libraryRoots | ForEach-Object {
        $workshop = Join-Path $_ 'steamapps\workshop\content\1256670'
        [pscustomobject]@{
            LibraryRoot = $_
            WorkshopRoot = $workshop
            HasInstalledMod = Test-Path -LiteralPath (Join-Path $workshop $modDirName) -PathType Container
            HasGameManifest = Test-Path -LiteralPath (Join-Path $_ 'steamapps\appmanifest_1256670.acf')
            WorkshopExists = Test-Path -LiteralPath $workshop -PathType Container
        }
    })

    $matches = @($candidates | Where-Object HasInstalledMod)
    if ($matches.Count -eq 0) { $matches = @($candidates | Where-Object { $_.HasGameManifest -and $_.WorkshopExists }) }
    if ($matches.Count -eq 0) { $matches = @($candidates | Where-Object WorkshopExists) }
    if ($matches.Count -eq 0) { $matches = @($candidates | Where-Object HasGameManifest) }
    if ($matches.Count -ne 1)
    {
        $found = ($matches.WorkshopRoot -join "`n  ")
        throw "无法唯一确定《废墟图书馆》的 Workshop 根目录。可使用 -WorkshopRoot 手动指定。`n  $found"
    }

    $selectedWorkshop = [IO.Path]::GetFullPath($matches[0].WorkshopRoot).TrimEnd('\')
    if (!(Test-Path -LiteralPath $selectedWorkshop -PathType Container))
    {
        New-Item -ItemType Directory -Path $selectedWorkshop -Force | Out-Null
    }
    return $selectedWorkshop
}

function Remove-ResidualGameRootMods([string]$workshopParent, [string[]]$libraryRoots, [string]$packageId)
{
    $modsRoots = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

    foreach ($libRoot in @($libraryRoots))
    {
        $candidate = Join-Path $libRoot 'steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods'
        if (Test-Path -LiteralPath $candidate -PathType Container)
        {
            [void]$modsRoots.Add([IO.Path]::GetFullPath($candidate).TrimEnd('\'))
        }
    }

    # 同时从已解析的 workshopParent 反推同级 steamapps 下的游戏 Mods 目录
    $steamappsDir = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $workshopParent))
    if (![string]::IsNullOrWhiteSpace($steamappsDir))
    {
        $siblingMods = Join-Path $steamappsDir 'common\Library Of Ruina\LibraryOfRuina_Data\Mods'
        if (Test-Path -LiteralPath $siblingMods -PathType Container)
        {
            [void]$modsRoots.Add([IO.Path]::GetFullPath($siblingMods).TrimEnd('\'))
        }
    }

    if ($modsRoots.Count -eq 0) { return }

    Add-Type -AssemblyName Microsoft.VisualBasic -ErrorAction SilentlyContinue
    $removed = New-Object System.Collections.Generic.List[string]

    foreach ($modsRoot in $modsRoots)
    {
        foreach ($dir in @(Get-ChildItem -LiteralPath $modsRoot -Directory -ErrorAction SilentlyContinue))
        {
            $isTarget = $false
            $infoPath = Join-Path $dir.FullName 'StageModInfo.xml'
            if (Test-Path -LiteralPath $infoPath -PathType Leaf)
            {
                $infoRaw = Get-Content -LiteralPath $infoPath -Raw -ErrorAction SilentlyContinue
                if ($infoRaw -match "<ID>$([regex]::Escape($packageId))</ID>")
                {
                    $isTarget = $true
                }
            }

            if (-not $isTarget -and ($dir.Name -in @('XiXiFuSiBaiHuo', '裁缝之王')))
            {
                if ((Test-Path -LiteralPath (Join-Path $dir.FullName 'Assemblies\XiXiFuSiBaiHuo.dll') -PathType Leaf) -or
                    (Test-Path -LiteralPath (Join-Path $dir.FullName 'Assemblies\裁缝之王.dll') -PathType Leaf))
                {
                    $isTarget = $true
                }
            }

            if ($isTarget)
            {
                [Microsoft.VisualBasic.FileIO.FileSystem]::DeleteDirectory(
                    $dir.FullName,
                    [Microsoft.VisualBasic.FileIO.UIOption]::OnlyErrorDialogs,
                    [Microsoft.VisualBasic.FileIO.RecycleOption]::SendToRecycleBin
                )
                $removed.Add($dir.FullName)
            }
        }
    }

    if ($removed.Count -gt 0)
    {
        foreach ($p in $removed)
        {
            Write-Host "已清理游戏根目录残余 MOD（移入回收站）：$p" -ForegroundColor Yellow
        }
    }
}

if (!(Test-Path -LiteralPath $projectPath)) { throw "找不到工程：$projectPath" }
if (!(Test-Path -LiteralPath $modSource)) { throw "找不到仓库 MOD 根目录：$modSource" }

$stageModInfo = Join-Path $modSource 'StageModInfo.xml'
if (!(Test-Path -LiteralPath $stageModInfo) -or
    (Get-Content -LiteralPath $stageModInfo -Raw) -notmatch "<ID>$([regex]::Escape($modPackageId))</ID>")
{
    throw '仓库 MOD 根目录的 StageModInfo.xml 校验失败。'
}

Push-Location -LiteralPath $repoRoot
try
{
    Write-Host "[1/6] 编译 $Configuration DLL..."
    & dotnet msbuild $redDiceProjectPath `
        -t:Build `
        "-p:Configuration=$Configuration" `
        -p:Platform=AnyCPU `
        -nologo `
        -v:minimal `
        -clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) { throw "DLL 编译失败，退出代码：$LASTEXITCODE" }
    if (!(Test-Path -LiteralPath $dllSource -PathType Leaf)) { throw "编译完成但找不到主 DLL：$dllSource" }
    if (!(Test-Path -LiteralPath $redDiceDllSource -PathType Leaf)) { throw "编译完成但找不到 RedDice 延迟 DLL：$redDiceDllSource" }

    $sourceHash = (Get-FileHash -LiteralPath $dllSource -Algorithm SHA256).Hash
    $redDiceHash = (Get-FileHash -LiteralPath $redDiceDllSource -Algorithm SHA256).Hash
    $legacyDll = Join-Path $modSource 'Assemblies\裁缝之王.dll'
    if (Test-Path -LiteralPath $legacyDll) { Remove-Item -LiteralPath $legacyDll -Force }

    $modDll = Join-Path $modSource 'Assemblies\XiXiFuSiBaiHuo.dll'
    $modRedDiceDll = Join-Path $modSource 'Assemblies\Deferred\XiXiFuSiBaiHuo.RedDice.dll'
    Write-Host '[2/6] 将 DLL 复制到仓库 MOD 根目录...'
    New-Item -ItemType Directory -Path (Split-Path -Parent $modDll) -Force | Out-Null
    New-Item -ItemType Directory -Path (Split-Path -Parent $modRedDiceDll) -Force | Out-Null
    Copy-Item -LiteralPath $dllSource -Destination $modDll -Force
    Copy-Item -LiteralPath $redDiceDllSource -Destination $modRedDiceDll -Force
    if ((Get-FileHash -LiteralPath $modDll -Algorithm SHA256).Hash -ne $sourceHash)
    {
        throw '复制到 MOD 根目录后的主 DLL 哈希校验失败。'
    }
    if ((Get-FileHash -LiteralPath $modRedDiceDll -Algorithm SHA256).Hash -ne $redDiceHash)
    {
        throw '复制到 MOD 根目录后的 RedDice 延迟 DLL 哈希校验失败。'
    }

    if ($BuildOnly)
    {
        Write-Host "BUILD OK：$modDll"
        Write-Host "BUILD OK：$modRedDiceDll"
        return
    }

    $gameProcs = @(Get-Process -Name LibraryOfRuina -ErrorAction SilentlyContinue)
    $activeGameProcs = @($gameProcs | Where-Object { -not $_.HasExited -and $_.Threads.Count -gt 0 })
    if ($activeGameProcs.Count -gt 0)
    {
        $pids = ($activeGameProcs | ForEach-Object { $_.Id }) -join ', '
        throw "LibraryOfRuina 正在运行 (PID: $pids)，已跳过部署以免占用 DLL。"
    }
    elseif ($gameProcs.Count -gt 0)
    {
        $zombiePids = ($gameProcs | ForEach-Object { $_.Id }) -join ', '
        Write-Warning "检测到已退出的 LibraryOfRuina 僵尸进程 (PID: $zombiePids, Threads=0)，已忽略并继续部署。"
    }

    $libraryRoots = Get-SteamLibraryRoots
    $workshopParent = Resolve-LibraryOfRuinaWorkshopRoot $WorkshopRoot $modDirectoryName $libraryRoots
    Write-Host "Workshop 根目录：$workshopParent"

    $modTarget = Join-Path $workshopParent $modDirectoryName
    $stagingTarget = Join-Path $workshopParent "$modDirectoryName.__deploy_staging"
    $backupTarget = Join-Path $workshopParent "$modDirectoryName.__deploy_backup"

    function Assert-WorkshopChild([string]$path, [string]$expectedName)
    {
        $fullPath = [IO.Path]::GetFullPath($path).TrimEnd('\')
        $parent = [IO.Path]::GetFullPath((Split-Path -Parent $fullPath)).TrimEnd('\')
        if ($parent -ne $workshopParent -or (Split-Path -Leaf $fullPath) -ne $expectedName)
        {
            throw "部署目录安全检查失败：$fullPath"
        }
    }

    Assert-WorkshopChild $modTarget $modDirectoryName
    Assert-WorkshopChild $stagingTarget "$modDirectoryName.__deploy_staging"
    Assert-WorkshopChild $backupTarget "$modDirectoryName.__deploy_backup"

    if (Test-Path -LiteralPath $backupTarget)
    {
        throw "检测到上次部署留下的备份目录，请先人工确认后处理：$backupTarget"
    }
    if (Test-Path -LiteralPath $stagingTarget)
    {
        Remove-Item -LiteralPath $stagingTarget -Recurse -Force
    }

    Write-Host '[3/6] 准备完整 MOD 临时目录...'
    New-Item -ItemType Directory -Path $stagingTarget | Out-Null
    Get-ChildItem -LiteralPath $modSource -Force |
        Copy-Item -Destination $stagingTarget -Recurse -Force

    $stagingDll = Join-Path $stagingTarget 'Assemblies\XiXiFuSiBaiHuo.dll'
    if (!(Test-Path -LiteralPath $stagingDll -PathType Leaf)) { throw '临时目录缺少 XiXiFuSiBaiHuo.dll。' }
    if ((Get-FileHash -LiteralPath $stagingDll -Algorithm SHA256).Hash -ne $sourceHash)
    {
        throw '临时目录中的 DLL 哈希校验失败。'
    }
    if (!(Test-Path -LiteralPath (Join-Path $stagingTarget 'StageModInfo.xml') -PathType Leaf))
    {
        throw '临时目录缺少 StageModInfo.xml。'
    }

    Write-Host '[4/6] 整体替换 Workshop MOD 目录...'
    $originalMoved = $false
    try
    {
        if (Test-Path -LiteralPath $modTarget)
        {
            Move-Item -LiteralPath $modTarget -Destination $backupTarget
            $originalMoved = $true
        }
        Move-Item -LiteralPath $stagingTarget -Destination $modTarget
    }
    catch
    {
        if (!(Test-Path -LiteralPath $modTarget) -and $originalMoved -and (Test-Path -LiteralPath $backupTarget))
        {
            Move-Item -LiteralPath $backupTarget -Destination $modTarget
        }
        throw
    }

    $deployedDll = Join-Path $modTarget 'Assemblies\XiXiFuSiBaiHuo.dll'
    if ((Get-FileHash -LiteralPath $deployedDll -Algorithm SHA256).Hash -ne $sourceHash)
    {
        throw '目录替换完成，但最终 DLL 哈希校验失败；旧目录仍保留在备份位置。'
    }

    Write-Host '[5/6] 清理部署备份...'
    if (Test-Path -LiteralPath $backupTarget)
    {
        Remove-Item -LiteralPath $backupTarget -Recurse -Force
    }

    Write-Host '[6/6] 检查并清理游戏根目录 Mods 中残余的同源 MOD...'
    Remove-ResidualGameRootMods $workshopParent $libraryRoots $modPackageId

    Write-Host "DEPLOY OK：$modTarget" -ForegroundColor Green
    Write-Host "DLL SHA256：$sourceHash"
}
finally
{
    Pop-Location
}

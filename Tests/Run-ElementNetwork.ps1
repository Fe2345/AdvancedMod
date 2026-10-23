param(
    [Parameter(Mandatory = $true)][string] $TmlDirectory,
    [int] $Port = 17777,
    [int] $TimeoutSeconds = 240
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$seed = Join-Path $projectRoot 'obj/element-live'
& dotnet build (Join-Path $projectRoot 'AdvancedMod.csproj') --no-restore -p:BuildMod=false -v:minimal
if ($LASTEXITCODE -ne 0) { throw '构建失败。' }
Push-Location $TmlDirectory
try {
    & dotnet tModLoader.dll -server -build $projectRoot -eac (Join-Path $projectRoot 'bin/Debug/net8.0/AdvancedMod.dll') -tmlsavedirectory $seed
    if ($LASTEXITCODE -ne 0) { throw '打包失败。' }
} finally { Pop-Location }
$runRoot = Join-Path $projectRoot ('obj/element-network/' + [DateTime]::Now.ToString('yyyyMMdd-HHmmss'))
$roles = @('server', 'client1', 'client2')
foreach ($role in $roles) {
    $save = Join-Path $runRoot $role
    New-Item -ItemType Directory -Path $save -Force | Out-Null
    foreach ($folder in @('Mods', 'Players', 'Worlds')) {
        Copy-Item -LiteralPath (Join-Path $seed $folder) -Destination $save -Recurse
    }
    Copy-Item -LiteralPath (Join-Path $seed 'config.json') -Destination $save
}
$processes = @()
$oldRole = $env:ADVANCEDMOD_ELEMENT_NET_TEST
try {
    foreach ($role in $roles) {
        $save = Join-Path $runRoot $role
        $env:ADVANCEDMOD_ELEMENT_NET_TEST = $role
        $arguments = @('tModLoader.dll', '-nosteam', '-tmlsavedirectory', ('"' + $save + '"'), '-port', $Port)
        if ($role -eq 'server') {
            $arguments += @('-server', '-world', ('"' + (Join-Path $save 'Worlds/testWorld.wld') + '"'), '-maxplayers', '8', '-password', 'element-test', '-noupnp')
        } else {
            $arguments += @('-join', '127.0.0.1', '-player', ('"' + (Join-Path $save 'Players/testPlayer.plr') + '"'), '-password', 'element-test')
        }
        $processes += Start-Process -FilePath 'dotnet' -ArgumentList $arguments -WorkingDirectory $TmlDirectory -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $save 'stdout.log') -RedirectStandardError (Join-Path $save 'stderr.log')
        if ($role -eq 'server') { Start-Sleep -Seconds 20 } else { Start-Sleep -Seconds 5 }
    }
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        Start-Sleep -Seconds 2
        $reports = @($roles | ForEach-Object { Join-Path $runRoot "$_/Logs/AdvancedMod/element-network-tests.json" } | Where-Object { Test-Path -LiteralPath $_ })
        if ($reports.Count -eq 3) { break }
        if (@($processes | Where-Object HasExited).Count -gt 0) { throw "测试进程提前退出：$runRoot" }
    } while ([DateTime]::UtcNow -lt $deadline)
    if ($reports.Count -ne 3) { throw "联机验收超时，收到 $($reports.Count)/3 份报告：$runRoot" }
    $failed = $false
    foreach ($path in $reports) {
        $report = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
        Write-Output $report.role
        $report.results | Format-Table name, status
        if (@($report.results | Where-Object status -ne 'PASS').Count -gt 0) { $failed = $true }
    }
    if ($failed) { throw "联机验收失败：$runRoot" }
    Write-Output "联机验收通过：$runRoot"
} finally {
    $env:ADVANCEDMOD_ELEMENT_NET_TEST = $oldRole
    foreach ($process in $processes) {
        if (-not $process.HasExited) { Stop-Process -Id $process.Id }
    }
    # SubworldLibrary 可能派生专用服务器辅助进程，只清理本轮隔离目录对应的进程。
    Get-CimInstance Win32_Process -Filter "name = 'dotnet.exe'" | Where-Object {
        $_.CommandLine -and $_.CommandLine.Contains($runRoot)
    } | ForEach-Object { Stop-Process -Id $_.ProcessId -ErrorAction SilentlyContinue }
}

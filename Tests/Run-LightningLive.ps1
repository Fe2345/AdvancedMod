# 在项目obj隔离存档中运行真实客户端，保留闪电截图和本轮报告。
param([string]$TmlDirectory = 'D:/Program Files (x86)/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$save = Join-Path $root 'obj/lightning-live'
if (-not (Test-Path -LiteralPath $save)) {
    New-Item -ItemType Directory -Path $save | Out-Null
    foreach ($name in @('Players','Worlds','Mods')) { Copy-Item -LiteralPath (Join-Path $root "obj/element-live/$name") -Destination $save -Recurse }
    Copy-Item -LiteralPath (Join-Path $root 'obj/element-live/config.json') -Destination $save
}
& dotnet build (Join-Path $root 'AdvancedMod.csproj') -p:BuildMod=false -p:NuGetAudit=false --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw '主项目构建失败' }
$started = [DateTime]::UtcNow
$previous = $env:ADVANCEDMOD_LIGHTNING_TEST
Push-Location $TmlDirectory
try {
    & dotnet tModLoader.dll -server -build $root -eac (Join-Path $root 'bin/Debug/net8.0/AdvancedMod.dll') -tmlsavedirectory $save
    if ($LASTEXITCODE -ne 0) { throw '隔离打包失败' }
    $env:ADVANCEDMOD_LIGHTNING_TEST = '1'
    & dotnet tModLoader.dll -nosteam -tmlsavedirectory $save -skipselect 'testPlayer:testWorld'
    if ($LASTEXITCODE -ne 0) { throw '客户端退出失败' }
} finally { $env:ADVANCEDMOD_LIGHTNING_TEST = $previous; Pop-Location }
$report = Join-Path $save 'LightningCaptures/report.json'
if (-not (Test-Path -LiteralPath $report) -or (Get-Item -LiteralPath $report).LastWriteTimeUtc -lt $started) { throw '缺少本轮报告' }
$results = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
$results | Format-Table name,status
if (@($results | Where-Object status -ne 'PASS').Count -gt 0 -or $results.Count -ne 11) { throw '实机闪电验证失败或截图不完整' }
$regressionPath = Join-Path $save 'Logs/AdvancedMod/element-live-tests.json'
if (-not (Test-Path -LiteralPath $regressionPath) -or (Get-Item -LiteralPath $regressionPath).LastWriteTimeUtc -lt $started) { throw '缺少本轮元素回归报告' }
$regression = Get-Content -LiteralPath $regressionPath -Raw | ConvertFrom-Json
if ($regression.results.Count -ne 15 -or @($regression.results | Where-Object status -ne 'PASS').Count -gt 0) { throw '元素实机回归失败' }
Write-Output '元素实机回归15/15通过；截图需要另行检查。'

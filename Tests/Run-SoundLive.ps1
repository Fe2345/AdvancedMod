param(
    [string] $TmlDirectory = 'D:\Program Files (x86)\Steam\steamapps\common\tModLoader',
    [string] $TestSaveDirectory = (Join-Path $PSScriptRoot '../obj/sound-live')
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$isolatedRoot = [IO.Path]::GetFullPath($TestSaveDirectory)
$allowedRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'obj')) + [IO.Path]::DirectorySeparatorChar
if (-not $isolatedRoot.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw '自动视觉验收只允许使用项目 obj 下面的隔离存档。'
}
foreach ($required in @('Players/testPlayer.plr', 'Worlds/testWorld.wld', 'Mods/enabled.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $isolatedRoot $required))) { throw "缺少隔离测试文件：$required" }
}
& dotnet build (Join-Path $projectRoot 'AdvancedMod.csproj') -p:BuildMod=false --no-restore -v:minimal
if ($LASTEXITCODE -ne 0) { throw '主项目构建失败。' }
Push-Location $TmlDirectory
$previousMode = $env:ADVANCEDMOD_SOUND_LIVE_TEST
$started = [DateTime]::UtcNow
try {
    & dotnet tModLoader.dll -server -build $projectRoot -eac (Join-Path $projectRoot 'bin/Debug/net8.0/AdvancedMod.dll') -tmlsavedirectory $isolatedRoot
    if ($LASTEXITCODE -ne 0) { throw '模组打包失败。' }
    $env:ADVANCEDMOD_SOUND_LIVE_TEST = '1'
    # 当前进程保持运行以便观察游戏窗口，完成演示后自动退出且不保存隔离存档。
    & dotnet tModLoader.dll -nosteam -tmlsavedirectory $isolatedRoot -skipselect 'testPlayer:testWorld'
    if ($LASTEXITCODE -ne 0) { throw '客户端异常退出。' }
} finally {
    $env:ADVANCEDMOD_SOUND_LIVE_TEST = $previousMode
    Pop-Location
}
$reportPath = Join-Path $isolatedRoot 'SoundCaptures/report.json'
if (-not (Test-Path -LiteralPath $reportPath) -or (Get-Item -LiteralPath $reportPath).LastWriteTimeUtc -lt $started) {
    throw '本轮没有新的截图报告，不能沿用旧结果；请保持测试窗口焦点。'
}
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
$report.results | Format-Table name,status
if (@($report.results | Where-Object status -ne 'PASS').Count -gt 0) { throw "声波实机测试失败：$reportPath" }
if ($report.captures.Count -ne 6 -or @($report.captures | Where-Object status -ne 'CAPTURED').Count -gt 0) { throw "未取得六种场景的全部截图：$reportPath" }
Write-Output "声波真实波包交互测试通过，截图需人工检查：$reportPath"



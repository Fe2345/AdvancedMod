param(
    [Parameter(Mandatory = $true)][string] $TmlDirectory,
    [Parameter(Mandatory = $true)][string] $TestSaveDirectory
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$isolatedRoot = [IO.Path]::GetFullPath($TestSaveDirectory)
$allowedRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'obj')) + [IO.Path]::DirectorySeparatorChar
if (-not $isolatedRoot.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw '自动验收只允许使用当前项目 obj 下面的隔离存档目录。'
}
foreach ($required in @('Players/testPlayer.plr', 'Worlds/testWorld.wld', 'Mods/enabled.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $isolatedRoot $required))) { throw "缺少隔离测试文件：$required" }
}
$reportPath = Join-Path $isolatedRoot 'Logs/AdvancedMod/element-live-tests.json'
$started = [DateTime]::UtcNow
& dotnet build (Join-Path $projectRoot 'AdvancedMod.csproj') -p:BuildMod=false --no-restore -v:minimal
if ($LASTEXITCODE -ne 0) { throw '主项目构建失败。' }
Push-Location $TmlDirectory
$previousMode = $env:ADVANCEDMOD_ELEMENT_LIVE_TEST
try {
    & dotnet tModLoader.dll -server -build $projectRoot -eac (Join-Path $projectRoot 'bin/Debug/net8.0/AdvancedMod.dll') -tmlsavedirectory $isolatedRoot
    if ($LASTEXITCODE -ne 0) { throw '模组打包失败。' }
    $env:ADVANCEDMOD_ELEMENT_LIVE_TEST = '1'
    & dotnet tModLoader.dll -nosteam -tmlsavedirectory $isolatedRoot -skipselect 'testPlayer:testWorld'
    if ($LASTEXITCODE -ne 0) { throw '客户端异常退出。' }
} finally {
    $env:ADVANCEDMOD_ELEMENT_LIVE_TEST = $previousMode
    Pop-Location
}
if (-not (Test-Path -LiteralPath $reportPath) -or (Get-Item -LiteralPath $reportPath).LastWriteTimeUtc -lt $started) {
    throw '本轮没有生成新的实机测试报告，不能沿用旧结果。'
}
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
$report.results | Format-Table name, status
if (@($report.results | Where-Object status -ne 'PASS').Count -gt 0) { throw "实机测试有失败项：$reportPath" }
Write-Output "实机验收通过：$reportPath"

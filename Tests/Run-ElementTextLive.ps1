# 2026-10-26：在项目 obj 隔离存档中验证元素文字并保留截图。
param([string]$TmlDirectory = 'D:/Program Files (x86)/Steam/steamapps/common/tModLoader')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$save = Join-Path $root 'obj/element-text-live'
if (-not (Test-Path -LiteralPath $save)) {
    New-Item -ItemType Directory -Path $save | Out-Null
    foreach ($name in @('Players','Worlds','Mods')) { Copy-Item -LiteralPath (Join-Path $root "obj/element-live/$name") -Destination $save -Recurse }
    Copy-Item -LiteralPath (Join-Path $root 'obj/element-live/config.json') -Destination $save
}
$configPath = Join-Path $save 'config.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$config.Language = 'zh-Hans'
$config.ThrottleWhenInactive = $false
$config.DisplayWidth = 1280; $config.DisplayHeight = 900; $config.UIScale = 1.0; $config.Zoom = 1.0
$config | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $configPath -Encoding utf8
& dotnet build (Join-Path $root 'AdvancedMod.csproj') -p:BuildMod=false -p:NuGetAudit=false --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw '主项目构建失败' }
$started = [DateTime]::UtcNow
$previous = $env:ADVANCEDMOD_ELEMENT_TEXT_TEST
Push-Location $TmlDirectory
try {
    & dotnet tModLoader.dll -server -build $root -eac (Join-Path $root 'bin/Debug/net8.0/AdvancedMod.dll') -tmlsavedirectory $save
    if ($LASTEXITCODE -ne 0) { throw '隔离打包失败' }
    $env:ADVANCEDMOD_ELEMENT_TEXT_TEST = '1'
    & dotnet tModLoader.dll -nosteam -tmlsavedirectory $save -skipselect 'testPlayer:testWorld'
    if ($LASTEXITCODE -ne 0) { throw '客户端退出失败' }
} finally { $env:ADVANCEDMOD_ELEMENT_TEXT_TEST = $previous; Pop-Location }
$report = Join-Path $save 'ElementTextCaptures/report.json'
if (-not (Test-Path -LiteralPath $report) -or (Get-Item -LiteralPath $report).LastWriteTimeUtc -lt $started) { throw '缺少本轮报告' }
$results = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
$results | Format-Table name,status
if (@($results | Where-Object status -ne 'PASS').Count -gt 0) { throw '实机文字验证失败' }

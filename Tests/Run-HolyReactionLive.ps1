# 使用隔离存档执行神圣反应数值与真实引擎结算验收。
param([string]$TmlDirectory='D:/Program Files (x86)/Steam/steamapps/common/tModLoader')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$save=Join-Path $root "obj/holy-live"
if(-not(Test-Path -LiteralPath $save)) {
    New-Item -ItemType Directory -Path $save | Out-Null
    foreach($name in @('Players','Worlds','Mods','config.json')) { Copy-Item -LiteralPath (Join-Path $root "obj/element-live/$name") -Destination $save -Recurse }
}
$configPath=Join-Path $save 'config.json'
$config=Get-Content $configPath -Raw | ConvertFrom-Json
$config.DisplayWidth=1280; $config.DisplayHeight=900; $config.UIScale=1.0; $config.Language='zh-Hans'
$config.ThrottleWhenInactive=$false
$config | ConvertTo-Json -Depth 30 | Set-Content $configPath -Encoding utf8
& dotnet build (Join-Path $root 'AdvancedMod.csproj') -p:BuildMod=false -p:NuGetAudit=false --no-restore --nologo -m:1 -nr:false
if($LASTEXITCODE -ne 0){throw '构建失败'}
$old=$env:ADVANCEDMOD_HOLY_TEST
$started=[DateTime]::UtcNow
Push-Location $TmlDirectory
try {
    & dotnet tModLoader.dll -server -build $root -eac (Join-Path $root 'bin/Debug/net8.0/AdvancedMod.dll') -tmlsavedirectory $save
    if($LASTEXITCODE -ne 0){throw '打包失败'}
    $env:ADVANCEDMOD_HOLY_TEST='1'
    & dotnet tModLoader.dll -nosteam -tmlsavedirectory $save -skipselect 'testPlayer:testWorld'
    if($LASTEXITCODE -ne 0){throw '客户端退出失败'}
} finally { $env:ADVANCEDMOD_HOLY_TEST=$old; Pop-Location }
$report=Join-Path $save 'Logs/AdvancedMod/holy-live.json'
if(-not(Test-Path -LiteralPath $report) -or (Get-Item -LiteralPath $report).LastWriteTimeUtc -lt $started){throw '缺少本轮报告'}
$data=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
$data.results | Format-Table name,status
if(@($data.results | Where-Object status -ne 'PASS').Count -gt 0){throw '神圣反应实机验收失败'}

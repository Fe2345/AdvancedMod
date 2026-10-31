# 在隔离真实客户端中执行毁灭者二阶段与探测怪元素验收。
param( [string]$TmlDirectory='D:/Program Files (x86)/Steam/steamapps/common/tModLoader')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$save=Join-Path $root 'obj/destroyer-phase2-live'
if(-not(Test-Path -LiteralPath $save)) {
    New-Item -ItemType Directory -Path $save | Out-Null
    foreach($name in @('Players','Worlds','Mods','config.json')) { Copy-Item -LiteralPath (Join-Path $root "obj/element-live/$name") -Destination $save -Recurse }
}
& dotnet build (Join-Path $root 'AdvancedMod.csproj') -p:BuildMod=false -p:NuGetAudit=false --no-restore --nologo -m:1 -nr:false
if($LASTEXITCODE -ne 0){throw '构建失败'}
$old=$env:ADVANCEDMOD_DESTROYER_PHASE2_TEST
$started=[DateTime]::UtcNow
Push-Location $TmlDirectory
try {
    & dotnet tModLoader.dll -server -build $root -eac (Join-Path $root 'bin/Debug/net8.0/AdvancedMod.dll') -tmlsavedirectory $save
    if($LASTEXITCODE -ne 0){throw '打包失败'}
    $env:ADVANCEDMOD_DESTROYER_PHASE2_TEST='1'
    & dotnet tModLoader.dll -nosteam -tmlsavedirectory $save -skipselect 'testPlayer:testWorld'
    if($LASTEXITCODE -ne 0){throw '客户端退出失败'}
} finally { $env:ADVANCEDMOD_DESTROYER_PHASE2_TEST=$old; Pop-Location }
$report=Join-Path $save 'PhaseTwoCaptures/report.json'
if(-not(Test-Path -LiteralPath $report) -or (Get-Item -LiteralPath $report).LastWriteTimeUtc -lt $started){throw '缺少本轮报告'}
$data=Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
$data.results | Format-Table name,status,detail
if(@($data.results | Where-Object status -ne 'PASS').Count -gt 0){throw '毁灭者二阶段实机验收失败'}

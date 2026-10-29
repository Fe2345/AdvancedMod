# 2026-10-27：独立测试存档中的水体最小接入；不改用户正常存档。
param([string]$TmlDirectory='D:/Program Files (x86)/Steam/steamapps/common/tModLoader')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$save=Join-Path $root 'obj/water-live'
if(-not(Test-Path -LiteralPath $save)) {
    New-Item -ItemType Directory $save | Out-Null
    foreach($name in @('Players','Worlds','Mods','config.json')) {Copy-Item -LiteralPath (Join-Path $root "obj/lightning-live/$name") -Destination $save -Recurse}
}
& dotnet build (Join-Path $root 'AdvancedMod.csproj') -p:BuildMod=false -p:NuGetAudit=false -m:1 -nr:false --nologo
if($LASTEXITCODE -ne 0){throw '模组构建失败'}
$previous=$env:ADVANCEDMOD_WATER_TEST
$started=[DateTime]::UtcNow
Push-Location $TmlDirectory
try {
    & dotnet tModLoader.dll -server -build $root -eac (Join-Path $root 'bin/Debug/net8.0/AdvancedMod.dll') -tmlsavedirectory $save
    if($LASTEXITCODE -ne 0){throw '打包失败'}
    $env:ADVANCEDMOD_WATER_TEST='1'
    & dotnet tModLoader.dll -nosteam -tmlsavedirectory $save -skipselect 'testPlayer:testWorld'
    if($LASTEXITCODE -ne 0){throw '客户端失败'}
} finally {$env:ADVANCEDMOD_WATER_TEST=$previous;Pop-Location}
if(-not(Test-Path -LiteralPath (Join-Path $save 'WaterCaptures/fusion.png'))){throw '未生成截图'}
if((Get-Item -LiteralPath (Join-Path $save 'WaterCaptures/fusion.png')).LastWriteTimeUtc -lt $started){throw '仅有旧截图，本轮未验证'}

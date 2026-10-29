# 独立复制已有测试存档；仅在本次客户端进程启用贴图验收。
param([string]$TmlDirectory='D:/Program Files (x86)/Steam/steamapps/common/tModLoader')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$save=Join-Path $root 'obj/water-sprite-live'
if(-not(Test-Path -LiteralPath $save)) {
    New-Item -ItemType Directory $save | Out-Null
    foreach($name in @('Players','Worlds','Mods','config.json')) {
        Copy-Item -LiteralPath (Join-Path $root "obj/water-live/$name") -Destination $save -Recurse
    }
}
$configPath=Join-Path $save 'config.json'
$config=Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$config.DisplayWidth=1280
$config.DisplayHeight=900
$config.Fullscreen=$false
$config.WindowMaximized=$false
$config | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $configPath -Encoding utf8
& dotnet build (Join-Path $root 'AdvancedMod.csproj') -p:BuildMod=false -p:NuGetAudit=false -m:1 -nr:false --nologo
if($LASTEXITCODE -ne 0){throw '模组构建失败'}
$previous=$env:ADVANCEDMOD_WATER_SPRITE_TEST
$started=[DateTime]::UtcNow
Push-Location $TmlDirectory
try {
    & dotnet tModLoader.dll -server -build $root -eac (Join-Path $root 'bin/Debug/net8.0/AdvancedMod.dll') -tmlsavedirectory $save
    if($LASTEXITCODE -ne 0){throw '打包失败'}
    $env:ADVANCEDMOD_WATER_SPRITE_TEST=Join-Path $root 'docs/assets/water-sprite-top/WaterTornado_24_grid.png'
    & dotnet tModLoader.dll -nosteam -tmlsavedirectory $save -skipselect 'testPlayer:testWorld'
    if($LASTEXITCODE -ne 0){throw '客户端失败'}
} finally {$env:ADVANCEDMOD_WATER_SPRITE_TEST=$previous;Pop-Location}
$capture=Join-Path $save 'WaterSpriteCaptures/comparison-1.png'
if(-not(Test-Path -LiteralPath $capture)){throw '未生成第二张截图'}
if((Get-Item -LiteralPath $capture).LastWriteTimeUtc -lt $started){throw '仅有旧截图'}
$evidence=Join-Path $root 'docs/testing/water-sprite'
New-Item -ItemType Directory -Force $evidence | Out-Null
Copy-Item -Path (Join-Path $save 'WaterSpriteCaptures/*') -Destination $evidence -Force

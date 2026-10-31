# 2026-10-30：使用既有隔离测试存档，构建、打包并运行风刃 GPU 采集。
param([string]$TmlDirectory='D:/Program Files (x86)/Steam/steamapps/common/tModLoader')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$save=Join-Path $root 'obj/idyllic-live'
if(-not(Test-Path -LiteralPath $save)) {
 New-Item -ItemType Directory -Path $save | Out-Null
 foreach($name in @('Players','Worlds','Mods','config.json')){Copy-Item -LiteralPath (Join-Path $root "obj/element-live/$name") -Destination $save -Recurse}
}
$configPath=Join-Path $save 'config.json'
$config=Get-Content $configPath -Raw | ConvertFrom-Json
$config.DisplayWidth=1280;$config.DisplayHeight=900;$config.UIScale=1.0;$config.ThrottleWhenInactive=$false
$config | ConvertTo-Json -Depth 30 | Set-Content $configPath -Encoding utf8
& dotnet build (Join-Path $root 'AdvancedMod.csproj') -p:BuildMod=false --no-restore -m:1 -nr:false
if($LASTEXITCODE -ne 0){throw '构建失败'}
$old=$env:ADVANCEDMOD_IDYLLIC_TEST
$started=[DateTime]::UtcNow
Push-Location $TmlDirectory
try{
 & dotnet tModLoader.dll -server -build $root -eac (Join-Path $root 'bin/Debug/net8.0/AdvancedMod.dll') -tmlsavedirectory $save
 if($LASTEXITCODE -ne 0){throw '打包失败'}
 $env:ADVANCEDMOD_IDYLLIC_TEST='1'
 & dotnet tModLoader.dll -nosteam -tmlsavedirectory $save -skipselect 'testPlayer:testWorld'
 if($LASTEXITCODE -ne 0){throw '客户端失败'}
}finally{$env:ADVANCEDMOD_IDYLLIC_TEST=$old;Pop-Location}
$report=Join-Path $save 'IdyllicCaptures/report.json'
if(-not(Test-Path -LiteralPath $report) -or (Get-Item -LiteralPath $report).LastWriteTimeUtc -lt $started){throw '无本轮报告，请检查 IdyllicCaptures/failure.txt 与客户端日志'}
Copy-Item (Join-Path $save 'IdyllicCaptures/*') (Join-Path $root 'docs/testing/idyllic') -Force

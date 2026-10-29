# 2026-10-27：从任意目录构建并启动共享核心的 FNA 水体实验。
param([switch]$Capture,[switch]$Vortex,[switch]$Test)
$ErrorActionPreference='Stop'
Push-Location (Join-Path $PSScriptRoot '../..')
try {
    & python Tools/ShaderBytecode.py compile Assets/Shaders/WaterVortex.fx Assets/Shaders/WaterVortex.xnb
    if($LASTEXITCODE -ne 0) {throw 'Effect 编译失败'}
    & dotnet build Tools/WaterPreview -c Release -p:NuGetAudit=false -m:1 -nr:false --nologo
    if($LASTEXITCODE -ne 0) {throw '预览器构建失败'}
    $arguments=@('Tools/WaterPreview/bin/Release/net8.0/WaterPreview.dll','--shaded')
    if($Capture) {$arguments+='--capture'}
    if($Vortex) {$arguments+='--vortex'}
    if($Test) {$arguments+='--test'}
    & dotnet @arguments
    if($LASTEXITCODE -ne 0) {throw '预览或测试失败'}
} finally {Pop-Location}

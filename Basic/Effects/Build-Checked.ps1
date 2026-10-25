# 2026-10-25：强制重建并执行分析器；可选将已检查的程序集交给 tML 打包。
param([switch]$Package)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '../../AdvancedMod.csproj'
$packageValue = if ($Package) { 'true' } else { 'false' }
& dotnet build $project -t:Rebuild "-p:BuildMod=$packageValue" -p:RunAnalyzers=true -p:RunAnalyzersDuringBuild=true -p:NuGetAudit=false --nologo
if ($LASTEXITCODE -ne 0) { throw "效果检查构建失败，退出码 $LASTEXITCODE" }

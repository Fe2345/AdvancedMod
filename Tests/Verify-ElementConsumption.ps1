$ErrorActionPreference = 'Stop'
$fixtureProject = Join-Path $PSScriptRoot 'ElementConsumption/ElementConsumption.csproj'
& dotnet restore $fixtureProject -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { throw '验证工程依赖恢复失败。' }
foreach ($symbol in @('ZERO_LITERAL', 'ZERO_CONSTANT', 'ZERO_DEFAULT')) {
    $buildOutput = & dotnet build $fixtureProject --no-restore "-p:DefineConstants=$symbol" -v:minimal 2>&1
    $buildExitCode = $LASTEXITCODE
    if ($buildExitCode -eq 0 -or ($buildOutput -join "`n") -notmatch 'error ADVCalc002') {
        throw "未获得预期的 ADVCalc002 编译错误：$symbol`n$buildOutput"
    }
    Write-Output "通过：$symbol 在编译期被拒绝。"
}
& dotnet run --project $fixtureProject --no-restore '-p:DefineConstants='
if ($LASTEXITCODE -ne 0) { throw '修正字段正例验证失败。' }

$ErrorActionPreference = 'Stop'
$fixtureProject = Join-Path $PSScriptRoot 'ContributorIds/ContributorIds.csproj'

function Invoke-FixtureBuild([string] $symbols, [string] $expectedError) {
    $buildOutput = & dotnet build $fixtureProject --no-restore "-p:DefineConstants=$symbols" -v:minimal 2>&1
    $buildExitCode = $LASTEXITCODE
    $buildText = $buildOutput -join [Environment]::NewLine
    if ($expectedError) {
        if ($buildExitCode -eq 0 -or $buildText -notmatch "error $expectedError") {
            throw "未获得预期的编译错误 $expectedError。`n$buildText"
        }
        Write-Output "通过：$symbols 导致编译失败（$expectedError）。"
    } elseif ($buildExitCode -ne 0) {
        throw "唯一 ID 的构建失败。`n$buildText"
    }
}

& dotnet restore $fixtureProject -p:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { throw '验证工程依赖恢复失败。' }

Invoke-FixtureBuild 'DUPLICATE_IDS' 'ADVCalc001'
Invoke-FixtureBuild 'DUPLICATE_GENERATED_ID' 'ADVCalc001'
Invoke-FixtureBuild 'NON_CONSTANT_ID' 'CS0182'
# 最后恢复无冲突构建，避免后续运行误用负例的输出。
Invoke-FixtureBuild '' ''
& dotnet run --project $fixtureProject --no-build --no-restore
if ($LASTEXITCODE -ne 0) { throw '特性运行时读取验证失败。' }

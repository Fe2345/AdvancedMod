# 2026-10-25：验证合法共享节点成功，以及非法声明实际阻止 dotnet 构建。
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'BuildProbe/BuildProbe.csproj'
foreach ($case in @('DIAMOND', 'SELF', 'CYCLE', 'BAD_TARGET', 'BAD_WEIGHT')) {
    $output = & dotnet build $project -t:Rebuild "-p:DefineConstants=$case" -p:RunAnalyzers=true -p:RunAnalyzersDuringBuild=true -p:NuGetAudit=false --nologo 2>&1
    $exitCode = $LASTEXITCODE
    $expected = if ($case -in @('SELF', 'CYCLE')) { 'ADVEFF002' } else { 'ADVEFF001' }
    if ($case -eq 'DIAMOND') {
        if ($exitCode -ne 0) { throw ($output -join "`n") }
    } elseif ($exitCode -eq 0 -or ($output -join "`n") -notmatch "error $expected") {
        throw "未出现预期编译失败 $case / $expected : $($output -join "`n")"
    }
    Write-Output "PASS $case (exit=$exitCode)"
}

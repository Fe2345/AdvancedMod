# 在独立存档执行持续伤害数值与跳字验收，不部署日常 Mods。
param([string]$TmlDirectory = 'D:/Program Files (x86)/Steam/steamapps/common/tModLoader', [int]$TimeoutSeconds = 180, [switch]$Headless)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$save = Join-Path $root 'obj/dot-live'
if (-not (Test-Path -LiteralPath $save)) {
    New-Item -ItemType Directory -Path $save | Out-Null
    foreach ($name in @('Players', 'Worlds', 'Mods', 'config.json')) {
        Copy-Item -LiteralPath (Join-Path $root "obj/element-live/$name") -Destination $save -Recurse
    }
}
$configPath = Join-Path $save 'config.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$config.AutoSave = $false; $config.ThrottleWhenInactive = $false; $config.Language = 'zh-Hans'
$config | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $configPath -Encoding utf8
& dotnet build (Join-Path $root 'AdvancedMod.csproj') -p:BuildMod=false --no-restore -m:1 -nr:false
if ($LASTEXITCODE -ne 0) { throw '主项目构建失败' }
$started = [DateTime]::UtcNow
$previous = $env:ADVANCEDMOD_DOT_TEST
$process = $null
Push-Location $TmlDirectory
try {
    & dotnet tModLoader.dll -server -build $root -eac (Join-Path $root 'bin/Debug/net8.0/AdvancedMod.dll') -tmlsavedirectory $save
    if ($LASTEXITCODE -ne 0) { throw '隔离打包失败' }
    $env:ADVANCEDMOD_DOT_TEST = '1'
    $arguments = @('tModLoader.dll', '-nosteam', '-tmlsavedirectory', ('"' + $save + '"'), '-skipselect', 'testPlayer:testWorld')
    if ($Headless) {
        $arguments = @('tModLoader.dll', '-server', '-nosteam', '-tmlsavedirectory', ('"' + $save + '"'), '-world', ('"' + (Join-Path $save 'Worlds/testWorld.wld') + '"'), '-port', '17786', '-maxplayers', '2')
    }
    $process = Start-Process -FilePath 'dotnet' -ArgumentList $arguments -WorkingDirectory $TmlDirectory -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $save 'stdout.log') -RedirectStandardError (Join-Path $save 'stderr.log')
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) { throw '隔离客户端验收超时' }
    if ($process.ExitCode -ne 0) { throw "隔离客户端退出码：$($process.ExitCode)" }
} finally {
    if ($null -ne $process -and -not $process.HasExited) { Stop-Process -Id $process.Id }
    $env:ADVANCEDMOD_DOT_TEST = $previous
    Pop-Location
}
$report = Join-Path $save 'Logs/AdvancedMod/dot-live.json'
if (-not (Test-Path -LiteralPath $report) -or (Get-Item -LiteralPath $report).LastWriteTimeUtc -lt $started) { throw '缺少本轮验收报告' }
$data = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
$data.results | Format-Table name, status
if (@($data.results | Where-Object status -ne 'PASS').Count -gt 0) { throw '持续伤害实机验收失败' }

# 真实服务器、首客户端、初始同步后加入的第二客户端；仅使用隔离存档。
param([string]$TmlDirectory='D:/Program Files (x86)/Steam/steamapps/common/tModLoader',[int]$Port=17784,[int]$InspectSeconds=0,[switch]$SkipBuild)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$seed=Join-Path $root 'obj/holy-live'
if(-not $SkipBuild){
    & dotnet build (Join-Path $root 'AdvancedMod.csproj') --no-restore -p:BuildMod=false -v:minimal
    if($LASTEXITCODE -ne 0){throw '构建失败'}
    Push-Location $TmlDirectory
    try {
        & dotnet tModLoader.dll -server -build $root -eac (Join-Path $root 'bin/Debug/net8.0/AdvancedMod.dll') -tmlsavedirectory $seed
        if($LASTEXITCODE -ne 0){throw '打包失败'}
    } finally {Pop-Location}
}
$run=Join-Path $root ('obj/holy-network/'+[DateTime]::Now.ToString('yyyyMMdd-HHmmss'))
$roles=@('server','client1','client2')
foreach($role in $roles){
    $save=Join-Path $run $role
    New-Item -ItemType Directory -Path $save -Force | Out-Null
    foreach($name in @('Players','Worlds','Mods','config.json')){Copy-Item -LiteralPath (Join-Path $seed $name) -Destination $save -Recurse}
    $configPath=Join-Path $save 'config.json'
    $config=Get-Content $configPath -Raw | ConvertFrom-Json
    $config.ThrottleWhenInactive=$false
    $config | ConvertTo-Json -Depth 30 | Set-Content $configPath -Encoding utf8
}
$processes=@(); $old=$env:ADVANCEDMOD_HOLY_NET_TEST; $oldRun=$env:ADVANCEDMOD_HOLY_NET_RUN
$env:ADVANCEDMOD_HOLY_NET_RUN=$run
try {
    foreach($role in $roles){
        if($role -eq 'client2'){
            $deadline=[DateTime]::UtcNow.AddSeconds(150)
            $ready=Join-Path $run 'client1/HolyFieldCaptures/ready.txt'
            while(-not(Test-Path $ready) -and [DateTime]::UtcNow -lt $deadline){Start-Sleep -Seconds 2}
            if(-not(Test-Path $ready)){throw "首次同步超时：$run"}
        }
        $save=Join-Path $run $role
        $env:ADVANCEDMOD_HOLY_NET_TEST=$role
        $launchArgs=@('tModLoader.dll','-nosteam','-tmlsavedirectory',('"'+$save+'"'),'-port',$Port)
        if($role -eq 'server'){$launchArgs+=@('-server','-world',('"'+(Join-Path $save 'Worlds/testWorld.wld')+'"'),'-maxplayers','8','-password','holy-test','-noupnp')}
        else{$launchArgs+=@('-join','127.0.0.1','-player',('"'+(Join-Path $save 'Players/testPlayer.plr')+'"'),'-password','holy-test')}
        $processes+=Start-Process dotnet -ArgumentList $launchArgs -WorkingDirectory $TmlDirectory -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $save 'stdout.log') -RedirectStandardError (Join-Path $save 'stderr.log')
        if($role -eq 'server'){Start-Sleep -Seconds 15}
    }
    $deadline=[DateTime]::UtcNow.AddSeconds(150)
    do {
        Start-Sleep -Seconds 2
        $reports=@($roles | ForEach-Object {Join-Path $run "$_/HolyFieldCaptures/report.json"} | Where-Object {Test-Path $_})
    } while($reports.Count -lt 3 -and [DateTime]::UtcNow -lt $deadline)
    if($reports.Count -ne 3){throw "联机报告超时：$run"}
    foreach($path in $reports){$r=Get-Content $path -Raw | ConvertFrom-Json; $r | ConvertTo-Json -Depth 6; if(@($r.results | Where-Object status -ne 'PASS').Count -gt 0){throw "联机失败：$path"}}
    Write-Output "联机通过：$run"
    if($InspectSeconds -gt 0){Start-Sleep -Seconds $InspectSeconds}
} finally {
    $env:ADVANCEDMOD_HOLY_NET_TEST=$old
    $env:ADVANCEDMOD_HOLY_NET_RUN=$oldRun
    foreach($process in $processes){if(-not $process.HasExited){Stop-Process -Id $process.Id}}
}


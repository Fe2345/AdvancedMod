# 真实服务端、召唤客户端、召唤完成后加入的客户端；所有存档均隔离。
param([string]$TmlDirectory='D:/Program Files (x86)/Steam/steamapps/common/tModLoader',[int]$Port=17781)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$seed=Join-Path $root 'obj/mechanical-state-live'
& dotnet build (Join-Path $root 'AdvancedMod.csproj') -p:BuildMod=false -p:NuGetAudit=false --no-restore --nologo -m:1 -nr:false
if($LASTEXITCODE -ne 0){throw '构建失败'}
Push-Location $TmlDirectory
try { & dotnet tModLoader.dll -server -build $root -eac (Join-Path $root 'bin/Debug/net8.0/AdvancedMod.dll') -tmlsavedirectory $seed; if($LASTEXITCODE -ne 0){throw '打包失败'} } finally {Pop-Location}
$run=Join-Path $root ('obj/mechanical-network/'+[DateTime]::Now.ToString('yyyyMMdd-HHmmss'))
$roles=@('server','client1','client2')
foreach($role in $roles){
    $save=Join-Path $run $role
    New-Item -ItemType Directory -Path $save -Force | Out-Null
    foreach($name in @('Players','Worlds','Mods','config.json')){ Copy-Item -LiteralPath (Join-Path $seed $name) -Destination $save -Recurse }
}
$processes=@()
$old=$env:ADVANCEDMOD_MECHANICAL_NET_TEST
try {
    foreach($role in $roles){
        if($role -eq 'client2'){
            $deadline=[DateTime]::UtcNow.AddSeconds(150)
            $first=Join-Path $run 'client1/MechanicalCaptures/network-report.json'
            while(-not(Test-Path $first) -and [DateTime]::UtcNow -lt $deadline){Start-Sleep -Seconds 2}
            if(-not(Test-Path $first)){throw "首客户端超时：$run"}
        }
        $save=Join-Path $run $role
        $env:ADVANCEDMOD_MECHANICAL_NET_TEST=$role
        $args=@('tModLoader.dll','-nosteam','-tmlsavedirectory',('"'+$save+'"'),'-port',$Port)
        if($role -eq 'server'){$args+=@('-server','-world',('"'+(Join-Path $save 'Worlds/testWorld.wld')+'"'),'-maxplayers','8','-password','mechanical-test','-noupnp')}
        else{$args+=@('-join','127.0.0.1','-player',('"'+(Join-Path $save 'Players/testPlayer.plr')+'"'),'-password','mechanical-test')}
        $processes+=Start-Process dotnet -ArgumentList $args -WorkingDirectory $TmlDirectory -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $save 'stdout.log') -RedirectStandardError (Join-Path $save 'stderr.log')
        if($role -eq 'server'){Start-Sleep -Seconds 15}
    }
    $deadline=[DateTime]::UtcNow.AddSeconds(150)
    do {
        Start-Sleep -Seconds 2
        $reports=@($roles | ForEach-Object {Join-Path $run "$_/MechanicalCaptures/network-report.json"} | Where-Object {Test-Path $_})
    } while($reports.Count -lt 3 -and [DateTime]::UtcNow -lt $deadline)
    if($reports.Count -ne 3){throw "联机报告超时：$run"}
    foreach($path in $reports){$r=Get-Content $path -Raw | ConvertFrom-Json; $r | ConvertTo-Json -Depth 6; if(@($r.results | Where-Object status -ne 'PASS').Count -gt 0){throw "联机失败：$path"}}
    Write-Output "联机通过：$run"
} finally {
    $env:ADVANCEDMOD_MECHANICAL_NET_TEST=$old
    foreach($process in $processes){if(-not $process.HasExited){Stop-Process -Id $process.Id}}
}

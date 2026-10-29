# 2026-10-27：串行测量 18 种配置；运行前先执行 Run.ps1 完成 Release 构建。
$ErrorActionPreference='Stop'
Push-Location (Join-Path $PSScriptRoot '../..')
try {
    foreach($r in @(128,256,512)) {foreach($n in @(1,5,10)) {foreach($overlap in @($false,$true)) {
        $arguments=@('Tools/WaterPreview/bin/Release/net8.0/WaterPreview.dll','--bench','--vortex','--shaded','--frames','150','--resolution',"$r",'--instances',"$n")
        if($overlap) {$arguments+='--overlap'}
        & dotnet @arguments
        if($LASTEXITCODE -ne 0){throw "性能场景失败：$r / $n / $overlap"}
    }}}
} finally {Pop-Location}

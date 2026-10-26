param([string] $Python = 'python')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
# Windows + DirectX June 2010的d3dcompiler_43；编译SM2/FNA可读取的XNA Effect格式。
foreach ($source in Get-ChildItem -LiteralPath (Join-Path $root 'Assets/Shaders') -Filter '*.fx') {
    $output = [IO.Path]::ChangeExtension($source.FullName, '.xnb')
    & $Python (Join-Path $PSScriptRoot 'ShaderBytecode.py') compile $source.FullName $output
    if ($LASTEXITCODE -ne 0) { throw "着色器编译失败：$($source.Name)" }
}

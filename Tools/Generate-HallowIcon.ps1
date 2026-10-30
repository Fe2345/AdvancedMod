# 从正式三叶结控制点生成可复用透明图标；无光晕、不应用下穿缺口。
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path $PSScriptRoot -Parent
$source=Get-Content (Join-Path $root 'Contents/Vfx/GoldenWard.cs') -Raw
$curve=[regex]::Match($source,'Reference.*?new CubicBezier\((.*?)\), 0\.025f').Groups[1].Value
$pairs=[regex]::Matches($curve,'new\(([-\d.]+)f?, ([-\d.]+)f?\)')
if($pairs.Count -ne 4){throw '无法读取正式三弧控制点'}
$points=@($pairs | ForEach-Object { [System.Drawing.PointF]::new([float]::Parse($_.Groups[1].Value,[cultureinfo]::InvariantCulture),[float]::Parse($_.Groups[2].Value,[cultureinfo]::InvariantCulture)) })
$size=64; $supersample=8; $radius=28; $stroke=0.075
$directory=Join-Path $root 'Assets/UI'; New-Item -ItemType Directory -Path $directory -Force | Out-Null
$bitmap=[System.Drawing.Bitmap]::new($size*$supersample,$size*$supersample)
$graphics=[System.Drawing.Graphics]::FromImage($bitmap)
$pen=[System.Drawing.Pen]::new([System.Drawing.Color]::White,$stroke)
try {
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TranslateTransform($size*$supersample/2,$size*$supersample/2)
    $graphics.ScaleTransform($radius*$supersample,$radius*$supersample)
    for($i=0;$i -lt 3;$i++){
        $graphics.DrawBezier($pen,$points[0],$points[1],$points[2],$points[3])
        $graphics.RotateTransform(120)
    }
    $small=[System.Drawing.Bitmap]::new($size,$size)
    $scaled=[System.Drawing.Graphics]::FromImage($small)
    try {
        $scaled.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $scaled.DrawImage($bitmap,0,0,$size,$size)
        $small.Save((Join-Path $directory 'HallowIcon.png'),[System.Drawing.Imaging.ImageFormat]::Png)
    } finally {$scaled.Dispose();$small.Dispose()}
} finally {$pen.Dispose();$graphics.Dispose();$bitmap.Dispose()}

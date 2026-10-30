// 2026-10-30：风刃 demo 的局部圆弧几何；不包含材质复现或显示变换。
using System;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Contents.Vfx;

internal readonly record struct IdyllicArcGeometry
{
    public const float MinimumAngle = 15, MaximumAngle = 120;
    public const float MinimumSize = 32, MaximumSize = 1024;
    public float ArcAngleDegrees { get; }
    public float Radius { get; }
    public float AngleRadians => ArcAngleDegrees * MathF.PI / 180;
    public float ChordLength => 2 * Radius * MathF.Sin(AngleRadians / 2);
    public float ArcLength => Radius * AngleRadians;
    public float Sagitta => 2 * Radius * MathF.Pow(MathF.Sin(AngleRadians / 4), 2);

    // 固定半径模式以 60 度对应半径为基准，因此两种模式在默认角度重合。
    public IdyllicArcGeometry(float arcAngleDegrees = 60, float size = 240, bool fixedRadius = false)
    {
        if (!float.IsFinite(arcAngleDegrees) || !float.IsFinite(size))
            throw new ArgumentException("角度和尺寸必须为有限数值。");
        ArcAngleDegrees = Math.Clamp(arcAngleDegrees, MinimumAngle, MaximumAngle);
        size = Math.Clamp(size, MinimumSize, MaximumSize);
        Radius = fixedRadius ? size : size / (2 * MathF.Sin(AngleRadians / 2));
    }

    // 局部原点在弧顶。绘制范围只随弦长与弓高变化，不覆盖完整大圆。
    // 径向偏移与角度独立；归一化 u 同时可用于完整头尾包络。
    public Vector2 Point(float u, float radialOffset = 0)
    {
        float theta = (Math.Clamp(u, 0, 1) - 0.5f) * AngleRadians;
        float sine = MathF.Sin(theta / 2);
        return new((Radius + radialOffset) * MathF.Sin(theta),
            2 * Radius * sine * sine - radialOffset * MathF.Cos(theta));
    }

    public float TextureDistance(float u) => (u - 0.5f) * ArcLength;
}

using System;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Contents.Acoustics;

/// <summary>声波 Demo 的机制与美术参数；距离为像素，时间为游戏刻。</summary>
public static class SoundRules
{
    public const int Lifetime = 180, SentryLifetime = 900, MaxWaves = 48, MaxSentries = 12;
    public const int MaxReflections = 4, MaxFusionDepth = 3, BaseDamage = 100;
    public const float Speed = 6, Wavelength = 32, HalfLength = 64, HalfWidth = 64;
    public const float Thickness = 3, Opacity = 0.85f, EdgeFraction = 0.2f;
    public const float Decay = 0.993f, MinimumEnergy = 0.025f, ReflectionRetention = 0.92f, FusionRetention = 0.9f;
    public const float CancelDamage = 0.2f, ConstructiveDamage = 1, CancelElement = 2, ConstructiveElement = 0.5f;
    public const float BaseElement = 1, SentryRadius = 18, PulseSeconds = 0.3f, MinimumVisibility = 0.16f;
    public const float DemoSeparation = 240, DemoHeight = 120, DebugScale = 0.65f;
    public const float DirectionToleranceSquared = 0.0001f;
    public const int EmissionDelay = 30;
    public const float DemoTargetDistance = 360, DemoReflectTargetDistance = 180;
    public const float MinimumThicknessScale = 0.6f, MaximumThicknessScale = 1.6f, PulseSpacingScale = 0.5f;
    public const float VisualLeaseSeconds = 0.1f;
    public static readonly Color WaveColor = new(100, 235, 220), CancelColor = new(30, 210, 130), BrightColor = new(225, 255, 250);

    public static float Phase(float phase) => MathF.IEEERemainder(phase, MathHelper.TwoPi);
    public static Vector2 Normal(Vector2 d) => new(-d.Y, d.X);
    public static Vector2 Reflect(Vector2 d, Vector2 normal) => d - 2 * Vector2.Dot(d, normal) * normal;
    public static Vector2 FusionDirection(Vector2 a, Vector2 b, float energyA, float energyB)
    {
        Vector2 momentum = a * energyA + b * energyB;
        return momentum.LengthSquared() <= DirectionToleranceSquared * (energyA + energyB) * (energyA + energyB)
            ? a : Vector2.Normalize(momentum);
    }
    public static float PhaseAt(Vector2 point, Vector2 center, Vector2 direction, float phase, float wavelength) =>
        Phase(phase + MathHelper.TwoPi * Vector2.Dot(point - center, direction) / wavelength);

    /// <summary>相量只决定分配比例；输出能量永远不大于输入剩余预算。</summary>
    public static SoundFusion Fuse(float energyA, float energyB, float phaseA, float phaseB, float coherenceA = 1, float coherenceB = 1)
    {
        float a = MathF.Sqrt(energyA * coherenceA), b = MathF.Sqrt(energyB * coherenceB);
        float x = a * MathF.Cos(phaseA) + b * MathF.Cos(phaseB);
        float y = a * MathF.Sin(phaseA) + b * MathF.Sin(phaseB);
        float amplitude = MathF.Sqrt(x * x + y * y);
        float maximum = MathF.Sqrt(energyA) + MathF.Sqrt(energyB);
        float coherence = maximum > 0 ? Math.Clamp(amplitude * amplitude / (maximum * maximum), 0, 1) : 0;
        return new((energyA + energyB) * FusionRetention, amplitude, coherence,
            amplitude < 0.0001f ? Phase(phaseA) : MathF.Atan2(y, x));
    }
    public static float DamageFactor(float coherence) => MathHelper.Lerp(CancelDamage, ConstructiveDamage, coherence);
    public static float ElementFactor(float coherence) => MathHelper.Lerp(CancelElement, ConstructiveElement, coherence);
    public static string Kind(float coherence) => coherence > 0.85f ? "相长" : coherence < 0.15f ? "相消" : "部分干涉";

    /// <summary>四轴 SAT；波包渲染包络与碰撞均使用同一有向矩形。</summary>
    public static bool Overlap(Vector2 a, Vector2 da, Vector2 sizeA, Vector2 b, Vector2 db, Vector2 sizeB)
    {
        Vector2 na = Normal(da), nb = Normal(db), delta = b - a;
        return Axis(da) && Axis(na) && Axis(db) && Axis(nb);
        bool Axis(Vector2 axis) => MathF.Abs(Vector2.Dot(delta, axis)) <=
            sizeA.X * MathF.Abs(Vector2.Dot(da, axis)) + sizeA.Y * MathF.Abs(Vector2.Dot(na, axis)) +
            sizeB.X * MathF.Abs(Vector2.Dot(db, axis)) + sizeB.Y * MathF.Abs(Vector2.Dot(nb, axis));
    }
}

public readonly record struct SoundFusion(float Energy, float Amplitude, float Coherence, float Phase);

using System;
using Microsoft.Xna.Framework;
using AdvancedMod.Basic.Graphics;
using AdvancedMod.Basic.Graphics.Geometry;
using AdvancedMod.Basic.Graphics.Rendering;

namespace AdvancedMod.Contents.Vfx;

public readonly record struct WardVisualState(Vector2 Center, float TargetRadius, float TargetRotation = 0, bool Snap = false)
{
    public bool IsValid => Paths.Finite(Center) && float.IsFinite(TargetRadius) && TargetRadius > 0 && float.IsFinite(TargetRotation);
}

public sealed record WardStyle
{
    public Color Color { get; init; } = new(255, 188, 58);
    public Color CoreColor { get; init; } = new(255, 229, 148);
    public float OpeningSeconds { get; init; } = 0.8f;
    public float ClosingSeconds { get; init; } = 0.65f;
    public float FollowRate { get; init; } = 10;
    public float RotationSpeed { get; init; } = 0.32f;
    public float PatternScale { get; init; } = 0.8f;
    public float CoreHalfWidth { get; init; } = 1.2f;
    public float GlowHalfWidth { get; init; } = 5;
    public int ParticleCount { get; init; } = 72;
    public float ParticleRadius { get; init; } = 5;
    public float ParticleDrift { get; init; } = 7;
    public KnotPath Path { get; init; } = KnotPath.Reference;
}

/// <summary>参考图宽条的中心线近似：三段贝塞尔弧旋转复制，交叉处循环下穿。</summary>
public sealed class KnotPath
{
    internal readonly PathPoint[][] Arcs;
    internal readonly bool[][] Gaps;
    public static KnotPath Reference { get; } = new(new CubicBezier(new(0, -1), new(0.65f, -0.3f), new(0.12f, 1), new(-0.8660254f, 0.5f)), 0.025f, 0.048f);
    public KnotPath(CubicBezier arc, float spacing, float crossingGap)
    {
        if (!float.IsFinite(crossingGap) || crossingGap < 0) throw new ArgumentOutOfRangeException(nameof(crossingGap));
        Arcs = new PathPoint[3][]; Gaps = new bool[3][];
        var source = Paths.Beziers(new[] { arc }, spacing);
        for (int a = 0; a < 3; a++)
        {
            Arcs[a] = new PathPoint[source.Length]; Gaps[a] = new bool[source.Length - 1];
            for (int i = 0; i < source.Length; i++) Arcs[a][i] = new(Paths.Rotate(source[i].Position, MathHelper.TwoPi * a / 3), source[i].Length);
        }
        for (int a = 0; a < 3; a++)
        {
            var under = Arcs[a]; var over = Arcs[(a + 1) % 3];
            for (int i = 1; i < under.Length - 2; i++)
            for (int j = 1; j < over.Length - 2; j++)
            {
                Vector2 p = under[i].Position, r = under[i + 1].Position - p;
                Vector2 q = over[j].Position, s = over[j + 1].Position - q;
                float cross = Cross(r, s);
                if (MathF.Abs(cross) < 0.000001f) continue;
                float t = Cross(q - p, s) / cross, u = Cross(q - p, r) / cross;
                if (t < 0 || t > 1 || u < 0 || u > 1) continue;
                float distance = MathHelper.Lerp(under[i].Length, under[i + 1].Length, t);
                for (int k = 0; k < Gaps[a].Length; k++)
                    if (MathF.Abs((under[k].Length + under[k + 1].Length) * 0.5f - distance) < crossingGap) Gaps[a][k] = true;
            }
        }
    }
    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
}

public sealed class GoldenWard : WorldEffect, IVfxTarget<WardVisualState>
{
    private readonly WardStyle style;
    private readonly record struct Particle(float Angle, float Phase, float Size, float Brightness);
    private readonly Particle[] particles;
    private readonly Vector2[][] transformed;
    private readonly VfxMesh core = new(), glow = new();
    private WardVisualState target;
    private Vector2 center;
    private float radius, rotation, spin, age, closingAge, stoppedRadius, stoppedAlpha;
    private bool closing;
    public float DisplayRadius { get; private set; }
    public float Alpha { get; private set; }
    public override bool IsFinished => closing && closingAge >= style.ClosingSeconds;

    public GoldenWard(WardVisualState state, WardStyle style = null, int seed = 1)
    {
        if (!state.IsValid) throw new ArgumentException("领域目标无效。");
        this.style = style ?? new();
        if (!float.IsFinite(this.style.OpeningSeconds) || this.style.OpeningSeconds <= 0 ||
            !float.IsFinite(this.style.ClosingSeconds) || this.style.ClosingSeconds <= 0 ||
            !float.IsFinite(this.style.FollowRate) || this.style.FollowRate < 0 ||
            !float.IsFinite(this.style.RotationSpeed) || !float.IsFinite(this.style.PatternScale) || this.style.PatternScale <= 0 ||
            !float.IsFinite(this.style.CoreHalfWidth) || this.style.CoreHalfWidth <= 0 ||
            !float.IsFinite(this.style.GlowHalfWidth) || this.style.GlowHalfWidth <= 0 ||
            !float.IsFinite(this.style.ParticleRadius) || this.style.ParticleRadius <= 0 ||
            !float.IsFinite(this.style.ParticleDrift) || this.style.ParticleDrift < 0 ||
            this.style.ParticleCount < 0 || this.style.ParticleCount > 512 || this.style.Path is null)
            throw new ArgumentException("领域样式参数无效。");
        target = state; center = state.Center; radius = state.TargetRadius; rotation = state.TargetRotation;
        particles = new Particle[this.style.ParticleCount];
        var random = new Random(seed);
        for (int i = 0; i < particles.Length; i++) particles[i] = new(MathHelper.TwoPi * i / particles.Length,
            random.NextSingle() * MathHelper.TwoPi, 0.6f + random.NextSingle() * 0.8f, 0.4f + random.NextSingle() * 0.6f);
        transformed = new Vector2[3][];
        for (int i = 0; i < 3; i++) transformed[i] = new Vector2[this.style.Path.Arcs[i].Length];
    }
    public void SetTarget(in WardVisualState state)
    {
        if (closing || !state.IsValid) return;
        target = state;
        if (state.Snap) { center = state.Center; radius = state.TargetRadius; rotation = state.TargetRotation; }
    }
    public override void Stop()
    {
        if (closing) return;
        closing = true; stoppedRadius = DisplayRadius; stoppedAlpha = Alpha;
    }
    public override void Update(in VfxUpdateContext context)
    {
        float dt = context.DeltaSeconds;
        age += dt; spin += style.RotationSpeed * dt;
        if (closing)
        {
            closingAge += dt;
            float t = Math.Clamp(closingAge / style.ClosingSeconds, 0, 1);
            DisplayRadius = stoppedRadius * (1 - Smooth(t)); Alpha = stoppedAlpha * (1 - t);
        }
        else
        {
            float follow = 1 - MathF.Exp(-style.FollowRate * dt);
            center = Vector2.Lerp(center, target.Center, follow); radius = MathHelper.Lerp(radius, target.TargetRadius, follow);
            rotation += MathHelper.WrapAngle(target.TargetRotation - rotation) * follow;
            Alpha = Smooth(Math.Clamp(age / style.OpeningSeconds, 0, 1)); DisplayRadius = radius * Alpha;
        }
    }
    private static float Smooth(float t) => t * t * (3 - 2 * t);
    public override void CollectDrawCommands(VfxDrawQueue queue, in VfxDrawContext context)
    {
        if (DisplayRadius < 1 || Alpha <= 0) return;
        float margin = DisplayRadius + style.ParticleDrift + style.ParticleRadius;
        if (center.X + margin < context.Camera.X || center.Y + margin < context.Camera.Y ||
            center.X - margin > context.Camera.X + context.ViewSize.X || center.Y - margin > context.Camera.Y + context.ViewSize.Y) return;
        core.Clear(); glow.Clear();
        float angle = rotation + spin;
        for (int a = 0; a < 3; a++)
        {
            var path = style.Path.Arcs[a]; var points = transformed[a];
            for (int i = 0; i < points.Length; i++) points[i] = center + Paths.Rotate(path[i].Position, angle) * (DisplayRadius * style.PatternScale);
            int start = 0;
            for (int i = 0; i < points.Length - 1; i++)
            {
                if (!style.Path.Gaps[a][i]) continue;
                DrawPart(points.AsSpan(start, i - start + 1)); start = i + 1;
            }
            DrawPart(points.AsSpan(start));
        }
        float envelope = Math.Clamp(DisplayRadius / radius, 0, 1);
        foreach (var p in particles)
        {
            Vector2 position = center + Paths.Direction(p.Angle + angle) * (DisplayRadius + style.ParticleDrift * MathF.Sin(age * 1.8f + p.Phase) * envelope);
            position.Y += style.ParticleDrift * MathF.Sin(age * 1.3f + p.Phase) * envelope;
            glow.GlowPoint(position, style.ParticleRadius * p.Size, style.Color * (Alpha * p.Brightness));
            core.GlowPoint(position, style.ParticleRadius * p.Size * 0.25f, style.CoreColor * Alpha);
        }
        queue.Add(glow, VfxMaterial.SoftGlow); queue.Add(core, VfxMaterial.PathCore, 1);
    }
    private void DrawPart(ReadOnlySpan<Vector2> points)
    {
        glow.Stroke(points, style.GlowHalfWidth, style.Color * (Alpha * 0.08f));
        glow.Stroke(points, style.GlowHalfWidth * 0.55f, style.Color * (Alpha * 0.12f));
        core.Stroke(points, style.CoreHalfWidth, style.CoreColor * Alpha);
    }
}

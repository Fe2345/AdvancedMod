using System;
using AdvancedMod.Basic.Graphics.Geometry;
using AdvancedMod.Basic.Graphics.Rendering;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Basic.Graphics;

/// <summary>两点之间一次生成的闪电快照；不持有实体、不处理伤害、不逐帧重抽形状。</summary>
public sealed class LightningEffect : WorldEffect
{
    public const float Lifetime = 0.3f;
    public const float HalfWidth = 3.5f;
    private const float FadeStart = 0.1f, FadePeak = 0.25f, FadeEnd = 0.9f;
    private const float WaveLength = 0.35f, WaveIn = 0.15f, WaveOut = 0.75f;
    private readonly Vector2[] points;
    private readonly Vector2[] normals;
    private readonly Color color;
    private readonly VfxMesh mesh = new();
    private float age;
    public override bool IsFinished => age >= Lifetime;
    public LightningEffect(Vector2 start, Vector2 end, Color color, int seed)
    {
        points = LightningPath.Create(start, end, seed);
        normals = new Vector2[points.Length];
        this.color = color;
        for (int i = 0; i < points.Length; i++)
        {
            // 以相邻单位切线平均法线，限制尖角接缝长度。
            Vector2 before = points[i] - points[Math.Max(0, i - 1)];
            Vector2 after = points[Math.Min(points.Length - 1, i + 1)] - points[i];
            if (before.LengthSquared() < 0.000001f) before = after;
            if (after.LengthSquared() < 0.000001f) after = before;
            if (after.LengthSquared() < 0.000001f) continue;
            before.Normalize(); after.Normalize();
            Vector2 n = new(-before.Y - after.Y, before.X + after.X);
            if (n.LengthSquared() < 0.000001f) n = new(-after.Y, after.X);
            n.Normalize();
            normals[i] = n / Math.Max(0.65f, Vector2.Dot(n, new Vector2(-after.Y, after.X)));
        }
    }
    public override void Update(in VfxUpdateContext context) => age += context.DeltaSeconds;
    public override void Stop() => age = Lifetime;
    private static float Ramp(float x, float from, float to) => Math.Clamp((x - from) / (to - from), 0, 1);
    public static float Opacity(float progress) =>
        MathHelper.Lerp(0.5f, 1, Ramp(progress, FadeStart, FadePeak)) * (1 - Ramp(progress, FadePeak, FadeEnd));
    public static float Visibility(float along, float progress)
    {
        float entering = Ramp(progress, 0, WaveIn), leaving = Ramp(progress, 1 - WaveOut, 1);
        float front = 1 - Ramp(along, MathHelper.Lerp(-WaveLength, 1, entering), MathHelper.Lerp(0, 1 + WaveLength, entering));
        float back = Ramp(along, MathHelper.Lerp(-WaveLength, 1, leaving), MathHelper.Lerp(0, 1 + WaveLength, leaving));
        return front * back;
    }
    public override void CollectDrawCommands(VfxDrawQueue queue, in VfxDrawContext context)
    {
        mesh.Clear();
        float progress = Math.Clamp(age / Lifetime, 0, 1), opacity = Opacity(progress);
        if (IsFinished || opacity <= 0) return;
        float timeWidth = MathHelper.Lerp(1, 0.5f, Ramp(progress, 0.5f, 1));
        for (int i = 1; i < points.Length; i++)
        {
            float a = (float)(i - 1) / (points.Length - 1), b = (float)i / (points.Length - 1);
            float Width(float t) => HalfWidth * timeWidth * MathHelper.Lerp(1, 0.5f, Ramp(t, 0.5f, 1));
            Vector2 na = normals[i - 1] * Width(a), nb = normals[i] * Width(b);
            Color ca = color * (opacity * Visibility(a, progress)), cb = color * (opacity * Visibility(b, progress));
            Vector2 al = points[i - 1] - na, ar = points[i - 1] + na, bl = points[i] - nb, br = points[i] + nb;
            mesh.Triangle(al, ar, bl, ca, ca, cb, new(a, 0), new(a, 1), new(b, 0));
            mesh.Triangle(ar, br, bl, ca, cb, cb, new(a, 1), new(b, 1), new(b, 0));
        }
        queue.Add(mesh, VfxMaterial.Lightning);
    }
}

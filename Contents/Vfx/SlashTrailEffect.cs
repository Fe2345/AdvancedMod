using System;
using Microsoft.Xna.Framework;
using AdvancedMod.Basic.Graphics;
using AdvancedMod.Basic.Graphics.Geometry;
using AdvancedMod.Basic.Graphics.Rendering;

namespace AdvancedMod.Contents.Vfx;

public sealed record SlashStyle
{
    public Color Color { get; init; } = new(255, 180, 46);
    public float Lifetime { get; init; } = 0.32f;
    public float EdgeHalfWidth { get; init; } = 1.5f;
    public int Capacity { get; init; } = 96;
    public bool OuterEdgeOnly { get; init; }
}

public sealed class SlashTrailEffect : WorldEffect, IVfxSamples<SlashPoseSample>
{
    private readonly SlashStyle style;
    private readonly TrailBuffer trail;
    private readonly VfxMesh body = new(), edge = new(), glow = new();
    private double time, lastInput;
    private bool started, stopped;
    public override bool IsFinished => started && time - lastInput >= style.Lifetime;
    public SlashTrailEffect(SlashStyle style = null)
    {
        this.style = style ?? new();
        if (!float.IsFinite(this.style.Lifetime) || this.style.Lifetime <= 0 || !float.IsFinite(this.style.EdgeHalfWidth) || this.style.EdgeHalfWidth <= 0)
            throw new ArgumentException("剑气样式参数无效。");
        trail = new(this.style.Capacity);
    }
    public void AppendSample(in SlashPoseSample sample)
    {
        if (stopped || !Paths.Finite(sample.InnerWorldPosition) || !Paths.Finite(sample.OuterWorldPosition) || !double.IsFinite(sample.SampleTime)) return;
        trail.Append(sample);
        lastInput = Math.Max(lastInput, sample.SampleTime);
    }
    public override void Stop() { stopped = true; trail.Close(); }
    public override void Update(in VfxUpdateContext context)
    {
        if (!started) { started = true; if (trail.Count == 0) lastInput = context.Time; }
        time = context.Time;
        trail.Prune(time - style.Lifetime);
    }
    public override void CollectDrawCommands(VfxDrawQueue queue, in VfxDrawContext context)
    {
        body.Clear(); edge.Clear(); glow.Clear();
        body.Sweep(trail, time, style.Lifetime, style.Color * 0.55f, style.OuterEdgeOnly);
        Span<Vector2> segment = stackalloc Vector2[2];
        for (int i = 1; i < trail.Count; i++)
        {
            var a = trail[i - 1]; var b = trail[i];
            if (b.BreakBefore) continue;
            segment[0] = a.OuterWorldPosition; segment[1] = b.OuterWorldPosition;
            float alpha = Math.Clamp(1 - (float)(time - a.SampleTime) / style.Lifetime, 0, 1);
            edge.Stroke(segment, style.EdgeHalfWidth, Color.Lerp(style.Color, Color.White, 0.5f) * alpha);
            glow.Stroke(segment, style.EdgeHalfWidth * 4, style.Color * (alpha * 0.12f));
        }
        queue.Add(body, VfxMaterial.SlashBody); queue.Add(glow, VfxMaterial.SoftGlow); queue.Add(edge, VfxMaterial.PathCore, 1);
    }
}

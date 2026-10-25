using System;
using AdvancedMod.Basic.Graphics;
using AdvancedMod.Basic.Graphics.Rendering;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Contents.Acoustics;

public readonly record struct SoundPose(Vector2 Center, Vector2 Direction, float Phase, float Wavelength,
    float Energy, float Coherence, bool Fused, bool Sentry = false);

/// <summary>纯表现快照；共享网格后端管理设备状态，本组件不创建 GPU 资源。</summary>
public sealed class SoundVisual : WorldEffect, IVfxTarget<SoundPose>
{
    private readonly VfxMesh mesh = new();
    private SoundPose pose;
    private bool ended;
    private readonly bool pulse;
    private float elapsed;
    public SoundVisual(SoundPose pose, bool pulse = false) { this.pose = pose; this.pulse = pulse; }
    // 引擎在越出世界边界时可能直接置 active=false 而不调用 OnKill；失去姿态心跳后自动清理。
    public override bool IsFinished => ended || elapsed >= (pulse ? SoundRules.PulseSeconds : SoundRules.VisualLeaseSeconds);
    public void SetTarget(in SoundPose target) { pose = target; if (!pulse) elapsed = 0; }
    public override void Stop() => ended = true;
    public override void Update(in VfxUpdateContext context) => elapsed += context.DeltaSeconds;
    public override void CollectDrawCommands(VfxDrawQueue queue, in VfxDrawContext context)
    {
        mesh.Clear();
        var d = pose.Direction;
        var n = SoundRules.Normal(d);
        var color = pose.Fused ? Color.Lerp(SoundRules.CancelColor, SoundRules.BrightColor, pose.Coherence) : SoundRules.WaveColor;
        float brightness = SoundRules.Opacity * Math.Min(1, MathF.Sqrt(pose.Energy)) *
            (pose.Fused ? MathHelper.Lerp(SoundRules.MinimumVisibility, 1, MathF.Sqrt(pose.Coherence)) : 1);
        if (pose.Sentry)
        {
            Band(pose.Center, d, n, SoundRules.Thickness, SoundRules.SentryRadius, color);
            mesh.GlowPoint(pose.Center + d * SoundRules.SentryRadius, SoundRules.Thickness, SoundRules.BrightColor);
        }
        else
        {
            float envelope = pulse ? Math.Clamp(1 - elapsed / SoundRules.PulseSeconds, 0, 1) : 1;
            float spacing = pose.Wavelength * (pulse ? MathHelper.Lerp(SoundRules.PulseSpacingScale, 1, 1 - envelope) : 1);
            float offset = -pose.Phase / MathHelper.TwoPi * spacing;
            int count = (int)MathF.Ceiling(SoundRules.HalfLength / spacing) + 1;
            for (int i = -count; i <= count; i++)
            {
                float x = i * spacing + offset;
                if (MathF.Abs(x) >= SoundRules.HalfLength) continue;
                float edge = Math.Clamp((SoundRules.HalfLength - MathF.Abs(x)) / (spacing * 0.5f), 0, 1);
                float width = SoundRules.Thickness * (pose.Fused ? MathHelper.Lerp(SoundRules.MinimumThicknessScale, SoundRules.MaximumThicknessScale, pose.Coherence) : 1);
                width = Math.Min(width, SoundRules.HalfLength - MathF.Abs(x));
                Band(pose.Center + d * x, d, n, width, SoundRules.HalfWidth, color * (brightness * edge * envelope));
            }
        }
        queue.Add(mesh, VfxMaterial.SoftGlow);
    }

    // 中央亮脊向两侧透明，端点再渐隐；每道波峰仅十二个三角形。
    private void Band(Vector2 center, Vector2 d, Vector2 n, float thickness, float halfWidth, Color color)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 edge = d * thickness * side;
            for (int section = 0; section < 3; section++)
            {
                float y0 = section == 0 ? -halfWidth : section == 1 ? -halfWidth * (1 - SoundRules.EdgeFraction) : halfWidth * (1 - SoundRules.EdgeFraction);
                float y1 = section == 0 ? -halfWidth * (1 - SoundRules.EdgeFraction) : section == 1 ? halfWidth * (1 - SoundRules.EdgeFraction) : halfWidth;
                var a = center + n * y0; var b = center + n * y1;
                Color ca = section == 0 ? Color.Transparent : color, cb = section == 2 ? Color.Transparent : color;
                mesh.Triangle(a, b, a + edge, ca, cb, Color.Transparent);
                mesh.Triangle(b, b + edge, a + edge, cb, Color.Transparent, Color.Transparent);
            }
        }
    }
}

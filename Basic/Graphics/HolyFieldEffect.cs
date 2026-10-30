using System;
using AdvancedMod.Basic.Graphics.Rendering;
using AdvancedMod.Contents.Vfx;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Basic.Graphics;

/// <summary>把已有三叶结领域接到神圣结界快照；位置插值与动画不参与数值结算。</summary>
public sealed class HolyFieldEffect : WorldEffect
{
    internal static long DrawCount;
    public const float OpenSeconds = 0.5f, CloseSeconds = 0.4f;
    public const float CoreRatio = 1f / 8, LineWidthMultiplier = 4;
    public const int ParticleDensityMultiplier = 3;
    private static readonly WardStyle demoStyle = new();
    internal static readonly WardStyle Style = demoStyle with
    {
        OpeningSeconds = OpenSeconds,
        ClosingSeconds = CloseSeconds,
        PatternScale = CoreRatio,
        CoreHalfWidth = demoStyle.CoreHalfWidth * LineWidthMultiplier,
        GlowHalfWidth = demoStyle.GlowHalfWidth * LineWidthMultiplier,
        ParticleCount = demoStyle.ParticleCount * ParticleDensityMultiplier
    };
    private readonly GoldenWard ward;
    private Vector2 center, target;
    private float interpolation;
    private bool closing;
    public float Radius;
    public HolyFieldEffect(Vector2 center, float radius, int seed, bool mature = false)
    {
        this.center = target = center; Radius = radius;
        ward = new GoldenWard(new(center, radius), Style, seed);
        // 晚加入直接显示成熟结界；传送和新建仍从开启动画开始。
        if (mature) ward.Update(new(OpenSeconds, 0));
    }
    public void MoveTo(Vector2 position, float seconds) { target = position; interpolation = seconds; }
    public override bool IsFinished => ward.IsFinished;
    public override void Stop() { if (closing) return; closing = true; ward.Stop(); }
    public override void Update(in VfxUpdateContext context)
    {
        if (!closing)
        {
            if (interpolation > 0)
            {
                center = Vector2.Lerp(center, target, Math.Min(1, context.DeltaSeconds / interpolation));
                interpolation -= context.DeltaSeconds;
            }
            // 位置已按网络快照插值，不再叠加demo的指数追随延迟。
            ward.SetTarget(new(center, Radius, Snap: true));
        }
        ward.Update(context);
    }
    public override void CollectDrawCommands(VfxDrawQueue queue, in VfxDrawContext context)
    {
        if (ward.DisplayRadius >= 1 && ward.Alpha > 0) DrawCount++;
        ward.CollectDrawCommands(queue, context);
    }
}

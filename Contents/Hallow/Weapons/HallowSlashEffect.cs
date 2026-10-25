using System;
using Microsoft.Xna.Framework;
using AdvancedMod.Basic.Graphics;
using AdvancedMod.Basic.Graphics.Rendering;

namespace AdvancedMod.Contents.Hallow.Weapons;

public readonly record struct HallowSlashPose(Vector2 Center, float Radius, float Angle, float SweptAngle);

/// <summary>天裁的三层相切亮弧；源弹幕停止后保留最后姿态并短暂淡出。</summary>
public sealed class HallowSlashEffect : WorldEffect, IVfxTarget<HallowSlashPose>
{
    public const float MaximumArc = MathF.PI * 0.75f;
    public const float MaximumBandWidth = 64;
    public const float InnerCutoff = 1f / 3;
    public const float FadeSeconds = 0.12f;
    private readonly VfxMesh mesh = new();
    private HallowSlashPose pose;
    private bool stopped;
    private float fade;
    public override bool IsFinished => stopped && fade >= FadeSeconds;
    public void SetTarget(in HallowSlashPose target) { if (!stopped) pose = target; }
    public override void Stop() => stopped = true;
    public override void Update(in VfxUpdateContext context) { if (stopped) fade += context.DeltaSeconds; }
    public override void CollectDrawCommands(VfxDrawQueue queue, in VfxDrawContext context)
    {
        mesh.Clear();
        float arc = Math.Min(MaximumArc, MathF.Abs(pose.SweptAngle));
        mesh.LayeredTangentCrescent(pose.Center, pose.Radius, pose.Angle, Math.Sign(pose.SweptAngle) * arc,
            MaximumArc, MaximumBandWidth, InnerCutoff,
            new Color(255, 240, 150), new Color(255, 255, 80) * 0.8f, new Color(180, 160, 60) * 0.9f,
            new Color(245, 185, 55) * 0.42f,
            Math.Clamp(1 - fade / FadeSeconds, 0, 1));
        queue.Add(mesh, VfxMaterial.SlashBody);
    }
}

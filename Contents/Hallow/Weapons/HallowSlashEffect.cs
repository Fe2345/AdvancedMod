using System;
using Microsoft.Xna.Framework;
using AdvancedMod.Basic.Graphics;
using AdvancedMod.Basic.Graphics.Geometry;
using AdvancedMod.Basic.Graphics.Rendering;
using AdvancedMod.Basic.Tag;

namespace AdvancedMod.Contents.Hallow.Weapons;

public readonly record struct HallowSlashPose(Vector2 Center, float Radius, float Angle, float SweptAngle,
    float ElapsedSeconds, float DurationSeconds, TagEnum? Resonance = null);

/// <summary>保存相对持握中心的真实挥刀历史；快速展开、慢速收短，停止后自然消散。</summary>
public sealed class HallowSlashEffect : WorldEffect, IVfxTarget<HallowSlashPose>
{
    public const float WidthRatio = 0.5f;
    // 相对首版宽度同比延长记忆，维持宽厚剑气的弧长比例。
    public const float MemoryFraction = 0.22f * (WidthRatio / (1f / 3));
    public const float FadeSeconds = 0.12f;
    private const float TeleportDistance = 256;
    private const float AngularSampleStep = 0.025f;
    private readonly TrailBuffer history = new(256, 0, 6, 256);
    private readonly VfxMesh mesh = new(), glow = new();
    private HallowSlashPose pose;
    private bool stopped, initialized;
    private float fade;
    public override bool IsFinished => stopped && fade >= FadeSeconds;
    public void SetTarget(in HallowSlashPose target)
    {
        if (stopped || !Paths.Finite(target.Center) || !float.IsFinite(target.Radius) || target.Radius < 1 ||
            !float.IsFinite(target.Angle) || !float.IsFinite(target.ElapsedSeconds) ||
            !float.IsFinite(target.DurationSeconds) || target.DurationSeconds <= 0) return;
        bool discontinuity = initialized && (Vector2.DistanceSquared(pose.Center, target.Center) > TeleportDistance * TeleportDistance ||
            target.ElapsedSeconds < pose.ElapsedSeconds);
        if (discontinuity) history.Prune(double.MaxValue);
        // 高攻速两圈段相邻AI姿态相差较大，按真实展开角度补圆弧，不能直接补直线弦。
        int steps = initialized && !discontinuity
            ? Math.Clamp((int)MathF.Ceiling(MathF.Abs(target.Angle - pose.Angle) / AngularSampleStep), 1, 128) : 1;
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            float angle = initialized && !discontinuity ? MathHelper.Lerp(pose.Angle, target.Angle, t) : target.Angle;
            float radius = initialized && !discontinuity ? MathHelper.Lerp(pose.Radius, target.Radius, t) : target.Radius;
            double time = initialized && !discontinuity ? MathHelper.Lerp(pose.ElapsedSeconds, target.ElapsedSeconds, t) : target.ElapsedSeconds;
            Vector2 tip = Paths.Direction(angle) * radius;
            history.Append(new(tip * (1 - WidthRatio), tip, time));
        }
        pose = target;
        initialized = true;
        history.Prune(target.ElapsedSeconds - target.DurationSeconds * MemoryFraction);
    }
    public override void Stop() => stopped = true;
    public override void Update(in VfxUpdateContext context) { if (stopped) fade += context.DeltaSeconds; }
    public override void CollectDrawCommands(VfxDrawQueue queue, in VfxDrawContext context)
    {
        mesh.Clear(); glow.Clear();
        if (!initialized || history.Count < 2) return;
        float lifetime = pose.DurationSeconds * MemoryFraction;
        double now = pose.ElapsedSeconds + fade;
        float opacity = Math.Clamp(1 - fade / FadeSeconds, 0, 1);
        for (int i = 1; i < history.Count; i++)
        {
            var a = history[i - 1]; var b = history[i];
            if (b.BreakBefore || Vector2.Dot(a.OuterWorldPosition, b.OuterWorldPosition) <= 0) continue;
            float ua = Math.Clamp(1 - (float)(now - a.SampleTime) / lifetime, 0, 1);
            float ub = Math.Clamp(1 - (float)(now - b.SampleTime) / lifetime, 0, 1);
            if (ub <= 0) continue;
            // 尾部变窄并淡出；头部仍在实际剑尖，移动时整条轨迹跟随持握中心。
            Vector2 ao = pose.Center + a.OuterWorldPosition, bo = pose.Center + b.OuterWorldPosition;
            Vector2 ai = Vector2.Lerp(ao, pose.Center + a.InnerWorldPosition, MathF.Sqrt(ua));
            Vector2 bi = Vector2.Lerp(bo, pose.Center + b.InnerWorldPosition, MathF.Sqrt(ub));
            // 刀光FX按UV生成颜色和衰减；顶点色不参与该材质，避免重复压暗。
            Color ca = Color.White, cb = Color.White;
            mesh.Triangle(ai, ao, bi, ca, ca, cb, new(ua, 0), new(ua, 1), new(ub, 0));
            mesh.Triangle(ao, bo, bi, ca, cb, cb, new(ua, 1), new(ub, 1), new(ub, 0));
        }
        // 星芒沿少量历史样本分布，不随弹幕更新次数增加数量。
        const int stars = 4;
        for (int i = 0; i < stars; i++)
        {
            int index = (history.Count - 1) * (stars + i) / (stars * 2);
            var sample = history[index];
            float life = Math.Clamp(1 - (float)(now - sample.SampleTime) / lifetime, 0, 1);
            float pulse = 0.75f + 0.25f * MathF.Sin(pose.ElapsedSeconds * 16 + i * 2);
            float size = pose.Radius * 0.065f * life * pulse;
            Vector2 center = pose.Center + sample.OuterWorldPosition * (0.88f + i * 0.025f);
            Color starColor = pose.Resonance.HasValue ? Color.Lerp(HallowSwordVisuals.ElementColor(pose.Resonance), Color.White, 0.45f) : new Color(173, 255, 170);
            Color color = starColor * (life * opacity * 0.65f);
            Star(center, size, color);
        }
        Color head = pose.Resonance.HasValue ? HallowSwordVisuals.ElementColor(pose.Resonance) : new Color(71, 233, 60);
        Color tail = pose.Resonance.HasValue ? head * 0.55f : new Color(0, 162, 232);
        queue.AddSlash(mesh, tail, head, head, opacity);
        queue.Add(glow, VfxMaterial.SoftGlow, 1);
    }
    private void Star(Vector2 center, float size, Color color)
    {
        for (int i = 0; i < 4; i++)
        {
            Vector2 axis = Paths.Direction(i * MathHelper.PiOver2), normal = new(-axis.Y, axis.X);
            glow.Triangle(center - normal * size * 0.12f, center + axis * size, center + normal * size * 0.12f,
                color, Color.Transparent, color);
        }
    }
}

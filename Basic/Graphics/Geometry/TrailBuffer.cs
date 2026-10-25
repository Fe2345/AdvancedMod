using System;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Basic.Graphics.Geometry;

public readonly record struct SlashPoseSample(Vector2 InnerWorldPosition, Vector2 OuterWorldPosition, double SampleTime, bool BreakBefore = false);

/// <summary>固定容量、真实时间戳的刀刃双端轨迹。传送断开，运动段按距离补样。</summary>
public sealed class TrailBuffer
{
    private readonly SlashPoseSample[] samples;
    private int head;
    public int Count { get; private set; }
    public bool Closed { get; private set; }
    public float MinimumDistance { get; }
    public float MaximumStep { get; }
    public float TeleportDistance { get; }
    public SlashPoseSample this[int index] => index >= 0 && index < Count ? samples[(head + index) % samples.Length] : throw new ArgumentOutOfRangeException(nameof(index));
    public TrailBuffer(int capacity = 96, float minimumDistance = 0.5f, float maximumStep = 12, float teleportDistance = 256)
    {
        if (capacity < 2 || !float.IsFinite(minimumDistance) || minimumDistance < 0 ||
            !float.IsFinite(maximumStep) || maximumStep <= minimumDistance || !float.IsFinite(teleportDistance) || teleportDistance <= maximumStep)
            throw new ArgumentException("轨迹预算或距离阈值无效。");
        samples = new SlashPoseSample[capacity]; MinimumDistance = minimumDistance; MaximumStep = maximumStep; TeleportDistance = teleportDistance;
    }
    public void Append(in SlashPoseSample sample)
    {
        if (Closed || !Paths.Finite(sample.InnerWorldPosition) || !Paths.Finite(sample.OuterWorldPosition) || !double.IsFinite(sample.SampleTime)) return;
        if (Count == 0) { Push(sample); return; }
        var last = this[Count - 1];
        if (sample.SampleTime < last.SampleTime) return;
        float distance = Math.Max(Vector2.Distance(last.InnerWorldPosition, sample.InnerWorldPosition), Vector2.Distance(last.OuterWorldPosition, sample.OuterWorldPosition));
        if (sample.BreakBefore || distance > TeleportDistance) { Push(sample with { BreakBefore = true }); return; }
        if (distance < MinimumDistance) return;
        int steps = Math.Min(samples.Length, (int)MathF.Ceiling(distance / MaximumStep));
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Push(new(Vector2.Lerp(last.InnerWorldPosition, sample.InnerWorldPosition, t), Vector2.Lerp(last.OuterWorldPosition, sample.OuterWorldPosition, t),
                last.SampleTime + (sample.SampleTime - last.SampleTime) * t));
        }
    }
    private void Push(SlashPoseSample sample)
    {
        if (Count == samples.Length) { head = (head + 1) % samples.Length; Count--; }
        samples[(head + Count++) % samples.Length] = sample;
    }
    public void Prune(double oldestTime)
    {
        while (Count > 0 && this[0].SampleTime < oldestTime) { head = (head + 1) % samples.Length; Count--; }
    }
    public void Close() => Closed = true;
}

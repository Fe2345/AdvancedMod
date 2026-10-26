// 2026-10-26：反应文字的世界坐标排斥范围、固定倾角和 2.5 秒生命周期。
using System;
using System.Collections.Generic;

namespace AdvancedMod.Basic.Element.Presentation;

internal sealed class ReactionTextLayout
{
    internal const int LifetimeTicks = 150;
    internal const float ExclusionRadius = 32f;
    internal const float MaxRotationDegrees = 10f;
    internal const float TargetGap = 12f;

    internal sealed class Label(float x, float y, float rotation)
    {
        internal readonly float X = x;
        internal readonly float Y = y;
        internal readonly float Rotation = rotation;
        internal int RemainingTicks = LifetimeTicks;
    }

    private readonly List<Label> labels = new();
    internal bool CanPlace(float x, float y)
    {
        foreach (var label in labels)
        {
            float dx = x - label.X, dy = y - label.Y;
            if (label.RemainingTicks > 0 && dx * dx + dy * dy < ExclusionRadius * ExclusionRadius) return false;
        }
        return true;
    }

    internal Label Add(float x, float y, float randomUnit)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(randomUnit) || randomUnit < 0 || randomUnit > 1)
            throw new ArgumentOutOfRangeException(nameof(randomUnit), "文字坐标必须有限，随机值须位于 [0,1]");
        if (!CanPlace(x, y)) throw new InvalidOperationException("反应文字位于现有文字 32 像素排斥范围内");
        var label = new Label(x, y, (randomUnit * 2 - 1) * MaxRotationDegrees * MathF.PI / 180f);
        labels.Add(label);
        return label;
    }

    internal void Remove(Label label) => labels.Remove(label);
    internal void Clear() => labels.Clear();
}

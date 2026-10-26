using System;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Basic.Graphics.Geometry;

/// <summary>参考原版多层扰动与末端收束；主轴单调、横向有界的双端点电弧。</summary>
public static class LightningPath
{
    public const float Spacing = 1.5f;
    public const float MaximumDeviation = 7f;
    public const float DeviationRatio = 0.065f;
    public const int MaximumSegments = 512;
    private const int Layers = 5;
    private const float LayerDecay = 0.63f;
    private const float EndTaper = 0.18f;
    public static float Deviation(float length) => Math.Min(MaximumDeviation, length * DeviationRatio);
    public static Vector2[] Create(Vector2 start, Vector2 end, int seed)
    {
        if (!Paths.Finite(start) || !Paths.Finite(end)) throw new ArgumentException("闪电端点必须有限。");
        Vector2 delta = end - start;
        float length = delta.Length();
        if (!float.IsFinite(length)) throw new ArgumentException("闪电长度超出范围。");
        if (length < 0.001f) return new[] { start, end };
        int count = (int)Math.Clamp(MathF.Ceiling(length / Spacing), 1, MaximumSegments);
        var points = new Vector2[count + 1];
        var random = new Random(seed);
        var offsets = new float[Layers];
        float maximum = Deviation(length), weightSum = 0, weight = 1;
        for (int layer = 0; layer < Layers; layer++, weight *= LayerDecay) weightSum += weight;
        Vector2 normal = new(-delta.Y / length, delta.X / length);
        points[0] = start; points[count] = end;
        for (int i = 1; i < count; i++)
        {
            float t = (float)i / count, sum = 0;
            weight = 1;
            // 最细层逐点改变，较粗层低频刷新且幅度递减，避免大尺度随机游走。
            for (int layer = 0; layer < Layers; layer++, weight *= LayerDecay)
            {
                if (i == 1 || i % (1 << layer) == 0) offsets[layer] = random.NextSingle() * 2 - 1;
                sum += offsets[layer] * weight;
            }
            float envelope = Math.Clamp(Math.Min(t, 1 - t) / EndTaper, 0, 1);
            points[i] = Vector2.Lerp(start, end, t) + normal * (sum / weightSum * maximum * envelope);
        }
        return points;
    }
}

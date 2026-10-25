using System;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Basic.Graphics.Geometry;

public readonly record struct PathPoint(Vector2 Position, float Length);
public readonly record struct CubicBezier(Vector2 Start, Vector2 Control1, Vector2 Control2, Vector2 End);

public static class Paths
{
    public const int MaximumPoints = 1024;
    public static bool Finite(Vector2 p) => float.IsFinite(p.X) && float.IsFinite(p.Y);
    public static Vector2 Direction(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));
    public static Vector2 Rotate(Vector2 value, float angle)
    {
        float c = MathF.Cos(angle), s = MathF.Sin(angle);
        return new(value.X * c - value.Y * s, value.X * s + value.Y * c);
    }
    public static PathPoint[] Arc(Vector2 center, Vector2 radii, float start, float sweep, int segments)
    {
        if (!Finite(center) || !Finite(radii) || !float.IsFinite(start) || !float.IsFinite(sweep))
            throw new ArgumentException("弧线参数必须有限。");
        segments = Math.Clamp(segments, 2, MaximumPoints - 1);
        var result = new PathPoint[segments + 1];
        float length = 0;
        for (int i = 0; i <= segments; i++)
        {
            Vector2 p = center + Direction(start + sweep * i / segments) * radii;
            if (i > 0) length += Vector2.Distance(result[i - 1].Position, p);
            result[i] = new(p, length);
        }
        return result;
    }

    /// <summary>按控制多边形长度设定采样密度，所有曲线共用顶点上限与连续弧长。</summary>
    public static PathPoint[] Beziers(ReadOnlySpan<CubicBezier> curves, float spacing)
    {
        if (curves.IsEmpty || !float.IsFinite(spacing) || spacing <= 0) throw new ArgumentException("路径为空或间距无效。");
        var output = new System.Collections.Generic.List<PathPoint>();
        float length = 0;
        foreach (var b in curves)
        {
            if (!Finite(b.Start) || !Finite(b.Control1) || !Finite(b.Control2) || !Finite(b.End))
                throw new ArgumentException("路径控制点必须有限。");
            int count = (int)Math.Clamp(MathF.Ceiling((Vector2.Distance(b.Start, b.Control1) +
                Vector2.Distance(b.Control1, b.Control2) + Vector2.Distance(b.Control2, b.End)) / spacing), 2, MaximumPoints);
            for (int i = output.Count == 0 ? 0 : 1; i <= count; i++)
            {
                if (output.Count >= MaximumPoints) throw new ArgumentException("路径超过采样预算，请增大间距。");
                float t = (float)i / count, u = 1 - t;
                Vector2 p = u * u * u * b.Start + 3 * u * u * t * b.Control1 + 3 * u * t * t * b.Control2 + t * t * t * b.End;
                if (output.Count > 0) length += Vector2.Distance(output[^1].Position, p);
                output.Add(new(p, length));
            }
        }
        return output.ToArray();
    }
}

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AdvancedMod.Basic.Graphics.Geometry;

namespace AdvancedMod.Basic.Graphics.Rendering;

/// <summary>可复用的世界坐标三角形缓冲；所有宽度参数均为半宽。</summary>
public sealed class VfxMesh
{
    private VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[384];
    public VertexPositionColorTexture[] Vertices => vertices;
    public int Count { get; private set; }
    public void Clear() => Count = 0;
    public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color ca, Color cb, Color cc)
        => Triangle(a, b, c, ca, cb, cc, Vector2.Zero, Vector2.Zero, Vector2.Zero);

    public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color ca, Color cb, Color cc,
        Vector2 uvA, Vector2 uvB, Vector2 uvC)
    {
        if (!Paths.Finite(a) || !Paths.Finite(b) || !Paths.Finite(c)) return;
        float area = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        if (!float.IsFinite(area) || MathF.Abs(area) < 0.001f) return;
        if (Count + 3 > vertices.Length) Array.Resize(ref vertices, vertices.Length * 2);
        vertices[Count++] = new(new Vector3(a, 0), ca, uvA);
        vertices[Count++] = new(new Vector3(b, 0), cb, uvB);
        vertices[Count++] = new(new Vector3(c, 0), cc, uvC);
    }
    public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color start, Color end)
    { Triangle(a, b, c, start, start, end); Triangle(b, d, c, start, end, end); }

    public void Stroke(ReadOnlySpan<Vector2> points, float halfWidth, Color color, bool closed = false)
    {
        if (points.Length < 2 || !float.IsFinite(halfWidth) || halfWidth <= 0) return;
        int count = points.Length;
        if (closed && Vector2.DistanceSquared(points[0], points[^1]) < 0.0001f) count--;
        if (count < 2) return;
        for (int i = 0; i < (closed ? count : count - 1); i++)
        {
            int j = (i + 1) % count;
            if (Vector2.DistanceSquared(points[i], points[j]) < 0.0001f) continue;
            Vector2 na = Join(points, count, i, closed, halfWidth), nb = Join(points, count, j, closed, halfWidth);
            Quad(points[i] - na, points[i] + na, points[j] - nb, points[j] + nb, color, color);
        }
    }
    private static Vector2 Join(ReadOnlySpan<Vector2> points, int count, int i, bool closed, float width)
    {
        int before = closed ? (i + count - 1) % count : Math.Max(0, i - 1);
        int after = closed ? (i + 1) % count : Math.Min(count - 1, i + 1);
        Vector2 incoming = points[i] - points[before], outgoing = points[after] - points[i];
        if (incoming.LengthSquared() < 0.0001f) incoming = outgoing;
        if (outgoing.LengthSquared() < 0.0001f) outgoing = incoming;
        if (incoming.LengthSquared() < 0.0001f) return Vector2.Zero;
        incoming.Normalize(); outgoing.Normalize();
        Vector2 normal = new(-outgoing.Y, outgoing.X), miter = new(-incoming.Y - outgoing.Y, incoming.X + outgoing.X);
        if (miter.LengthSquared() < 0.0001f) return normal * width;
        miter.Normalize();
        return miter * Math.Min(width * 2, width / Math.Max(0.5f, Vector2.Dot(miter, normal)));
    }

    public void Sweep(TrailBuffer trail, double time, float lifetime, Color color, bool outerEdgeOnly = false)
    {
        if (!double.IsFinite(time) || !float.IsFinite(lifetime) || lifetime <= 0) return;
        for (int i = 1; i < trail.Count; i++)
        {
            var a = trail[i - 1]; var b = trail[i];
            if (b.BreakBefore) continue;
            Vector2 bladeA = a.OuterWorldPosition - a.InnerWorldPosition, bladeB = b.OuterWorldPosition - b.InnerWorldPosition;
            if (Vector2.Dot(bladeA, bladeB) <= 0) continue;
            Vector2 innerMotion = b.InnerWorldPosition - a.InnerWorldPosition, outerMotion = b.OuterWorldPosition - a.OuterWorldPosition;
            float areaA = bladeA.X * innerMotion.Y - bladeA.Y * innerMotion.X;
            float areaB = bladeB.X * outerMotion.Y - bladeB.Y * outerMotion.X;
            if (areaA * areaB < 0) continue;
            float wa = Math.Clamp(1 - (float)(time - a.SampleTime) / lifetime, 0, 1);
            float wb = Math.Clamp(1 - (float)(time - b.SampleTime) / lifetime, 0, 1);
            if (outerEdgeOnly)
            {
                // 单一外缘向内衰减，不叠加内侧亮环。
                Triangle(a.InnerWorldPosition, a.OuterWorldPosition, b.InnerWorldPosition, Color.Transparent, color * wa, Color.Transparent);
                Triangle(a.OuterWorldPosition, b.OuterWorldPosition, b.InnerWorldPosition, color * wa, color * wb, Color.Transparent);
                continue;
            }
            // 内侧透明、外侧柔化，避免把扫掠画成实心扇面。
            Vector2 ma = Vector2.Lerp(a.InnerWorldPosition, a.OuterWorldPosition, 0.7f), mb = Vector2.Lerp(b.InnerWorldPosition, b.OuterWorldPosition, 0.7f);
            Triangle(a.InnerWorldPosition, ma, b.InnerWorldPosition, Color.Transparent, color * wa, Color.Transparent);
            Triangle(ma, mb, b.InnerWorldPosition, color * wa, color * wb, Color.Transparent);
            Triangle(ma, a.OuterWorldPosition, mb, color * wa, Color.Transparent, color * wb);
            Triangle(a.OuterWorldPosition, b.OuterWorldPosition, mb, Color.Transparent, Color.Transparent, color * wb);
        }
    }

    /// <summary>外圆与偏心内圆在尾端内切；亮带内侧骤暗，再向中心空洞渐隐。</summary>
    public void TangentCrescent(Vector2 center, float radius, float headAngle, float sweep, float offsetRatio,
        float cutoffRatio, Color bright, Color dim, float opacity)
    {
        if (!Paths.Finite(center) || !float.IsFinite(radius) || radius <= 0 || !float.IsFinite(headAngle) ||
            !float.IsFinite(sweep) || MathF.Abs(sweep) < 0.0001f || !float.IsFinite(opacity) || opacity <= 0 ||
            offsetRatio <= 0 || offsetRatio >= 0.5f || cutoffRatio < 0 || cutoffRatio >= 1 - 2 * offsetRatio) return;
        const float angularStep = 0.025f;
        const float innerBrightness = 0.22f;
        int segments = Math.Clamp((int)MathF.Ceiling(MathF.Abs(sweep) / angularStep), 2, 256);
        float tail = headAngle - sweep;
        float d = radius * offsetRatio, innerRadius = radius - d;
        // 每段分别写亮带和暗内层顶点，使交界亮度不被三角形插值抹平。
        for (int i = 0; i < segments; i++)
        {
            float a = sweep * i / segments, b = sweep * (i + 1) / segments;
            Vector2 ua = Paths.Direction(tail + a), ub = Paths.Direction(tail + b);
            float ra = d * MathF.Cos(a) + MathF.Sqrt(innerRadius * innerRadius - d * d * MathF.Sin(a) * MathF.Sin(a));
            float rb = d * MathF.Cos(b) + MathF.Sqrt(innerRadius * innerRadius - d * d * MathF.Sin(b) * MathF.Sin(b));
            Vector2 oa = center + ua * radius, ob = center + ub * radius;
            Vector2 ia = center + ua * ra, ib = center + ub * rb;
            Vector2 ca = center + ua * (radius * cutoffRatio), cb = center + ub * (radius * cutoffRatio);
            Color outer = bright * opacity, inner = Color.Lerp(bright, dim, 0.18f) * (opacity * 0.9f);
            Triangle(oa, ob, ia, outer, outer, inner); Triangle(ob, ib, ia, outer, inner, inner);
            float ta = Math.Clamp((radius - ra) / (radius * offsetRatio), 0, 1);
            float tb = Math.Clamp((radius - rb) / (radius * offsetRatio), 0, 1);
            Color da = dim * (opacity * innerBrightness * ta), db = dim * (opacity * innerBrightness * tb);
            Triangle(ia, ib, ca, da, db, Color.Transparent); Triangle(ib, cb, ca, db, Color.Transparent, Color.Transparent);
        }
    }

    /// <summary>由弧头宽度反解偏心距；所有边界圆都与外圆在弧尾内切。</summary>
    public static float TangentCircleOffset(float radius, float width, float arc)
    {
        float inner = radius - width;
        return (radius * radius - inner * inner) / (2 * (radius - inner * MathF.Cos(arc)));
    }

    public static float TangentBoundaryRadius(float radius, float offset, float angle) =>
        offset * MathF.Cos(angle) + MathF.Sqrt(MathF.Max(0,
            (radius - offset) * (radius - offset) - offset * offset * MathF.Sin(angle) * MathF.Sin(angle)));

    /// <summary>三层条带各有独立相切圆边界；每层轻渐变，跨层保留亮度阶跃。</summary>
    public void LayeredTangentCrescent(Vector2 center, float radius, float headAngle, float sweep,
        float maximumArc, float maximumWidth, float cutoffRatio, Color outer, Color middle, Color inner, Color interior, float opacity)
    {
        if (!Paths.Finite(center) || !float.IsFinite(radius) || radius <= 0 || !float.IsFinite(headAngle) ||
            !float.IsFinite(sweep) || !float.IsFinite(maximumArc) || maximumArc <= 0 || maximumArc > MathF.PI ||
            !float.IsFinite(maximumWidth) || maximumWidth <= 0 || !float.IsFinite(cutoffRatio) || cutoffRatio < 0 || cutoffRatio >= 1 ||
            !float.IsFinite(opacity) || opacity <= 0) return;
        sweep = Math.Clamp(sweep, -maximumArc, maximumArc);
        if (MathF.Abs(sweep) < 0.0001f) return;
        float width = Math.Min(maximumWidth, radius * (1 - cutoffRatio) * 0.9f);
        Span<float> offsets = stackalloc float[4];
        offsets[0] = 0;
        for (int layer = 1; layer <= 3; layer++) offsets[layer] = TangentCircleOffset(radius, width * layer / 3, maximumArc);
        Span<Color> colors = stackalloc Color[3] { outer * opacity, middle * opacity, inner * opacity };
        const float angularStep = 0.02f;
        const float layerInnerFade = 0.88f;
        int segments = Math.Clamp((int)MathF.Ceiling(MathF.Abs(sweep) / angularStep), 2, 256);
        float tail = headAngle - sweep;
        for (int i = 0; i < segments; i++)
        {
            float a = sweep * i / segments, b = sweep * (i + 1) / segments;
            Vector2 ua = Paths.Direction(tail + a), ub = Paths.Direction(tail + b);
            for (int layer = 0; layer < 3; layer++)
            {
                Vector2 oa = center + ua * TangentBoundaryRadius(radius, offsets[layer], a);
                Vector2 ob = center + ub * TangentBoundaryRadius(radius, offsets[layer], b);
                Vector2 ia = center + ua * TangentBoundaryRadius(radius, offsets[layer + 1], a);
                Vector2 ib = center + ub * TangentBoundaryRadius(radius, offsets[layer + 1], b);
                Color c = colors[layer], faded = c * layerInnerFade;
                Triangle(oa, ob, ia, c, c, faded); Triangle(ob, ib, ia, c, faded, faded);
            }
            float ra = TangentBoundaryRadius(radius, offsets[3], a), rb = TangentBoundaryRadius(radius, offsets[3], b);
            Vector2 da = center + ua * ra, db = center + ub * rb;
            Vector2 ca = center + ua * radius * cutoffRatio, cb = center + ub * radius * cutoffRatio;
            Color ac = interior * (opacity * Math.Clamp((radius - ra) / width, 0, 1));
            Color bc = interior * (opacity * Math.Clamp((radius - rb) / width, 0, 1));
            Triangle(da, db, ca, ac, bc, Color.Transparent); Triangle(db, cb, ca, bc, Color.Transparent, Color.Transparent);
        }
    }

    public void GlowPoint(Vector2 center, float radius, Color color, int segments = 12)
    {
        for (int i = 0; i < segments; i++)
            Triangle(center, center + Paths.Direction(MathHelper.TwoPi * i / segments) * radius,
                center + Paths.Direction(MathHelper.TwoPi * (i + 1) / segments) * radius, color, Color.Transparent, Color.Transparent);
    }
}

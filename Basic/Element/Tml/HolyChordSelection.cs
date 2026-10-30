using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Xna.Framework;
using Terraria;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace AdvancedMod.Basic.Element.Tml;

/// <summary>圆内任意弦选取；矩形顶点支撑线形成候选覆盖集合，再搜索五条以内的最大覆盖。</summary>
internal static class HolyChordSelection
{
    internal sealed record Line(Vector2 Start, Vector2 End, int[] Targets, BigInteger Mask);
    internal static List<Line> Select(Vector2 center, float radius, Rectangle[] boxes)
    {
        var points = boxes.SelectMany(b => new[] { new Vector2(b.Left, b.Top), new Vector2(b.Right, b.Top),
            new Vector2(b.Left, b.Bottom), new Vector2(b.Right, b.Bottom), b.Center.ToVector2() }).Distinct().ToArray();
        var unique = new Dictionary<BigInteger, Line>();
        void Add(Vector2 p, Vector2 direction)
        {
            if (direction.LengthSquared() == 0) return;
            direction.Normalize();
            Vector2 foot = p + direction * Vector2.Dot(center - p, direction);
            float squared = radius * radius - Vector2.DistanceSquared(foot, center);
            if (squared < 0) return;
            float extent = MathF.Sqrt(squared);
            Vector2 a = foot - direction * extent, b = foot + direction * extent;
            var hits = Enumerable.Range(0, boxes.Length).Where(i => Intersects(a, b, boxes[i])).ToArray();
            if (hits.Length == 0) return;
            BigInteger mask = 0;
            foreach (int i in hits) mask |= BigInteger.One << i;
            unique.TryAdd(mask, new(a, b, hits, mask));
        }
        foreach (var p in points) Add(p, Vector2.UnitX);
        for (int i = 0; i < points.Length; i++)
            for (int j = i + 1; j < points.Length; j++) Add(points[i], points[j] - points[i]);
        var sorted = unique.Values.OrderByDescending(l => l.Targets.Length).ToArray();
        var useful = new List<Line>();
        foreach (var line in sorted)
            if (!useful.Any(other => (line.Mask | other.Mask) == other.Mask)) useful.Add(line);
        var best = new List<Line>();
        var path = new List<Line>();
        int bestCount = 0;
        int Count(BigInteger mask) => (int)BigInteger.PopCount(mask);
        void Search(int start, BigInteger covered)
        {
            int count = Count(covered);
            if (count > bestCount) { bestCount = count; best = new(path); }
            int remaining = HolyReactionRules.MaximumLines - path.Count;
            if (remaining == 0 || bestCount == boxes.Length) return;
            var choices = Enumerable.Range(start, useful.Count - start)
                .Select(i => (Index: i, Gain: Count(useful[i].Mask & ~covered))).Where(x => x.Gain > 0)
                .OrderByDescending(x => x.Gain).ToArray();
            if (count + choices.Take(remaining).Sum(x => x.Gain) <= bestCount) return;
            foreach (var choice in choices)
            {
                path.Add(useful[choice.Index]);
                Search(choice.Index + 1, covered | useful[choice.Index].Mask);
                path.RemoveAt(path.Count - 1);
                if (bestCount == boxes.Length) return;
            }
        }
        Search(0, BigInteger.Zero);
        return best;
    }
    internal static bool Intersects(Vector2 a, Vector2 b, Rectangle box)
    {
        double low = 0, high = 1;
        bool Clip(double start, double delta, double min, double max)
        {
            if (Math.Abs(delta) < 1e-12) return start >= min && start <= max;
            double t0 = (min - start) / delta, t1 = (max - start) / delta;
            if (t0 > t1) (t0, t1) = (t1, t0);
            low = Math.Max(low, t0); high = Math.Min(high, t1); return low <= high;
        }
        return Clip(a.X, b.X - a.X, box.Left, box.Right) && Clip(a.Y, b.Y - a.Y, box.Top, box.Bottom);
    }
}

using System;
using System.Collections.Generic;

namespace AdvancedMod.Basic.Element;

/// <summary>神圣反应的数值规则；时间使用游戏刻，位置使用世界像素。</summary>
public static class HolyReactionRules
{
    public const int Lifetime = 1200, RecordWindow = 1200, TornadoCooldown = 900;
    public const int TornadoThreshold = 150, LifePerBounce = 15, MaximumBounces = 64;
    public const int MaximumNodes = 10, ChargeThreshold = 20, MaximumLines = 5, MaximumFlight = 90;
    public const float BaseRadius = 1024, TeleportRadiusFactor = 4, BridgeRadius = 60;
    // 原版速度仪使用每英里42240像素、每小时216000游戏刻。
    public const float PixelsPerTickPerMph = 42240f / 216000f;
    public const float FieldSpeed = 100 * PixelsPerTickPerMph, TornadoSpeed = 45 * PixelsPerTickPerMph;
    public const float ResistancePerLife = 0.0002f, MaximumReduction = 0.15f, TornadoDamagePerLevel = 2.5f;

    public static int Payment(int proposed, int lifeBefore, int predictedDamage) =>
        proposed <= 0 || lifeBefore <= 0 || predictedDamage <= 0 ? 0 :
        (int)Math.Ceiling(proposed * Math.Min(1d, (double)lifeBefore / predictedDamage));
    public static int Bounces(int recordedLife) => Math.Min(MaximumBounces, Math.Max(0, recordedLife) / LifePerBounce);
    public static float Reduction(int recordedLife) => Math.Min(MaximumReduction, Math.Max(0, recordedLife) * ResistancePerLife);
    public static float FlightSpeed(float distance, int elapsed) =>
        Math.Max(TornadoSpeed, distance / Math.Max(1, MaximumFlight - elapsed));
}

/// <summary>逐笔过期的实际生命支付记录，不把拟支付量当作已扣生命。</summary>
internal sealed class HolyLifeRecord
{
    private readonly Queue<(long Tick, int Life)> entries = new();
    internal int Total { get; private set; }
    internal void Expire(long tick)
    {
        while (entries.TryPeek(out var entry) && tick - entry.Tick >= HolyReactionRules.RecordWindow)
        { entries.Dequeue(); Total -= entry.Life; }
    }
    internal void Add(long tick, int life)
    {
        Expire(tick);
        if (life <= 0) return;
        entries.Enqueue((tick, life)); Total = checked(Total + life);
    }
}

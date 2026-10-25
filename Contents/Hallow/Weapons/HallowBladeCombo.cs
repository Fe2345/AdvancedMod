using System;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>天裁之刃的四段数值与衔接状态；角度采用屏幕坐标，正角为顺时针。</summary>
public static class HallowBladeRules
{
    public const int BaseDamage = 135;
    public const int BaseUseTicks = 40;
    public const int ComboWindowTicks = 150;
    public const int StageCount = 4;
    public const int BonusCrit = 4; // 原生玩家基础暴击 4%，武器追加 4%，无加成时面板为 8%。
    public const float Knockback = 6;
    //攻击半径
    public const float BladeLength = 192;
    public const float CollisionWidth = 16;
    public const int Subupdates = 3;
    public static float Sweep(int stage) => stage switch
    {
        0 => MathF.PI, 1 => -MathF.Tau, 2 => 2 * MathF.Tau, 3 => MathF.Tau,
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };
    public static float DamageMultiplier(int stage) => stage switch
    {
        0 or 1 => 1, 2 => 0.75f, 3 => 2.75f,
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };
    public const float AccelerationEnd = 0.10f;
    public const float StrikeEnd = 0.45f;
    public const float FollowThroughStart = 0.62f;
    public const float DecelerationStart = 0.94f;
    public const float FollowThroughSpeed = 0.22f;
    // 短起手、集中快挥、低速随动、短收尾。积分后归一化，保留原有段末角度。
    public static float MotionProgress(float progress)
    {
        float t = Math.Clamp(progress, 0, 1);
        static float Segment(float t, float start, float end, float from, float to)
        {
            float x = Math.Clamp((t - start) / (end - start), 0, 1);
            return (end - start) * (from * x + (to - from) * (x * x * x - x * x * x * x / 2));
        }
        float Area(float x) => Segment(x, 0, AccelerationEnd, 0, 1) +
            Segment(x, AccelerationEnd, StrikeEnd, 1, 1) +
            Segment(x, StrikeEnd, FollowThroughStart, 1, FollowThroughSpeed) +
            Segment(x, FollowThroughStart, DecelerationStart, FollowThroughSpeed, FollowThroughSpeed) +
            Segment(x, DecelerationStart, 1, FollowThroughSpeed, 0);
        return Area(t) / Area(1);
    }
    public static float Angle(int stage, float startAngle, float progress) => startAngle + Sweep(stage) * MotionProgress(progress);
    public static int HitCycle(int stage, float progress) => stage == 2 && MotionProgress(progress) > 0.5f ? 1 : 0;
}

/// <summary>每把物品独立保存；等待窗口从上一段自然结束时开始，不随攻速缩短。</summary>
public sealed class HallowBladeCombo
{
    public int NextStage { get; private set; }
    public float EndAngle { get; private set; }
    private ulong completedAt;
    private bool canContinue;
    public (int Stage, float StartAngle) Begin(float aimAngle, ulong tick)
    {
        if (!float.IsFinite(aimAngle)) throw new ArgumentOutOfRangeException(nameof(aimAngle));
        if (!canContinue || tick < completedAt || tick - completedAt >= HallowBladeRules.ComboWindowTicks) Reset();
        return (NextStage, canContinue ? EndAngle : aimAngle - MathF.PI / 2);
    }
    public void Complete(int stage, float endAngle, ulong tick)
    {
        if (stage < 0 || stage >= HallowBladeRules.StageCount || !float.IsFinite(endAngle)) { Reset(); return; }
        NextStage = (stage + 1) % HallowBladeRules.StageCount;
        EndAngle = endAngle; completedAt = tick;
        // 四段完成后开始新一轮，首段可重新选择攻击方向。
        canContinue = NextStage != 0;
    }
    public void Reset() { NextStage = 0; EndAngle = 0; completedAt = 0; canContinue = false; }
}

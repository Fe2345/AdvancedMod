#nullable enable
using System;
using AdvancedMod.Basic.Calculate.Core;

namespace AdvancedMod.Basic.Calculate.DamageOverTime;

/// <summary>白值已经包含职业增伤。暴击率和额外暴伤均以百分数表示。</summary>
public sealed record DotRequest(double BaseDamage, double CriticalChance = 0,
    double CriticalDamage = 100, Func<double, double, double>? DefenseFormula = null)
{
    public void Validate()
    {
        DotMath.NonNegative(BaseDamage, nameof(BaseDamage));
        DotMath.NonNegative(CriticalChance, nameof(CriticalChance));
        if (CriticalChance > 100) throw new ArgumentOutOfRangeException(nameof(CriticalChance));
        DotMath.NonNegative(CriticalDamage, nameof(CriticalDamage));
    }
}

/// <summary>本次提交的计算值，不代表生命恢复合并之后的实际扣血或普通命中。</summary>
public readonly record struct DotResult(double Damage, bool Critical);

public static class DotMath
{
    public const int LifeRegenUnitsPerHealth = 120;
    public const double DefaultDefenseK = 300;
    public const double DefaultDefenseRho = 0.15;

    /// <summary>预制防御减伤；每次提交独立扣减，不按秒换算。</summary>
    public static double ApplyDefense(double damage, double defense) =>
        ApplyDefense(damage, defense, DefaultDefenseK, DefaultDefenseRho);

    public static double ApplyDefense(double damage, double defense, double k, double rho)
    {
        NonNegative(damage, nameof(damage));
        if (!double.IsFinite(defense)) throw new ArgumentOutOfRangeException(nameof(defense));
        if (!double.IsFinite(k) || k <= 0) throw new ArgumentOutOfRangeException(nameof(k));
        if (!double.IsFinite(rho) || rho < 0 || rho > 1) throw new ArgumentOutOfRangeException(nameof(rho));
        double ratio = defense / k;
        return Math.Max(0, damage - defense * (rho + (1 - rho) / Math.Sqrt(1 + ratio * ratio)));
    }

    /// <summary>投掷值使用 [0,1)，便于离线重现；不读取实体或全局随机数。</summary>
    public static DotResult Calculate(DotRequest request, CalculationResult modifiers, double defense, double roll)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(modifiers);
        request.Validate();
        if (modifiers.Plan.Kind is not (HitKind.DamageOverTimeToNpc or HitKind.DamageOverTimeToPlayer))
            throw new ArgumentException("持续伤害必须使用专用上下文。", nameof(modifiers));
        if (!double.IsFinite(defense)) throw new ArgumentOutOfRangeException(nameof(defense));
        if (!double.IsFinite(roll) || roll < 0 || roll >= 1) throw new ArgumentOutOfRangeException(nameof(roll));
        double damage = request.BaseDamage * modifiers.ReactionFactor * modifiers.ResistanceFactor;
        // 自定义乘区允许负值；持续伤害不能借负伤害变成回血。
        damage = Math.Max(0, damage);
        NonNegative(damage, nameof(damage));
        if (request.DefenseFormula is not null) damage = request.DefenseFormula(damage, defense);
        NonNegative(damage, "防御委托返回值");
        bool critical = roll < request.CriticalChance / 100;
        if (critical) damage *= 1 + request.CriticalDamage / 100;
        damage = Math.Max(0, damage * modifiers.AfterDefenseFactor);
        NonNegative(damage, nameof(damage));
        return new(damage, critical);
    }

    internal static void NonNegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(name);
    }
}


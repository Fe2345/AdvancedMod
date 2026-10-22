#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AdvancedMod.Basic.Calculate.Core;

public enum NativeField
{
    SourceDamage, FinalDamage, Knockback, Defense, CritDamage, NonCritDamage,
    ArmorPenetration, ScalingArmorPenetration, FlatBonusDamage, ScalingBonusDamage,
    DefenseEffectiveness, DamageVariationScale, IncomingDamageMultiplier, KnockbackImmunityEffectiveness,
    CriticalStrike, IgnoreDefense, MaximumDamage, InstantKill, CancelHurt, KnockbackDisabled, HitDirection
}
public enum NumericOperation { AddBase, AddPercent, Multiply, AddFlat, Add }
public enum CriticalStrikePolicy { Inherit, Force, Disable }
internal enum NativeValueKind { Stat, Add, Multiply, Control }

public sealed record NativeNumericContribution(int ContributorId, string EntryId, NativeField Field,
    NumericOperation Operation, float Value, long Sequence);
public sealed record ControlContribution(int ContributorId, string EntryId, NativeField Field, int Value, long Sequence);
public sealed record StatModifierDelta(float Base, float AdditiveBonus, float Multiplicative, float Flat)
{
    public static StatModifierDelta Identity { get; } = new(0f, 0f, 1f, 0f);
}
public sealed record NativeFieldResult(NativeField Field, GateResolution Gate, StatModifierDelta? Stat,
    float? NumericValue, int? ControlValue);

/// <summary>按原生字段枚举保存强类型增量，不包含最终伤害，不使用任意对象载荷。</summary>
public sealed class NativeModifierPlan
{
    public HitKind Kind { get; }
    public ReadOnlyCollection<NativeFieldResult> Fields { get; }
    internal NativeModifierPlan(HitKind kind, NativeFieldResult[] fields)
    {
        Kind = kind;
        Fields = Array.AsReadOnly(fields);
    }
}

/// <summary>首版固定的 TML 能力表，未支持的字段／操作必须报错。</summary>
internal static class NativeCapabilities
{
    public static bool Supports(HitKind hit, NativeField field)
    {
        if (!Enum.IsDefined(field)) return false;
        bool npc = hit is HitKind.ItemToNpc or HitKind.ProjectileToNpc;
        return field switch
        {
            NativeField.Defense or NativeField.CritDamage or NativeField.NonCritDamage or
            NativeField.FlatBonusDamage or NativeField.ScalingBonusDamage or NativeField.DefenseEffectiveness or
            NativeField.DamageVariationScale or NativeField.CriticalStrike or NativeField.InstantKill or NativeField.KnockbackDisabled => npc,
            NativeField.IncomingDamageMultiplier or NativeField.KnockbackImmunityEffectiveness or NativeField.CancelHurt => !npc,
            _ => true
        };
    }

    public static NativeValueKind Kind(NativeField field) => field switch
    {
        NativeField.SourceDamage or NativeField.FinalDamage or NativeField.Knockback or NativeField.Defense or
            NativeField.CritDamage or NativeField.NonCritDamage => NativeValueKind.Stat,
        NativeField.ArmorPenetration or NativeField.ScalingArmorPenetration or NativeField.FlatBonusDamage or
            NativeField.ScalingBonusDamage => NativeValueKind.Add,
        NativeField.DefenseEffectiveness or NativeField.DamageVariationScale or NativeField.IncomingDamageMultiplier or
            NativeField.KnockbackImmunityEffectiveness => NativeValueKind.Multiply,
        _ when Enum.IsDefined(field) => NativeValueKind.Control,
        _ => throw new ArgumentOutOfRangeException(nameof(field))
    };

    public static void Validate(NativeField field, NumericOperation operation, float value, HitKind? hit = null)
    {
        if (hit.HasValue && !Supports(hit.Value, field)) throw new ArgumentException($"命中方向不支持字段 {field}。");
        bool allowed = Kind(field) switch
        {
            NativeValueKind.Stat => operation is NumericOperation.AddBase or NumericOperation.AddPercent or NumericOperation.Multiply or NumericOperation.AddFlat,
            NativeValueKind.Add => operation == NumericOperation.Add,
            NativeValueKind.Multiply => operation == NumericOperation.Multiply,
            _ => false
        };
        if (!allowed || !float.IsFinite(value)) throw new ArgumentException($"字段 {field} 的操作或数值无效。");
    }

    public static void Validate(NativeField field, int value, HitKind? hit = null)
    {
        if (hit.HasValue && !Supports(hit.Value, field)) throw new ArgumentException($"命中方向不支持字段 {field}。");
        bool allowed = field switch
        {
            NativeField.CriticalStrike => Enum.IsDefined((CriticalStrikePolicy)value),
            NativeField.MaximumDamage => value > 0,
            NativeField.HitDirection => value is -1 or 0 or 1,
            NativeField.IgnoreDefense or NativeField.InstantKill or NativeField.CancelHurt or NativeField.KnockbackDisabled => value is 0 or 1,
            _ => false
        };
        if (!allowed) throw new ArgumentException($"字段 {field} 的控制值无效。");
    }
}

internal static class NativeCombiner
{
    public static NativeModifierPlan Evaluate(CalculationInput input, List<ContributionDecision>? diagnostics,
        float reactionFactor, float resistanceFactor, float afterDefenseFactor)
    {
        var results = new List<NativeFieldResult>();
        foreach (NativeField field in Enum.GetValues<NativeField>())
        {
            if (!NativeCapabilities.Supports(input.Hit.Kind, field)) continue;
            ModifierTarget target = ModifierTarget.Native(field);
            GateDecision? winner = input.Gates.LastOrDefault(gate => gate.Target == target);
            var gate = new GateResolution(target, winner?.Enabled ?? true, winner?.Sequence);
            StatModifierDelta stat = StatModifierDelta.Identity;
            float value = NativeCapabilities.Kind(field) == NativeValueKind.Multiply ? 1f : 0f;
            foreach (NativeNumericContribution candidate in input.NativeContributions.Where(c => c.Field == field))
            {
                if (gate.Enabled)
                {
                    if (NativeCapabilities.Kind(field) == NativeValueKind.Stat)
                        stat = candidate.Operation switch
                        {
                            NumericOperation.AddBase => stat with { Base = Finite(stat.Base + candidate.Value) },
                            NumericOperation.AddPercent => stat with { AdditiveBonus = Finite(stat.AdditiveBonus + candidate.Value) },
                            NumericOperation.Multiply => stat with { Multiplicative = Finite(stat.Multiplicative * candidate.Value) },
                            NumericOperation.AddFlat => stat with { Flat = Finite(stat.Flat + candidate.Value) },
                            _ => throw new InvalidOperationException("无效的 StatModifier 操作。")
                        };
                    else value = Finite(candidate.Operation == NumericOperation.Multiply ? value * candidate.Value : value + candidate.Value);
                }
                diagnostics?.Add(new(candidate.Sequence, gate.Enabled ? ContributionDisposition.Accepted : ContributionDisposition.TargetDisabled, gate.WinningSequence));
            }

            ControlContribution[] controls = input.Controls.Where(c => c.Field == field).ToArray();
            ControlContribution? selected = !gate.Enabled ? null : field == NativeField.MaximumDamage
                ? controls.OrderBy(c => c.Value).ThenByDescending(c => c.Sequence).FirstOrDefault()
                : controls.LastOrDefault();
            foreach (ControlContribution candidate in controls)
                diagnostics?.Add(new(candidate.Sequence,
                    !gate.Enabled ? ContributionDisposition.TargetDisabled : candidate == selected ? ContributionDisposition.Accepted
                    : field == NativeField.MaximumDamage && candidate.Value != selected?.Value ? ContributionDisposition.LessStrictCap : ContributionDisposition.Overridden,
                    gate.WinningSequence, selected?.Sequence));

            NativeValueKind kind = NativeCapabilities.Kind(field);
            // 各目标的候选已经分别筛选；关闭原生目标不连带关闭独立自定义槽。
            if (field == NativeField.SourceDamage)
                stat = stat with { Multiplicative = Finite(Finite(stat.Multiplicative * reactionFactor) * resistanceFactor) };
            else if (field == NativeField.FinalDamage)
                stat = stat with { Multiplicative = Finite(stat.Multiplicative * afterDefenseFactor) };
            results.Add(new(field, gate, kind == NativeValueKind.Stat ? stat : null,
                kind is NativeValueKind.Add or NativeValueKind.Multiply ? value : null, selected?.Value));
        }
        return new(input.Hit.Kind, results.ToArray());
    }

    private static float Finite(float value) => float.IsFinite(value) ? value
        : throw new ArithmeticException("原生字段折叠产生非有限数值。");
}

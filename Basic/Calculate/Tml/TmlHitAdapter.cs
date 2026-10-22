using System;
using AdvancedMod.Basic.Calculate.Core;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Basic.Calculate.Tml;

/// <summary>将已折叠的自定义倍率合并到现有 TML 修饰器，不重置原有修正。</summary>
public static class TmlHitAdapter
{
    public static void Apply(CalculationResult result, ref NPC.HitModifiers modifiers)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Plan.Kind is not (HitKind.ItemToNpc or HitKind.ProjectileToNpc))
            throw new ArgumentException("不能将玩家受伤计划写入 NPC 修饰器。", nameof(result));
        bool ignoreDefense = false;
        foreach (NativeFieldResult field in result.Plan.Fields)
        {
            switch (field.Field)
            {
                case NativeField.SourceDamage: Merge(ref modifiers.SourceDamage, field.Stat); break;
                case NativeField.FinalDamage: Merge(ref modifiers.FinalDamage, field.Stat); break;
                case NativeField.Knockback: Merge(ref modifiers.Knockback, field.Stat); break;
                case NativeField.Defense: Merge(ref modifiers.Defense, field.Stat); break;
                case NativeField.CritDamage: Merge(ref modifiers.CritDamage, field.Stat); break;
                case NativeField.NonCritDamage: Merge(ref modifiers.NonCritDamage, field.Stat); break;
                case NativeField.ArmorPenetration: modifiers.ArmorPenetration += field.NumericValue.Value; break;
                case NativeField.ScalingArmorPenetration: modifiers.ScalingArmorPenetration += field.NumericValue.Value; break;
                case NativeField.FlatBonusDamage: modifiers.FlatBonusDamage += field.NumericValue.Value; break;
                case NativeField.ScalingBonusDamage: modifiers.ScalingBonusDamage += field.NumericValue.Value; break;
                case NativeField.DefenseEffectiveness: modifiers.DefenseEffectiveness *= field.NumericValue.Value; break;
                case NativeField.DamageVariationScale: modifiers.DamageVariationScale *= field.NumericValue.Value; break;
                case NativeField.CriticalStrike:
                    if (field.ControlValue == (int)CriticalStrikePolicy.Force) modifiers.SetCrit();
                    else if (field.ControlValue == (int)CriticalStrikePolicy.Disable) modifiers.DisableCrit();
                    break;
                case NativeField.IgnoreDefense: ignoreDefense = field.ControlValue == 1; break;
                case NativeField.MaximumDamage: if (field.ControlValue is int limit) modifiers.SetMaxDamage(limit); break;
                case NativeField.InstantKill: if (field.ControlValue == 1) modifiers.SetInstantKill(); break;
                case NativeField.KnockbackDisabled: if (field.ControlValue == 1) modifiers.DisableKnockback(); break;
                case NativeField.HitDirection: if (field.ControlValue is int direction) modifiers.HitDirectionOverride = direction; break;
                default: throw new InvalidOperationException("计划包含 NPC 不支持的原生字段。");
            }
        }
        if (ignoreDefense) modifiers.ScalingArmorPenetration += Math.Max(0f, 1f - modifiers.ScalingArmorPenetration.Value);
    }

    public static void Apply(CalculationResult result, ref Player.HurtModifiers modifiers)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Plan.Kind is not (HitKind.NpcToPlayer or HitKind.ProjectileToPlayer))
            throw new ArgumentException("不能将 NPC 受击计划写入玩家修饰器。", nameof(result));
        bool ignoreDefense = false;
        foreach (NativeFieldResult field in result.Plan.Fields)
        {
            switch (field.Field)
            {
                case NativeField.SourceDamage: Merge(ref modifiers.SourceDamage, field.Stat); break;
                case NativeField.FinalDamage: Merge(ref modifiers.FinalDamage, field.Stat); break;
                case NativeField.Knockback: Merge(ref modifiers.Knockback, field.Stat); break;
                case NativeField.ArmorPenetration: modifiers.ArmorPenetration += field.NumericValue.Value; break;
                case NativeField.ScalingArmorPenetration: modifiers.ScalingArmorPenetration += field.NumericValue.Value; break;
                case NativeField.IncomingDamageMultiplier: modifiers.IncomingDamageMultiplier *= field.NumericValue.Value; break;
                case NativeField.KnockbackImmunityEffectiveness: modifiers.KnockbackImmunityEffectiveness *= field.NumericValue.Value; break;
                case NativeField.IgnoreDefense: ignoreDefense = field.ControlValue == 1; break;
                case NativeField.MaximumDamage: if (field.ControlValue is int limit) modifiers.SetMaxDamage(limit); break;
                case NativeField.CancelHurt: if (field.ControlValue == 1) modifiers.Cancel(); break;
                case NativeField.HitDirection: if (field.ControlValue is int direction) modifiers.HitDirectionOverride = direction; break;
                default: throw new InvalidOperationException("计划包含玩家不支持的原生字段。");
            }
        }
        if (ignoreDefense) modifiers.ScalingArmorPenetration += Math.Max(0f, 1f - modifiers.ScalingArmorPenetration.Value);
    }

    private static void Merge(ref StatModifier target, StatModifierDelta delta)
    {
        target.Base += delta.Base;
        target += delta.AdditiveBonus;
        target *= delta.Multiplicative;
        target.Flat += delta.Flat;
    }
}

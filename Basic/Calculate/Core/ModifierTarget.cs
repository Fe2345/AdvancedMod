#nullable enable
using System;
using System.Collections.Generic;

namespace AdvancedMod.Basic.Calculate.Core;

/// <summary>自定义乘区的容量和公式参数。</summary>
public static class DamageConstants
{
    public const int ReactionSlotCount = 20;
    public const int ElementSlotCount = 10;
    public const int AscensionSlotCount = 4;
    public const float PositiveMasteryScale = 6f;
    public const float PositiveMasteryDenominator = 2000f;
    public const float NegativeMasteryDenominator = 500f;
}

public enum ModifierTargetKind
{
    Invalid,
    Reaction,
    Resistance,
    ElementDamage,
    Mastery,
    Ascension,
    Reserve,
    Native
}

/// <summary>逻辑阶段；本阶段不据此直接修改 TML 字段。</summary>
public enum DamageStage
{
    AfterBaseDamage,
    BeforeDefense,
    AfterDefense,
    Native
}

/// <summary>一个可独立启停的自定义目标，不提供整个数组的目标。</summary>
public readonly record struct ModifierTarget
{
    public ModifierTargetKind Kind { get; }
    public int? Index { get; }
    public string? Key { get; }
    public NativeField? Field { get; }

    private ModifierTarget(ModifierTargetKind kind, int? index = null, string? key = null, NativeField? field = null)
    {
        Kind = kind;
        Index = index;
        Key = key;
        Field = field;
    }

    public static ModifierTarget Reaction(int index) => Slot(ModifierTargetKind.Reaction, index, DamageConstants.ReactionSlotCount);
    public static ModifierTarget Resistance => new(ModifierTargetKind.Resistance);
    public static ModifierTarget ElementDamage(int index) => Slot(ModifierTargetKind.ElementDamage, index, DamageConstants.ElementSlotCount);
    public static ModifierTarget Mastery => new(ModifierTargetKind.Mastery);
    public static ModifierTarget Ascension(int index) => Slot(ModifierTargetKind.Ascension, index, DamageConstants.AscensionSlotCount);
    public static ModifierTarget Native(NativeField field)
    {
        if (!Enum.IsDefined(field)) throw new ArgumentOutOfRangeException(nameof(field));
        return new(ModifierTargetKind.Native, field: field);
    }

    public static ModifierTarget Reserve(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return new(ModifierTargetKind.Reserve, key: key);
    }

    public DamageStage Stage => Kind switch
    {
        ModifierTargetKind.Reaction => DamageStage.AfterBaseDamage,
        ModifierTargetKind.Native => DamageStage.Native,
        ModifierTargetKind.Resistance => DamageStage.BeforeDefense,
        ModifierTargetKind.ElementDamage or ModifierTargetKind.Mastery or
            ModifierTargetKind.Ascension or ModifierTargetKind.Reserve => DamageStage.AfterDefense,
        _ => throw new InvalidOperationException("乘区目标尚未初始化。")
    };

    internal void Validate(ISet<string> reserveKeys)
    {
        _ = Stage;
        if (Kind == ModifierTargetKind.Reserve && !reserveKeys.Contains(Key!))
            throw new ArgumentException($"备用区键尚未登记：{Key}。");
    }

    private static ModifierTarget Slot(ModifierTargetKind kind, int index, int count)
    {
        if ((uint)index >= count)
            throw new ArgumentOutOfRangeException(nameof(index), index, $"槽号必须在 0 至 {count - 1} 之间。");
        return new(kind, index);
    }
}

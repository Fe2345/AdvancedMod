#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Tag;

namespace AdvancedMod.Basic.Element;

/// <summary>显式槽位映射，沿用现有标签注释；预留槽不自动登记。</summary>
public static class ElementSlots
{
    public static int? FromTag(TagEnum tag) => tag switch
    {
        TagEnum.Pyro => 1, TagEnum.Hydro => 2, TagEnum.Electro => 3,
        TagEnum.Cryo => 4, TagEnum.Chlorophyte => 5, TagEnum.Anemo => 6, _ => null
    };
    public static int? ReactionFromTag(TagEnum tag) => tag switch
    {
        TagEnum.HydroElectro => 1, TagEnum.HydroAnemo => 2, TagEnum.PyroCryo => 3, _ => null
    };
}

public sealed record ElementCost(int Slot, double Amount);

/// <summary>纯数值反应定义；槽编号就是优先级，数值由注册方提供。</summary>
public sealed class ReactionDefinition
{
    public int Slot { get; }
    public TagEnum Tag { get; }
    public double Coefficient { get; }
    public float BaseCooldown { get; }
    public ReadOnlyCollection<ElementCost> Costs { get; }
    public ReactionDefinition(TagEnum tag, double coefficient, float baseCooldown, params ElementCost[] costs)
    {
        Slot = ElementSlots.ReactionFromTag(tag) ?? throw new ArgumentException("未登记的反应标签。", nameof(tag));
        if (!double.IsFinite(coefficient) || coefficient <= 0 || !float.IsFinite(baseCooldown) || baseCooldown < 0)
            throw new ArgumentOutOfRangeException(nameof(coefficient), "系数须为有限正数，CD 须为有限非负数。");
        ArgumentNullException.ThrowIfNull(costs);
        if (costs.Length < 2) throw new ArgumentException("反应至少需要两种元素。", nameof(costs));
        var seen = new HashSet<int>();
        foreach (var cost in costs)
        {
            if (cost is null || cost.Slot < 0 || cost.Slot >= DamageConstants.ElementSlotCount ||
                !double.IsFinite(cost.Amount) || cost.Amount <= 0 || !seen.Add(cost.Slot))
                throw new ArgumentException("基础消耗须有限且为正，元素槽须合法且唯一。", nameof(costs));
        }
        Tag = tag;
        Coefficient = coefficient;
        BaseCooldown = baseCooldown;
        Costs = Array.AsReadOnly((ElementCost[])costs.Clone());
    }
}

/// <summary>固定容量的反应表，按槽编号遍历，不提供额外优先级函数。</summary>
public sealed class ReactionTable
{
    internal ReadOnlyCollection<ReactionDefinition?> Slots { get; }
    public ReactionTable(params ReactionDefinition[] definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        var slots = new ReactionDefinition?[DamageConstants.ReactionSlotCount];
        foreach (var definition in definitions)
        {
            ArgumentNullException.ThrowIfNull(definition);
            if (slots[definition.Slot] is not null) throw new ArgumentException("反应槽重复登记。");
            slots[definition.Slot] = definition;
        }
        Slots = Array.AsReadOnly(slots);
    }
}

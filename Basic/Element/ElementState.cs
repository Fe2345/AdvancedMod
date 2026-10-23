#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AdvancedMod.Basic.Calculate.Core;

namespace AdvancedMod.Basic.Element;

/// <summary>一次附着；面板白值沿用 TML 的整数伤害字段。</summary>
public sealed record ElementAttachment(int Slot, double Amount, int PanelDamage);
public sealed record ElementPortion(int Slot, int PanelDamage, double Amount);

/// <summary>只读元素槽快照；分量以攻击面板白值为键。</summary>
public sealed class ElementGauge
{
    public double Total { get; }
    public IReadOnlyDictionary<int, double> Portions { get; }
    internal ElementGauge(Dictionary<int, double> portions)
    {
        Total = portions.Values.Sum();
        Portions = new ReadOnlyDictionary<int, double>(new Dictionary<int, double>(portions));
    }
}

/// <summary>目标已提交状态的独立快照，不能通过读取结果修改目标。</summary>
public sealed class ElementState
{
    public ReadOnlyCollection<ElementGauge> Elements { get; }
    public ReadOnlyCollection<float> ReactionCooldowns { get; }
    internal ElementState(ElementWorkState state)
    {
        Elements = Array.AsReadOnly(state.Elements.Select(p => new ElementGauge(p)).ToArray());
        ReactionCooldowns = Array.AsReadOnly((float[])state.Cooldowns.Clone());
    }
}

/// <summary>只在目标事务内部使用的工作副本。</summary>
internal sealed class ElementWorkState
{
    internal Dictionary<int, double>[] Elements = Enumerable.Range(0, DamageConstants.ElementSlotCount)
        .Select(_ => new Dictionary<int, double>()).ToArray();
    internal float[] Cooldowns = new float[DamageConstants.ReactionSlotCount];
    internal double[] Totals = new double[DamageConstants.ElementSlotCount];
    internal ElementWorkState Copy() => new()
    {
        Elements = Elements.Select(p => new Dictionary<int, double>(p)).ToArray(),
        Cooldowns = (float[])Cooldowns.Clone(),
        Totals = (double[])Totals.Clone()
    };
    internal static void Validate(ElementAttachment attachment)
    {
        if (attachment.Slot < 0 || attachment.Slot >= DamageConstants.ElementSlotCount ||
            !double.IsFinite(attachment.Amount) || attachment.Amount < 0 || attachment.PanelDamage < 0)
            throw new ArgumentOutOfRangeException(nameof(attachment), "附着槽、元素量或面板白值非法。");
    }
    internal void Attach(ElementAttachment attachment)
    {
        Validate(attachment);
        if (attachment.Amount == 0) return;
        var portions = Elements[attachment.Slot];
        double sum = portions.Values.Sum() + attachment.Amount;
        if (!double.IsFinite(sum)) throw new ArithmeticException("元素总量溢出。");
        portions.TryGetValue(attachment.PanelDamage, out double previous);
        portions[attachment.PanelDamage] = previous + attachment.Amount;
        Totals[attachment.Slot] = portions.Values.Sum();
    }
    internal ElementPortion[] Take(int slot, double amount, bool highFirst)
    {
        if (!double.IsFinite(amount) || amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (Totals[slot] < amount) throw new InvalidOperationException("方案消耗超过目标库存。");
        var portions = Elements[slot];
        var result = new List<ElementPortion>();
        var keys = (highFirst ? portions.Keys.OrderByDescending(k => k) : portions.Keys.OrderBy(k => k)).ToArray();
        foreach (int key in keys)
        {
            double taken = Math.Min(amount, portions[key]);
            if (taken <= 0) break;
            result.Add(new(slot, key, taken));
            double left = portions[key] - taken;
            if (left == 0) portions.Remove(key); else portions[key] = left;
            amount -= taken;
        }
        // 足量已按同一缓存总量校验。逐项减法可能残留舍入尾差，实际消耗以分量记录为准，
        // 不为补齐尾差制造负分量，也不使用固定 epsilon 清除合法微量。
        Totals[slot] = portions.Values.Sum();
        return result.ToArray();
    }
}

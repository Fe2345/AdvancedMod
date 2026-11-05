#nullable enable
using System;
using System.Collections.Generic;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Tag;

namespace AdvancedMod.Basic.Calculate.DamageOverTime;

/// <summary>同一目标内以效果标识、施加玩家及显式来源共同区分来源。</summary>
public sealed record DotSource(string EffectId, int? AttackerPlayer, SourceInfo? Origin);
public readonly record struct DotText(DotSource Source, int Damage, TagEnum? Element, bool Critical);
public sealed record DotTextAllocation(int NativeDamage, IReadOnlyList<DotText> Separate);

/// <summary>按提交顺序记账；只有已写入 lifeRegen 的份额可以分配原版扣血数字。</summary>
public sealed class DotLedger
{
    private sealed class Entry(DotSource source, int units, TagEnum? element, bool critical, bool separate)
    {
        internal readonly DotSource Source = source;
        internal int Units = units;
        internal readonly TagEnum? Element = element;
        internal readonly bool Critical = critical;
        internal readonly bool Separate = separate;
    }
    private readonly Queue<Entry> pending = new();
    private readonly Queue<Entry> applied = new();
    private int pendingUnits;
    private long totalUnits;
    private double fraction;
    public bool HasBalance => totalUnits != 0 || fraction != 0;
    public double PendingDamage => (pendingUnits + fraction) / DotMath.LifeRegenUnitsPerHealth;
    public double AppliedDamage => (totalUnits - pendingUnits) / (double)DotMath.LifeRegenUnitsPerHealth;

    public void Submit(DotSource source, DotResult result, TagEnum? element = null, bool separate = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(source.EffectId);
        DotMath.NonNegative(result.Damage, nameof(result));
        double scaled = result.Damage * DotMath.LifeRegenUnitsPerHealth + fraction;
        if (!double.IsFinite(scaled) || scaled > int.MaxValue - totalUnits)
            throw new ArgumentOutOfRangeException(nameof(result), "持续伤害余额超出生命恢复整数容量。");
        int units = (int)Math.Floor(scaled);
        fraction = scaled - units;
        // 不足一个恢复单位的尾数归入下一笔完成该单位的来源，不向引擎提交零值。
        if (units == 0) return;
        pending.Enqueue(new(source, units, element, result.Critical, separate));
        pendingUnits += units;
        totalUnits += units;
    }

    public bool Apply(ref int lifeRegen, int lifeRegenCount)
    {
        if (pendingUnits == 0) return false;
        int next = checked(lifeRegen - pendingUnits);
        // 同时检查紧接其后的原生累加，不溢出后再试图补救。
        _ = checked(lifeRegenCount + next);
        while (pending.TryDequeue(out var entry)) applied.Enqueue(entry);
        pendingUnits = 0;
        lifeRegen = next;
        return true;
    }

    /// <summary>分配引擎产生的整数数字，不声称该数字等于致死时的净生命差。</summary>
    public DotTextAllocation Allocate(int damage)
    {
        if (damage <= 0) throw new ArgumentOutOfRangeException(nameof(damage));
        long remaining = (long)damage * DotMath.LifeRegenUnitsPerHealth;
        long consumed = 0;
        int separateDamage = 0;
        var separate = new List<DotText>();
        while (remaining > 0 && applied.TryPeek(out var entry))
        {
            int units = (int)Math.Min(remaining, entry.Units);
            long previous = consumed / DotMath.LifeRegenUnitsPerHealth;
            consumed += units;
            int amount = (int)(consumed / DotMath.LifeRegenUnitsPerHealth - previous);
            // 跨来源凑成一个生命整数时，该整数归于完成它的后一来源。
            if (amount > 0 && entry.Separate)
            {
                separate.Add(new(entry.Source, amount, entry.Element, entry.Critical));
                separateDamage += amount;
            }
            entry.Units -= units;
            totalUnits -= units;
            remaining -= units;
            if (entry.Units == 0) applied.Dequeue();
        }
        return new(damage - separateDamage, separate);
    }

    public void Clear()
    {
        pending.Clear(); applied.Clear();
        pendingUnits = 0; totalUnits = 0; fraction = 0;
    }
}

/// <summary>每帧按首次出现顺序合并同来源；颜色沿用首笔，暴击标记取任一笔暴击。</summary>
public sealed class DotFrameText
{
    private readonly List<DotText> entries = new();
    private readonly Dictionary<DotSource, int> indices = new();
    public IReadOnlyList<DotText> Entries => entries;
    public void Add(DotText text)
    {
        if (indices.TryGetValue(text.Source, out int index))
        {
            var old = entries[index];
            entries[index] = old with { Damage = checked(old.Damage + text.Damage), Critical = old.Critical || text.Critical };
        }
        else { indices.Add(text.Source, entries.Count); entries.Add(text); }
    }
    public void Clear() { entries.Clear(); indices.Clear(); }
}

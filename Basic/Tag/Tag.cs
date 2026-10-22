using System;
using System.Collections;
using System.Collections.Generic;

namespace AdvancedMod.Basic.Tag;

/// <summary>一条标签及其覆盖限制；本版只保存限制，不执行覆盖。</summary>
public readonly struct TagEntry
{
    public bool cannotBeCovered { get; }
    public TagEnum type { get; }

    public TagEntry(bool cannotBeCovered, TagEnum type)
    {
        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(nameof(type), type, "标签类型未定义。");

        this.cannotBeCovered = cannotBeCovered;
        this.type = type;
    }
}

/// <summary>标签的只读快照；同一类型只能出现一次，不提供继承或修改操作。</summary>
public sealed class TagSet : IReadOnlyList<TagEntry>
{
    private readonly TagEntry[] entries;

    public static TagSet Empty { get; } = new(Array.Empty<TagEntry>());

    public TagSet(IEnumerable<TagEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var snapshot = new List<TagEntry>();
        var types = new HashSet<TagEnum>();
        foreach (TagEntry entry in entries)
        {
            if (!Enum.IsDefined(entry.type))
                throw new ArgumentException("标签集合包含未定义的类型。", nameof(entries));
            if (!types.Add(entry.type))
                throw new ArgumentException($"标签类型重复：{entry.type}。", nameof(entries));

            snapshot.Add(entry);
        }

        // 独立持有数据，调用方随后修改输入集合不会影响本次快照。
        this.entries = snapshot.ToArray();
    }

    public int Count => entries.Length;
    public TagEntry this[int index] => entries[index];

    public bool Contains(TagEnum type) => TryGet(type, out _);

    public bool TryGet(TagEnum type, out TagEntry entry)
    {
        foreach (TagEntry candidate in entries)
        {
            if (candidate.type == type)
            {
                entry = candidate;
                return true;
            }
        }

        entry = default;
        return false;
    }

    public IEnumerator<TagEntry> GetEnumerator() =>
        ((IEnumerable<TagEntry>)entries).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

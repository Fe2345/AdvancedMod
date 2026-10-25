// 2026-10-25：加载期验证静态 DAG 并缓存拓扑顺序，不在每刻反射。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AdvancedMod.Basic.Effects;

public sealed class EffectGraph
{
    private readonly Type[] types;
    private readonly Dictionary<Type, int> indices;
    private readonly (int Target, int Weight)[][] edges;
    private readonly int[] order;
    public int Count => types.Length;
    public Type TypeAt(int index) => types[index];

    public EffectGraph(IEnumerable<Type> effectTypes)
    {
        types = effectTypes.OrderBy(t => t.FullName, StringComparer.Ordinal).ToArray();
        indices = new Dictionary<Type, int>();
        edges = new (int, int)[types.Length][];
        for (int i = 0; i < types.Length; i++)
        {
            Type t = types[i];
            if (!IsValid(t)) throw new InvalidOperationException($"无效效果类型：{t}");
            if (!indices.TryAdd(t, i)) throw new InvalidOperationException($"重复效果注册：{t}");
        }
        var degrees = new int[Count];
        for (int i = 0; i < Count; i++)
        {
            var list = new List<(int, int)>();
            var targets = new HashSet<Type>();
            foreach (var include in types[i].GetCustomAttributes<IncludesEffectAttribute>(false))
            {
                if (include.EffectType == null || !indices.TryGetValue(include.EffectType, out int target))
                    throw new InvalidOperationException($"{types[i]} 包含未注册或无效效果：{include.EffectType}");
                if (include.Weight <= 0) throw new InvalidOperationException($"{types[i]} 包含权重必须为正整数");
                if (!targets.Add(include.EffectType)) throw new InvalidOperationException($"{types[i]} 重复包含 {include.EffectType}，请使用权重");
                list.Add((target, include.Weight));
                degrees[target]++;
            }
            edges[i] = list.ToArray();
        }
        var queue = new Queue<int>(Enumerable.Range(0, Count).Where(i => degrees[i] == 0));
        var sorted = new List<int>();
        while (queue.Count > 0)
        {
            int i = queue.Dequeue();
            sorted.Add(i);
            foreach (var edge in edges[i]) if (--degrees[edge.Target] == 0) queue.Enqueue(edge.Target);
        }
        if (sorted.Count != Count)
            throw new InvalidOperationException("效果包含图存在循环：" + string.Join(", ", Enumerable.Range(0, Count).Where(i => degrees[i] > 0).Select(i => types[i].FullName)));
        order = sorted.ToArray();
    }

    public static bool IsValid(Type type) => type != null && type.IsClass && !type.IsAbstract &&
        !type.ContainsGenericParameters && typeof(EffectDefinition).IsAssignableFrom(type) &&
        type.GetConstructor(Type.EmptyTypes) != null;

    internal int Index(Type type) => indices.TryGetValue(type, out int index) ? index :
        throw new InvalidOperationException($"效果未注册：{type}");

    internal void Expand(int[] stacks)
    {
        foreach (int i in order)
            foreach (var edge in edges[i])
                stacks[edge.Target] = checked(stacks[edge.Target] + checked(stacks[i] * edge.Weight));
    }
}

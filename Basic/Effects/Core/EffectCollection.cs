// 2026-10-25：逐玩家逐刻收集、冻结、只执行一次的纯逻辑核心。
using System;

namespace AdvancedMod.Basic.Effects;

public sealed class EffectCollection
{
    private enum Phase { Empty, Collecting, Frozen, Executing, Executed, Faulted }
    private readonly EffectGraph graph;
    private readonly int[] roots;
    private readonly int[] final;
    private Phase phase;
    public bool IsFrozen => phase is Phase.Frozen or Phase.Executing or Phase.Executed;

    public EffectCollection(EffectGraph graph)
    {
        this.graph = graph;
        roots = new int[graph.Count];
        final = new int[graph.Count];
    }

    public void Reset(bool collecting = true)
    {
        if (phase == Phase.Executing) throw new InvalidOperationException("执行期间禁止重置效果");
        Array.Clear(roots, 0, roots.Length);
        Array.Clear(final, 0, final.Length);
        phase = collecting ? Phase.Collecting : Phase.Empty;
    }

    public void Add<TEffect>(int stacks = 1) where TEffect : EffectDefinition => Add(typeof(TEffect), stacks);
    public void Add(Type type, int stacks = 1)
    {
        if (phase != Phase.Collecting) throw new InvalidOperationException("只允许在收集阶段提交根效果");
        if (stacks <= 0) throw new ArgumentOutOfRangeException(nameof(stacks), "贡献层数必须为正整数");
        int i = graph.Index(type);
        roots[i] = checked(roots[i] + stacks);
    }

    public int Stacks<TEffect>() where TEffect : EffectDefinition => Stacks(typeof(TEffect));
    public bool Has<TEffect>() where TEffect : EffectDefinition => Stacks<TEffect>() > 0;
    public int Stacks(Type type)
    {
        int i = graph.Index(type);
        if (phase == Phase.Empty) return 0;
        if (!IsFrozen) throw new InvalidOperationException("最终层数仅在展开冻结之后可查询");
        return final[i];
    }

    public void Freeze()
    {
        if (phase != Phase.Collecting) throw new InvalidOperationException("冻结要求处于收集阶段");
        Array.Copy(roots, final, roots.Length);
        // 失败状态不可被查询或执行；异常原样交给调用端。
        phase = Phase.Faulted;
        graph.Expand(final);
        phase = Phase.Frozen;
    }

    public void Execute(Action<Type, int> apply)
    {
        if (phase != Phase.Frozen) throw new InvalidOperationException("效果只能在冻结后执行一次");
        phase = Phase.Executing;
        bool completed = false;
        try
        {
            for (int i = 0; i < final.Length; i++) if (final[i] > 0) apply(graph.TypeAt(i), final[i]);
            completed = true;
        }
        finally { phase = completed ? Phase.Executed : Phase.Faulted; }
    }
}

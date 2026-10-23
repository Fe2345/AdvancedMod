#nullable enable
using System;

namespace AdvancedMod.Basic.Element;

/// <summary>延迟到真实 Modify 阶段才准备方案，完整原生入口返回后提交。</summary>
internal sealed class NativeElementTransaction
{
    private readonly ElementTarget target;
    private readonly ReactionTable table;
    private bool finished;
    internal Func<bool> SameIdentity { get; }
    internal Action<ElementResolution> Completed { get; }
    internal bool OwnsExecution { get; }
    internal ReactionPlan? Plan { get; private set; }
    internal double DamageDone { get; set; }
    internal NativeElementTransaction(ElementTarget target, ReactionTable table, Func<bool> sameIdentity,
        Action<ElementResolution> completed, bool ownsExecution)
    {
        this.target = target;
        this.table = table;
        SameIdentity = sameIdentity;
        Completed = completed;
        OwnsExecution = ownsExecution;
    }
    internal ReactionPlan Prepare(ElementRequest request)
    {
        if (finished || Plan is not null) throw new InvalidOperationException("原生命中方案不能重复准备。");
        return Plan = target.PrepareNative(request, table);
    }
    internal void Finish(bool returned)
    {
        if (finished) return;
        finished = true;
        target.FinishNative(this, returned);
    }
}

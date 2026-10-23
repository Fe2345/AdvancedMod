#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdvancedMod.Basic.Element;

/// <summary>
/// 一个目标的串行结算外壳，仅在游戏主线程调用。回调负责 Calculate 与应用伤害，
/// 返回是否提交状态；取消、免疫等玩法由外层明确裁定。本层不推断引擎伤害结果。
/// </summary>
public sealed class ElementTarget
{
    /// <summary>防止回调无限派生命中；超限显式报错，宿主可在构造时覆盖。</summary>
    public const int DefaultOperationLimit = 1024;
    private ElementWorkState state = new();
    private readonly Queue<Action> pending = new();
    private readonly int maxOperationsPerDrain;
    private long lastTick = -1;
    public bool IsResolving { get; private set; }
    public ElementState Snapshot => new(state);
    public bool HasState => state.Totals.Any(amount => amount > 0) || state.Cooldowns.Any(cd => cd > 0);

    /// <summary>原生伤害入口持有到整个引擎调用返回的事务；仅供 TML 适配层使用。</summary>
    internal NativeElementTransaction OpenNative(ReactionTable table, Func<bool> sameIdentity,
        Action<ElementResolution> completed)
    {
        if (IsResolving) throw new InvalidOperationException("目标忙碌，必须先排队原生请求。");
        IsResolving = true;
        return new(this, table, sameIdentity, completed, ownsExecution: true);
    }

    internal void RunNative(ReactionTable table, Action<NativeElementTransaction> execute,
        Func<bool> sameIdentity, Action<ElementResolution> completed)
    {
        Enqueue(() =>
        {
            if (!sameIdentity()) return;
            var transaction = new NativeElementTransaction(this, table, sameIdentity, completed, ownsExecution: false);
            bool returned = false;
            try { execute(transaction); returned = true; }
            finally { transaction.Finish(returned); }
        });
    }

    internal ReactionPlan PrepareNative(ElementRequest request, ReactionTable table) =>
        Reaction.Build(request, Prepare.Run(request, state, table));

    internal void FinishNative(NativeElementTransaction transaction, bool returned)
    {
        bool clean = returned;
        try
        {
            if (transaction.Plan is not { } plan) return;
            bool commit = returned && transaction.DamageDone > 0 && transaction.SameIdentity();
            if (commit) state = plan.NextState;
            var outcome = commit && plan.Definition is not null
                ? new ReactionOutcome(plan.Hit.HitId, plan.Definition.Slot, plan.Hit.Source, plan.Hit.Target) : null;
            var presentations = commit && plan.Route is ElementRoute.React or ElementRoute.DirectReaction
                ? plan.Hit.Tags.Where(t => ElementSlots.ReactionFromTag(t.type).HasValue)
                    .Select(t => new PresentationRequest(plan.Hit.HitId, t.type)).ToArray()
                : Array.Empty<PresentationRequest>();
            transaction.Completed(new(plan, commit, outcome, Array.AsReadOnly(presentations)));
        }
        catch { clean = false; throw; }
        finally
        {
            if (transaction.OwnsExecution)
            {
                IsResolving = false;
                if (!clean) pending.Clear();
                else Drain();
            }
        }
    }

    // 上限由宿主明确提供，用来报告派生循环，而不是静默截断反应。
    public ElementTarget(int maxOperationsPerDrain)
    {
        if (maxOperationsPerDrain <= 0) throw new ArgumentOutOfRangeException(nameof(maxOperationsPerDrain));
        this.maxOperationsPerDrain = maxOperationsPerDrain;
    }

    public void Attach(ElementAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        ElementWorkState.Validate(attachment);
        Enqueue(() =>
        {
            var working = state.Copy();
            working.Attach(attachment);
            state = working;
        });
    }

    public void Submit(ElementRequest request, ReactionTable table, Func<ReactionPlan, bool> applyDamage,
        Action<ElementResolution>? completed = null, Func<bool>? isTargetValid = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(applyDamage);
        Enqueue(() => Resolve(request, table, applyDamage, completed, isTargetValid));
    }

    /// <summary>每刻一次：先递减 CD，再扫描静置反应；相同刻调用不重复扫描或递减。</summary>
    public void Tick(long gameTick, ElementRequest idleRequest, ReactionTable table,
        Func<ReactionPlan, bool> applyDamage, Action<ElementResolution>? completed = null,
        Func<bool>? isTargetValid = null)
    {
        ArgumentNullException.ThrowIfNull(idleRequest);
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(applyDamage);
        if (!idleRequest.IsIdle || gameTick < 0) throw new ArgumentException("Tick 需要合法游戏刻和静置请求。");
        Enqueue(() =>
        {
            if (gameTick < lastTick) throw new InvalidOperationException("游戏刻不能倒退；实体重建应创建新目标。");
            if (gameTick == lastTick || isTargetValid?.Invoke() == false) return;
            long elapsed = lastTick < 0 ? 1 : gameTick - lastTick;
            lastTick = gameTick;
            var working = state.Copy();
            for (int i = 0; i < working.Cooldowns.Length; i++)
                working.Cooldowns[i] = Math.Max(0, working.Cooldowns[i] - elapsed);
            state = working;
            Resolve(idleRequest, table, applyDamage, completed, isTargetValid);
        });
    }

    private void Resolve(ElementRequest request, ReactionTable table, Func<ReactionPlan, bool> applyDamage,
        Action<ElementResolution>? completed, Func<bool>? isTargetValid)
    {
        if (isTargetValid?.Invoke() == false) return;
        var plan = Reaction.Build(request, Prepare.Run(request, state, table));
        // 静置未匹配时不产生普通攻击，也不调用伤害回调。
        if (request.IsIdle && plan.Route != ElementRoute.React) return;
        bool commit = applyDamage(plan);
        if (isTargetValid?.Invoke() == false) commit = false;
        ReactionOutcome? outcome = null;
        if (commit)
        {
            state = plan.NextState;
            if (plan.Definition is not null)
                outcome = new(plan.Hit.HitId, plan.Definition.Slot, plan.Hit.Source, plan.Hit.Target);
        }
        var presentations = commit && (plan.Route == ElementRoute.React || plan.Route == ElementRoute.DirectReaction)
            ? plan.Hit.Tags.Where(t => ElementSlots.ReactionFromTag(t.type).HasValue)
                .Select(t => new PresentationRequest(plan.Hit.HitId, t.type)).ToArray()
            : Array.Empty<PresentationRequest>();
        completed?.Invoke(new(plan, commit, outcome, Array.AsReadOnly(presentations)));
    }

    private void Enqueue(Action operation)
    {
        if (pending.Count >= maxOperationsPerDrain) throw new InvalidOperationException("目标事务队列超过容量限制。");
        pending.Enqueue(operation);
        Drain();
    }

    private void Drain()
    {
        if (IsResolving) return;
        IsResolving = true;
        int processed = 0;
        try
        {
            while (pending.Count > 0)
            {
                if (++processed > maxOperationsPerDrain) throw new InvalidOperationException("目标派生链超过单次排空限制。");
                pending.Dequeue()();
            }
        }
        catch
        {
            // 已发生的外部伤害不能回滚；报错并清空未处理请求，保留最后已提交状态。
            pending.Clear();
            throw;
        }
        finally { IsResolving = false; }
    }
}

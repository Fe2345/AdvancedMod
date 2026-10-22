#nullable enable
using System;
using System.Collections.Generic;

namespace AdvancedMod.Basic.Calculate.Core;

/// <summary>绑定贡献者的短生命周期写入器，不能指定其他作者或直接修改累计数值。</summary>
public sealed class ContributionWriter
{
    private readonly HitProposalCollector collector;
    private readonly int contributorId;
    private readonly List<SourceInfo> sources;
    private bool active = true;

    internal ContributionWriter(HitProposalCollector collector, int contributorId, List<SourceInfo> sources)
    {
        this.collector = collector;
        this.contributorId = contributorId;
        this.sources = sources;
    }

    public void AddSource(SourceInfo source)
    {
        EnsureActive();
        ArgumentNullException.ThrowIfNull(source);
        sources.Add(source);
    }

    public void AddValue(string entryId, ModifierTarget target, float value)
    {
        EnsureActive();
        collector.AddNumeric(contributorId, entryId, target, value);
    }

    public void SetEnabled(string entryId, ModifierTarget target, bool enabled, string reason)
    {
        EnsureActive();
        collector.AddGate(DecisionStage.Contributor, contributorId, null, entryId, target, enabled, reason);
    }

    public void AddNative(string entryId, NativeField field, NumericOperation operation, float value)
    {
        EnsureActive();
        collector.AddNative(contributorId, entryId, field, operation, value);
    }

    public void SetCriticalStrike(string entryId, CriticalStrikePolicy policy) => Control(entryId, NativeField.CriticalStrike, (int)policy);
    public void SetIgnoreDefense(string entryId, bool enabled) => Control(entryId, NativeField.IgnoreDefense, enabled ? 1 : 0);
    public void SetMaximumDamage(string entryId, int limit) => Control(entryId, NativeField.MaximumDamage, limit);
    public void SetInstantKill(string entryId, bool enabled) => Control(entryId, NativeField.InstantKill, enabled ? 1 : 0);
    public void SetCancelHurt(string entryId, bool enabled) => Control(entryId, NativeField.CancelHurt, enabled ? 1 : 0);
    public void SetKnockbackDisabled(string entryId, bool enabled) => Control(entryId, NativeField.KnockbackDisabled, enabled ? 1 : 0);
    public void SetHitDirection(string entryId, int direction) => Control(entryId, NativeField.HitDirection, direction);

    private void Control(string entryId, NativeField field, int value)
    {
        EnsureActive();
        collector.AddControl(contributorId, entryId, field, value);
    }

    internal void Close() => active = false;
    private void EnsureActive()
    {
        if (!active)
            throw new InvalidOperationException("贡献写入器已离开本次收集作用域。");
    }
}

/// <summary>标签规则和后置规则只能追加启停决定。</summary>
public sealed class GateWriter
{
    private readonly HitProposalCollector collector;
    private readonly DecisionStage stage;
    private readonly string ruleId;
    private bool active = true;

    internal GateWriter(HitProposalCollector collector, DecisionStage stage, string ruleId)
    {
        this.collector = collector;
        this.stage = stage;
        this.ruleId = ruleId;
    }

    public void SetEnabled(string entryId, ModifierTarget target, bool enabled, string reason)
    {
        if (!active)
            throw new InvalidOperationException("规则写入器已离开本次规则作用域。");
        collector.AddGate(stage, null, ruleId, entryId, target, enabled, reason);
    }

    internal void Close() => active = false;
}

/// <summary>由调度器持有；内容只能获得绑定身份的写入器，全部候选收齐后统一筛选。</summary>
internal sealed class HitProposalCollector
{
    private readonly HashSet<string> reserveKeys;
    private readonly List<ContributorActivation> contributors = new();
    private readonly List<NumericContribution> contributions = new();
    private readonly List<GateDecision> gates = new();
    private readonly List<NativeNumericContribution> nativeContributions = new();
    private readonly List<ControlContribution> controls = new();
    private readonly HashSet<(int, string)> controlIds = new();
    private readonly HitKind? hitKind;
    private readonly HashSet<(int, string)> numericIds = new();
    private readonly HashSet<(DecisionStage, int?, string?, string)> gateIds = new();
    private readonly HashSet<int> contributorIds = new();
    private readonly HashSet<(DecisionStage, string)> ruleIds = new();
    private DecisionStage currentStage;
    private int lastRegistrationOrder = -1;
    private long sequence;
    private bool inCallback;
    private bool closed;

    public HitProposalCollector(IEnumerable<string> reserveKeys, HitKind? hitKind = null)
    {
        ArgumentNullException.ThrowIfNull(reserveKeys);
        if (hitKind.HasValue && !Enum.IsDefined(hitKind.Value)) throw new ArgumentOutOfRangeException(nameof(hitKind));
        this.hitKind = hitKind;
        this.reserveKeys = new(StringComparer.Ordinal);
        foreach (string key in reserveKeys)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (!this.reserveKeys.Add(key))
                throw new ArgumentException($"备用区键重复：{key}。", nameof(reserveKeys));
        }
    }

    public void Collect(int contributorId, int registrationOrder, Action<ContributionWriter> collect)
    {
        ArgumentNullException.ThrowIfNull(collect);
        EnsureCanEnter(DecisionStage.Contributor);
        if (registrationOrder <= lastRegistrationOrder || contributorIds.Contains(contributorId))
            throw new ArgumentException("贡献者必须唯一且按注册序号升序调用。");
        contributorIds.Add(contributorId);
        lastRegistrationOrder = registrationOrder;
        var sources = new List<SourceInfo>();
        var writer = new ContributionWriter(this, contributorId, sources);
        inCallback = true;
        try
        {
            collect(writer);
            contributors.Add(new(contributorId, registrationOrder, sources));
        }
        catch
        {
            closed = true;
            throw;
        }
        finally
        {
            writer.Close();
            inCallback = false;
        }
    }

    public void RunTagRule(string ruleId, Action<GateWriter> evaluate) =>
        RunRule(DecisionStage.TagRule, ruleId, evaluate);

    public void RunPostRule(string ruleId, Action<GateWriter> evaluate) =>
        RunRule(DecisionStage.PostRule, ruleId, evaluate);

    public CalculationInput Seal(HitSnapshot hit)
    {
        EnsureOpen();
        if (inCallback)
            throw new InvalidOperationException("不能在贡献者或规则内部封闭收集器。");
        closed = true;
        if (hitKind.HasValue && hit.Kind != hitKind.Value) throw new ArgumentException("封闭时的命中方向发生变化。", nameof(hit));
        return new(hit, contributors, contributions, gates, reserveKeys, nativeContributions, controls);
    }

    public RuleContext CreateRuleView(HitSnapshot hit)
    {
        EnsureOpen();
        ArgumentNullException.ThrowIfNull(hit);
        if (inCallback)
            throw new InvalidOperationException("规则快照由调度器在回调之外构建。");
        return new(hit, contributors, contributions, gates, nativeContributions, controls);
    }

    internal void AddNumeric(int author, string entryId, ModifierTarget target, float value)
    {
        EnsureOpen();
        target.Validate(reserveKeys);
        if (target.Kind == ModifierTargetKind.Native) throw new ArgumentException("原生字段必须通过 AddNative 或控制方法提交。");
        ArgumentException.ThrowIfNullOrWhiteSpace(entryId);
        if (!float.IsFinite(value))
            throw new ArgumentException("数值候选必须是有限浮点数。", nameof(value));
        if (!numericIds.Add((author, entryId)))
            throw new ArgumentException("同一贡献者的数值 EntryId 重复。", nameof(entryId));
        contributions.Add(new(author, entryId, target, value, sequence++));
    }

    internal void AddNative(int author, string entryId, NativeField field, NumericOperation operation, float value)
    {
        EnsureOpen();
        NativeCapabilities.Validate(field, operation, value, hitKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryId);
        if (!numericIds.Add((author, entryId))) throw new ArgumentException("同一作者的数值 EntryId 重复。");
        nativeContributions.Add(new(author, entryId, field, operation, value, sequence++));
    }

    internal void AddControl(int author, string entryId, NativeField field, int value)
    {
        EnsureOpen();
        NativeCapabilities.Validate(field, value, hitKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryId);
        if (!controlIds.Add((author, entryId))) throw new ArgumentException("同一作者的控制 EntryId 重复。");
        controls.Add(new(author, entryId, field, value, sequence++));
    }

    internal void AddGate(DecisionStage stage, int? author, string? ruleId, string entryId,
        ModifierTarget target, bool enabled, string reason)
    {
        EnsureOpen();
        target.Validate(reserveKeys);
        if (hitKind.HasValue && target.Kind == ModifierTargetKind.Native && !NativeCapabilities.Supports(hitKind.Value, target.Field!.Value))
            throw new ArgumentException("启停目标不受本次命中方向支持。");
        ArgumentException.ThrowIfNullOrWhiteSpace(entryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (!gateIds.Add((stage, author, ruleId, entryId)))
            throw new ArgumentException("同一作者的启停 EntryId 重复。", nameof(entryId));
        gates.Add(new(stage, author, ruleId, entryId, target, enabled, reason, sequence++));
    }

    private void RunRule(DecisionStage stage, string ruleId, Action<GateWriter> evaluate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
        ArgumentNullException.ThrowIfNull(evaluate);
        EnsureCanEnter(stage);
        if (!ruleIds.Add((stage, ruleId)))
            throw new ArgumentException("同一阶段的规则只能执行一次。", nameof(ruleId));
        var writer = new GateWriter(this, stage, ruleId);
        inCallback = true;
        try { evaluate(writer); }
        catch { closed = true; throw; }
        finally { writer.Close(); inCallback = false; }
    }

    private void EnsureCanEnter(DecisionStage stage)
    {
        EnsureOpen();
        if (inCallback)
            throw new InvalidOperationException("不能在当前收集器中嵌套调用贡献者或规则。");
        if (stage < currentStage)
            throw new InvalidOperationException("不能回到已经完成的收集阶段。");
        currentStage = stage;
    }

    private void EnsureOpen()
    {
        if (closed)
            throw new InvalidOperationException("收集器已封闭或因异常终止。");
    }
}

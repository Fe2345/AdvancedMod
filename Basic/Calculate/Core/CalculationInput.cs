#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AdvancedMod.Basic.Tag;

namespace AdvancedMod.Basic.Calculate.Core;

public enum HitKind { ItemToNpc, ProjectileToNpc, NpcToPlayer, ProjectileToPlayer }
public enum ExecutionSide { SinglePlayer, Client, Server }
public enum SourceKind { Player, Npc, Projectile, Item, Buff, World, Environment }
public enum DecisionStage { TagRule, Contributor, PostRule }

/// <summary>来源描述，不持有 Terraria 实体，也不用于重新查找历史实体。</summary>
public sealed record SourceInfo(SourceKind Kind, string ContentId, int? PlayerIndex = null,
    int? EntityIndex = null, int? InventorySlot = null, int? ProjectileIdentity = null);

/// <summary>纯计算读取的命中摘要。未知来源使用 null，标签为上游提供的只读快照。</summary>
public sealed record HitSnapshot(long HitId, HitKind Kind, ExecutionSide ExecutionSide, TagSet Tags,
    string? AttackId = null, bool IsManagedAttack = false, string? NativeDamageClassId = null,
    SourceInfo? Attacker = null, SourceInfo? Source = null, SourceInfo? Target = null);

public sealed class ContributorActivation
{
    public int ContributorId { get; }
    public int RegistrationOrder { get; }
    public ReadOnlyCollection<SourceInfo> Sources { get; }

    public ContributorActivation(int contributorId, int registrationOrder, IEnumerable<SourceInfo> sources)
    {
        if (registrationOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(registrationOrder));
        ArgumentNullException.ThrowIfNull(sources);
        ContributorId = contributorId;
        RegistrationOrder = registrationOrder;
        Sources = Array.AsReadOnly(sources.ToArray());
        foreach (SourceInfo source in Sources)
            ArgumentNullException.ThrowIfNull(source);
    }
}

/// <summary>所有自定义乘区提交可累加的值，精通为原始点数。</summary>
public sealed record NumericContribution(int ContributorId, string EntryId, ModifierTarget Target,
    float Value, long Sequence);

/// <summary>作者按阶段二选一：普通贡献者使用整数 ID，标签规则和后置规则使用规则 ID。</summary>
public sealed record GateDecision(DecisionStage Stage, int? ContributorId, string? RuleId,
    string EntryId, ModifierTarget Target, bool Enabled, string Reason, long Sequence);

/// <summary>候选收集完毕后的纯计算输入，复制集合并验证记录完整性。</summary>
public sealed class CalculationInput
{
    public HitSnapshot Hit { get; }
    public ReadOnlyCollection<ContributorActivation> Contributors { get; }
    public ReadOnlyCollection<NumericContribution> Contributions { get; }
    public ReadOnlyCollection<GateDecision> Gates { get; }
    public ReadOnlyCollection<string> ReserveKeys { get; }
    public ReadOnlyCollection<NativeNumericContribution> NativeContributions { get; }
    public ReadOnlyCollection<ControlContribution> Controls { get; }

    public CalculationInput(HitSnapshot hit, IEnumerable<ContributorActivation> contributors,
        IEnumerable<NumericContribution> contributions, IEnumerable<GateDecision> gates,
        IEnumerable<string> reserveKeys, IEnumerable<NativeNumericContribution>? nativeContributions = null,
        IEnumerable<ControlContribution>? controls = null)
    {
        ArgumentNullException.ThrowIfNull(hit);
        ArgumentNullException.ThrowIfNull(hit.Tags);
        if (!Enum.IsDefined(hit.Kind) || !Enum.IsDefined(hit.ExecutionSide))
            throw new ArgumentException("命中类型或执行侧未定义。", nameof(hit));
        ArgumentNullException.ThrowIfNull(contributors);
        ArgumentNullException.ThrowIfNull(contributions);
        ArgumentNullException.ThrowIfNull(gates);
        ArgumentNullException.ThrowIfNull(reserveKeys);
        Hit = hit;
        Contributors = Array.AsReadOnly(contributors.ToArray());
        Contributions = Array.AsReadOnly(contributions.ToArray());
        Gates = Array.AsReadOnly(gates.ToArray());
        ReserveKeys = Array.AsReadOnly(reserveKeys.OrderBy(key => key, StringComparer.Ordinal).ToArray());
        NativeContributions = Array.AsReadOnly((nativeContributions ?? Array.Empty<NativeNumericContribution>()).ToArray());
        Controls = Array.AsReadOnly((controls ?? Array.Empty<ControlContribution>()).ToArray());
        Validate();
    }

    private void Validate()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (string key in ReserveKeys)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (!keys.Add(key))
                throw new ArgumentException($"备用区键重复：{key}。");
        }

        var authors = new Dictionary<int, int>();
        int previousOrder = -1;
        foreach (ContributorActivation activation in Contributors)
        {
            ArgumentNullException.ThrowIfNull(activation);
            if (activation.RegistrationOrder <= previousOrder ||
                !authors.TryAdd(activation.ContributorId, activation.RegistrationOrder))
                throw new ArgumentException("贡献者必须唯一且按注册序号升序排列。");
            previousOrder = activation.RegistrationOrder;
        }

        var sequences = new HashSet<long>();
        var numericIds = new HashSet<(int, string)>();
        long previousSequence = -1;
        foreach (NumericContribution contribution in Contributions)
        {
            ArgumentNullException.ThrowIfNull(contribution);
            ValidateSequence(contribution.Sequence, ref previousSequence, sequences);
            contribution.Target.Validate(keys);
            if (contribution.Target.Kind == ModifierTargetKind.Native)
                throw new ArgumentException("原生字段必须通过具名原生操作提交。");
            ArgumentException.ThrowIfNullOrWhiteSpace(contribution.EntryId);
            if (!authors.ContainsKey(contribution.ContributorId))
                throw new ArgumentException("数值候选的贡献者没有激活记录。");
            if (!numericIds.Add((contribution.ContributorId, contribution.EntryId)))
                throw new ArgumentException("同一贡献者的数值 EntryId 重复。");
            if (!float.IsFinite(contribution.Value))
                throw new ArgumentException("数值候选必须是有限浮点数。");
        }

        previousSequence = -1;
        foreach (NativeNumericContribution candidate in NativeContributions)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            ValidateSequence(candidate.Sequence, ref previousSequence, sequences);
            ArgumentException.ThrowIfNullOrWhiteSpace(candidate.EntryId);
            if (!authors.ContainsKey(candidate.ContributorId) || !numericIds.Add((candidate.ContributorId, candidate.EntryId)))
                throw new ArgumentException("原生数值候选作者未激活或数值 EntryId 重复。");
            NativeCapabilities.Validate(candidate.Field, candidate.Operation, candidate.Value, Hit.Kind);
        }
        var controlIds = new HashSet<(int, string)>();
        previousSequence = -1;
        foreach (ControlContribution candidate in Controls)
        {
            ArgumentNullException.ThrowIfNull(candidate);
            ValidateSequence(candidate.Sequence, ref previousSequence, sequences);
            ArgumentException.ThrowIfNullOrWhiteSpace(candidate.EntryId);
            if (!authors.ContainsKey(candidate.ContributorId) || !controlIds.Add((candidate.ContributorId, candidate.EntryId)))
                throw new ArgumentException("控制候选作者未激活或控制 EntryId 重复。");
            NativeCapabilities.Validate(candidate.Field, candidate.Value, Hit.Kind);
        }

        var gateIds = new HashSet<(DecisionStage, int?, string?, string)>();
        previousSequence = -1;
        DecisionStage previousStage = DecisionStage.TagRule;
        foreach (GateDecision gate in Gates)
        {
            ArgumentNullException.ThrowIfNull(gate);
            ValidateSequence(gate.Sequence, ref previousSequence, sequences);
            gate.Target.Validate(keys);
            if (gate.Target.Kind == ModifierTargetKind.Native && !NativeCapabilities.Supports(Hit.Kind, gate.Target.Field!.Value))
                throw new ArgumentException("启停目标不受本次命中方向支持。");
            ArgumentException.ThrowIfNullOrWhiteSpace(gate.EntryId);
            ArgumentException.ThrowIfNullOrWhiteSpace(gate.Reason);
            if (!Enum.IsDefined(gate.Stage) || gate.Stage < previousStage)
                throw new ArgumentException("启停阶段必须按标签规则、普通贡献者、后置规则排列。");
            previousStage = gate.Stage;
            if (gate.Stage == DecisionStage.Contributor)
            {
                if (!gate.ContributorId.HasValue || gate.RuleId is not null || !authors.ContainsKey(gate.ContributorId.Value))
                    throw new ArgumentException("普通启停决定必须属于本次已激活的贡献者。");
            }
            else
            {
                if (gate.ContributorId.HasValue)
                    throw new ArgumentException("规则不能伪装成普通贡献者。");
                ArgumentException.ThrowIfNullOrWhiteSpace(gate.RuleId);
            }
            if (!gateIds.Add((gate.Stage, gate.ContributorId, gate.RuleId, gate.EntryId)))
                throw new ArgumentException("同一作者的启停 EntryId 重复。");
        }

        // 两类记录合并检查，保证更晚的规则序号确实晚于所有普通候选。
        var events = Contributions.Select(c => (c.Sequence, Stage: DecisionStage.Contributor, Author: (int?)c.ContributorId))
            .Concat(NativeContributions.Select(c => (c.Sequence, Stage: DecisionStage.Contributor, Author: (int?)c.ContributorId)))
            .Concat(Controls.Select(c => (c.Sequence, Stage: DecisionStage.Contributor, Author: (int?)c.ContributorId)))
            .Concat(Gates.Select(g => (g.Sequence, g.Stage, Author: g.ContributorId)))
            .OrderBy(e => e.Sequence);
        previousStage = DecisionStage.TagRule;
        previousOrder = -1;
        foreach (var entry in events)
        {
            if (entry.Stage < previousStage)
                throw new ArgumentException("记录序号违反标签、贡献者、后置规则的执行阶段。");
            previousStage = entry.Stage;
            if (entry.Stage == DecisionStage.Contributor)
            {
                int order = authors[entry.Author!.Value];
                if (order < previousOrder)
                    throw new ArgumentException("普通贡献记录违反注册顺序。");
                previousOrder = order;
            }
        }
    }

    private static void ValidateSequence(long value, ref long previous, HashSet<long> all)
    {
        if (value < 0 || value <= previous || !all.Add(value))
            throw new ArgumentException("记录序号必须非负、唯一，并在列表中严格递增。");
        previous = value;
    }
}

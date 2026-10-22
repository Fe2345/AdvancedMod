#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AdvancedMod.Basic.Calculate.Core;

namespace AdvancedMod.Basic.Calculate.Tml;

/// <summary>冻结后只能读取；整数身份来自特性，调用顺序来自显式注册顺序。</summary>
public sealed class CombatRegistry
{
    internal sealed record ContributorRegistration(int Id, int Order, HitKinds SupportedHits, ICombatContributor Contributor);
    internal sealed record RuleRegistration(string Id, HitKinds SupportedHits, ICombatRule Rule);
    private readonly List<ContributorRegistration> contributors = new();
    private readonly List<RuleRegistration> rules = new();
    private readonly List<TagGateRule> tagRules = new();
    private readonly List<string> reserveKeys = new();
    private readonly HashSet<int> contributorIds = new();
    private readonly HashSet<string> ruleIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> keySet = new(StringComparer.Ordinal);

    public bool IsFrozen { get; private set; }
    internal ReadOnlyCollection<ContributorRegistration> Contributors => contributors.AsReadOnly();
    internal ReadOnlyCollection<RuleRegistration> Rules => rules.AsReadOnly();
    internal ReadOnlyCollection<TagGateRule> TagRules => tagRules.AsReadOnly();
    internal ReadOnlyCollection<string> ReserveKeys => reserveKeys.AsReadOnly();

    public void RegisterReserveKey(string key)
    {
        EnsureWritable();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!keySet.Add(key)) throw new ArgumentException($"备用键重复：{key}。", nameof(key));
        reserveKeys.Add(key);
    }

    public void Register(TagGateRule rule)
    {
        EnsureWritable();
        ArgumentNullException.ThrowIfNull(rule);
        if (!ruleIds.Add(rule.Id)) throw new ArgumentException($"规则 ID 重复：{rule.Id}。", nameof(rule));
        tagRules.Add(rule);
    }

    public void Register(ICombatContributor contributor)
    {
        EnsureWritable();
        ArgumentNullException.ThrowIfNull(contributor);
        int id = CombatContributorAttribute.GetId(contributor.GetType());
        HitKinds supported = contributor.SupportedHits;
        supported.Validate();
        if (!contributorIds.Add(id)) throw new ArgumentException($"贡献者 ID 重复登记：{id}。", nameof(contributor));
        contributors.Add(new(id, contributors.Count, supported, contributor));
    }

    public void Register(ICombatRule rule)
    {
        EnsureWritable();
        ArgumentNullException.ThrowIfNull(rule);
        string id = rule.Id;
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        HitKinds supported = rule.SupportedHits;
        supported.Validate();
        if (!ruleIds.Add(id)) throw new ArgumentException($"规则 ID 重复：{id}。", nameof(rule));
        rules.Add(new(id, supported, rule));
    }

    public void Freeze()
    {
        EnsureWritable();
        foreach (TagGateRule rule in tagRules)
        {
            rule.Target.Validate(keySet);
            if (rule.Target.Kind != ModifierTargetKind.Native) continue;
            foreach (HitKind kind in Enum.GetValues<HitKind>())
                if (rule.SupportedHits.Includes(kind) && !NativeCapabilities.Supports(kind, rule.Target.Field!.Value))
                    throw new ArgumentException($"规则 {rule.Id} 对 {kind} 使用了不支持的原生目标。");
        }
        IsFrozen = true;
    }

    internal void Clear()
    {
        contributors.Clear(); rules.Clear(); tagRules.Clear(); reserveKeys.Clear();
        contributorIds.Clear(); ruleIds.Clear(); keySet.Clear();
        IsFrozen = false;
    }

    private void EnsureWritable()
    {
        if (IsFrozen) throw new InvalidOperationException("贡献者注册表已冻结，战斗中不能变更定义。");
    }
}

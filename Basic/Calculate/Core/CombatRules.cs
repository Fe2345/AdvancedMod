#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AdvancedMod.Basic.Tag;

namespace AdvancedMod.Basic.Calculate.Core;

[Flags]
public enum HitKinds
{
    None = 0,
    ItemToNpc = 1,
    ProjectileToNpc = 2,
    NpcToPlayer = 4,
    ProjectileToPlayer = 8,
    All = ItemToNpc | ProjectileToNpc | NpcToPlayer | ProjectileToPlayer
}

public static class HitKindExtensions
{
    public static bool Includes(this HitKinds kinds, HitKind kind) => (kinds & kind.ToMask()) != 0;

    public static HitKinds ToMask(this HitKind kind) => kind switch
    {
        HitKind.ItemToNpc => HitKinds.ItemToNpc,
        HitKind.ProjectileToNpc => HitKinds.ProjectileToNpc,
        HitKind.NpcToPlayer => HitKinds.NpcToPlayer,
        HitKind.ProjectileToPlayer => HitKinds.ProjectileToPlayer,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    internal static void Validate(this HitKinds kinds)
    {
        if ((kinds & ~HitKinds.All) != 0)
            throw new ArgumentOutOfRangeException(nameof(kinds), "包含未定义的命中路径。");
    }
}

/// <summary>标签只参与匹配，不在这里更改标签，也不自动提供数值贡献。</summary>
public sealed class TagGateRule
{
    public string Id { get; }
    public HitKinds SupportedHits { get; }
    public ModifierTarget Target { get; }
    public bool Enabled { get; }
    public ReadOnlyCollection<TagEnum> RequiredAll { get; }
    public ReadOnlyCollection<TagEnum> RequiredAny { get; }
    public ReadOnlyCollection<TagEnum> Excluded { get; }

    public TagGateRule(string id, ModifierTarget target, bool enabled,
        IEnumerable<TagEnum> requiredAll, IEnumerable<TagEnum> requiredAny,
        IEnumerable<TagEnum> excluded, HitKinds supportedHits = HitKinds.All)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        supportedHits.Validate();
        Id = id;
        Target = target;
        Enabled = enabled;
        SupportedHits = supportedHits;
        RequiredAll = CopyTags(requiredAll);
        RequiredAny = CopyTags(requiredAny);
        Excluded = CopyTags(excluded);
    }

    public bool Matches(HitSnapshot hit)
    {
        ArgumentNullException.ThrowIfNull(hit);
        ArgumentNullException.ThrowIfNull(hit.Tags);
        return SupportedHits.Includes(hit.Kind) && RequiredAll.All(hit.Tags.Contains) &&
            (RequiredAny.Count == 0 || RequiredAny.Any(hit.Tags.Contains)) && !Excluded.Any(hit.Tags.Contains);
    }

    private static ReadOnlyCollection<TagEnum> CopyTags(IEnumerable<TagEnum> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        TagEnum[] copy = tags.Distinct().ToArray();
        foreach (TagEnum tag in copy)
            if (!Enum.IsDefined(tag))
                throw new ArgumentException("规则包含未定义的标签。", nameof(tags));
        return Array.AsReadOnly(copy);
    }
}

/// <summary>按标签注释显式定义基础启用规则；槽号不从枚举值推导。</summary>
internal static class DefaultTagRules
{
    public static ReadOnlyCollection<TagGateRule> All { get; } = Array.AsReadOnly(new[]
    {
        Enable(TagEnum.Pyro, ModifierTarget.ElementDamage(1)),
        Enable(TagEnum.Hydro, ModifierTarget.ElementDamage(2)),
        Enable(TagEnum.Electro, ModifierTarget.ElementDamage(3)),
        Enable(TagEnum.Cryo, ModifierTarget.ElementDamage(4)),
        Enable(TagEnum.Chlorophyte, ModifierTarget.ElementDamage(5)),
        Enable(TagEnum.Anemo, ModifierTarget.ElementDamage(6)),
        Enable(TagEnum.HydroElectro, ModifierTarget.Reaction(1)),
        Enable(TagEnum.HydroAnemo, ModifierTarget.Reaction(2)),
        Enable(TagEnum.PytoCryo, ModifierTarget.Reaction(3)),
        Enable(TagEnum.Hallowed, ModifierTarget.Ascension(1)),
        Enable(TagEnum.Lunacy, ModifierTarget.Ascension(2)),
        Enable(TagEnum.Originated, ModifierTarget.Ascension(3))
    });

    // Hallow／Lunar／Origin 仅表示异化资格，不代表反应已异化，不在此启用擢升。
    private static TagGateRule Enable(TagEnum tag, ModifierTarget target) =>
        new($"AdvancedMod:Tag.{tag}", target, true, new[] { tag },
            Array.Empty<TagEnum>(), Array.Empty<TagEnum>());
}

public interface ICombatRule
{
    string Id { get; }
    HitKinds SupportedHits { get; }
    void Evaluate(in RuleContext context, GateWriter writer);
}

/// <summary>后置规则看到的已收集数据；每次创建快照，不能修改历史记录。</summary>
public sealed class RuleContext
{
    public HitSnapshot Hit { get; }
    public ReadOnlyCollection<ContributorActivation> Contributors { get; }
    public ReadOnlyCollection<NumericContribution> Contributions { get; }
    public ReadOnlyCollection<GateDecision> Gates { get; }
    public ReadOnlyCollection<NativeNumericContribution> NativeContributions { get; }
    public ReadOnlyCollection<ControlContribution> Controls { get; }

    internal RuleContext(HitSnapshot hit, IEnumerable<ContributorActivation> contributors,
        IEnumerable<NumericContribution> contributions, IEnumerable<GateDecision> gates,
        IEnumerable<NativeNumericContribution> nativeContributions, IEnumerable<ControlContribution> controls)
    {
        Hit = hit;
        Contributors = Array.AsReadOnly(contributors.ToArray());
        Contributions = Array.AsReadOnly(contributions.ToArray());
        Gates = Array.AsReadOnly(gates.ToArray());
        NativeContributions = Array.AsReadOnly(nativeContributions.ToArray());
        Controls = Array.AsReadOnly(controls.ToArray());
    }

    public bool HasContributor(int id) => Contributors.Any(entry => entry.ContributorId == id);

    public IReadOnlyList<SourceInfo> GetSources(int contributorId) =>
        Contributors.FirstOrDefault(entry => entry.ContributorId == contributorId)?.Sources
        ?? (IReadOnlyList<SourceInfo>)Array.Empty<SourceInfo>();
}

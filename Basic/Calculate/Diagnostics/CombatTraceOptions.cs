using System;
using System.Collections.Generic;
using AdvancedMod.Basic.Calculate.Core;

namespace AdvancedMod.Basic.Calculate.Diagnostics;

/// <summary>同一维度匹配任意值，不同非空维度必须同时满足。</summary>
public sealed class CombatTraceOptions
{
    public static CombatTraceOptions Disabled { get; } = new(false, Array.Empty<string>(), Array.Empty<int>(), Array.Empty<string>());
    public bool Enabled { get; }
    private readonly HashSet<string> sources;
    private readonly HashSet<int> players;
    private readonly HashSet<string> targets;

    public CombatTraceOptions(bool enabled, IEnumerable<string> sourceIds, IEnumerable<int> playerIndices, IEnumerable<string> targetIds)
    {
        Enabled = enabled;
        sources = new(sourceIds, StringComparer.Ordinal);
        players = new(playerIndices);
        targets = new(targetIds, StringComparer.Ordinal);
    }

    public bool Matches(HitSnapshot hit) => Enabled &&
        (sources.Count == 0 || sources.Contains(hit.AttackId ?? "") || sources.Contains(hit.Source?.ContentId ?? "")) &&
        (players.Count == 0 || hit.Attacker?.PlayerIndex is int attacker && players.Contains(attacker) ||
            hit.Target?.PlayerIndex is int target && players.Contains(target) || hit.Source?.PlayerIndex is int source && players.Contains(source)) &&
        (targets.Count == 0 || targets.Contains(hit.Target?.ContentId ?? ""));
}

using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedMod.Basic.Calculate.Core;

namespace AdvancedMod.Basic.Calculate.Diagnostics;

public sealed record ReplayComparison(bool Matches, string[] Differences);

/// <summary>只重放已收集输入，不执行贡献者、标签事务或 Terraria 的最终伤害公式。</summary>
public static class CombatTraceReplay
{
    public static ReplayComparison Replay(CombatTrace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);
        if (trace.SchemaVersion != CombatTrace.CurrentSchema || trace.RulesetVersion != CombatTrace.CurrentRuleset)
            throw new NotSupportedException("Trace 格式或计算规则版本不兼容。");
        ArgumentNullException.ThrowIfNull(trace.Input);
        ArgumentNullException.ThrowIfNull(trace.Expected);
        if (trace.HitId != trace.Input.Hit.HitId) throw new ArgumentException("Trace 命中编号不一致。");
        CalculationResult actual = DamageCombiner.Evaluate(trace.Input.ToCalculationInput(), true);
        var differences = new List<string>();
        Compare(trace.Expected.Targets.Select(t => t.Gate).ToArray(), actual.Targets.Select(t => t.Gate).ToArray(), "自定义启停", differences);
        Compare(trace.Expected.Targets, actual.Targets.ToArray(), "逐槽结果", differences);
        Compare(trace.Expected.Decisions, actual.Decisions.ToArray(), "候选采纳", differences);
        Compare(trace.Expected.NativeFields, actual.Plan.Fields.ToArray(), "原生字段计划", differences);
        if (trace.Expected.ReactionFactor != actual.ReactionFactor) differences.Add("ReactionFactor 不一致");
        if (trace.Expected.ResistanceFactor != actual.ResistanceFactor) differences.Add("ResistanceFactor 不一致");
        if (trace.Expected.AfterDefenseFactor != actual.AfterDefenseFactor) differences.Add("AfterDefenseFactor 不一致");
        return new(differences.Count == 0, differences.ToArray());
    }

    private static void Compare<T>(T[] expected, T[] actual, string name, List<string> differences)
    {
        ArgumentNullException.ThrowIfNull(expected);
        if (expected.Length != actual.Length) differences.Add($"{name}数量：记录 {expected.Length}，重算 {actual.Length}");
        for (int i = 0; i < Math.Min(expected.Length, actual.Length); i++)
            if (!EqualityComparer<T>.Default.Equals(expected[i], actual[i])) differences.Add($"{name}[{i}] 不一致");
    }
}

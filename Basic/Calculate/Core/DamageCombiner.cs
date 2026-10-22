#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AdvancedMod.Basic.Calculate.Core;

public sealed record GateResolution(ModifierTarget Target, bool Enabled, long? WinningSequence);
public sealed record TargetResult(ModifierTarget Target, GateResolution Gate, float Sum, float Factor);
public enum ContributionDisposition { Accepted, TargetDisabled, Overridden, LessStrictCap }
public sealed record ContributionDecision(long Sequence, ContributionDisposition Disposition, long? GateSequence, long? RelatedSequence = null);

/// <summary>自定义乘区与完整原生增量计划，不计算游戏最终伤害。</summary>
public sealed class CalculationResult
{
    public ReadOnlyCollection<TargetResult> Targets { get; }
    public ReadOnlyCollection<ContributionDecision> Decisions { get; }
    public bool IncludesDiagnostics { get; }
    public float ReactionFactor { get; }
    public float ResistanceFactor { get; }
    public float AfterDefenseFactor { get; }
    public NativeModifierPlan Plan { get; }

    internal CalculationResult(TargetResult[] targets, ContributionDecision[] decisions,
        bool includesDiagnostics, float reactionFactor, float resistanceFactor, float afterDefenseFactor, NativeModifierPlan plan)
    {
        Targets = Array.AsReadOnly(targets);
        Decisions = Array.AsReadOnly(decisions);
        IncludesDiagnostics = includesDiagnostics;
        ReactionFactor = reactionFactor;
        ResistanceFactor = resistanceFactor;
        AfterDefenseFactor = afterDefenseFactor;
        Plan = plan;
    }
}

/// <summary>折叠自定义与原生候选，不读取实体、改标签或复刻 TML 的伤害公式。</summary>
public static class DamageCombiner
{
    public static CalculationResult Evaluate(CalculationInput input, bool includeDiagnostics = false)
    {
        ArgumentNullException.ThrowIfNull(input);
        ModifierTarget[] targets = EnumerateTargets(input.ReserveKeys).ToArray();
        var gates = targets.ToDictionary(target => target, target => new GateResolution(target, false, null));
        foreach (GateDecision decision in input.Gates)
        {
            if (decision.Target.Kind == ModifierTargetKind.Native) continue;
            gates[decision.Target] = new(decision.Target, decision.Enabled, decision.Sequence);
        }

        var sums = targets.ToDictionary(target => target, _ => 0f);
        List<ContributionDecision>? diagnostics = includeDiagnostics ? new() : null;
        foreach (NumericContribution candidate in input.Contributions)
        {
            GateResolution gate = gates[candidate.Target];
            if (gate.Enabled)
                sums[candidate.Target] = Finite(sums[candidate.Target] + candidate.Value, candidate.Target);
            diagnostics?.Add(new(candidate.Sequence,
                gate.Enabled ? ContributionDisposition.Accepted : ContributionDisposition.TargetDisabled,
                gate.WinningSequence));
        }

        var results = new List<TargetResult>(targets.Length);
        float reaction = 1f, resistance = 1f, afterDefense = 1f;
        foreach (ModifierTarget target in targets)
        {
            GateResolution gate = gates[target];
            float sum = sums[target];
            float factor = gate.Enabled ? GetFactor(target, sum) : 1f;
            results.Add(new(target, gate, sum, factor));
            switch (target.Stage)
            {
                case DamageStage.AfterBaseDamage:
                    reaction = Finite(reaction * factor, target);
                    break;
                case DamageStage.BeforeDefense:
                    resistance = Finite(resistance * factor, target);
                    break;
                case DamageStage.AfterDefense:
                    afterDefense = Finite(afterDefense * factor, target);
                    break;
            }
        }

        NativeModifierPlan plan = NativeCombiner.Evaluate(input, diagnostics, reaction, resistance, afterDefense);
        return new(results.ToArray(), diagnostics?.OrderBy(d => d.Sequence).ToArray() ?? Array.Empty<ContributionDecision>(),
            includeDiagnostics, reaction, resistance, afterDefense, plan);
    }

    private static float GetFactor(ModifierTarget target, float sum)
    {
        float factor = target.Kind switch
        {
            ModifierTargetKind.Resistance => 1f - sum,
            // 先使用双精度中间值，避免有限的极大精通在分子处溢出。
            ModifierTargetKind.Mastery => sum >= 0f
                ? (float)(1d + DamageConstants.PositiveMasteryScale * (double)sum / (DamageConstants.PositiveMasteryDenominator + (double)sum))
                : (float)(1d + sum / (DamageConstants.NegativeMasteryDenominator - (double)sum)),
            _ => 1f + sum
        };
        return Finite(factor, target);
    }

    private static float Finite(float value, ModifierTarget target)
    {
        if (!float.IsFinite(value))
            throw new ArithmeticException($"乘区计算产生非有限值：{target}。");
        return value;
    }

    private static IEnumerable<ModifierTarget> EnumerateTargets(IEnumerable<string> reserveKeys)
    {
        for (int i = 0; i < DamageConstants.ReactionSlotCount; i++)
            yield return ModifierTarget.Reaction(i);
        yield return ModifierTarget.Resistance;
        for (int i = 0; i < DamageConstants.ElementSlotCount; i++)
            yield return ModifierTarget.ElementDamage(i);
        yield return ModifierTarget.Mastery;
        for (int i = 0; i < DamageConstants.AscensionSlotCount; i++)
            yield return ModifierTarget.Ascension(i);
        foreach (string key in reserveKeys)
            yield return ModifierTarget.Reserve(key);
    }
}

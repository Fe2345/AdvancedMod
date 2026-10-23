#nullable enable
using System;
using System.Collections.ObjectModel;
using System.Linq;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Tag;

namespace AdvancedMod.Basic.Element;

/// <summary>冻结后的计算输入及状态提交计划；系数已经计入 DamageSeed，不能再次相乘。</summary>
public sealed class ReactionPlan
{
    public ElementRoute Route { get; }
    public HitSnapshot Hit { get; }
    public ReactionDefinition? Definition { get; }
    public ReadOnlyCollection<ElementPortion> Consumption { get; }
    public double WeightedPanelDamage { get; }
    public double DamageSeed { get; }
    public int LifeCost { get; }
    public float CooldownIncrease { get; }
    internal ElementWorkState NextState { get; }
    internal ReactionPlan(ElementRequest request, PreparedReaction prepared)
    {
        Route = prepared.Route;
        Definition = prepared.Definition;
        NextState = prepared.Working;
        Hit = request.Hit;
        var consumed = Array.Empty<ElementPortion>();
        WeightedPanelDamage = request.PanelDamage;
        DamageSeed = request.PanelDamage;
        if (Definition is not null)
        {
            consumed = Definition.Costs.SelectMany(c => NextState.Take(c.Slot,
                c.Amount * request.ConsumptionMultiplier, request.IsIdle)).ToArray();
            double total = consumed.Sum(c => c.Amount);
            if (!double.IsFinite(total) || total <= 0) throw new ArithmeticException("实际消耗必须为有限正数。");
            // 先归一化再加权，避免白值乘元素量的中间乘积溢出。
            WeightedPanelDamage = consumed.Sum(c => c.PanelDamage * (c.Amount / total));
            DamageSeed = WeightedPanelDamage * Definition.Coefficient;
            if (!double.IsFinite(DamageSeed)) throw new ArithmeticException("反应伤害种子溢出。");
            CooldownIncrease = Definition.BaseCooldown * request.CooldownMultiplier;
            float cooldown = NextState.Cooldowns[Definition.Slot] + CooldownIncrease;
            if (!float.IsFinite(cooldown)) throw new ArithmeticException("反应 CD 溢出。");
            NextState.Cooldowns[Definition.Slot] = cooldown;
            if (!Hit.Tags.Contains(Definition.Tag))
                Hit = Hit with { Tags = new TagSet(Hit.Tags.Append(new TagEntry(false, Definition.Tag))) };
            // 资格与最终反应标签成立后才产生支付计划，纯计算阶段不触碰玩家。
            if (Definition.Tag == TagEnum.HydroAnemo)
            {
                LifeCost = request.ProposedLifeCost;
                DamageSeed = WeightedPanelDamage * (Definition.Coefficient + LifeCost * AmplificationReactions.CoefficientPerLife);
            }
        }
        Consumption = Array.AsReadOnly(consumed);
    }
}

/// <summary>准备完整方案，不修改已提交目标，不调用引擎伤害。</summary>
internal static class Reaction
{
    internal static ReactionPlan Build(ElementRequest request, PreparedReaction prepared) => new(request, prepared);
}

public sealed record ReactionOutcome(long HitId, int ReactionSlot, SourceInfo? Source, SourceInfo? Target);
public sealed record PresentationRequest(long HitId, TagEnum ReactionTag);
public sealed record ElementResolution(ReactionPlan Plan, bool Committed, ReactionOutcome? Outcome,
    ReadOnlyCollection<PresentationRequest> Presentations);

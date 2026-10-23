#nullable enable
using System;
using System.Linq;
using AdvancedMod.Basic.Calculate.Core;

namespace AdvancedMod.Basic.Element;

public enum ElementRoute { Direct, DirectReaction, AttachOnly, React }

/// <summary>尚未针对目标解析的请求；生成时冻结本击面板、标签及两个修正。</summary>
public sealed class ElementRequest
{
    public HitSnapshot Hit { get; }
    public int PanelDamage { get; }
    public double Amount { get; }
    public float ConsumptionMultiplier { get; }
    public float CooldownMultiplier { get; }
    public bool IsIdle { get; }
    public int ProposedLifeCost { get; }
    internal int? IncomingSlot { get; }
    public ElementRequest(HitSnapshot hit, int panelDamage, double amount = 0,
        float consumptionMultiplier = 1, float cooldownMultiplier = 1, bool isIdle = false, int proposedLifeCost = 0)
    {
        ArgumentNullException.ThrowIfNull(hit);
        ArgumentNullException.ThrowIfNull(hit.Tags);
        if (proposedLifeCost < 0 || panelDamage < 0 || !double.IsFinite(amount) || amount < 0 ||
            !float.IsFinite(consumptionMultiplier) || consumptionMultiplier <= 0 ||
            !float.IsFinite(cooldownMultiplier) || cooldownMultiplier < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "元素量与 CD 修正须非负，消耗修正须为正，输入须有限。");
        var slots = hit.Tags.Select(t => ElementSlots.FromTag(t.type)).Where(s => s.HasValue).ToArray();
        if (amount > 0 && slots.Length != 1)
            throw new ArgumentException("正元素量请求目前必须指定唯一元素标签；多元素申请尚未定义。");
        if (isIdle && (amount != 0 || consumptionMultiplier != 1 || cooldownMultiplier != 1))
            throw new ArgumentException("静置请求不能携带附着，两个修正必须为 1。");
        Hit = isIdle ? hit with { Attacker = null, Source = new(SourceKind.Environment, "AdvancedMod:IdleReaction") } : hit;
        PanelDamage = panelDamage;
        Amount = amount;
        ConsumptionMultiplier = consumptionMultiplier;
        CooldownMultiplier = cooldownMultiplier;
        IsIdle = isIdle;
        ProposedLifeCost = isIdle ? 0 : proposedLifeCost;
        IncomingSlot = slots.Length == 1 ? slots[0] : null;
    }
}

internal sealed record PreparedReaction(ElementRoute Route, ReactionDefinition? Definition, ElementWorkState Working);

/// <summary>仅在串行事务内读取状态、检查 CD 与足量消耗，并按槽序选择一项。</summary>
internal static class Prepare
{
    internal static PreparedReaction Run(ElementRequest request, ElementWorkState committed, ReactionTable table)
    {
        var working = committed.Copy();
        if (!request.IsIdle && request.Amount == 0)
        {
            bool hasElement = request.Hit.Tags.Any(t => ElementSlots.FromTag(t.type).HasValue);
            bool hasReaction = request.Hit.Tags.Any(t => ElementSlots.ReactionFromTag(t.type).HasValue);
            return new(hasElement && hasReaction ? ElementRoute.DirectReaction : ElementRoute.Direct, null, working);
        }
        if (!request.IsIdle)
            working.Attach(new(request.IncomingSlot!.Value, request.Amount, request.PanelDamage));
        foreach (var definition in table.Slots)
        {
            if (definition is null || working.Cooldowns[definition.Slot] != 0) continue;
            if (!request.IsIdle && !definition.Costs.Any(c => c.Slot == request.IncomingSlot)) continue;
            bool enough = definition.Costs.All(c =>
            {
                double required = c.Amount * request.ConsumptionMultiplier;
                if (!double.IsFinite(required) || required <= 0)
                    throw new ArithmeticException("修正后的元素消耗溢出或下溢为零。");
                return working.Totals[c.Slot] >= required;
            });
            if (enough) return new(ElementRoute.React, definition, working);
        }
        return new(request.IsIdle ? ElementRoute.Direct : ElementRoute.AttachOnly, null, working);
    }
}

#nullable enable
using System;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Calculate.Tml;
using Terraria;
using Terraria.ID;
using AdvancedMod.Basic.Element.Presentation;

namespace AdvancedMod.Basic.Calculate.DamageOverTime;

/// <summary>每次调用独立计算；上层负责调用时机及向正确执行侧传递请求。</summary>
public static class DotService
{
    /// <summary>仅在目标权威侧接受请求；失败时不投掷暴击、不计算或排队。</summary>
    public static bool TrySubmit(CombatContext context, DotRequest request, out DotResult result, bool separateText = true)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(context.Profile.AttackId);
        result = default;
        if (!CanSubmit(context)) return false;
        result = Calculate(context, request, request.CriticalChance is > 0 and < 100 ? Main.rand.NextDouble() : 0);
        if (result.Damage == 0) return true;
        DotTargetState state = context.TargetNpc is { } npc
            ? npc.GetGlobalNPC<DotNpc>().GetState(npc)
            : context.TargetPlayer!.GetModPlayer<DotPlayer>().State;
        var source = new DotSource(context.Profile.AttackId!, context.AttackerPlayer?.whoAmI, context.ExplicitSource);
        state.Ledger.Submit(source, result, ElementTextStyle.SelectElement(context.Profile.Tags), separateText);
        if (context.TargetNpc is { } textNpc) textNpc.GetGlobalNPC<DotNpc>().EnableText(textNpc);
        else context.TargetPlayer!.GetModPlayer<DotPlayer>().EnableText();
        DotSystem.Track(state);
        if (context.TargetNpc is { } target && context.AttackerPlayer is { active: true } attacker)
            target.PlayerInteraction(attacker.whoAmI);
        return true;
    }

    public static bool CanSubmit(CombatContext context) => context.Kind switch
    {
        HitKind.DamageOverTimeToNpc => Main.netMode != NetmodeID.MultiplayerClient &&
            context.TargetNpc is { active: true, life: > 0, dontTakeDamage: false, immortal: false },
        HitKind.DamageOverTimeToPlayer => Main.netMode != NetmodeID.Server &&
            context.TargetPlayer is { active: true, dead: false, ghost: false } player && player.whoAmI == Main.myPlayer,
        _ => false
    };

    /// <summary>只计算不排队，供预览及上层验证；不会触发附着、反应事务或 OnHit。</summary>
    public static DotResult Calculate(CombatContext context, DotRequest request, double roll)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();
        double defense = context.Kind switch
        {
            HitKind.DamageOverTimeToNpc => context.TargetNpc!.defense,
            HitKind.DamageOverTimeToPlayer => context.TargetPlayer!.statDefense,
            _ => throw new ArgumentException("必须使用持续伤害上下文。", nameof(context))
        };
        return DotMath.Calculate(request, CalculateSystem.Pipeline.Calculate(context), defense, roll);
    }
}

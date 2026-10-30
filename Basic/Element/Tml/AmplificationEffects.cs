#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedMod.Basic.Calculate.Tml;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Basic.Element.Tml;

/// <summary>反应本体的引擎副作用；只在原生事务确认成功后执行。</summary>
internal static class AmplificationEffects
{
    private sealed class PendingEffect(CombatContext context, ReactionPlan plan)
    {
        internal readonly CombatContext Context = context;
        internal readonly ReactionPlan Plan = plan;
        internal readonly NpcDamageObservation.Sample Damage = new();
        internal bool Crit;
        internal float InputDamage;
        internal float InheritedDamage;
        internal int Payment;
        internal bool SkyFire;
    }
    private static readonly Dictionary<long, PendingEffect> pending = new();
    internal static void Clear() => pending.Clear();
    internal static void Prepare(CombatContext context, ReactionPlan plan, float? inputDamage = null, bool skyFire = false)
    {
        if (plan.Definition is null && !skyFire) return;
        var effect = new PendingEffect(context, plan) { InputDamage = inputDamage ?? (float)plan.DamageSeed,
            InheritedDamage = (float)plan.DamageSeed, Payment = plan.LifeCost, SkyFire = skyFire };
        pending.Add(plan.Hit.HitId, effect);
        if (context.TargetNpc is not null) NpcDamageObservation.Pending = effect.Damage;
    }
    internal static void CaptureSource(CombatContext context, StatModifier source, CalculationResult result)
    {
        if (!pending.TryGetValue(context.HitId, out var effect)) return;
        var delta = result.Plan.SourceDamageBeforeResistance;
        source.Base += delta.Base;
        source += delta.AdditiveBonus;
        source *= delta.Multiplicative;
        source.Flat += delta.Flat;
        effect.InheritedDamage = source.ApplyTo(effect.InputDamage);
    }
    internal static void RecordHit(ReactionPlan plan, NPC.HitInfo hit)
    {
        if (pending.TryGetValue(plan.Hit.HitId, out var effect))
        {
            effect.Crit = hit.Crit;
            if (plan.IsHallowed && effect.Context.TargetNpc is { } npc)
            {
                var root = Main.npc[Root(npc)];
                effect.Payment = HolyReactionRules.Payment(plan.LifeCost, root.life, hit.Damage);
            }
        }
        NpcDamageObservation.Pending = null;
    }
    internal static void Complete(ElementResolution result)
    {
        if (!pending.Remove(result.Plan.Hit.HitId, out var effect) || !result.Committed) return;
        if (effect.SkyFire) { HolyFieldSystem.SkyFire(effect.Context); return; }
        HolyFieldSystem.Field? field = null;
        if (effect.Context.AttackerPlayer is { active: true, dead: false } player)
        {
            if (effect.Plan.IsHallowed && effect.Context.TargetNpc is { } target)
                field = HolyFieldSystem.Activate(player, target.Center);
            else if (HolyFieldSystem.Fields.TryGetValue(player.whoAmI, out var existing) && existing.IsActive)
                existing.Expires = HolyFieldSystem.Tick + HolyReactionRules.Lifetime;
            if (effect.Payment > 0)
            {
                int before = player.statLife;
                ElementNetwork.PayReactionLife(player, effect.Payment);
                if (field is not null) HolyFieldSystem.RecordLife(field, before - player.statLife);
            }
        }
        if (effect.Context.TargetNpc is not { } npc) return;
        if (effect.Plan.Definition!.Tag == TagEnum.PyroCryo) HolyFieldSystem.ResetMeltWindow(npc);
        ElementNetwork.ShowReaction(npc.Center, npc.Center, effect.Plan.Definition!.Tag);
        if (effect.Plan.Definition.Tag != TagEnum.HydroElectro || !(effect.Damage.BeforeMinimum > 0)) return;
        field?.PlaceNode(npc.Center);
        var visited = new HashSet<int> { Root(npc) };
        Continue(effect.Context, npc.Center, effect.InheritedDamage, effect.Crit, visited, field, false);
    }

    private static int Root(NPC npc) => npc.realLife >= 0 ? npc.realLife : npc.whoAmI;
    private static void Continue(CombatContext source, Vector2 origin, float inheritedDamage, bool crit, HashSet<int> visited,
        HolyFieldSystem.Field? field, bool charged)
    {
        NPC? nearest = null;
        float nearestDistance = AmplificationReactions.ChainRadius * AmplificationReactions.ChainRadius;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!npc.CanBeChasedBy() || npc.friendly || npc.immortal || visited.Contains(Root(npc))) continue;
            float distance = Vector2.DistanceSquared(origin, npc.Center);
            if (distance > nearestDistance || distance == nearestDistance && nearest is not null && npc.whoAmI > nearest.whoAmI ||
                !Collision.CanHitLine(origin, 1, 1, npc.Center, 1, 1)) continue;
            nearest = npc;
            nearestDistance = distance;
        }
        Vector2? bridge = null;
        bool usedCore = false;
        if (nearest is null && field is not null)
        {
            foreach (NPC candidate in Main.npc.Where(n => HolyFieldSystem.Enemy(n) && !visited.Contains(Root(n)))
                .OrderBy(n => Vector2.DistanceSquared(origin, n.Center)).ThenBy(n => n.whoAmI))
            {
                if (!HolyFieldSystem.TryBridge(field, origin, candidate, out var via, out var core)) continue;
                nearest = candidate; bridge = via; usedCore = core; break;
            }
        }
        if (nearest is null) return;
        NPC targetNpc = nearest;
        visited.Add(Root(targetNpc));
        var target = targetNpc.GetGlobalNPC<CombatGlobalNPC>().Elements;
        float seed = inheritedDamage * AmplificationReactions.ChainDecay;
        var entries = new List<TagEntry> { new(false, TagEnum.Electro), new(false, TagEnum.HydroElectro) };
        if (field is not null) entries.Add(new(false, TagEnum.Hallowed));
        var tags = new TagSet(entries);
        var context = source.ForChain(targetNpc, new(tags, "AdvancedMod:ConductiveArc", true));
        var sample = new NpcDamageObservation.Sample();
        bool penetrated = false;
        Vector2 endpoint = targetNpc.Center;
        target.RunNative(ElementHitSystem.Reactions, tx =>
        {
            if (!targetNpc.active || targetNpc.dontTakeDamage || targetNpc.immortal) return;
            var plan = tx.Prepare(new(context.CreateSnapshot(), (int)Math.Round(seed)));
            var modifiers = targetNpc.GetIncomingStrikeModifiers(DamageClass.Generic, 0);
            ElementTextSystem.PrepareNpc(context, plan, ref modifiers);
            var sourceModifier = modifiers.SourceDamage;
            var result = CalculateSystem.Pipeline.ModifyNpcHit(context, ref modifiers);
            // 攻击源前段增伤已包含在传入种子中；每个新目标只重算自身抗性，不再叠乘反应增伤。
            modifiers.SourceDamage = sourceModifier * result.ResistanceFactor;
            var previous = NpcDamageObservation.Pending;
            NPC.HitInfo hit;
            try
            {
                NpcDamageObservation.Pending = sample;
                hit = modifiers.ToHitInfo(seed, crit, 0, false);
            }
            finally { NpcDamageObservation.Pending = previous; }
            penetrated = sample.BeforeMinimum > 0;
            // 未破甲节点连保底伤害也不施加，且不继续寻找其他目标。
            if (!penetrated) return;
            if (source.AttackerPlayer is { active: true } attacker) targetNpc.PlayerInteraction(attacker.whoAmI);
            tx.DamageDone = targetNpc.StrikeNPC(hit, false, source.IsEnvironmental);
            if (Main.netMode == NetmodeID.Server) NetMessage.SendStrikeNPC(targetNpc, in hit);
        }, () => ReferenceEquals(target, targetNpc.GetGlobalNPC<CombatGlobalNPC>().Elements), result =>
        {
            ElementHitSystem.Complete(result);
            if (!result.Committed || !penetrated) return;
            if (bridge is { } via) ElementNetwork.ShowReaction(origin, via, TagEnum.HydroElectro);
            ElementNetwork.ShowReaction(bridge ?? origin, endpoint, TagEnum.HydroElectro);
            if (field is not null)
            {
                field.PlaceNode(endpoint);
                if (usedCore && !charged) { HolyFieldSystem.Charge(field); charged = true; }
            }
            Continue(source, endpoint, seed, crit, visited, field, charged);
        });
    }
}

#nullable enable
using System;
using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Data;
using AdvancedMod.DamageClasses.Element.Definitions;
using AdvancedMod.DamageClasses.Element.Input;
using AdvancedMod.DamageClasses.Element.Plans;
using AdvancedMod.DamageClasses.Element.Reactions;
using AdvancedMod.DamageClasses.Element.Services;
using AdvancedMod.DamageClasses.Element.State;
using AdvancedMod.Utils;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.DamageClasses.Element
{
    /// <summary>
    /// 协调一次命中的元素输入、反应选择与统一执行。
    /// </summary>
    public static class ElementCombatSystem
    {
        private sealed class PlannedHit
        {
            public required AttackPayload Payload { get; init; }

            public required ElementApplyPlan ApplyPlan { get; init; }

            public required ReactionResult ReactionResult { get; init; }
        }

        /// <summary>
        /// 处理近战或直接物品命中的元素主链入口。
        /// </summary>
        public static void ProcessItemHit(Player player, NPC target, Item item, NPC.HitInfo hit, int damageDone, ElementType element, int buildUp = 1, AttackFlags flags = AttackFlags.None)
        {
            AttackSourceIntent intent = BuildIntent(player.whoAmI, item.type, AttackSourceKind.Item, damageDone, item.DamageType, item.knockBack, element, buildUp, flags);
            ProcessHit(player, target, intent);
        }

        /// <summary>
        /// 在正式命中前，根据元素快照修饰近战或直接物品命中的原始伤害。
        /// </summary>
        public static void ModifyItemHit(Player player, NPC target, Item item, ref NPC.HitModifiers modifiers, ElementType element, int buildUp = 1, AttackFlags flags = AttackFlags.None)
        {
            AttackSourceIntent intent = BuildIntent(player.whoAmI, item.type, AttackSourceKind.Item, Math.Max(1, item.damage), item.DamageType, item.knockBack, element, buildUp, flags);
            ModifyOutgoingHit(player, target, intent, ref modifiers);
        }

        /// <summary>
        /// 处理弹幕命中的元素主链入口。
        /// </summary>
        public static void ProcessProjectileHit(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone, ElementType element, int buildUp = 1, AttackFlags flags = AttackFlags.None)
        {
            Player? owner = projectile.owner >= 0 && projectile.owner < Main.maxPlayers ? Main.player[projectile.owner] : null;
            AttackSourceIntent intent = BuildIntent(projectile.owner, projectile.type, AttackSourceKind.Projectile, damageDone, projectile.DamageType, projectile.knockBack, element, buildUp, flags);
            ProcessHit(owner, target, intent);
        }

        /// <summary>
        /// 在正式命中前，根据元素快照修饰弹幕命中的原始伤害。
        /// </summary>
        public static void ModifyProjectileHit(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers, ElementType element, int buildUp = 1, AttackFlags flags = AttackFlags.None)
        {
            Player? owner = projectile.owner >= 0 && projectile.owner < Main.maxPlayers ? Main.player[projectile.owner] : null;
            int previewDamage = projectile.originalDamage > 0 ? projectile.originalDamage : projectile.damage;
            AttackSourceIntent intent = BuildIntent(projectile.owner, projectile.type, AttackSourceKind.Projectile, previewDamage, projectile.DamageType, projectile.knockBack, element, buildUp, flags);
            ModifyOutgoingHit(owner, target, intent, ref modifiers);
        }

        private static AttackSourceIntent BuildIntent(int playerId, int sourceEntityId, AttackSourceKind sourceKind, int damage, DamageClass damageClass, float knockback, ElementType element, int buildUp, AttackFlags flags)
        {
            return new AttackSourceIntent
            {
                SourcePlayerId = playerId,
                SourceEntityId = sourceEntityId,
                SourceKind = sourceKind,
                DamageClass = damageClass,
                BaseDamage = Math.Max(1, damage),
                Knockback = knockback,
                Flags = flags,
                ElementSeeds =
                [
                    new AttackElementSeed
                    {
                        Element = element,
                        BaseBuildUp = Math.Max(1, buildUp),
                        CooldownGroup = 0,
                        CooldownTicks = 0,
                    }
                ],
            };
        }

        private static void ProcessHit(Player? sourcePlayer, NPC target, AttackSourceIntent intent)
        {
            PlannedHit plannedHit = ResolvePlannedHit(sourcePlayer, target, intent);
            AdvancedGlobelNPC advancedNpc = target.Advanced();
            ElementComponent component = advancedNpc.ElementComponent;

            ReactionExecutor.Execute(sourcePlayer, target, component, plannedHit.ApplyPlan, plannedHit.ReactionResult);
            CoordinatedAttackDispatcher.Dispatch(sourcePlayer, target, plannedHit.Payload);
        }

        private static void ModifyOutgoingHit(Player? sourcePlayer, NPC target, AttackSourceIntent intent, ref NPC.HitModifiers modifiers)
        {
            PlannedHit plannedHit = ResolvePlannedHit(sourcePlayer, target, intent);
            float sourceDamageMultiplier = ResolveCustomElementDamageMultiplier(plannedHit.Payload);

            if (plannedHit.ReactionResult.SemanticKind == ReactionSemanticKind.Amplification)
            {
                sourceDamageMultiplier *= Math.Max(1f, plannedHit.ReactionResult.AmplificationMultiplier);
            }

            modifiers.SourceDamage *= sourceDamageMultiplier;
        }

        private static PlannedHit ResolvePlannedHit(Player? sourcePlayer, NPC target, AttackSourceIntent intent)
        {
            CombatContextSnapshot combatContextSnapshot = sourcePlayer?.Advanced().ElementCombatState.ExportSnapshot() ?? new CombatContextSnapshot();
            AttackInputResolveContext resolveContext = new()
            {
                SourcePlayer = sourcePlayer,
                Target = target,
                Intent = intent,
                CombatContextSnapshot = combatContextSnapshot,
            };

            InputAugmentPlan augmentPlan = AttackInputModifierPipeline.Resolve(resolveContext);
            AttackPayload payload = PayloadBuilder.Build(intent, augmentPlan, combatContextSnapshot);
            ElementApplyPlan applyPlan = BuildApplyPlan(payload);
            TemporaryElementStateView temporaryStateView = new(target.Advanced().ElementComponent, applyPlan);
            IReadOnlyList<ReactionCandidate> reactionCandidates = payload.CanTriggerReaction
                ? ReactionCandidateCollector.Collect(payload, temporaryStateView)
                : [];
            IReadOnlyList<ReactionValidationResult> validationResults = payload.CanTriggerReaction
                ? ReactionValidator.ValidateAll(reactionCandidates, temporaryStateView)
                : [];
            ReactionValidationResult? selectedReaction = payload.CanTriggerReaction
                ? ReactionPriorityResolver.Resolve(validationResults)
                : null;

            return new PlannedHit
            {
                Payload = payload,
                ApplyPlan = applyPlan,
                ReactionResult = BuildReactionResult(sourcePlayer, payload, selectedReaction),
            };
        }

        private static ElementApplyPlan BuildApplyPlan(AttackPayload payload)
        {
            if (!payload.CanApplyElement)
            {
                return new ElementApplyPlan();
            }

            List<ElementApplyEntry> entries = [];

            foreach (ElementApplication application in payload.Applications)
            {
                entries.Add(new ElementApplyEntry
                {
                    Element = application.Element,
                    AppliedBuildUp = application.BuildUp,
                    SourceApplication = application,
                });
            }

            return new ElementApplyPlan
            {
                Entries = entries,
            };
        }

        private static ReactionResult BuildReactionResult(Player? sourcePlayer, AttackPayload payload, ReactionValidationResult? selectedReaction)
        {
            if (selectedReaction is null || !selectedReaction.IsValid)
            {
                return new ReactionResult();
            }

            return ReactionDamageCalculator.Calculate(new ReactionDamageContext
            {
                Payload = payload,
                SelectedReaction = selectedReaction,
                SourcePlayer = sourcePlayer,
            });
        }

        private static float ResolveCustomElementDamageMultiplier(AttackPayload payload)
        {
            float additiveBonus = 0f;
            HashSet<ElementType> countedElements = [];

            foreach (ElementApplication application in payload.Applications)
            {
                if (!countedElements.Add(application.Element))
                {
                    continue;
                }

                if (payload.ContextSnapshot.ElementOffense.ElementDamageBonus.TryGetValue(application.Element, out float elementBonus))
                {
                    additiveBonus += elementBonus;
                }
            }

            return Math.Max(0f, 1f + additiveBonus);
        }

    }
}

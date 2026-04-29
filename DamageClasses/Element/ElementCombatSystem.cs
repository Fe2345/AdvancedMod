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
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.DamageClasses.Element
{
    /// <summary>
    /// 协调一次命中的元素输入、反应选择与统一执行。
    /// </summary>
    public static class ElementCombatSystem
    {
        private const int PrototypeGaugePerHit = 1;
        private const int PrototypeExtraWindDamage = 10000;
        private const float OverloadedBonusKnockback = 8f;

        /// <summary>
        /// 处理近战或直接物品命中的元素主链入口。
        /// </summary>
        public static void ProcessItemHit(Player player, NPC target, Item item, NPC.HitInfo hit, int damageDone, ElementType element, int buildUp = 1)
        {
            AttackSourceIntent intent = BuildIntent(player.whoAmI, item.type, AttackSourceKind.Item, damageDone, item.DamageType, item.knockBack, element, buildUp);
            ProcessHit(player, target, intent);
        }

        /// <summary>
        /// 处理弹幕命中的元素主链入口。
        /// </summary>
        public static void ProcessProjectileHit(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone, ElementType element, int buildUp = 1)
        {
            Player? owner = projectile.owner >= 0 && projectile.owner < Main.maxPlayers ? Main.player[projectile.owner] : null;
            AttackSourceIntent intent = BuildIntent(projectile.owner, projectile.type, AttackSourceKind.Projectile, damageDone, projectile.DamageType, projectile.knockBack, element, buildUp);
            ProcessHit(owner, target, intent);
        }

        private static AttackSourceIntent BuildIntent(int playerId, int sourceEntityId, AttackSourceKind sourceKind, int damage, DamageClass damageClass, float knockback, ElementType element, int buildUp)
        {
            return new AttackSourceIntent
            {
                SourcePlayerId = playerId,
                SourceEntityId = sourceEntityId,
                SourceKind = sourceKind,
                DamageClass = damageClass,
                BaseDamage = Math.Max(1, damage),
                Knockback = knockback,
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
            AttackInputResolveContext resolveContext = new()
            {
                SourcePlayer = sourcePlayer,
                Target = target,
                Intent = intent,
            };

            InputAugmentPlan augmentPlan = AttackInputModifierPipeline.Resolve(resolveContext);
            AttackPayload payload = PayloadBuilder.Build(intent, augmentPlan);
            AdvancedGlobelNPC advancedNpc = target.Advanced();
            ElementComponent component = advancedNpc.ElementComponent;
            ElementApplyPlan applyPlan = BuildApplyPlan(payload);
            TemporaryElementStateView temporaryStateView = new(component, applyPlan);
            IReadOnlyList<ReactionCandidate> reactionCandidates = payload.CanTriggerReaction
                ? ReactionCandidateCollector.Collect(payload, temporaryStateView)
                : [];
            IReadOnlyList<ReactionValidationResult> validationResults = payload.CanTriggerReaction
                ? ReactionValidator.ValidateAll(reactionCandidates, temporaryStateView)
                : [];
            ReactionValidationResult? selectedReaction = payload.CanTriggerReaction
                ? ReactionPriorityResolver.Resolve(validationResults)
                : null;
            ReactionResult result = BuildReactionResult(sourcePlayer, payload, selectedReaction);

            ReactionExecutor.Execute(sourcePlayer, target, component, applyPlan, result);
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

            ReactionRule rule = selectedReaction.Rule;
            int primaryDamage = CalculatePrimaryDamage(payload, rule);
            List<DamageInstancePlan> extras = [];

            if (rule.Reaction == ReactionType.ThunderWind && sourcePlayer?.Advanced().PrototypeReactionExtraWindBurst == true)
            {
                extras.Add(new DamageInstancePlan
                {
                    Damage = PrototypeExtraWindDamage,
                    Element = ElementType.Wind,
                });
            }

            return new ReactionResult
            {
                Triggered = true,
                Reaction = rule.Reaction,
                TriggerElement = selectedReaction.Candidate.TriggerElement,
                TargetElement = selectedReaction.Candidate.TargetElement,
                TriggerConsumeAmount = selectedReaction.TriggerConsumeAmount,
                TargetConsumeAmount = selectedReaction.TargetConsumeAmount,
                PrimaryDamage = new DamageInstancePlan
                {
                    Damage = primaryDamage,
                    Element = rule.DamageElement,
                },
                ExtraDamages = extras,
                DisplayText = ReactionTextResolver.GetDisplayText(rule.Reaction),
                BonusKnockback = rule.Reaction == ReactionType.Overloaded ? OverloadedBonusKnockback : 0f,
            };
        }

        private static int CalculatePrimaryDamage(AttackPayload payload, ReactionRule rule)
        {
            return rule.DamageModel switch
            {
                ReactionDamageModel.ScaleFromSourceDamage => Math.Max(1, (int)Math.Round(payload.BaseDamage * Math.Max(0f, rule.BaseDamageFactor - 1f))),
                _ => Math.Max(1, payload.BaseDamage),
            };
        }
    }
}

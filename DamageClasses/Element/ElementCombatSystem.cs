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
    /// Implements the minimal thunder-wind prototype combat flow used to validate the full element pipeline in game.
    /// </summary>
    public static class ElementCombatSystem
    {
        private const int PrototypeGaugePerHit = 1;
        private const int PrototypeConsumePerSide = 1;
        private const int PrototypeExtraWindDamage = 10000;
        private const string PrototypeReactionText = "TW";

        private static readonly ReactionRule ThunderWindRule = new()
        {
            Reaction = ReactionType.ThunderWind,
            TriggerElement = ElementType.Thunder,
            TargetElement = ElementType.Wind,
            TriggerConsumeMode = ReactionConsumeMode.Fixed,
            TriggerConsumeValue = PrototypeConsumePerSide,
            TargetConsumeMode = ReactionConsumeMode.Fixed,
            TargetConsumeValue = PrototypeConsumePerSide,
            DamageModel = ReactionDamageModel.ScaleFromSourceDamage,
            BaseDamageFactor = 1.5f,
            DamageElement = ElementType.Thunder,
        };

        /// <summary>
        /// Processes a direct item hit for the prototype system.
        /// </summary>
        public static void ProcessItemHit(Player player, NPC target, Item item, NPC.HitInfo hit, int damageDone, ElementType element)
        {
            AttackSourceIntent intent = BuildIntent(player.whoAmI, item.type, AttackSourceKind.Item, damageDone, item.DamageType, item.knockBack, element);
            ProcessHit(player, target, intent);
        }

        /// <summary>
        /// Processes a projectile hit for the prototype system.
        /// </summary>
        public static void ProcessProjectileHit(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone, ElementType element)
        {
            Player? owner = projectile.owner >= 0 && projectile.owner < Main.maxPlayers ? Main.player[projectile.owner] : null;
            AttackSourceIntent intent = BuildIntent(projectile.owner, projectile.type, AttackSourceKind.Projectile, damageDone, projectile.DamageType, projectile.knockBack, element);
            ProcessHit(owner, target, intent);
        }

        private static AttackSourceIntent BuildIntent(int playerId, int sourceEntityId, AttackSourceKind sourceKind, int damage, DamageClass damageClass, float knockback, ElementType element)
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
                        BaseBuildUp = PrototypeGaugePerHit,
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
            ElementApplyPlan? applyPlan = null;
            TemporaryElementStateView? temporaryStateView = null;
            ReactionCandidate? reactionCandidate = null;
            ReactionValidationResult? reactionValidationResult = null;

            _ = payload;
            _ = applyPlan;
            _ = temporaryStateView;
            _ = reactionCandidate;
            _ = reactionValidationResult;

            if (false)
            {
                IReadOnlyList<ReactionCandidate> reactionCandidates = ReactionCandidateCollector.Collect(payload, temporaryStateView!);
                IReadOnlyList<ReactionValidationResult> validationResults = ReactionValidator.ValidateAll(reactionCandidates, temporaryStateView!);
                ReactionValidationResult? selectedReaction = ReactionPriorityResolver.Resolve(validationResults);
                _ = selectedReaction;
            }

            foreach (ElementApplication application in payload.Applications)
            {
                ElementState currentState = GetState(component, application.Element);
                ElementState oppositeState = GetOppositeState(component, application.Element);

                currentState.CurrentGauge += Math.Max(PrototypeGaugePerHit, (int)Math.Round(application.BuildUp));

                if (!payload.CanTriggerReaction || oppositeState.CurrentGauge <= 0)
                {
                    continue;
                }

                currentState.CurrentGauge = Math.Max(0, currentState.CurrentGauge - PrototypeConsumePerSide);
                oppositeState.CurrentGauge = Math.Max(0, oppositeState.CurrentGauge - PrototypeConsumePerSide);

                ReactionResult result = BuildReactionResult(sourcePlayer, payload);
                ApplyReactionResult(target, component, result);
            }
        }

        private static ReactionResult BuildReactionResult(Player? sourcePlayer, AttackPayload payload)
        {
            int primaryDamage = (int)Math.Round(payload.BaseDamage * ThunderWindRule.BaseDamageFactor);
            List<DamageInstancePlan> extras = [];

            if (sourcePlayer?.Advanced().PrototypeReactionExtraWindBurst == true)
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
                Reaction = ThunderWindRule.Reaction,
                PrimaryDamage = new DamageInstancePlan
                {
                    Damage = primaryDamage,
                    Element = ThunderWindRule.DamageElement,
                },
                ExtraDamages = extras,
                DisplayText = PrototypeReactionText,
            };
        }

        private static void ApplyReactionResult(NPC target, ElementComponent component, ReactionResult result)
        {
            if (!result.Triggered)
            {
                return;
            }

            if (result.PrimaryDamage is not null)
            {
                ApplyDamage(target, result.PrimaryDamage.Damage);
            }

            foreach (DamageInstancePlan extraDamage in result.ExtraDamages)
            {
                ApplyDamage(target, extraDamage.Damage);
            }

            component.LastReactionText = result.DisplayText;
            component.LastReactionTick = (int)Main.GameUpdateCount;
            CombatText.NewText(target.Hitbox, Color.Gold, result.DisplayText);
        }

        private static void ApplyDamage(NPC target, int damage)
        {
            target.SimpleStrikeNPC(Math.Max(1, damage), 0, false, 0f, ModContent.GetInstance<ElementDamage>());
        }

        private static ElementState GetState(ElementComponent component, ElementType element)
        {
            return element switch
            {
                ElementType.Thunder => component.Thunder,
                ElementType.Wind => component.Wind,
                _ => component.Thunder,
            };
        }

        private static ElementState GetOppositeState(ElementComponent component, ElementType element)
        {
            return element switch
            {
                ElementType.Thunder => component.Wind,
                ElementType.Wind => component.Thunder,
                _ => component.Wind,
            };
        }
    }
}

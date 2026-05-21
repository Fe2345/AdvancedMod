#nullable enable
using System;
using AdvancedMod.DamageClasses.Element.Definitions;
using AdvancedMod.DamageClasses.Element.Plans;
using AdvancedMod.DamageClasses.Element.Reactions;
using AdvancedMod.DamageClasses.Element.State;
using AdvancedMod.Utils;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.DamageClasses.Element.Services
{
    /// <summary>
    /// 统一执行元素附着、元素消耗、反应伤害和反应显示。
    /// </summary>
    public static class ReactionExecutor
    {
        /// <summary>
        /// 执行本次命中的元素写回与反应结算。
        /// </summary>
        public static void Execute(Player? sourcePlayer, NPC target, ElementComponent component, ElementApplyPlan applyPlan, ReactionResult result)
        {
            ApplyElementPlan(component, applyPlan);

            if (!result.Triggered)
            {
                return;
            }

            ConsumeGauge(component, result.TriggerElement, result.TriggerConsumeAmount);
            ConsumeGauge(component, result.TargetElement, result.TargetConsumeAmount);

            if (result.SemanticKind == ReactionSemanticKind.Prototype && result.PrimaryDamage is not null)
            {
                ApplyDamage(sourcePlayer, target, result.PrimaryDamage);
            }

            if (result.SemanticKind == ReactionSemanticKind.Transformative && result.TransformativeDamage is not null)
            {
                ApplyDamage(sourcePlayer, target, result.TransformativeDamage);
            }

            if (result.SemanticKind != ReactionSemanticKind.Prototype)
            {
                foreach (DamageInstancePlan extraDamage in result.AdditionalEffectDamages)
                {
                    ApplyDamage(sourcePlayer, target, extraDamage);
                }
            }

            if (result.SemanticKind == ReactionSemanticKind.Prototype)
            {
                foreach (DamageInstancePlan extraDamage in result.ExtraDamages)
                {
                    ApplyDamage(sourcePlayer, target, extraDamage);
                }
            }

            if (!string.IsNullOrWhiteSpace(result.DisplayText))
            {
                component.LastReactionText = result.DisplayText;
                component.LastReactionTick = (int)Main.GameUpdateCount;
                CombatText.NewText(target.Hitbox, Color.Gold, result.DisplayText);
            }

            if (result.BonusKnockback > 0f)
            {
                ApplyKnockback(sourcePlayer, target, result.BonusKnockback);
            }
        }

        private static void ApplyElementPlan(ElementComponent component, ElementApplyPlan applyPlan)
        {
            foreach (ElementApplyEntry entry in applyPlan.Entries)
            {
                ElementState? state = GetState(component, entry.Element);

                if (state is null)
                {
                    continue;
                }

                state.CurrentGauge += Math.Max(0, (int)Math.Round(entry.AppliedBuildUp));
            }
        }

        private static void ConsumeGauge(ElementComponent component, ElementType element, int consumeAmount)
        {
            if (consumeAmount <= 0)
            {
                return;
            }

            ElementState? state = GetState(component, element);

            if (state is null)
            {
                return;
            }

            state.CurrentGauge = Math.Max(0, state.CurrentGauge - consumeAmount);
        }

        private static void ApplyDamage(Player? sourcePlayer, NPC target, DamageInstancePlan damagePlan)
        {
            DamageClass damageClass = damagePlan.DamageClass ?? ModContent.GetInstance<ElementDamage>();

            if (sourcePlayer is not null &&
                ExtraStrikeHelper.TryStrikeExtraDamage(
                    sourcePlayer,
                    target,
                    Math.Max(1, damagePlan.Damage),
                    damageClass,
                    0,
                    damagePlan.Knockback,
                    damagePlan.SourceMultiplier,
                    damagePlan.FinalMultiplier,
                    damagePlan.CritChanceBonus,
                    damagePlan.CanCrit,
                    damagePlan.DamageVariation))
            {
                return;
            }
            //如果玩家已丢失，或者额外伤害未成功应用（如被免疫），则直接应用基础伤害。
            target.SimpleStrikeNPC(Math.Max(1, damagePlan.Damage), 0, false, damagePlan.Knockback, damageClass);
        }

        private static void ApplyKnockback(Player? sourcePlayer, NPC target, float knockbackStrength)
        {
            float horizontalDirection = 1f;

            if (sourcePlayer is not null)
            {
                horizontalDirection = sourcePlayer.Center.X <= target.Center.X ? 1f : -1f;
            }

            target.velocity.X += knockbackStrength * horizontalDirection;
            target.netUpdate = true;
        }

        private static ElementState? GetState(ElementComponent component, ElementType element)
        {
            return element switch
            {
                ElementType.Thunder => component.Thunder,
                ElementType.Wind => component.Wind,
                ElementType.Pyro => component.Pyro,
                _ => null,
            };
        }
    }
}

using System;
using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Data;
using AdvancedMod.DamageClasses.Element.Definitions;
using AdvancedMod.DamageClasses.Element.Reactions;
using AdvancedMod.Utils;

namespace AdvancedMod.DamageClasses.Element.Services
{
    /// <summary>
    /// 根据反应语义计算反应结果中的伤害与附加效果。
    /// </summary>
    public static class ReactionDamageCalculator
    {
        private const int PrototypeExtraWindDamage = 10000;
        private const float OverloadedBonusKnockback = 8f;
        private const float MasteryDivisor = 2000f;

        /// <summary>
        /// 计算一次命中的反应伤害结果。
        /// </summary>
        public static ReactionResult Calculate(ReactionDamageContext context)
        {
            ReactionValidationResult selectedReaction = context.SelectedReaction;

            if (!selectedReaction.IsValid)
            {
                return new ReactionResult();
            }

            ReactionRule rule = selectedReaction.Rule;
            List<DamageInstancePlan> additionalEffectDamages = BuildAdditionalEffectDamages(context, rule);
            ReactionResult baseResult = new()
            {
                Triggered = true,
                Reaction = rule.Reaction,
                SemanticKind = rule.SemanticKind,
                TriggerElement = selectedReaction.Candidate.TriggerElement,
                TargetElement = selectedReaction.Candidate.TargetElement,
                TriggerConsumeAmount = selectedReaction.TriggerConsumeAmount,
                TargetConsumeAmount = selectedReaction.TargetConsumeAmount,
                DisplayText = ReactionTextResolver.GetDisplayText(rule.Reaction),
                BonusKnockback = rule.Reaction == ReactionType.Overloaded ? OverloadedBonusKnockback : 0f,
                AdditionalEffectDamages = additionalEffectDamages,
            };

            return rule.SemanticKind switch
            {
                ReactionSemanticKind.Amplification => BuildAmplificationResult(baseResult, context, rule),
                ReactionSemanticKind.Transformative => BuildTransformativeResult(baseResult, context, rule),
                ReactionSemanticKind.Prototype => BuildPrototypeResult(baseResult, context, rule, additionalEffectDamages),
                _ => baseResult,
            };
        }

        private static ReactionResult BuildAmplificationResult(ReactionResult baseResult, ReactionDamageContext context, ReactionRule rule)
        {
            return new ReactionResult
            {
                Triggered = baseResult.Triggered,
                Reaction = baseResult.Reaction,
                SemanticKind = baseResult.SemanticKind,
                TriggerElement = baseResult.TriggerElement,
                TargetElement = baseResult.TargetElement,
                TriggerConsumeAmount = baseResult.TriggerConsumeAmount,
                TargetConsumeAmount = baseResult.TargetConsumeAmount,
                AmplificationMultiplier = CalculateAmplificationMultiplier(context, rule),
                AdditionalEffectDamages = baseResult.AdditionalEffectDamages,
                DisplayText = baseResult.DisplayText,
                BonusKnockback = baseResult.BonusKnockback,
            };
        }

        private static ReactionResult BuildTransformativeResult(ReactionResult baseResult, ReactionDamageContext context, ReactionRule rule)
        {
            DamageInstancePlan damagePlan = CalculateResolvedDamageInstance(context, rule, usePrototypeDelta: false);

            return new ReactionResult
            {
                Triggered = baseResult.Triggered,
                Reaction = baseResult.Reaction,
                SemanticKind = baseResult.SemanticKind,
                TriggerElement = baseResult.TriggerElement,
                TargetElement = baseResult.TargetElement,
                TriggerConsumeAmount = baseResult.TriggerConsumeAmount,
                TargetConsumeAmount = baseResult.TargetConsumeAmount,
                TransformativeDamage = damagePlan,
                AdditionalEffectDamages = baseResult.AdditionalEffectDamages,
                DisplayText = baseResult.DisplayText,
                BonusKnockback = baseResult.BonusKnockback,
            };
        }

        private static ReactionResult BuildPrototypeResult(ReactionResult baseResult, ReactionDamageContext context, ReactionRule rule, IReadOnlyList<DamageInstancePlan> additionalEffectDamages)
        {
            DamageInstancePlan damagePlan = CalculateResolvedDamageInstance(context, rule, usePrototypeDelta: true);

            return new ReactionResult
            {
                Triggered = baseResult.Triggered,
                Reaction = baseResult.Reaction,
                SemanticKind = baseResult.SemanticKind,
                TriggerElement = baseResult.TriggerElement,
                TargetElement = baseResult.TargetElement,
                TriggerConsumeAmount = baseResult.TriggerConsumeAmount,
                TargetConsumeAmount = baseResult.TargetConsumeAmount,
                PrimaryDamage = damagePlan,
                ExtraDamages = additionalEffectDamages,
                AdditionalEffectDamages = additionalEffectDamages,
                DisplayText = baseResult.DisplayText,
                BonusKnockback = baseResult.BonusKnockback,
            };
        }

        private static List<DamageInstancePlan> BuildAdditionalEffectDamages(ReactionDamageContext context, ReactionRule rule)
        {
            List<DamageInstancePlan> damages = [];

            if (rule.Reaction == ReactionType.ThunderWind && context.SourcePlayer?.Advanced().PrototypeReactionExtraWindBurst == true)
            {
                damages.Add(new DamageInstancePlan
                {
                    Damage = PrototypeExtraWindDamage,
                    Element = ElementType.Wind,
                    Reaction = rule.Reaction,
                });
            }

            return damages;
        }

        private static float CalculateAmplificationMultiplier(ReactionDamageContext context, ReactionRule rule)
        {
            ReactionStatSnapshot snapshot = context.Payload.ContextSnapshot.ReactionStats;
            ReactionDamageModifierSet global = snapshot.GlobalModifiers;
            ReactionDamageModifierSet byReaction = GetReactionModifier(snapshot, rule.Reaction);

            float multiplier = Math.Max(1f, rule.BaseDamageFactor);
            multiplier *= CombineBonusBucket(global.ReactionTypeMult, byReaction.ReactionTypeMult);
            multiplier *= CombineBonusBucket(global.BasicMult, byReaction.BasicMult);
            multiplier *= CombineBonusBucket(global.ExtraMult, byReaction.ExtraMult);
            multiplier *= CombineBonusBucket(global.MasteryMult, byReaction.MasteryMult);
            multiplier *= ResolveMasteryMultiplier(snapshot.ElementMastery, isAmplification: true);
            multiplier *= Math.Max(0f, global.ElementVulnerabilityMult * byReaction.ElementVulnerabilityMult);
            multiplier *= Math.Max(0f, global.ReactionTypeVulnerabilityMult * byReaction.ReactionTypeVulnerabilityMult);
            multiplier *= Math.Max(0f, global.ResistanceMult * byReaction.ResistanceMult);
            return Math.Max(1f, multiplier);
        }

        private static DamageInstancePlan CalculateResolvedDamageInstance(ReactionDamageContext context, ReactionRule rule, bool usePrototypeDelta)
        {
            ReactionStatSnapshot snapshot = context.Payload.ContextSnapshot.ReactionStats;
            ReactionDamageModifierSet global = snapshot.GlobalModifiers;
            ReactionDamageModifierSet byElement = GetElementModifier(snapshot, rule.DamageElement);
            ReactionDamageModifierSet byReaction = GetReactionModifier(snapshot, rule.Reaction);
            float baseDamage = ResolveBaseReactionDamage(context, rule, usePrototypeDelta);
            float masteryMultiplier = ResolveMasteryMultiplier(snapshot.ElementMastery, isAmplification: false);
            float elementMultiplier = CombineBonusBucket(global.ElementMult, byElement.ElementMult, byReaction.ElementMult);
            float reactionTypeMultiplier = CombineBonusBucket(global.ReactionTypeMult, byElement.ReactionTypeMult, byReaction.ReactionTypeMult);
            float basicMultiplier = CombineBonusBucket(global.BasicMult, byElement.BasicMult, byReaction.BasicMult);
            float extraMultiplier = CombineBonusBucket(global.ExtraMult, byElement.ExtraMult, byReaction.ExtraMult);
            float masteryBucketMultiplier = CombineBonusBucket(global.MasteryMult, byElement.MasteryMult, byReaction.MasteryMult);
            float vulnerabilityMultiplier = Math.Max(0f, global.ElementVulnerabilityMult * byElement.ElementVulnerabilityMult * byReaction.ElementVulnerabilityMult);
            float reactionVulnerabilityMultiplier = Math.Max(0f, global.ReactionTypeVulnerabilityMult * byElement.ReactionTypeVulnerabilityMult * byReaction.ReactionTypeVulnerabilityMult);
            float resistanceMultiplier = Math.Max(0f, global.ResistanceMult * byElement.ResistanceMult * byReaction.ResistanceMult);

            float resolvedDamage = baseDamage *
                elementMultiplier *
                reactionTypeMultiplier *
                basicMultiplier *
                extraMultiplier *
                masteryBucketMultiplier *
                masteryMultiplier *
                vulnerabilityMultiplier *
                reactionVulnerabilityMultiplier *
                resistanceMultiplier;

            return new DamageInstancePlan
            {
                Damage = Math.Max(1, (int)Math.Round(resolvedDamage)),
                Element = rule.DamageElement,
                Reaction = rule.Reaction,
            };
        }

        private static float ResolveBaseReactionDamage(ReactionDamageContext context, ReactionRule rule, bool usePrototypeDelta)
        {
            float sourceDamage = Math.Max(1, context.Payload.BaseDamage);
            float triggerConsumed = Math.Max(0, context.SelectedReaction.TriggerConsumeAmount);
            float targetConsumed = Math.Max(0, context.SelectedReaction.TargetConsumeAmount);
            float totalConsumed = triggerConsumed + targetConsumed;
            float factor = usePrototypeDelta ? Math.Max(0f, rule.BaseDamageFactor - 1f) : Math.Max(0f, rule.BaseDamageFactor);

            return rule.DamageModel switch
            {
                ReactionDamageModel.FlatByRule => Math.Max(1f, factor),
                ReactionDamageModel.ScaleFromSourceDamage => Math.Max(1f, sourceDamage * factor),
                ReactionDamageModel.ScaleFromConsumedGauge => Math.Max(1f, totalConsumed * factor),
                ReactionDamageModel.Hybrid => Math.Max(1f, sourceDamage * factor + totalConsumed * factor),
                _ => Math.Max(1f, sourceDamage),
            };
        }

        private static ReactionDamageModifierSet GetElementModifier(ReactionStatSnapshot snapshot, ElementType element)
        {
            return snapshot.ByElement.TryGetValue(element, out ReactionDamageModifierSet modifiers)
                ? modifiers
                : new ReactionDamageModifierSet();
        }

        private static ReactionDamageModifierSet GetReactionModifier(ReactionStatSnapshot snapshot, ReactionType reactionType)
        {
            return snapshot.ByReactionType.TryGetValue(reactionType, out ReactionDamageModifierSet modifiers)
                ? modifiers
                : new ReactionDamageModifierSet();
        }

        private static float CombineBonusBucket(params float[] values)
        {
            float bonus = 0f;

            foreach (float value in values)
            {
                bonus += value - 1f;
            }

            return Math.Max(0f, 1f + bonus);
        }

        private static float ResolveMasteryMultiplier(int elementMastery, bool isAmplification)
        {
            if (elementMastery <= 0)
            {
                return 1f;
            }

            float multi = 6 * elementMastery / MasteryDivisor;
            return 1f + multi;
        }

    }
}

using System;
using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Definitions;
using AdvancedMod.DamageClasses.Element.Reactions;
using AdvancedMod.DamageClasses.Element.State;

namespace AdvancedMod.DamageClasses.Element.Services
{
    /// <summary>
    /// 校验反应候选是否满足当前临时元素状态要求。
    /// </summary>
    public static class ReactionValidator
    {
        /// <summary>
        /// 批量校验反应候选。
        /// </summary>
        public static IReadOnlyList<ReactionValidationResult> ValidateAll(IReadOnlyList<ReactionCandidate> candidates, TemporaryElementStateView temporaryStateView)
        {
            List<ReactionValidationResult> results = [];

            foreach (ReactionCandidate candidate in candidates)
            {
                results.Add(Validate(candidate, temporaryStateView));
            }

            return results;
        }

        private static ReactionValidationResult Validate(ReactionCandidate candidate, TemporaryElementStateView temporaryStateView)
        {
            ReactionRule rule = candidate.Rule;

            if (!TryResolveFixedConsume(rule, candidate.TriggerElement, out int triggerConsumeAmount) ||
                !TryResolveFixedConsume(rule, candidate.TargetElement, out int targetConsumeAmount))
            {
                return new ReactionValidationResult
                {
                    IsValid = false,
                    Rule = rule,
                    Candidate = candidate,
                };
            }

            bool valid = temporaryStateView.GetProjectedGauge(candidate.TriggerElement) >= triggerConsumeAmount &&
                temporaryStateView.GetProjectedGauge(candidate.TargetElement) >= targetConsumeAmount;

            return new ReactionValidationResult
            {
                IsValid = valid,
                Rule = rule,
                Candidate = candidate,
                TriggerConsumeAmount = triggerConsumeAmount,
                TargetConsumeAmount = targetConsumeAmount,
            };
        }

        private static bool TryResolveFixedConsume(ReactionRule rule, ElementType element, out int consumeAmount)
        {
            consumeAmount = 0;

            if (element == rule.TriggerElement)
            {
                if (rule.TriggerConsumeMode != ReactionConsumeMode.Fixed)
                {
                    return false;
                }

                consumeAmount = Math.Max(0, (int)Math.Round(rule.TriggerConsumeValue));
                return true;
            }

            if (element == rule.TargetElement)
            {
                if (rule.TargetConsumeMode != ReactionConsumeMode.Fixed)
                {
                    return false;
                }

                consumeAmount = Math.Max(0, (int)Math.Round(rule.TargetConsumeValue));
                return true;
            }

            return false;
        }
    }
}

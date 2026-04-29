using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Reactions;

namespace AdvancedMod.DamageClasses.Element.Services
{
    /// <summary>
    /// 从多个合法反应候选中选出最终主反应。
    /// </summary>
    public static class ReactionPriorityResolver
    {
        /// <summary>
        /// 解析并返回当前优先级最高的合法反应。
        /// </summary>
        public static ReactionValidationResult? Resolve(IReadOnlyList<ReactionValidationResult> validationResults)
        {
            ReactionValidationResult? best = null;

            foreach (ReactionValidationResult result in validationResults)
            {
                if (!result.IsValid)
                {
                    continue;
                }

                if (best is null)
                {
                    best = result;
                    continue;
                }

                if (result.Rule.Priority > best.Rule.Priority)
                {
                    best = result;
                    continue;
                }

                if (result.Rule.Priority == best.Rule.Priority &&
                    GetRegistryOrder(result.Rule) < GetRegistryOrder(best.Rule))
                {
                    best = result;
                }
            }

            return best;
        }

        private static int GetRegistryOrder(ReactionRule rule)
        {
            IReadOnlyList<ReactionRule> rules = ReactionRuleRegistry.Rules;

            for (int i = 0; i < rules.Count; i++)
            {
                if (ReferenceEquals(rules[i], rule))
                {
                    return i;
                }
            }

            return int.MaxValue;
        }
    }
}

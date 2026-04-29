using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Data;
using AdvancedMod.DamageClasses.Element.Reactions;
using AdvancedMod.DamageClasses.Element.State;

namespace AdvancedMod.DamageClasses.Element.Services
{
    /// <summary>
    /// 根据本次命中输入收集可能成立的反应候选。
    /// </summary>
    public static class ReactionCandidateCollector
    {
        /// <summary>
        /// 从命中输入中收集全部反应候选。
        /// </summary>
        public static IReadOnlyList<ReactionCandidate> Collect(AttackPayload payload, TemporaryElementStateView temporaryStateView)
        {
            _ = temporaryStateView;

            List<ReactionCandidate> candidates = [];

            foreach (ElementApplication application in payload.Applications)
            {
                foreach (ReactionRule rule in ReactionRuleRegistry.Rules)
                {
                    if (application.Element == rule.TriggerElement)
                    {
                        candidates.Add(new ReactionCandidate
                        {
                            Rule = rule,
                            TriggerElement = rule.TriggerElement,
                            TargetElement = rule.TargetElement,
                            SourceApplication = application,
                        });
                    }
                    else if (rule.IgnoreOrder && application.Element == rule.TargetElement)
                    {
                        candidates.Add(new ReactionCandidate
                        {
                            Rule = rule,
                            TriggerElement = rule.TargetElement,
                            TargetElement = rule.TriggerElement,
                            SourceApplication = application,
                        });
                    }
                }
            }

            return candidates;
        }
    }
}

using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Reactions
{
    /// <summary>
    /// 统一登记当前已启用的元素反应规则。
    /// </summary>
    public static class ReactionRuleRegistry
    {
        private static readonly IReadOnlyList<ReactionRule> RulesInternal =
        [
            new ReactionRule
            {
                Reaction = ReactionType.ThunderWind,
                TriggerElement = ElementType.Thunder,
                TargetElement = ElementType.Wind,
                TriggerConsumeMode = ReactionConsumeMode.Fixed,
                TriggerConsumeValue = 1f,
                TargetConsumeMode = ReactionConsumeMode.Fixed,
                TargetConsumeValue = 1f,
                DamageModel = ReactionDamageModel.ScaleFromSourceDamage,
                BaseDamageFactor = 1.5f,
                DamageElement = ElementType.Thunder,
                Priority = 100,
                IgnoreOrder = true,
            },
            new ReactionRule
            {
                Reaction = ReactionType.Overloaded,
                TriggerElement = ElementType.Pyro,
                TargetElement = ElementType.Thunder,
                TriggerConsumeMode = ReactionConsumeMode.Fixed,
                TriggerConsumeValue = 4f,
                TargetConsumeMode = ReactionConsumeMode.Fixed,
                TargetConsumeValue = 2f,
                DamageModel = ReactionDamageModel.ScaleFromSourceDamage,
                BaseDamageFactor = 2.75f,
                DamageElement = ElementType.Thunder,
                Priority = 200,
                IgnoreOrder = true,
            }
        ];

        /// <summary>
        /// 获取当前所有反应规则。
        /// </summary>
        public static IReadOnlyList<ReactionRule> Rules => RulesInternal;
    }
}

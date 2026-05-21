using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Reactions
{
    /// <summary>
    /// 统一登记当前已启用的元素反应规则。
    /// </summary>
    public static class ReactionRuleRegistry
    {
        private static readonly IReadOnlyList<ReactionRule> EnabledRulesInternal =
        [
            new ReactionRule
            {
                Reaction = ReactionType.ThunderWind,
                SemanticKind = ReactionSemanticKind.Prototype,
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
                SemanticKind = ReactionSemanticKind.Transformative,
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

        private static readonly IReadOnlyList<ReactionRule> PlaceholderRulesInternal =
        [
            new ReactionRule
            {
                Reaction = ReactionType.ElectroCharged,
                SemanticKind = ReactionSemanticKind.Transformative,
                TriggerElement = ElementType.None,
                TargetElement = ElementType.None,
                TriggerConsumeMode = ReactionConsumeMode.Fixed,
                TriggerConsumeValue = 0f,
                TargetConsumeMode = ReactionConsumeMode.Fixed,
                TargetConsumeValue = 0f,
                DamageModel = ReactionDamageModel.ScaleFromSourceDamage,
                BaseDamageFactor = 1.2f,
                DamageElement = ElementType.None,
                Priority = 0,
                IgnoreOrder = true,
            },
            new ReactionRule
            {
                Reaction = ReactionType.Vaporize,
                SemanticKind = ReactionSemanticKind.Amplification,
                TriggerElement = ElementType.None,
                TargetElement = ElementType.None,
                TriggerConsumeMode = ReactionConsumeMode.Fixed,
                TriggerConsumeValue = 0f,
                TargetConsumeMode = ReactionConsumeMode.Fixed,
                TargetConsumeValue = 0f,
                DamageModel = ReactionDamageModel.ScaleFromSourceDamage,
                BaseDamageFactor = 2f,
                DamageElement = ElementType.None,
                Priority = 0,
                IgnoreOrder = true,
            }
        ];

        /// <summary>
        /// 获取当前已经正式启用的反应规则。
        /// </summary>
        public static IReadOnlyList<ReactionRule> Rules => EnabledRulesInternal;

        /// <summary>
        /// 获取尚未接入运行时、仅用于结构占位的未来反应规则。
        /// </summary>
        public static IReadOnlyList<ReactionRule> PlaceholderRules => PlaceholderRulesInternal;
    }
}

using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Reactions
{
    /// <summary>
    /// 定义一条可被元素系统评估的反应规则。
    /// </summary>
    public sealed class ReactionRule
    {
        /// <summary>
        /// 该规则对应的反应类型。
        /// </summary>
        public ReactionType Reaction { get; init; } = ReactionType.None;

        /// <summary>
        /// 该反应在数值层面的语义分类。
        /// </summary>
        public ReactionSemanticKind SemanticKind { get; init; } = ReactionSemanticKind.None;

        /// <summary>
        /// 触发侧元素。
        /// </summary>
        public ElementType TriggerElement { get; init; } = ElementType.None;

        /// <summary>
        /// 目标侧元素。
        /// </summary>
        public ElementType TargetElement { get; init; } = ElementType.None;

        /// <summary>
        /// 触发侧元素的消耗模式。
        /// </summary>
        public ReactionConsumeMode TriggerConsumeMode { get; init; } = ReactionConsumeMode.Fixed;

        /// <summary>
        /// 触发侧元素的消耗值。
        /// </summary>
        public float TriggerConsumeValue { get; init; } = 1f;

        /// <summary>
        /// 目标侧元素的消耗模式。
        /// </summary>
        public ReactionConsumeMode TargetConsumeMode { get; init; } = ReactionConsumeMode.Fixed;

        /// <summary>
        /// 目标侧元素的消耗值。
        /// </summary>
        public float TargetConsumeValue { get; init; } = 1f;

        /// <summary>
        /// 该反应使用的伤害模型。
        /// </summary>
        public ReactionDamageModel DamageModel { get; init; } = ReactionDamageModel.ScaleFromSourceDamage;

        /// <summary>
        /// 该反应使用的基础倍率配置值。
        /// 其在运行时的具体含义由反应语义与伤害模型共同解释。
        /// </summary>
        public float BaseDamageFactor { get; init; } = 1f;

        /// <summary>
        /// 反应伤害使用的输出元素类型。
        /// </summary>
        public ElementType DamageElement { get; init; } = ElementType.None;

        /// <summary>
        /// 反应优先级，值越大越优先。
        /// </summary>
        public int Priority { get; init; }

        /// <summary>
        /// 是否忽略元素前后顺序。
        /// </summary>
        public bool IgnoreOrder { get; init; }
    }
}

#nullable enable
using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Reactions
{
    /// <summary>
    /// 表示正式反应主链生成的一次反应执行结果。
    /// </summary>
    public sealed class ReactionResult
    {
        /// <summary>
        /// 本次命中是否成功触发反应。
        /// </summary>
        public bool Triggered { get; init; }

        /// <summary>
        /// 触发的反应类型。
        /// </summary>
        public ReactionType Reaction { get; init; } = ReactionType.None;

        /// <summary>
        /// 本次反应在数值层面的语义分类。
        /// </summary>
        public ReactionSemanticKind SemanticKind { get; init; } = ReactionSemanticKind.None;

        /// <summary>
        /// 本次反应的触发侧元素。
        /// </summary>
        public ElementType TriggerElement { get; init; } = ElementType.None;

        /// <summary>
        /// 本次反应的目标侧元素。
        /// </summary>
        public ElementType TargetElement { get; init; } = ElementType.None;

        /// <summary>
        /// 触发侧元素的实际消耗量。
        /// </summary>
        public int TriggerConsumeAmount { get; init; }

        /// <summary>
        /// 目标侧元素的实际消耗量。
        /// </summary>
        public int TargetConsumeAmount { get; init; }

        /// <summary>
        /// 对原始命中生效的增幅倍率。
        /// 增幅反应的本体应落在这里，而不是伪装成额外伤害段。
        /// </summary>
        public float AmplificationMultiplier { get; init; } = 1f;

        /// <summary>
        /// 过渡兼容字段。旧链路仍可能把主反应伤害写在这里。
        /// </summary>
        public DamageInstancePlan? PrimaryDamage { get; init; }

        /// <summary>
        /// 剧变反应本体产生的主伤害段。
        /// </summary>
        public DamageInstancePlan? TransformativeDamage { get; init; }

        /// <summary>
        /// 不属于反应本体的额外追加伤害段。
        /// </summary>
        public IReadOnlyList<DamageInstancePlan> AdditionalEffectDamages { get; init; } = [];

        /// <summary>
        /// 过渡兼容字段。旧链路仍可能读取这里的额外伤害段。
        /// </summary>
        public IReadOnlyList<DamageInstancePlan> ExtraDamages { get; init; } = [];

        /// <summary>
        /// 反应显示文本。
        /// </summary>
        public string DisplayText { get; init; } = string.Empty;

        /// <summary>
        /// 超载等反应使用的附加击退强度。
        /// </summary>
        public float BonusKnockback { get; init; }
    }
}

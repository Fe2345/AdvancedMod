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
        /// 反应主伤害计划。
        /// </summary>
        public DamageInstancePlan? PrimaryDamage { get; init; }

        /// <summary>
        /// 额外伤害计划。
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

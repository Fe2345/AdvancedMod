using AdvancedMod.DamageClasses.Element.Data;
using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Reactions
{
    /// <summary>
    /// 表示一次命中中可能成立的反应候选。
    /// </summary>
    public sealed class ReactionCandidate
    {
        /// <summary>
        /// 对应的反应规则。
        /// </summary>
        public ReactionRule Rule { get; init; } = new();

        /// <summary>
        /// 触发侧元素。
        /// </summary>
        public ElementType TriggerElement { get; init; } = ElementType.None;

        /// <summary>
        /// 目标侧元素。
        /// </summary>
        public ElementType TargetElement { get; init; } = ElementType.None;

        /// <summary>
        /// 触发该候选的输入条目。
        /// </summary>
        public ElementApplication? SourceApplication { get; init; }
    }
}

using AdvancedMod.DamageClasses.Element.Data;
using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Plans
{
    /// <summary>
    /// 描述一次命中准备施加的单条元素附着计划。
    /// </summary>
    public sealed class ElementApplyEntry
    {
        /// <summary>
        /// 要施加的元素类型。
        /// </summary>
        public ElementType Element { get; init; } = ElementType.None;

        /// <summary>
        /// 本次计划施加的附着量。
        /// </summary>
        public float AppliedBuildUp { get; init; }

        /// <summary>
        /// 对应的原始元素输入条目。
        /// </summary>
        public ElementApplication? SourceApplication { get; init; }
    }
}

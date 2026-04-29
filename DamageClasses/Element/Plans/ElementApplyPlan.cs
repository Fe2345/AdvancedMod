using System.Collections.Generic;

namespace AdvancedMod.DamageClasses.Element.Plans
{
    /// <summary>
    /// 描述一次命中的全部元素附着计划。
    /// </summary>
    public sealed class ElementApplyPlan
    {
        /// <summary>
        /// 本次命中准备施加的全部元素条目。
        /// </summary>
        public IReadOnlyList<ElementApplyEntry> Entries { get; init; } = [];
    }
}

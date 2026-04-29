using System;
using System.Linq;
using AdvancedMod.DamageClasses.Element.Definitions;
using AdvancedMod.DamageClasses.Element.Plans;

namespace AdvancedMod.DamageClasses.Element.State
{
    /// <summary>
    /// 基于真实状态与本次附着计划构建的临时元素视图。
    /// </summary>
    public sealed class TemporaryElementStateView
    {
        private readonly ElementComponent _component;
        private readonly ElementApplyPlan _applyPlan;

        /// <summary>
        /// 创建一个新的临时元素视图。
        /// </summary>
        public TemporaryElementStateView(ElementComponent component, ElementApplyPlan applyPlan)
        {
            _component = component ?? throw new ArgumentNullException(nameof(component));
            _applyPlan = applyPlan ?? throw new ArgumentNullException(nameof(applyPlan));
        }

        /// <summary>
        /// 获取目标当前真实元素量。
        /// </summary>
        public int GetCurrentGauge(ElementType element)
        {
            return GetState(element)?.CurrentGauge ?? 0;
        }

        /// <summary>
        /// 获取在本次附着计划生效后的投影元素量。
        /// </summary>
        public int GetProjectedGauge(ElementType element)
        {
            float plannedBuildUp = _applyPlan.Entries
                .Where(entry => entry.Element == element)
                .Sum(entry => entry.AppliedBuildUp);

            return Math.Max(0, GetCurrentGauge(element) + (int)Math.Round(plannedBuildUp));
        }

        private ElementState? GetState(ElementType element)
        {
            return element switch
            {
                ElementType.Thunder => _component.Thunder,
                ElementType.Wind => _component.Wind,
                _ => null,
            };
        }
    }
}

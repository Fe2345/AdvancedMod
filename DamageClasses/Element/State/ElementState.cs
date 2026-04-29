using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.State
{
    /// <summary>
    /// Stores the current runtime gauge for a single element on one target.
    /// </summary>
    public sealed class ElementState
    {
        /// <summary>
        /// Gets or sets the element represented by this state slot.
        /// </summary>
        public ElementType Element { get; set; } = ElementType.None;

        /// <summary>
        /// Gets or sets the current stored gauge for this element.
        /// </summary>
        public int CurrentGauge { get; set; }
    }
}

using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Reactions
{
    /// <summary>
    /// Represents one direct damage instance emitted by a prototype reaction result.
    /// </summary>
    public sealed class DamageInstancePlan
    {
        /// <summary>
        /// Gets the integer damage dealt by this instance.
        /// </summary>
        public int Damage { get; init; }

        /// <summary>
        /// Gets the element type associated with this damage instance.
        /// </summary>
        public ElementType Element { get; init; } = ElementType.None;
    }
}

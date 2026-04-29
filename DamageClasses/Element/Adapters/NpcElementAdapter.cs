using AdvancedMod.DamageClasses.Element.State;

namespace AdvancedMod.DamageClasses.Element.Adapters
{
    /// <summary>
    /// Owns the NPC-side element state for the thunder-wind prototype and centralizes future adapter logic.
    /// </summary>
    public sealed class NpcElementAdapter
    {
        /// <summary>
        /// Gets the element state container owned by the adapter.
        /// </summary>
        public ElementComponent Component { get; } = new();
    }
}

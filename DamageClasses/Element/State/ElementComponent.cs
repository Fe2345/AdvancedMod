namespace AdvancedMod.DamageClasses.Element.State
{
    /// <summary>
    /// Minimal NPC-side container for the thunder-wind prototype state.
    /// </summary>
    public sealed class ElementComponent
    {
        /// <summary>
        /// Gets the current thunder element state.
        /// </summary>
        public ElementState Thunder { get; } = new() { Element = Definitions.ElementType.Thunder };

        /// <summary>
        /// Gets the current wind element state.
        /// </summary>
        public ElementState Wind { get; } = new() { Element = Definitions.ElementType.Wind };

        /// <summary>
        /// Gets or sets the latest reaction text that should be shown over the NPC.
        /// </summary>
        public string LastReactionText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the game tick when the latest reaction text was recorded.
        /// </summary>
        public int LastReactionTick { get; set; }
    }
}

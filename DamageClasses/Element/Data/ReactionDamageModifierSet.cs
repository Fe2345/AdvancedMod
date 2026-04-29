namespace AdvancedMod.DamageClasses.Element.Data
{
    /// <summary>
    /// Stores one full bucketized modifier set for reaction damage calculation.
    /// </summary>
    public sealed class ReactionDamageModifierSet
    {
        /// <summary>
        /// Gets the target-side vulnerability multiplier for the reaction's output element.
        /// </summary>
        public float ElementVulnerabilityMult { get; init; } = 1f;

        /// <summary>
        /// Gets the target-side vulnerability multiplier for the reaction type.
        /// </summary>
        public float ReactionTypeVulnerabilityMult { get; init; } = 1f;

        /// <summary>
        /// Gets the resolved resistance multiplier after any prototype resistance math.
        /// </summary>
        public float ResistanceMult { get; init; } = 1f;

        /// <summary>
        /// Gets the element-specific damage multiplier bucket.
        /// </summary>
        public float ElementMult { get; init; } = 1f;

        /// <summary>
        /// Gets the reaction-type damage multiplier bucket.
        /// </summary>
        public float ReactionTypeMult { get; init; } = 1f;

        /// <summary>
        /// Gets the base reaction multiplier bucket.
        /// </summary>
        public float BasicMult { get; init; } = 1f;

        /// <summary>
        /// Gets the mastery multiplier bucket.
        /// </summary>
        public float MasteryMult { get; init; } = 1f;

        /// <summary>
        /// Gets the reserve multiplier bucket for unusual one-off effects.
        /// </summary>
        public float ExtraMult { get; init; } = 1f;
    }
}

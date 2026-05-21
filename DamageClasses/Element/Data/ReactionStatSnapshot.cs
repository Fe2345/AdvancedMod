using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Data
{
    /// <summary>
    /// Stores the compact reaction-side numeric snapshot captured from the player context.
    /// </summary>
    public sealed class ReactionStatSnapshot
    {
        /// <summary>
        /// Gets the global reaction modifier buckets applied to all element reactions.
        /// </summary>
        public ReactionDamageModifierSet GlobalModifiers { get; init; } = new();

        /// <summary>
        /// Gets per-element reaction modifier buckets.
        /// </summary>
        public IReadOnlyDictionary<ElementType, ReactionDamageModifierSet> ByElement { get; init; } =
            new Dictionary<ElementType, ReactionDamageModifierSet>();

        /// <summary>
        /// Gets per-reaction-type modifier buckets.
        /// </summary>
        public IReadOnlyDictionary<ReactionType, ReactionDamageModifierSet> ByReactionType { get; init; } =
            new Dictionary<ReactionType, ReactionDamageModifierSet>();

        /// <summary>
        /// Gets additive buildup modifiers keyed by element.
        /// </summary>
        public IReadOnlyDictionary<ElementType, float> BuildUpAdd { get; init; } =
            new Dictionary<ElementType, float>();

        /// <summary>
        /// Gets multiplicative buildup modifiers keyed by element.
        /// </summary>
        public IReadOnlyDictionary<ElementType, float> BuildUpMult { get; init; } =
            new Dictionary<ElementType, float>();

        /// <summary>
        /// Gets consume multipliers keyed by reaction type.
        /// </summary>
        public IReadOnlyDictionary<ReactionType, float> ConsumeMult { get; init; } =
            new Dictionary<ReactionType, float>();

        /// <summary>
        /// Gets the captured element mastery value for the current hit context.
        /// </summary>
        public int ElementMastery { get; init; }

    }
}

using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Data
{
    /// <summary>
    /// Stores the player-side combat interpretation parameters captured for one hit.
    /// </summary>
    public sealed class CombatContextSnapshot
    {
        /// <summary>
        /// Gets per-element buildup multipliers.
        /// </summary>
        public IReadOnlyDictionary<ElementType, float> ElementBuildUpMultiplier { get; init; } =
            new Dictionary<ElementType, float>();

        /// <summary>
        /// Gets per-element DOT multipliers.
        /// </summary>
        public IReadOnlyDictionary<ElementType, float> ElementDotMultiplier { get; init; } =
            new Dictionary<ElementType, float>();

        /// <summary>
        /// Gets per-reaction threshold modifiers.
        /// </summary>
        public IReadOnlyDictionary<ReactionType, float> ReactionThresholdModifier { get; init; } =
            new Dictionary<ReactionType, float>();

        /// <summary>
        /// Gets per-reaction priority bias values.
        /// </summary>
        public IReadOnlyDictionary<ReactionType, float> ReactionPriorityBias { get; init; } =
            new Dictionary<ReactionType, float>();

        /// <summary>
        /// Gets any reaction rule override keys applied by equipment or other effects.
        /// </summary>
        public IReadOnlyDictionary<ReactionType, string> ReactionRuleOverrides { get; init; } =
            new Dictionary<ReactionType, string>();

        /// <summary>
        /// Gets permission tags that explicitly allow special behavior.
        /// </summary>
        public IReadOnlySet<string> SpecialPermissions { get; init; } = new HashSet<string>();

        /// <summary>
        /// Gets restriction tags that explicitly forbid special behavior.
        /// </summary>
        public IReadOnlySet<string> SpecialRestrictions { get; init; } = new HashSet<string>();

        /// <summary>
        /// Gets general context flags captured for the hit.
        /// </summary>
        public IReadOnlySet<string> ContextFlags { get; init; } = new HashSet<string>();

        /// <summary>
        /// Gets the compact reaction damage and consume modifier snapshot.
        /// </summary>
        public ReactionStatSnapshot ReactionStats { get; init; } = new();
    }
}

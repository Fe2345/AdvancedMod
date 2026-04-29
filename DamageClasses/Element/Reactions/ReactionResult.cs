#nullable enable
using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Reactions
{
    /// <summary>
    /// Stores the minimal reaction output emitted by the thunder-wind prototype combat chain.
    /// </summary>
    public sealed class ReactionResult
    {
        /// <summary>
        /// Gets whether a reaction was actually triggered for the current hit.
        /// </summary>
        public bool Triggered { get; init; }

        /// <summary>
        /// Gets the triggered reaction type.
        /// </summary>
        public ReactionType Reaction { get; init; } = ReactionType.None;

        /// <summary>
        /// Gets the base reaction damage instance.
        /// </summary>
        public DamageInstancePlan? PrimaryDamage { get; init; }

        /// <summary>
        /// Gets any extra damage instances appended by prototype reaction modifiers.
        /// </summary>
        public IReadOnlyList<DamageInstancePlan> ExtraDamages { get; init; } = [];

        /// <summary>
        /// Gets the short reaction text that should be shown over the NPC.
        /// </summary>
        public string DisplayText { get; init; } = string.Empty;
    }
}

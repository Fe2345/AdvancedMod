using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Data;
using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Input
{
    /// <summary>
    /// Represents the finalized result of the prototype input reinterpretation stage.
    /// </summary>
    public sealed class InputAugmentPlan
    {
        /// <summary>
        /// Gets the finalized element seeds after input modifiers ran.
        /// </summary>
        public required IReadOnlyList<AttackElementSeed> ElementSeeds { get; init; }

        /// <summary>
        /// Gets whether element application is allowed for the current hit.
        /// </summary>
        public bool CanApplyElement { get; init; } = true;

        /// <summary>
        /// Gets whether reactions are allowed for the current hit.
        /// </summary>
        public bool CanTriggerReaction { get; init; } = true;

        /// <summary>
        /// Gets whether DOT side effects are allowed for the current hit.
        /// </summary>
        public bool CanApplyDot { get; init; } = true;

        /// <summary>
        /// Gets the finalized flags after input reinterpretation.
        /// </summary>
        public AttackFlags Flags { get; init; } = AttackFlags.None;

        /// <summary>
        /// Gets the finalized tag set after input reinterpretation.
        /// </summary>
        public required IReadOnlySet<string> Tags { get; init; }
    }
}

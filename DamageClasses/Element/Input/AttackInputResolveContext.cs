#nullable enable
using AdvancedMod.DamageClasses.Element.Data;
using Terraria;

namespace AdvancedMod.DamageClasses.Element.Input
{
    /// <summary>
    /// Provides the minimal read-only context required to reinterpret one attack intent before payload creation.
    /// </summary>
    public sealed class AttackInputResolveContext
    {
        /// <summary>
        /// Gets the player that owns the hit, if any.
        /// </summary>
        public Player? SourcePlayer { get; init; }

        /// <summary>
        /// Gets the target NPC, if already known when resolving the input.
        /// </summary>
        public NPC? Target { get; init; }

        /// <summary>
        /// Gets the raw hit intent to reinterpret.
        /// </summary>
        public required AttackSourceIntent Intent { get; init; }
    }
}

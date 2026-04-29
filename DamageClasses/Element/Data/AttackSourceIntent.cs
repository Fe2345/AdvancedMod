#nullable enable
using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Definitions;
using Terraria.ModLoader;

namespace AdvancedMod.DamageClasses.Element.Data
{
    /// <summary>
    /// Represents the raw hit intent produced by a Terraria attack source before the element pipeline interprets it.
    /// </summary>
    public sealed class AttackSourceIntent
    {
        /// <summary>
        /// Gets the owner player id of the source, or -1 when there is no player owner.
        /// </summary>
        public int SourcePlayerId { get; init; } = -1;

        /// <summary>
        /// Gets the entity id of the source that emitted the hit.
        /// </summary>
        public int SourceEntityId { get; init; } = -1;

        /// <summary>
        /// Gets the source category for this hit.
        /// </summary>
        public AttackSourceKind SourceKind { get; init; } = AttackSourceKind.None;

        /// <summary>
        /// Gets the Terraria damage class associated with the source hit.
        /// </summary>
        public DamageClass? DamageClass { get; init; }

        /// <summary>
        /// Gets the base damage associated with the source hit before element reactions reinterpret it.
        /// </summary>
        public int BaseDamage { get; init; }

        /// <summary>
        /// Gets the knockback attached to the source hit.
        /// </summary>
        public float Knockback { get; init; }

        /// <summary>
        /// Gets whether this hit is allowed to apply element gauge.
        /// </summary>
        public bool CanApplyElement { get; init; } = true;

        /// <summary>
        /// Gets whether this hit is allowed to trigger reactions.
        /// </summary>
        public bool CanTriggerReaction { get; init; } = true;

        /// <summary>
        /// Gets whether this hit is allowed to apply DOT side effects.
        /// </summary>
        public bool CanApplyDot { get; init; } = true;

        /// <summary>
        /// Gets general attack flags attached to the hit.
        /// </summary>
        public AttackFlags Flags { get; init; } = AttackFlags.None;

        /// <summary>
        /// Gets the raw element seeds carried by this hit.
        /// </summary>
        public IReadOnlyList<AttackElementSeed> ElementSeeds { get; init; } = [];

        /// <summary>
        /// Gets the high-level tags attached to this hit intent.
        /// </summary>
        public IReadOnlyList<string> Tags { get; init; } = [];
    }
}

#nullable enable
using System;
using System.Collections.Generic;
using AdvancedMod.DamageClasses.Element.Definitions;
using Terraria.ModLoader;
using Terraria;

namespace AdvancedMod.DamageClasses.Element.Data
{
    /// <summary>
    /// Represents the normalized input consumed by the element combat pipeline for one hit.
    /// </summary>
    public sealed class AttackPayload
    {
        /// <summary>
        /// Gets the trace id used to connect debugging and event records for this hit.
        /// </summary>
        public Guid TraceId { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Gets the owner player id of the source, or -1 when not player-owned.
        /// </summary>
        public int SourcePlayerId { get; init; } = -1;

        /// <summary>
        /// Gets the entity id of the source that emitted the hit.
        /// </summary>
        public int SourceEntityId { get; init; } = -1;

        /// <summary>
        /// Gets the source category for this payload.
        /// </summary>
        public AttackSourceKind SourceKind { get; init; } = AttackSourceKind.None;

        /// <summary>
        /// Gets the Terraria damage class associated with the original hit.
        /// </summary>
        public DamageClass? DamageClass { get; init; }

        /// <summary>
        /// Gets the base source-hit damage used by prototype reaction math.
        /// </summary>
        public int BaseDamage { get; init; }

        /// <summary>
        /// Gets whether element gauge may be applied from this payload.
        /// </summary>
        public bool CanApplyElement { get; init; } = true;

        /// <summary>
        /// Gets whether reactions may be triggered from this payload.
        /// </summary>
        public bool CanTriggerReaction { get; init; } = true;

        /// <summary>
        /// Gets whether DOT side effects are allowed for this payload.
        /// </summary>
        public bool CanApplyDot { get; init; } = true;

        /// <summary>
        /// Gets general attack flags associated with the payload.
        /// </summary>
        public AttackFlags Flags { get; init; } = AttackFlags.None;

        /// <summary>
        /// Gets the finalized element application entries contained in the payload.
        /// </summary>
        public IReadOnlyList<ElementApplication> Applications { get; init; } = [];

        /// <summary>
        /// Gets the normalized tags attached to this payload.
        /// </summary>
        public IReadOnlySet<string> Tags { get; init; } = new HashSet<string>();

        /// <summary>
        /// Gets the player-side combat context snapshot captured for the hit.
        /// </summary>
        public CombatContextSnapshot ContextSnapshot { get; init; } = new();
    }
}

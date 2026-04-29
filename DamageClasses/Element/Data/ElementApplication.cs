#nullable enable
using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Data
{
    /// <summary>
    /// Represents one finalized element application entry inside a normalized attack payload.
    /// </summary>
    public sealed class ElementApplication
    {
        /// <summary>
        /// Gets the element type applied by this entry.
        /// </summary>
        public ElementType Element { get; init; } = ElementType.None;

        /// <summary>
        /// Gets the finalized buildup value contributed by this element entry.
        /// </summary>
        public float BuildUp { get; init; }

        /// <summary>
        /// Gets the finalized DOT strength contributed by this element entry.
        /// </summary>
        public float DotStrength { get; init; }

        /// <summary>
        /// Gets the cooldown group used for repeated application throttling.
        /// </summary>
        public int ApplyCooldownGroup { get; init; }

        /// <summary>
        /// Gets the cooldown duration in ticks for the application group.
        /// </summary>
        public int ApplyCooldownTicks { get; init; }

        /// <summary>
        /// Gets additional behavior flags attached to the application.
        /// </summary>
        public ElementApplicationFlags ApplicationFlags { get; init; } = ElementApplicationFlags.None;

        /// <summary>
        /// Gets an optional reason string used for debugging where this application came from.
        /// </summary>
        public string? SourceReason { get; init; }
    }
}

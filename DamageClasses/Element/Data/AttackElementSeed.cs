using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Data
{
    /// <summary>
    /// Describes one raw element seed emitted by a weapon or projectile before the input pipeline modifies it.
    /// </summary>
    public sealed class AttackElementSeed
    {
        /// <summary>
        /// Gets the element type carried by this seed.
        /// </summary>
        public ElementType Element { get; init; } = ElementType.None;

        /// <summary>
        /// Gets the raw buildup contributed by this seed before runtime modifiers are applied.
        /// </summary>
        public float BaseBuildUp { get; init; }

        /// <summary>
        /// Gets the raw DOT strength contributed by this seed before runtime modifiers are applied.
        /// </summary>
        public float BaseDotStrength { get; init; }

        /// <summary>
        /// Gets the cooldown group used to throttle repeated applications from the same source.
        /// </summary>
        public int CooldownGroup { get; init; }

        /// <summary>
        /// Gets the cooldown duration, in ticks, for the application group.
        /// </summary>
        public int CooldownTicks { get; init; }

        /// <summary>
        /// Gets extra behavior flags attached to this element seed.
        /// </summary>
        public ElementApplicationFlags Flags { get; init; } = ElementApplicationFlags.None;
    }
}

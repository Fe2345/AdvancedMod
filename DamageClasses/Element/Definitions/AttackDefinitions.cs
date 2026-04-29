using System;

namespace AdvancedMod.DamageClasses.Element.Definitions
{
    /// <summary>
    /// Defines the concrete source category that produced an element-system hit.
    /// </summary>
    public enum AttackSourceKind
    {
        /// <summary>
        /// Uninitialized source kind.
        /// </summary>
        None = 0,

        /// <summary>
        /// A direct item hit, usually melee.
        /// </summary>
        Item = 1,

        /// <summary>
        /// A projectile-owned hit.
        /// </summary>
        Projectile = 2,

        /// <summary>
        /// An NPC-originated hit.
        /// </summary>
        NPC = 3,

        /// <summary>
        /// A world or environment-originated hit.
        /// </summary>
        Environment = 4,

        /// <summary>
        /// A hit produced by another combat result.
        /// </summary>
        DerivedEffect = 5,
    }

    /// <summary>
    /// Flags that describe general attack-side behavior before the hit enters the element pipeline.
    /// </summary>
    [Flags]
    public enum AttackFlags
    {
        /// <summary>
        /// No special behavior flags are present.
        /// </summary>
        None = 0,
    }
}

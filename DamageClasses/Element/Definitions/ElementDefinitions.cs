using System;

namespace AdvancedMod.DamageClasses.Element.Definitions
{
    /// <summary>
    /// All element types currently supported by the prototype element combat system.
    /// </summary>
    public enum ElementType
    {
        /// <summary>
        /// No element.
        /// </summary>
        None = 0,

        /// <summary>
        /// Prototype thunder element.
        /// </summary>
        Thunder = 1,

        /// <summary>
        /// Prototype wind element.
        /// </summary>
        Wind = 2,
    }

    /// <summary>
    /// Flags that describe element-side application behavior on a single incoming element payload entry.
    /// </summary>
    [Flags]
    public enum ElementApplicationFlags
    {
        /// <summary>
        /// No special application flags are present.
        /// </summary>
        None = 0,
    }
}

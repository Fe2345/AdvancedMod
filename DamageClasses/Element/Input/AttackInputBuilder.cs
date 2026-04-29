using System.Collections.Generic;
using System.Linq;
using AdvancedMod.DamageClasses.Element.Data;
using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Input
{
    /// <summary>
    /// Holds a mutable copy of one attack intent while prototype input modifiers rewrite it.
    /// </summary>
    public sealed class AttackInputBuilder
    {
        /// <summary>
        /// Initializes a new builder from a raw source intent.
        /// </summary>
        public AttackInputBuilder(AttackSourceIntent intent)
        {
            BaseDamage = intent.BaseDamage;
            CanApplyElement = intent.CanApplyElement;
            CanTriggerReaction = intent.CanTriggerReaction;
            CanApplyDot = intent.CanApplyDot;
            Flags = intent.Flags;
            ElementSeeds = intent.ElementSeeds
                .Select(seed => new AttackElementSeed
                {
                    Element = seed.Element,
                    BaseBuildUp = seed.BaseBuildUp,
                    BaseDotStrength = seed.BaseDotStrength,
                    CooldownGroup = seed.CooldownGroup,
                    CooldownTicks = seed.CooldownTicks,
                    Flags = seed.Flags,
                })
                .ToList();
            Tags = intent.Tags.ToList();
        }

        /// <summary>
        /// Gets the current base damage.
        /// </summary>
        public int BaseDamage { get; private set; }

        /// <summary>
        /// Gets whether element application is still allowed.
        /// </summary>
        public bool CanApplyElement { get; private set; }

        /// <summary>
        /// Gets whether reactions are still allowed.
        /// </summary>
        public bool CanTriggerReaction { get; private set; }

        /// <summary>
        /// Gets whether DOT side effects are still allowed.
        /// </summary>
        public bool CanApplyDot { get; private set; }

        /// <summary>
        /// Gets the current attack flags.
        /// </summary>
        public AttackFlags Flags { get; private set; }

        /// <summary>
        /// Gets the mutable element seed collection.
        /// </summary>
        public List<AttackElementSeed> ElementSeeds { get; }

        /// <summary>
        /// Gets the mutable tag collection.
        /// </summary>
        public List<string> Tags { get; }

        /// <summary>
        /// Replaces all element seeds with the specified element type while preserving the rest of the seed values.
        /// </summary>
        public void ReplaceAllElements(ElementType element)
        {
            for (int i = 0; i < ElementSeeds.Count; i++)
            {
                AttackElementSeed seed = ElementSeeds[i];
                ElementSeeds[i] = new AttackElementSeed
                {
                    Element = element,
                    BaseBuildUp = seed.BaseBuildUp,
                    BaseDotStrength = seed.BaseDotStrength,
                    CooldownGroup = seed.CooldownGroup,
                    CooldownTicks = seed.CooldownTicks,
                    Flags = seed.Flags,
                };
            }
        }
    }
}

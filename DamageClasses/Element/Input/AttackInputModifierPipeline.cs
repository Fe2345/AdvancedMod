#nullable enable
using System.Collections.Generic;
using System.Linq;
using AdvancedMod.DamageClasses.Element.Data;
using AdvancedMod.DamageClasses.Element.Definitions;
using AdvancedMod.Utils;

namespace AdvancedMod.DamageClasses.Element.Input
{
    /// <summary>
    /// Runs the minimal prototype input reinterpretation logic before payload construction.
    /// </summary>
    public static class AttackInputModifierPipeline
    {
        /// <summary>
        /// Resolves a normalized augment plan for the current hit.
        /// </summary>
        public static InputAugmentPlan Resolve(AttackInputResolveContext context)
        {
            AttackInputBuilder builder = new(context.Intent);

            if (context.SourcePlayer?.Advanced().PrototypeForceWindElement == true)
            {
                builder.ReplaceAllElements(ElementType.Wind);
            }

            return new InputAugmentPlan
            {
                ElementSeeds = builder.ElementSeeds.ToArray(),
                CanApplyElement = builder.CanApplyElement,
                CanTriggerReaction = builder.CanTriggerReaction,
                CanApplyDot = builder.CanApplyDot,
                Flags = builder.Flags,
                Tags = new HashSet<string>(builder.Tags),
            };
        }
    }
}

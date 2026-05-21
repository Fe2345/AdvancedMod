using System.Collections.Generic;
using System.Linq;
using AdvancedMod.DamageClasses.Element.Data;
using Terraria.ModLoader;

namespace AdvancedMod.DamageClasses.Element.Input
{
    /// <summary>
    /// Builds the minimal normalized payload consumed by the thunder-wind prototype combat system.
    /// </summary>
    public static class PayloadBuilder
    {
        /// <summary>
        /// Creates a payload from a source intent and an already-resolved input augment plan.
        /// </summary>
        public static AttackPayload Build(AttackSourceIntent intent, InputAugmentPlan augmentPlan, CombatContextSnapshot contextSnapshot)
        {
            List<ElementApplication> applications = augmentPlan.ElementSeeds
                .Select(seed => new ElementApplication
                {
                    Element = seed.Element,
                    BuildUp = seed.BaseBuildUp,
                    DotStrength = seed.BaseDotStrength,
                    ApplyCooldownGroup = seed.CooldownGroup,
                    ApplyCooldownTicks = seed.CooldownTicks,
                    ApplicationFlags = seed.Flags,
                })
                .ToList();

            return new AttackPayload
            {
                SourcePlayerId = intent.SourcePlayerId,
                SourceEntityId = intent.SourceEntityId,
                SourceKind = intent.SourceKind,
                DamageClass = intent.DamageClass,
                BaseDamage = intent.BaseDamage,
                CanApplyElement = augmentPlan.CanApplyElement,
                CanTriggerReaction = augmentPlan.CanTriggerReaction,
                CanApplyDot = augmentPlan.CanApplyDot,
                Flags = augmentPlan.Flags,
                Applications = applications,
                Tags = augmentPlan.Tags,
                ContextSnapshot = contextSnapshot,
            };
        }
    }
}

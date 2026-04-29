#nullable enable
namespace AdvancedMod.DamageClasses.Element.Input
{
    /// <summary>
    /// Provides a minimal extension point for rewriting incoming prototype element inputs.
    /// </summary>
    public interface IAttackInputModifier
    {
        /// <summary>
        /// Determines whether the modifier should run for the current input.
        /// </summary>
        bool IsActive(AttackInputResolveContext context);

        /// <summary>
        /// Rewrites the mutable builder state for the current hit input.
        /// </summary>
        void Modify(AttackInputResolveContext context, AttackInputBuilder builder);
    }
}

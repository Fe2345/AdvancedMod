using AdvancedMod.DamageClasses.Element.Definitions;

namespace AdvancedMod.DamageClasses.Element.Reactions
{
    /// <summary>
    /// Defines one concrete reaction rule that can be evaluated by the prototype element system.
    /// </summary>
    public sealed class ReactionRule
    {
        /// <summary>
        /// Gets the reaction identifier represented by this rule.
        /// </summary>
        public ReactionType Reaction { get; init; } = ReactionType.None;

        /// <summary>
        /// Gets the first participating element for this rule.
        /// </summary>
        public ElementType TriggerElement { get; init; } = ElementType.None;

        /// <summary>
        /// Gets the second participating element for this rule.
        /// </summary>
        public ElementType TargetElement { get; init; } = ElementType.None;

        /// <summary>
        /// Gets how the incoming element side should be consumed.
        /// </summary>
        public ReactionConsumeMode TriggerConsumeMode { get; init; } = ReactionConsumeMode.Fixed;

        /// <summary>
        /// Gets the consume value used for the incoming element side.
        /// </summary>
        public float TriggerConsumeValue { get; init; } = 1f;

        /// <summary>
        /// Gets how the target-side stored element should be consumed.
        /// </summary>
        public ReactionConsumeMode TargetConsumeMode { get; init; } = ReactionConsumeMode.Fixed;

        /// <summary>
        /// Gets the consume value used for the target-side stored element.
        /// </summary>
        public float TargetConsumeValue { get; init; } = 1f;

        /// <summary>
        /// Gets the damage model used by this reaction.
        /// </summary>
        public ReactionDamageModel DamageModel { get; init; } = ReactionDamageModel.ScaleFromSourceDamage;

        /// <summary>
        /// Gets the base factor applied to source damage when building the prototype reaction damage.
        /// </summary>
        public float BaseDamageFactor { get; init; } = 1f;

        /// <summary>
        /// Gets the output element type used by the reaction damage instance.
        /// </summary>
        public ElementType DamageElement { get; init; } = ElementType.None;
    }
}

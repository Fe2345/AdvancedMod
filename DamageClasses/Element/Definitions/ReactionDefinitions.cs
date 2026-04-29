namespace AdvancedMod.DamageClasses.Element.Definitions
{
    /// <summary>
    /// All reaction identifiers currently recognized by the element combat system.
    /// </summary>
    public enum ReactionType
    {
        /// <summary>
        /// No reaction.
        /// </summary>
        None = 0,

        /// <summary>
        /// Prototype thunder-wind shared reaction used for the first playable slice.
        /// </summary>
        ThunderWind = 1,
    }

    /// <summary>
    /// Defines how a reaction consumes element gauge from the attacker-side or target-side state.
    /// </summary>
    public enum ReactionConsumeMode
    {
        /// <summary>
        /// Consume a fixed amount.
        /// </summary>
        Fixed = 0,

        /// <summary>
        /// Consume a ratio of the currently stored target gauge.
        /// </summary>
        RatioOfCurrentGauge = 1,

        /// <summary>
        /// Consume a ratio of the newly applied gauge.
        /// </summary>
        RatioOfAppliedAmount = 2,

        /// <summary>
        /// Consume all available gauge.
        /// </summary>
        All = 3,
    }

    /// <summary>
    /// Defines how a reaction's damage plan should derive its numeric damage value.
    /// </summary>
    public enum ReactionDamageModel
    {
        /// <summary>
        /// Use a rule-defined flat damage model.
        /// </summary>
        FlatByRule = 0,

        /// <summary>
        /// Scale from the original source-hit damage.
        /// </summary>
        ScaleFromSourceDamage = 1,

        /// <summary>
        /// Scale from the amount of gauge consumed by the reaction.
        /// </summary>
        ScaleFromConsumedGauge = 2,

        /// <summary>
        /// Use a hybrid of source damage and consumed gauge.
        /// </summary>
        Hybrid = 3,
    }
}

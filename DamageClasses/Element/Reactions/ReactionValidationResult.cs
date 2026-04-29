namespace AdvancedMod.DamageClasses.Element.Reactions
{
    /// <summary>
    /// 表示反应候选的校验结果。
    /// </summary>
    public sealed class ReactionValidationResult
    {
        /// <summary>
        /// 该候选是否通过校验。
        /// </summary>
        public bool IsValid { get; init; }

        /// <summary>
        /// 校验对应的反应规则。
        /// </summary>
        public ReactionRule Rule { get; init; } = new();

        /// <summary>
        /// 触发侧元素的实际消耗量。
        /// </summary>
        public int TriggerConsumeAmount { get; init; }

        /// <summary>
        /// 目标侧元素的实际消耗量。
        /// </summary>
        public int TargetConsumeAmount { get; init; }
    }
}

using AdvancedMod.DamageClasses.Element.Reactions;
using Terraria;

namespace AdvancedMod.DamageClasses.Element.Data
{
    /// <summary>
    /// 描述一次反应伤害计算所需的只读输入。
    /// </summary>
    public sealed class ReactionDamageContext
    {
        /// <summary>
        /// 本次命中的标准化输入。
        /// </summary>
        public required AttackPayload Payload { get; init; }

        /// <summary>
        /// 本次命中选中的反应校验结果。
        /// </summary>
        public required ReactionValidationResult SelectedReaction { get; init; }

        /// <summary>
        /// 命中的来源玩家。
        /// </summary>
        public Player? SourcePlayer { get; init; }
    }
}

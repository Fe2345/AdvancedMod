#nullable enable
using AdvancedMod.DamageClasses.Element.Definitions;
using Terraria.ModLoader;

namespace AdvancedMod.DamageClasses.Element.Reactions
{
    /// <summary>
    /// 表示一次由反应或装备效果派生出的独立追加打击。
    /// </summary>
    public sealed class DamageInstancePlan
    {
        /// <summary>
        /// 追加打击的基础伤害。执行时仍会吃对应伤害职业的原生增伤。
        /// </summary>
        public int Damage { get; init; }

        /// <summary>
        /// 追加打击关联的元素类型。
        /// </summary>
        public ElementType Element { get; init; } = ElementType.None;

        /// <summary>
        /// 追加打击关联的反应类型，用于后续反应异化、显示和过滤。
        /// </summary>
        public ReactionType Reaction { get; init; } = ReactionType.None;

        /// <summary>
        /// 追加打击使用的伤害职业。为空时使用元素伤害职业。
        /// </summary>
        public DamageClass? DamageClass { get; init; }

        /// <summary>
        /// 追加打击的基础击退。
        /// </summary>
        public float Knockback { get; init; }

        /// <summary>
        /// 应用到 SourceDamage 乘区的额外倍率。
        /// </summary>
        public float SourceMultiplier { get; init; } = 1f;

        /// <summary>
        /// 应用到 FinalDamage 乘区的额外倍率。
        /// </summary>
        public float FinalMultiplier { get; init; } = 1f;

        /// <summary>
        /// 追加到原生暴击率上的额外暴击率，单位为百分点。
        /// </summary>
        public float CritChanceBonus { get; init; }

        /// <summary>
        /// 该追加打击是否允许按原生暴击率暴击。
        /// </summary>
        public bool CanCrit { get; init; } = true;

        /// <summary>
        /// 是否使用原版伤害浮动。
        /// </summary>
        public bool DamageVariation { get; init; } = true;
    }
}

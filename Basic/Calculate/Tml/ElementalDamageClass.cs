using Terraria.ModLoader;

namespace AdvancedMod.Basic.Calculate.Tml;

/// <summary>元素职业只继承 Generic 的统计属性，不继承其他职业效果或前缀。</summary>
public sealed class ElementalDamageClass : DamageClass
{
    public override StatInheritanceData GetModifierInheritance(DamageClass damageClass) =>
        damageClass == Generic ? StatInheritanceData.Full : StatInheritanceData.None;

    public override bool GetEffectInheritance(DamageClass damageClass) => false;
    public override bool GetPrefixInheritance(DamageClass damageClass) => false;
}

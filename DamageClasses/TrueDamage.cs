
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.DamageClasses
{
    public class TrueDamage : DamageClass
    {
        public override bool UseStandardCritCalcs => true;

        public override bool GetEffectInheritance(DamageClass damageClass)
        {
            return true; // 真实伤害受所有伤害类型的效果影响
        }
    }
}
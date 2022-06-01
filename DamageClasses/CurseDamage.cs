using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.DamageClasses
{
    public class CurseDamage : DamageClass
    {
        public override void SetStaticDefaults()
        {
            ClassName.SetDefault("◊Á÷‰…À∫¶");
        }

        public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
        {
            if (damageClass == DamageClass.Ranged)
            {
                return new StatInheritanceData(
                    damageInheritance:0.5f,
                    critChanceInheritance:0.5f,
                    attackSpeedInheritance:0.5f,
                    armorPenInheritance:0.5f,
                    knockbackInheritance:0.5f
                    );
            }
            else if (damageClass == DamageClass.Magic)
            {
                return new StatInheritanceData(
                    damageInheritance:0.5f,
                    critChanceInheritance:0.5f,
                    attackSpeedInheritance:0.5f,
                    armorPenInheritance:0.5f,
                    knockbackInheritance:0.5f
                    );
            }
            else
            {
                return new StatInheritanceData(
                    damageInheritance:0f,
                    critChanceInheritance:0f,
                    attackSpeedInheritance:0f,
                    armorPenInheritance:0f,
                    knockbackInheritance:0f
                    );
            }
        }

        public override bool GetEffectInheritance(DamageClass damageClass)
        {
            if (damageClass == DamageClass.Ranged) return true;
            if (damageClass == DamageClass.Magic) return true;
            return false;
        }

        public override void SetDefaultStats(Player player)
        {
            player.GetCritChance<CurseDamage>() += 4;
            player.GetArmorPenetration<CurseDamage>() += 10;
        }

        public override bool UseStandardCritCalcs => true;

        public override bool ShowStatTooltipLine(Player player, string lineName)
        {
            if (lineName.Equals("CritChance"))
            {
                return false;
            }
            return true;
        }
    }
}
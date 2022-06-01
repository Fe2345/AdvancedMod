using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Prefix
{
    public class Original : ModPrefix
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("本原");
        }

        public override PrefixCategory Category => PrefixCategory.AnyWeapon;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult = 1.3f;
            knockbackMult = 1.3f;
            useTimeMult = 0.7f;
            shootSpeedMult = 1.3f;
            manaMult = 0.7f;
        }

        public override bool CanRoll(Item item)
        {
            return item.DamageType == ModContent.GetInstance<DamageClasses.CurseDamage>();
        }

        public override float RollChance(Item item)
        {
            return 16f;
        }

        public override void ModifyValue(ref float valueMult)
        {
            valueMult = 0.92f;
        }
    }

    public class Thunder : ModPrefix
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("霹雳");
        }

        public override PrefixCategory Category => PrefixCategory.AnyWeapon;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult = 1.2f;
            knockbackMult = 1.2f;
            useTimeMult = 0.8f;
            shootSpeedMult = 1.2f;
            manaMult = 0.8f;
        }

        public override bool CanRoll(Item item)
        {
            return item.DamageType == ModContent.GetInstance<DamageClasses.CurseDamage>();
        }

        public override float RollChance(Item item)
        {
            return 16f;
        }

        public override void ModifyValue(ref float valueMult)
        {
            valueMult = 1.2f;
        }
    }

    public class Hallow : ModPrefix
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("神圣");
        }

        public override PrefixCategory Category => PrefixCategory.AnyWeapon;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult = 1.15f;
            knockbackMult = 1.15f;
            useTimeMult = 0.85f;
            shootSpeedMult = 1.15f;
            manaMult = 0.85f;
        }

        public override bool CanRoll(Item item)
        {
            return item.DamageType == ModContent.GetInstance<DamageClasses.CurseDamage>();
        }

        public override float RollChance(Item item)
        {
            return 16f;
        }

        public override void ModifyValue(ref float valueMult)
        {
            valueMult = 1.15f;
        }
    }

    public class Rotation : ModPrefix
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("回转");
        }

        public override PrefixCategory Category => PrefixCategory.AnyWeapon;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult = 1.08f;
            knockbackMult = 1.08f;
            useTimeMult = 0.92f;
            shootSpeedMult = 1.08f;
            manaMult = 0.92f;
        }

        public override bool CanRoll(Item item)
        {
            return item.DamageType == ModContent.GetInstance<DamageClasses.CurseDamage>();
        }

        public override float RollChance(Item item)
        {
            return 16f;
        }

        public override void ModifyValue(ref float valueMult)
        {
            valueMult = 1.08f;
        }
    }

    public class Abomination : ModPrefix
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("憎恶");
        }

        public override PrefixCategory Category => PrefixCategory.AnyWeapon;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult = 0.92f;
            knockbackMult = 0.92f;
            useTimeMult = 1.08f;
            shootSpeedMult = 0.92f;
            manaMult = 1.08f;
        }

        public override bool CanRoll(Item item)
        {
            return item.DamageType == ModContent.GetInstance<DamageClasses.CurseDamage>();
        }

        public override float RollChance(Item item)
        {
            return 16f;
        }

        public override void ModifyValue(ref float valueMult)
        {
            valueMult = 0.92f;
        }
    }

    public class Wither : ModPrefix
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("凋零");
        }

        public override PrefixCategory Category => PrefixCategory.AnyWeapon;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult = 0.85f;
            knockbackMult = 0.85f;
            useTimeMult = 1.15f;
            shootSpeedMult = 0.85f;
            manaMult = 1.15f;
        }

        public override bool CanRoll(Item item)
        {
            return item.DamageType == ModContent.GetInstance<DamageClasses.CurseDamage>();
        }

        public override float RollChance(Item item)
        {
            return 16f;
        }

        public override void ModifyValue(ref float valueMult)
        {
            valueMult = 0.85f;
        }
    }

    public class Corrupt : ModPrefix
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("腐化");
        }

        public override PrefixCategory Category => PrefixCategory.AnyWeapon;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult = 0.8f;
            knockbackMult = 0.8f;
            useTimeMult = 1.2f;
            shootSpeedMult = 0.8f;
            manaMult = 1.2f;
        }

        public override bool CanRoll(Item item)
        {
            return item.DamageType == ModContent.GetInstance<DamageClasses.CurseDamage>();
        }

        public override float RollChance(Item item)
        {
            return 16f;
        }

        public override void ModifyValue(ref float valueMult)
        {
            valueMult = 0.8f;
        }
    }

    public class Nihility : ModPrefix
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("虚无");
        }

        public override PrefixCategory Category => PrefixCategory.AnyWeapon;

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus)
        {
            damageMult = 0.7f;
            knockbackMult = 0.7f;
            useTimeMult = 1.3f;
            shootSpeedMult = 0.7f;
            manaMult = 1.3f;
        }

        public override bool CanRoll(Item item)
        {
            return item.DamageType == ModContent.GetInstance<DamageClasses.CurseDamage>();
        }

        public override float RollChance(Item item)
        {
            return 16f;
        }

        public override void ModifyValue(ref float valueMult)
        {
            valueMult = 0.7f;
        }
    }
}

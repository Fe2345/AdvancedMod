using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Projectiles.Weapons
{
    public class TreeDiagrammerLaserFriendly : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("树状图设计者激光");
        }

        public override string Texture => "Terraria/Images/Projectile_100";

        public override void SetDefaults()
        {
            Projectile.width = 5;
            Projectile.height = 50;
            Projectile.aiStyle = 1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.timeLeft = 200;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuff.ElectromagneticInduction>(), 10);
        }
    }
}

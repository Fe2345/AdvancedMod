using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Projectiles
{
    public class RoarOfFlame_Projectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("孢子射弹");
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 600;
            //Projectile.aiStyle = 0;
        }

        public override void AI()
        {
        }
    }
}

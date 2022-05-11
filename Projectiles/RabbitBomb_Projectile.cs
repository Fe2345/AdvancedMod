using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Projectiles
{
    public class RabbitBomb_Projectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("死亡幻影球");
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 240;
            Projectile.damage = 60;
            Projectile.tileCollide = true;
        }
    }
}

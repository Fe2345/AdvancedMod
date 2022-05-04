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
            projectile.width = 22;
            projectile.height = 22;
            projectile.friendly = true;
            projectile.hostile = false;
            projectile.penetrate = 3;
            projectile.timeLeft = 240;
            projectile.damage = 60;
            projectile.tileCollide = true;
        }
    }
}

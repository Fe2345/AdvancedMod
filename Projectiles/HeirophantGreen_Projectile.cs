using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System;

namespace AdvancedMod.Projectiles
{
     public class HeirophantGreen_Projectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("绿宝石水花");
        }

        public override void SetDefaults()
        {
            projectile.width = 10;
            projectile.height = 10;
            projectile.friendly = true;
            projectile.hostile = false;
            projectile.penetrate = 3;
            projectile.timeLeft = 600;
            //projectile.aiStyle = 0;
            projectile.tileCollide = true;
        }

        public override void AI()
        {
            projectile.rotation = (float)(Math.PI / 2 - Math.Atan2(projectile.velocity.Y, projectile.velocity.X));
        }
    }
}

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
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 600;
            //Projectile.aiStyle = 0;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            Projectile.rotation = (float)(Math.PI / 2 - Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X));
        }
    }
}

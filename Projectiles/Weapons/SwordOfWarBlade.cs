using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Projectiles.Weapons
{
    public  class SwordOfWarBlade : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("战乱之剑刃");
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 600;
            //Projectile.aiStyle = 0;
        }

        NPC target = null;
        public override void AI()
        {
            Projectile.rotation += 0.05f;
          
            if (target == null) target = Utils.Tool.GetClosestNPC(Projectile.Center);

            if (target.life == 0)
            {
                Projectile.Kill();
            }
            else
            {
                Vector2 ProjToNpc = target.Center - target.Center;

                Projectile.velocity = Vector2.Normalize(ProjToNpc) * 3;
            }
        }
    }
}

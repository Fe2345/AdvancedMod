using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Projectiles
{
    public class SwordOfPhantom_Projectile : ModProjectile
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
            projectile.timeLeft = 120;
            projectile.damage = 1000;
        }

        int Time;
        public override void AI()
        {
            Time++;
            if (Time <= 30)
            {
                projectile.velocity = projectile.oldVelocity;
            }
            else if (Time > 30 && Time <= 120)
            {
                foreach (var npc in Main.npc)
                {
                    if (npc.active && !npc.friendly)
                    {
                        Vector2 ProjToNpc = npc.Center - projectile.Center;
                        if (ProjToNpc.Length() == 0)
                        {
                            continue;
                        }
                        else
                        {
                            projectile.velocity = (ProjToNpc / ProjToNpc.Length())*20;
                        }
                    }
                }
            }
            else
            {
                projectile.Kill();
            }
        }
    }
}

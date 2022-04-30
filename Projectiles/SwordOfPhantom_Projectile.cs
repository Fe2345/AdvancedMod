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
            projectile.timeLeft = 600;
            projectile.damage = 1000;
            projectile.tileCollide = false;
        }

        int Time;
        NPC npc;
        float npcDistance;
        public override void AI()
        {
            Time++;
            if (Time <= 60)
            {
                projectile.velocity = projectile.oldVelocity;
            }
            else if (Time > 60 && Time <= 600)
            {
                for (int i = 0;i < Main.npc.Length; i++)
                {
                    if (npcDistance == 0)
                    {
                        continue;
                    }
                    else
                    {
                        if ((npc.Center - projectile.Center).Length() < npcDistance)
                        {
                            npc = Main.npc[i];
                        }
                    }
                }
                if (npc.active && !npc.friendly)
                {
                    Vector2 ProjToNpc = npc.Center - projectile.Center;
                     projectile.velocity = (ProjToNpc / ProjToNpc.Length())*20;
                }
            }
        }
    }
}

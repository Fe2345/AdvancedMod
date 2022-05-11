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
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 600;
            Projectile.damage = 1000;
            Projectile.tileCollide = false;
        }

        int Time;
        NPC npc;
        float npcDistance;
        public override void AI()
        {
            Time++;
            if (Time <= 60)
            {
                Projectile.velocity = Projectile.oldVelocity;
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
                        if ((npc.Center - Projectile.Center).Length() < npcDistance)
                        {
                            npc = Main.npc[i];
                        }
                    }
                }
                if (npc.active && !npc.friendly)
                {
                    Vector2 ProjToNpc = npc.Center - Projectile.Center;
                     Projectile.velocity = (ProjToNpc / ProjToNpc.Length())*20;
                }
            }
        }
    }
}

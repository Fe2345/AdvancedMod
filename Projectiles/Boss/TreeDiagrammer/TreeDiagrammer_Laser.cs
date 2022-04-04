using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Projectiles.Boss.TreeDiagrammer
{
    public class TreeDiagrammer_Laser : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("树状图设计者激光");
        }

        public override void SetDefaults()
        {
            projectile.width = 22;
            projectile.height = 22;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.penetrate = 3;
            projectile.timeLeft = 600;
            //projectile.aiStyle = 0;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            projectile.Kill();
        }

        public override void AI()
        {
            Player player = Main.LocalPlayer;
            Vector2 ProjToPlr = player.Center - projectile.Center;
            if (Vector2.Distance(projectile.Center,player.Center) != 0)
            {
                projectile.velocity = Vector2.Normalize(ProjToPlr) * 4;
            }
            
        }
    }
}

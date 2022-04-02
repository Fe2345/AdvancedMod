using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using AdvancedMod.Utils;

namespace AdvancedMod.Projectiles
{
    public class Grass_Staff_Projectiles : ModProjectile
    {
        private const string Grass_Staff_Projectiles_image = "AdvancedMod/Projectiles/Grass_Staff_Projectiles.png";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("孢子射弹");
        }

        public override void SetDefaults()
        {
            projectile.width = 22;
            projectile.height = 22;
            projectile.friendly = true;
            projectile.hostile = false;
            projectile.penetrate = 3;
            projectile.timeLeft = 600;
            //projectile.aiStyle = 0;
        }

        public override void AI()
        {
            Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height
    , MyDustId.GreyStone, 0f, 0f, 100, default(Color), 3f);
            dust.noGravity = true;
            dust.velocity *= 0;
            dust.position = projectile.position;
        }
    }
}

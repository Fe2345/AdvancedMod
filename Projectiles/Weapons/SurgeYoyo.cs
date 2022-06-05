using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System;

namespace AdvancedMod.Projectiles.Weapons
{
    public class SurgeYoyo : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("电涌");
            // Vanilla values range from 3f(Wood) to 16f(Chik), and defaults to -1f. Leaving as -1 will make the time infinite.
            ProjectileID.Sets.YoyosLifeTimeMultiplier[Projectile.type] = -1f;
            // Vanilla values range from 130f(Wood) to 400f(Terrarian), and defaults to 200f
            ProjectileID.Sets.YoyosMaximumRange[Projectile.type] = 350f;
            // Vanilla values range from 9f(Wood) to 17.5f(Terrarian), and defaults to 10f
            ProjectileID.Sets.YoyosTopSpeed[Projectile.type] = 15f;

            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.aiStyle = -1;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            Player player = Main.LocalPlayer;

            Projectile.position = Main.MouseWorld;
            Projectile.rotation += 1f;

            if (Projectile.ai[0] % 60 == 0)
            {
                for (int i = 0;i < 8; i++)
                {
                    Vector2 vel = Utils.Tool.TurnVector(new Vector2(-10, 0), (float)(Math.PI / 4 * i));
                    Vector2 pos = Projectile.Center + Utils.Tool.TurnVector(new Vector2(40,0),(float)(Math.PI / 4 * i));
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), pos, vel, ModContent.ProjectileType<TreeDiagrammerLaserFriendly>(), Projectile.damage, 0);
                }
            }

            if (player.HeldItem.type != ModContent.ItemType<Items.Weapon.Melee.Surge>()) Projectile.Kill();
        }
    }
}

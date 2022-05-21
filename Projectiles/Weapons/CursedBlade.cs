using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System;

namespace AdvancedMod.Projectiles.Weapons
{
    public class CursedBlade : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("诅咒之刃");
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<DamageClasses.CurseDamage>();
            Projectile.penetrate = 3;
            Projectile.timeLeft = 600;
            //Projectile.aiStyle = 0;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            foreach(var debuff in Utils.Tool.debuffs)
            {
                target.AddBuff(debuff, Main.rand.Next(10));
            }
        }

        public override void AI()
        {
            Projectile.rotation = (float)(Math.PI / 2 + Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X));
        }
    }
}

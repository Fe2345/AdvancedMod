using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System;

namespace AdvancedMod.Projectiles.Weapons
{
    public class StreamerSingleRotaryGunProj : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("无尽流光");
        }

        public override void SetDefaults()
        {
            Projectile.width = 5;
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 150;
            Projectile.penetrate = 200;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 10);
            target.HitEffect(target.whoAmI, target.defense);
            target.life -= target.defense;
        }

        public override void AI()
        {
            Projectile.rotation = (float)(Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X));
        }
    }
}

using AdvancedMod.DamageClasses.Element;
using AdvancedMod.DamageClasses.Element.Definitions;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Projectiles.Weapons.Element
{
    /// <summary>
    /// 可燃物发射器使用的火属性凝胶弹。
    /// </summary>
    public sealed class BurnedGel : ModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.CultistBossFireBall}";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.scale = 1f / 3f;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<ElementDamage>();
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ElementCombatSystem.ProcessProjectileHit(Projectile, target, hit, damageDone, ElementType.Pyro, 5);
        }
    }
}

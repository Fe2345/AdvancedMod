using AdvancedMod.DamageClasses.Element;
using AdvancedMod.DamageClasses.Element.Definitions;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Projectiles.Weapons
{
    /// <summary>
    /// Minimal straight projectile used by the prototype thunder staff.
    /// </summary>
    public sealed class PrototypeThunderBolt : ModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurpleLaser}";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<ElementDamage>();
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.35f, 0.35f, 0.9f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ElementCombatSystem.ProcessProjectileHit(Projectile, target, hit, damageDone, ElementType.Thunder);
        }
    }
}

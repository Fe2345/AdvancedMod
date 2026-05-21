using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Projectiles.NPCs
{
    public sealed class DiagrammerProbeExplosion : ModProjectile
    {
        private float Radius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 72f;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Grenade}";

        public override void SetDefaults()
        {
            Projectile.width = 144;
            Projectile.height = 144;

            Projectile.friendly = false;
            Projectile.hostile = true;

            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;

            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;

            Projectile.DamageType = DamageClass.Default;
        }

        public override void AI()
        {
            ResizeToRadius();

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                SpawnExplosionVisuals();
            }

            Projectile.alpha = 255;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closestPoint = ClosestPointInRectangle(Projectile.Center, targetHitbox);
            float distanceSquared = Vector2.DistanceSquared(Projectile.Center, closestPoint);

            return distanceSquared <= Radius * Radius;
        }

        public override bool CanHitPlayer(Player target)
        {
            return !target.dead && !target.ghost;
        }

        private void ResizeToRadius()
        {
            int size = (int)(Radius * 2f);

            if (size < 2)
            {
                size = 2;
            }

            if (Projectile.width == size && Projectile.height == size)
            {
                return;
            }

            Vector2 center = Projectile.Center;
            Projectile.width = size;
            Projectile.height = size;
            Projectile.Center = center;
        }

        private void SpawnExplosionVisuals()
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            for (int i = 0; i < 36; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);

                int dustIndex = Dust.NewDust(
                    Projectile.Center - new Vector2(8f, 8f),
                    16,
                    16,
                    DustID.Torch,
                    velocity.X,
                    velocity.Y,
                    100,
                    default,
                    1.8f
                );

                Main.dust[dustIndex].noGravity = true;
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);

                Dust.NewDust(
                    Projectile.Center - new Vector2(8f, 8f),
                    16,
                    16,
                    DustID.Smoke,
                    velocity.X,
                    velocity.Y,
                    130,
                    default,
                    1.5f
                );
            }
        }

        private static Vector2 ClosestPointInRectangle(Vector2 point, Rectangle rectangle)
        {
            float x = MathHelper.Clamp(point.X, rectangle.Left, rectangle.Right);
            float y = MathHelper.Clamp(point.Y, rectangle.Top, rectangle.Bottom);

            return new Vector2(x, y);
        }
    }
}
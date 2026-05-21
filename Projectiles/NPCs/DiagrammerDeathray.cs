using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Projectiles.NPCs
{
    public class DiagrammerDeathray : ModProjectile
    {
        private const float MaxLaserLength = 2400f;
        private const float LaserWidth = 42f;
        private const int TileScanSampleCount = 3;

        private readonly float[] tileScanSamples = new float[TileScanSampleCount];

        private float LaserLength
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private float Timer
        {
            get => Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SaucerDeathray;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = Main.projFrames[ProjectileID.SaucerDeathray];
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;

            Projectile.hostile = true;
            Projectile.friendly = false;

            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override void AI()
        {
            int ownerNpcIndex = (int)Projectile.ai[0];
            float duration = Projectile.ai[1];

            if (ownerNpcIndex < 0 || ownerNpcIndex >= Main.maxNPCs || !Main.npc[ownerNpcIndex].active)
            {
                Projectile.Kill();
                return;
            }

            NPC ownerNpc = Main.npc[ownerNpcIndex];

            if (Projectile.velocity.LengthSquared() < 0.0001f)
            {
                Projectile.Kill();
                return;
            }

            Projectile.velocity = Vector2.Normalize(Projectile.velocity);

            Vector2 muzzleOffset = new Vector2(0f, 28f);
            Projectile.Center = ownerNpc.Center + muzzleOffset;

            Timer++;

            if (duration > 0f && Timer >= duration)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.rotation = Projectile.velocity.ToRotation();

            UpdateLaserLength();

            AddLaserLight();
        }

        private void UpdateLaserLength()
        {
            Collision.LaserScan(
                Projectile.Center,
                Projectile.velocity,
                LaserWidth,
                MaxLaserLength,
                tileScanSamples
            );

            float targetLength = MaxLaserLength;

            for (int i = 0; i < tileScanSamples.Length; i++)
            {
                targetLength = Math.Min(targetLength, tileScanSamples[i]);
            }

            targetLength = MathHelper.Clamp(targetLength, 16f, MaxLaserLength);

            if (LaserLength <= 0f)
            {
                LaserLength = targetLength;
                return;
            }

            if (targetLength < LaserLength)
            {
                LaserLength = targetLength;
            }
            else
            {
                LaserLength = MathHelper.Lerp(LaserLength, targetLength, 0.35f);
            }
        }

        private void AddLaserLight()
        {
            Vector2 direction = Projectile.velocity;
            int lightCount = (int)(LaserLength / 160f);

            for (int i = 0; i <= lightCount; i++)
            {
                Vector2 lightPosition = Projectile.Center + direction * i * 160f;
                Lighting.AddLight(lightPosition, 0.9f, 0.15f, 0.05f);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = Projectile.Center + Projectile.velocity * LaserLength;

            float collisionPoint = 0f;

            Vector2 targetPosition = new Vector2(targetHitbox.Left, targetHitbox.Top);
            Vector2 targetSize = new Vector2(targetHitbox.Width, targetHitbox.Height);

            return Collision.CheckAABBvLineCollision(
                targetPosition,
                targetSize,
                start,
                end,
                LaserWidth,
                ref collisionPoint
            );
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[ProjectileID.SaucerDeathray].Value;

            int frameCount = Math.Max(1, Main.projFrames[ProjectileID.SaucerDeathray]);
            Rectangle frame = texture.Frame(1, frameCount, 0, 0);

            Vector2 direction = Projectile.velocity;
            float rotation = direction.ToRotation() - MathHelper.PiOver2;

            float scaleX = LaserWidth / frame.Width;
            float scaleY = Projectile.scale;

            float segmentLength = frame.Height * scaleY;

            for (float offset = 0f; offset < LaserLength; offset += segmentLength)
            {
                float remainingLength = LaserLength - offset;

                Rectangle sourceRectangle = frame;

                if (remainingLength < segmentLength)
                {
                    sourceRectangle.Height = Math.Max(1, (int)(sourceRectangle.Height * remainingLength / segmentLength));
                }

                Vector2 drawPosition =
                    Projectile.Center +
                    direction * (offset + sourceRectangle.Height * scaleY * 0.5f) -
                    Main.screenPosition;

                Vector2 origin = new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height * 0.5f);
                Vector2 scale = new Vector2(scaleX, scaleY);

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition,
                    sourceRectangle,
                    Color.White,
                    rotation,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0
                );
            }

            return false;
        }
    }
}
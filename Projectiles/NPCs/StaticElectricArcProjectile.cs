// 编写日期: 2026-05-15
// 用途: 敌方使用的固定位置直线电弧弹幕，连接起点和终点并持续一段时间

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Projectiles.NPCs
{
    public sealed class HostileStaticArcProjectile : ModProjectile
    {
        private const float LineWidth = 10f;
        private const float TelegraphMode = 0f;
        private const float DamageMode = 1f;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.None}";

        private Vector2 StartPoint => Projectile.Center;

        private Vector2 EndPoint => Projectile.Center + Projectile.velocity;

        private bool IsDamaging => Projectile.ai[1] == DamageMode;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;

            Projectile.friendly = false;
            Projectile.hostile = false;

            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.timeLeft = 3600;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                int durationTicks = (int)Projectile.ai[0];

                if (durationTicks <= 0)
                {
                    Projectile.Kill();
                    return;
                }

                Projectile.timeLeft = durationTicks;
                Projectile.localAI[0] = 1f;
            }

            Projectile.hostile = IsDamaging;

            Lighting.AddLight(StartPoint, 0.15f, 0.45f, 0.75f);
            Lighting.AddLight(EndPoint, 0.15f, 0.45f, 0.75f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!IsDamaging)
            {
                return false;
            }

            float collisionPoint = 0f;

            Vector2 targetPosition = new Vector2(targetHitbox.X, targetHitbox.Y);
            Vector2 targetSize = new Vector2(targetHitbox.Width, targetHitbox.Height);

            return Collision.CheckAABBvLineCollision(
                targetPosition,
                targetSize,
                StartPoint,
                EndPoint,
                LineWidth,
                ref collisionPoint
            );
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float opacity = MathHelper.Clamp(Projectile.timeLeft / 8f, 0f, 1f);

            Vector2 start = StartPoint - Main.screenPosition;
            Vector2 end = EndPoint - Main.screenPosition;

            Color outerColor = IsDamaging ? new Color(80, 180, 255, 0) : new Color(255, 80, 50, 0);
            Color innerColor = IsDamaging ? Color.White : new Color(255, 230, 160);

            DrawLineWithPixel(Main.spriteBatch, start, end, outerColor * opacity, 10f);
            DrawLineWithPixel(Main.spriteBatch, start, end, innerColor * opacity, 3f);

            return false;
        }

        private static void DrawLineWithPixel(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 delta = end - start;
            float length = delta.Length();

            if (!float.IsFinite(length) || length <= 0f)
            {
                return;
            }

            spriteBatch.Draw(
                TextureAssets.MagicPixel.Value,
                start,
                null,
                color,
                delta.ToRotation(),
                Vector2.Zero,
                new Vector2(length, width),
                SpriteEffects.None,
                0f
            );
        }

        public static int SpawnHostile(
            IEntitySource source,
            Vector2 start,
            Vector2 end,
            float durationSeconds,
            int damage,
            float knockBack
        )
        {
            return SpawnLine(source, start, end, durationSeconds, damage, knockBack, DamageMode);
        }

        public static int SpawnTelegraph(
            IEntitySource source,
            Vector2 start,
            Vector2 end,
            float durationSeconds
        )
        {
            return SpawnLine(source, start, end, durationSeconds, 0, 0f, TelegraphMode);
        }

        private static int SpawnLine(
            IEntitySource source,
            Vector2 start,
            Vector2 end,
            float durationSeconds,
            int damage,
            float knockBack,
            float mode
        )
        {
            if (!float.IsFinite(durationSeconds) || durationSeconds <= 0f)
            {
                return -1;
            }

            if (!IsFinite(start) || !IsFinite(end))
            {
                return -1;
            }

            Vector2 offset = end - start;

            if (offset.LengthSquared() <= 0f)
            {
                return -1;
            }

            int durationTicks = (int)MathF.Ceiling(durationSeconds * 60f);

            return Projectile.NewProjectile(
                source,
                start,
                offset,
                ModContent.ProjectileType<HostileStaticArcProjectile>(),
                damage,
                knockBack,
                Main.myPlayer,
                durationTicks,
                mode
            );
        }

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.X) && float.IsFinite(value.Y);
        }
    }
}

// 编写日期: 2026-05-22
// 用途: 敌方固定坐标电弧弹幕，在两个世界坐标之间绘制稳定淡蓝色电弧，并可切换为伤害判定线

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

        private const float SegmentLength = 24f;
        private const int MinSegments = 4;
        private const int MaxSegments = 48;

        private const float TelegraphJitterPixels = 7f;
        private const float DamageJitterPixels = 9f;

        private static readonly Rectangle PixelSource = new Rectangle(0, 0, 1, 1);

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

            Projectile.netImportant = true;
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

            Lighting.AddLight(StartPoint, 0.12f, 0.34f, 0.55f);
            Lighting.AddLight(EndPoint, 0.12f, 0.34f, 0.55f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!IsDamaging)
            {
                return false;
            }

            float collisionPoint = 0f;

            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                StartPoint,
                EndPoint,
                LineWidth,
                ref collisionPoint
            );
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 start = StartPoint - Main.screenPosition;
            Vector2 end = EndPoint - Main.screenPosition;

            DrawStableArc(start, end);

            return false;
        }

        private void DrawStableArc(Vector2 start, Vector2 end)
        {
            Vector2 offset = end - start;
            float length = offset.Length();

            if (!float.IsFinite(length) || length <= 0f)
            {
                return;
            }

            Vector2 direction = offset / length;
            Vector2 normal = new Vector2(-direction.Y, direction.X);

            int segmentCount = Math.Clamp((int)MathF.Ceiling(length / SegmentLength), MinSegments, MaxSegments);

            float fadeOpacity = MathHelper.Clamp(Projectile.timeLeft / 10f, 0f, 1f);
            float baseOpacity = IsDamaging ? 0.95f : 0.62f;
            float opacity = fadeOpacity * baseOpacity;

            float jitter = IsDamaging ? DamageJitterPixels : TelegraphJitterPixels;
            int seed = BuildStableSeed(StartPoint, EndPoint, Projectile.identity, IsDamaging);

            Vector2 previous = start;

            for (int i = 1; i <= segmentCount; i++)
            {
                float progress = i / (float)segmentCount;
                Vector2 current = start + offset * progress;

                if (i < segmentCount)
                {
                    float envelope = MathF.Sin(progress * MathHelper.Pi);
                    float noise = StableSignedNoise(seed, i);
                    current += normal * noise * jitter * envelope;
                }

                DrawArcSegment(previous, current, opacity);

                previous = current;
            }
        }

        private static void DrawArcSegment(Vector2 start, Vector2 end, float opacity)
        {
            DrawLine(start, end, new Color(80, 190, 255, 180) * (0.35f * opacity), 14f);
            DrawLine(start, end, new Color(130, 220, 255, 220) * (0.70f * opacity), 6f);
            DrawLine(start, end, new Color(230, 250, 255, 255) * (0.85f * opacity), 2f);
        }

        private static void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 delta = end - start;
            float length = delta.Length();

            if (!float.IsFinite(length) || length <= 0f)
            {
                return;
            }

            Main.EntitySpriteDraw(
                TextureAssets.MagicPixel.Value,
                start,
                PixelSource,
                color,
                delta.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(length, width),
                SpriteEffects.None,
                0f
            );
        }

        private static int BuildStableSeed(Vector2 start, Vector2 end, int identity, bool isDamaging)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (int)MathF.Round(start.X);
                hash = hash * 31 + (int)MathF.Round(start.Y);
                hash = hash * 31 + (int)MathF.Round(end.X);
                hash = hash * 31 + (int)MathF.Round(end.Y);
                hash = hash * 31 + identity;
                hash = hash * 31 + (isDamaging ? 1 : 0);
                return hash;
            }
        }

        private static float StableSignedNoise(int seed, int index)
        {
            unchecked
            {
                int value = seed;
                value ^= index * 374761393;
                value = (value ^ (value >> 13)) * 1274126177;
                value ^= value >> 16;

                int positive = value & 0x7fffffff;
                return positive / 1073741824f - 1f;
            }
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

            if (!IsFinite(offset) || offset.LengthSquared() <= 0f)
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
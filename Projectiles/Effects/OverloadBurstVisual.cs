using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Projectiles.Effects
{
    public sealed class OverloadBurstVisual : ModProjectile
    {
        private const int Lifetime = 24;
        private const int FirstRingDustCount = 54;
        private const int SecondaryRingDustCount = 22;
        private const float FirstRingSpeed = 7.2f;
        private const float SecondaryRingSpeed = 4.4f;

        public override string Texture => "Terraria/Images/Projectile_1"; //draw被关掉了，随便选个贴图过编译即可

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.alpha = 255;
            Projectile.hide = true;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        public override void AI()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }

            int age = Lifetime - Projectile.timeLeft;

            if (age == 0)
            {
                SpawnCoreFlash();
                SpawnOuterRing(FirstRingDustCount, FirstRingSpeed, 1.9f, true);
                SpawnSmoke(18, 2.8f);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.45f, Pitch = 0.25f }, Projectile.Center);
            }

            if (age == 4 || age == 8 || age == 12)
            {
                SpawnOuterRing(SecondaryRingDustCount, SecondaryRingSpeed, 1.2f, false);
            }

            SpawnElectricSparks(4);
            SpawnEmbers(6);

            float lightStrength = MathHelper.Clamp(1f - age / 24f, 0f, 1f);
            Lighting.AddLight(Projectile.Center, 1.1f * lightStrength, 0.38f * lightStrength, 0.08f * lightStrength);
        }

        private void SpawnCoreFlash()
        {
            for (int i = 0; i < 28; i++)
            {
                Vector2 direction = RandomUnitVector();
                Vector2 velocity = direction * RandFloat(1.8f, 5.8f);
                Vector2 position = Projectile.Center + direction * RandFloat(0f, 12f);

                Dust dust = Dust.NewDustPerfect(
                    position,
                    DustID.Torch,
                    velocity,
                    70,
                    Color.OrangeRed,
                    RandFloat(1.35f, 2.25f)
                );

                dust.noGravity = true;
                dust.fadeIn = 0.9f;
            }

            for (int i = 0; i < 12; i++)
            {
                Vector2 direction = RandomUnitVector();
                Vector2 velocity = direction * RandFloat(1.2f, 3.6f);

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + direction * RandFloat(0f, 8f),
                    DustID.GoldFlame,
                    velocity,
                    80,
                    Color.Gold,
                    RandFloat(1.05f, 1.75f)
                );

                dust.noGravity = true;
                dust.fadeIn = 1.1f;
            }
        }

        private void SpawnOuterRing(int dustCount, float speed, float scale, bool strong)
        {
            float angleOffset = RandFloat(0f, MathHelper.TwoPi);

            for (int i = 0; i < dustCount; i++)
            {
                float angle = angleOffset + MathHelper.TwoPi * i / dustCount + RandFloat(-0.045f, 0.045f);
                Vector2 direction = AngleToVector(angle);
                Vector2 velocity = direction * RandFloat(speed * 0.68f, speed);
                Vector2 position = Projectile.Center + direction * RandFloat(6f, 18f);

                Dust dust = Dust.NewDustPerfect(
                    position,
                    DustID.FireworkFountain_Red,
                    velocity,
                    strong ? 60 : 95,
                    strong ? Color.OrangeRed : Color.DarkOrange,
                    RandFloat(scale * 0.8f, scale * 1.25f)
                );

                dust.noGravity = true;
                dust.fadeIn = strong ? 1.15f : 0.75f;
            }
        }

        private void SpawnElectricSparks(int dustCount)
        {
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 direction = RandomUnitVector();
                Vector2 velocity = direction * RandFloat(3.6f, 8.4f);
                Vector2 position = Projectile.Center + direction * RandFloat(8f, 38f);

                Dust dust = Dust.NewDustPerfect(
                    position,
                    DustID.Electric,
                    velocity,
                    35,
                    Color.MediumPurple,
                    RandFloat(0.8f, 1.35f)
                );

                dust.noGravity = true;
                dust.fadeIn = 0.6f;
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 direction = RandomUnitVector();
                Vector2 velocity = direction * RandFloat(2.2f, 5.2f);

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + direction * RandFloat(12f, 44f),
                    DustID.PurpleTorch,
                    velocity,
                    55,
                    Color.Violet,
                    RandFloat(1.0f, 1.55f)
                );

                dust.noGravity = true;
                dust.fadeIn = 0.8f;
            }
        }

        private void SpawnEmbers(int dustCount)
        {
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 direction = RandomUnitVector();
                Vector2 velocity = direction * RandFloat(0.7f, 2.4f);
                velocity.Y -= RandFloat(0.3f, 1.2f);

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + direction * RandFloat(6f, 34f),
                    DustID.Flare,
                    velocity,
                    100,
                    Color.Orange,
                    RandFloat(0.7f, 1.25f)
                );

                dust.noGravity = false;
                dust.fadeIn = 0.35f;
            }
        }

        private void SpawnSmoke(int dustCount, float speed)
        {
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 direction = RandomUnitVector();
                Vector2 velocity = direction * RandFloat(0.4f, speed);
                velocity.Y -= RandFloat(0.2f, 1.1f);

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + direction * RandFloat(4f, 24f),
                    DustID.Smoke,
                    velocity,
                    120,
                    Color.Gray,
                    RandFloat(1.35f, 2.25f)
                );

                dust.noGravity = false;
                dust.fadeIn = 0.25f;
            }
        }

        private static Vector2 RandomUnitVector()
        {
            return AngleToVector(RandFloat(0f, MathHelper.TwoPi));
        }

        private static Vector2 AngleToVector(float angle)
        {
            return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }

        private static float RandFloat(float min, float max)
        {
            return min + (float)Main.rand.NextDouble() * (max - min);
        }
    }
}
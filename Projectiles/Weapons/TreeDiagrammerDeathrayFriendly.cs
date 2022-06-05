﻿using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System;
using Microsoft.Xna.Framework;
using Terraria.Audio;

namespace AdvancedMod.Projectiles.Weapons
{
    public class TreeDiagrammerDeathrayFriendly : ModProjectile
    {
        int maxTime = 60;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("激光器死亡光");
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.Electrified, 10);
        }

        public override string Texture => "Terraria/Images/Projectile_455";

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 600;
            Projectile.aiStyle = -1;
        }

        Vector2 TipOffset => 18f * Projectile.scale * Projectile.velocity;

        public override void AI()
        {
            base.AI();
            Projectile.frameCounter += 60;

            Player player = Main.player[Projectile.owner];

            Projectile.velocity = Projectile.velocity.SafeNormalize(-Vector2.UnitY);

            float num801 = 10f;
            Projectile.scale = (float)Math.Cos(Projectile.localAI[0] * MathHelper.PiOver2 / maxTime) * num801;
            if (Projectile.scale > num801)
                Projectile.scale = num801;

            if (player.active && !player.dead && !player.ghost)
            {
                Projectile.Center = player.Center + 50f * Projectile.velocity + TipOffset + Main.rand.NextVector2Circular(5, 5);
            }
            else
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.localAI[0] == 0f)
            {
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Sounds/Railgun"), Projectile.Center);
                    SoundEngine.PlaySound(new SoundStyle("FargowiltasSouls/Sounds/Thunder"), player.Center + Projectile.velocity * Math.Min(Main.screenWidth / 2, 900f));
                }

                Vector2 dustPos = player.Center + Projectile.velocity * 50f;

                for (int i = 0; i < 40; i++)
                {
                    int dust = Dust.NewDust(dustPos - new Vector2(16, 16), 32, 32, DustID.Smoke, 0f, 0f, 100, default(Color), 4f);
                    Main.dust[dust].velocity -= Projectile.velocity * 2;
                    Main.dust[dust].velocity *= 3f;
                    Main.dust[dust].velocity += player.velocity / 2;
                }

                for (int i = 0; i < 50; i++)
                {
                    int dust = Dust.NewDust(dustPos - new Vector2(16, 16), 32, 32, DustID.Torch, 0f, 0f, 100, default(Color), 4f);
                    Main.dust[dust].scale *= Main.rand.NextFloat(1, 2.5f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity -= Projectile.velocity * 2;
                    Main.dust[dust].velocity = Main.dust[dust].velocity.RotatedByRandom(MathHelper.ToRadians(40)) * 6f;
                    Main.dust[dust].velocity *= Main.rand.NextFloat(1f, 3f);
                    Main.dust[dust].velocity += player.velocity / 2;
                    dust = Dust.NewDust(dustPos - new Vector2(16, 16), 32, 32, DustID.Torch, 0f, 0f, 100, default(Color), 4f);
                    Main.dust[dust].velocity -= Projectile.velocity * 2;
                    Main.dust[dust].velocity *= 5f;
                    Main.dust[dust].velocity *= Main.rand.NextFloat(1f, 2f);
                    Main.dust[dust].velocity += player.velocity / 2;
                }

                float scaleFactor9 = 2;
                for (int j = 0; j < 20; j++)
                {
                    int gore = Gore.NewGore(Projectile.GetSource_FromThis(), dustPos, -Projectile.velocity, Main.rand.Next(61, 64), scaleFactor9);
                    Main.gore[gore].velocity -= Projectile.velocity;
                    Main.gore[gore].velocity.Y += 2f;
                    Main.gore[gore].velocity *= 4f;
                    Main.gore[gore].velocity += player.velocity / 2;
                }
            }

            if (++Projectile.localAI[0] >= maxTime)
            {
                Projectile.Kill();
                return;
            }
            //Projectile.scale = num801;
            //float num804 = Projectile.velocity.ToRotation();
            //num804 += Projectile.ai[0];
            //Projectile.rotation = num804 - 1.57079637f;
            //float num804 = Main.npc[(int)Projectile.ai[1]].ai[3] - 1.57079637f;
            //if (Projectile.ai[0] != 0f) num804 -= (float)Math.PI;
            //Projectile.rotation = num804;
            //num804 += 1.57079637f;
            //Projectile.velocity = num804.ToRotationVector2();
            float amount = 0.5f;
            Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], 3000f, amount);
            /*Vector2 vector79 = Projectile.Center + Projectile.velocity * (Projectile.localAI[1] - 14f);
            for (int num809 = 0; num809 < 2; num809 = num3 + 1)
            {
                float num810 = Projectile.velocity.ToRotation() + ((Main.rand.Next(2) == 1) ? -1f : 1f) * 1.57079637f;
                float num811 = (float)Main.rand.NextDouble() * 2f + 2f;
                Vector2 vector80 = new Vector2((float)Math.Cos((double)num810) * num811, (float)Math.Sin((double)num810) * num811);
                int num812 = Dust.NewDust(vector79, 0, 0, 244, vector80.X, vector80.Y, 0, default(Color), 1f);
                Main.dust[num812].noGravity = true;
                Main.dust[num812].scale = 1.7f;
                num3 = num809;
            }
            if (Main.rand.NextBool(5))
            {
                Vector2 value29 = Projectile.velocity.RotatedBy(1.5707963705062866, default(Vector2)) * ((float)Main.rand.NextDouble() - 0.5f) * (float)Projectile.width;
                int num813 = Dust.NewDust(vector79 + value29 - Vector2.One * 4f, 8, 8, 244, 0f, 0f, 100, default(Color), 1.5f);
                Dust dust = Main.dust[num813];
                dust.velocity *= 0.5f;
                Main.dust[num813].velocity.Y = -Math.Abs(Main.dust[num813].velocity.Y);
            }*/
            //DelegateMethods.v3_1 = new Vector3(0.3f, 0.65f, 0.7f);
            //Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Projectile.localAI[1], (float)Projectile.width * Projectile.scale, new Utils.PerLinePoint(DelegateMethods.CastLight));

            Projectile.position -= Projectile.velocity;
            Projectile.rotation = Projectile.velocity.ToRotation() - 1.57079637f;

            const int increment = 75;
            for (int i = 0; i < 3000; i += increment)
            {
                float offset = i + Main.rand.NextFloat(-increment, increment);
                if (offset < 0)
                    offset = 0;
                if (offset > 3000)
                    offset = 3000;
                float spawnRange = Projectile.scale * 32f;
                int d = Dust.NewDust(Projectile.position + Projectile.velocity * offset + Projectile.velocity.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-spawnRange, spawnRange),
                    Projectile.width, Projectile.height, DustID.GemTopaz, 0f, 0f, 0, new Color(255, 255, 255, 150), Main.rand.NextFloat(2f, 4f));
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity += Projectile.velocity * 2f;
                Main.dust[d].velocity *= Main.rand.NextFloat(12f, 24f) / 10f * Projectile.scale;
            }
        }
    }
}

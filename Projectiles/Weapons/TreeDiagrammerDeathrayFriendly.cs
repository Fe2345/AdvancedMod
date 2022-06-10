using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System;
using Microsoft.Xna.Framework;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using AdvancedMod.Utils;

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
            target.AddBuff(BuffID.Electrified, AdvancedPlayer.ClestialCloak ? 20 : 10);
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

		public float GetPrismHue(float indexing)
		{
			string name;
			if (Main.player[Projectile.owner].active && (name = Main.player[Projectile.owner].name) != null)
			{
				switch (name)
				{
					case "Tsuki":
					case "Yoraiz0r":
						return 0.85f;
					case "Ghostar":
						return 0.33f;
					case "Devalaous":
						return 0.66f + (float)Math.Cos(Main.time / 180.0 * 6.2831854820251465) * 0.1f;
					case "Leinfors":
						return 0.77f;
					case "Aeroblop":
						return 0.25f + (float)Math.Cos(Main.time / 180.0 * 6.2831854820251465) * 0.1f;
					case "Doylee":
						return 0f;
					case "Darkhalis":
					case "Arkhalis":
						return 0.75f + (float)Math.Cos(Main.time / 180.0 * 6.2831854820251465) * 0.07f;
					case "Nike Leon":
						return 0.075f + (float)Math.Cos(Main.time / 180.0 * 6.2831854820251465) * 0.07f;
					case "Suweeka":
						return 0.5f + (float)Math.Cos(Main.time / 180.0 * 6.2831854820251465) * 0.18f;
					case "W1K":
						return 0.75f + (float)Math.Cos(Main.time / 120.0 * 6.2831854820251465) * 0.05f;
					case "Grox The Great":
						return 0.31f + (float)Math.Cos(Main.time / 120.0 * 6.2831854820251465) * 0.03f;
					case "Random":
						return Main.rand.NextFloat();
					case "bluemagic123":
					case "blushiemagic":
						return 0.55f + (float)Math.Cos(Main.time / 120.0 * 6.2831854820251465) * 0.1f;
				}
			}
			return (float)(int)indexing / 6f;
		}

		public override void AI()
        {
			Vector2? vector134 = null;

			if (Main.projectile[(int)Projectile.ai[1]].active && Main.projectile[(int)Projectile.ai[1]].type == 460)
			{
				Vector2 value26 = Vector2.Normalize(Main.projectile[(int)Projectile.ai[1]].velocity);
				Projectile.position = Main.projectile[(int)Projectile.ai[1]].Center + value26 * 16f - new Vector2(Projectile.width, Projectile.height) / 2f + new Vector2(0f, 0f - Main.projectile[(int)Projectile.ai[1]].gfxOffY);
				Projectile.velocity = Vector2.Normalize(Main.projectile[(int)Projectile.ai[1]].velocity);
				Projectile.velocity = Vector2.Normalize(Main.projectile[(int)Projectile.ai[1]].velocity);
			}
			if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
			{
				Projectile.velocity = -Vector2.UnitY;
			}
			Projectile.ai[0] += 1f;
			if (Projectile.ai[0] >= 300f)
			{
				Projectile.Kill();
				return;
			}
			Projectile.scale = (float)Math.Sin(Projectile.ai[0] * (float)Math.PI / 300f) * 10f;
			if (Projectile.scale > 1f)
			{
				Projectile.scale = 1f;
			}

			float num1089 = Projectile.velocity.ToRotation();
			Projectile.rotation = num1089 - (float)Math.PI / 2f;
			Projectile.velocity = num1089.ToRotationVector2();
			float num1090 = 0f;
			float num1093 = 0f;
			Vector2 samplingPoint = Projectile.Center;
			if (vector134.HasValue)
			{
				samplingPoint = vector134.Value;
			}

			float[] array3 = new float[(int)num1090];
			Collision.LaserScan(samplingPoint, Projectile.velocity, num1093 * Projectile.scale, 2400f, array3);
			float num1095 = 0f;
			for (int num1096 = 0; num1096 < array3.Length; num1096++)
			{
				num1095 += array3[num1096];
			}
			num1095 /= num1090;
			float amount = 0.5f;
			Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], num1095, amount);
			if (!(Math.Abs(Projectile.localAI[1] - num1095) < 100f) || !(Projectile.scale > 0.15f))
			{
				return;
			}
			float prismHue = GetPrismHue(Projectile.ai[0]);
			Color color = Main.hslToRgb(prismHue, 1f, 0.5f);
			color.A = 0;
			Vector2 vector144 = Projectile.Center + Projectile.velocity * (Projectile.localAI[1] - 14.5f * Projectile.scale);
			float x = Main.rgbToHsl(new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB)).X;
			for (int num1119 = 0; num1119 < 2; num1119++)
			{
				float num1120 = Projectile.velocity.ToRotation() + ((Main.rand.NextBool(2)) ? (-1f) : 1f) * ((float)Math.PI / 2f);
				float num1121 = (float)Main.rand.NextDouble() * 0.8f + 1f;
				Vector2 vector145 = new Vector2((float)Math.Cos(num1120) * num1121, (float)Math.Sin(num1120) * num1121);
				int num1122 = Dust.NewDust(vector144, 0, 0, DustID.RainbowMk2, vector145.X, vector145.Y);
				Main.dust[num1122].color = color;
				Main.dust[num1122].scale = 1.2f;
				if (Projectile.scale > 1f)
				{
					Dust dust43 = Main.dust[num1122];
					dust43.velocity *= Projectile.scale;
					dust43 = Main.dust[num1122];
					dust43.scale *= Projectile.scale;
				}
				Main.dust[num1122].noGravity = true;
				if (Projectile.scale != 1.4f)
				{
					Dust dust42 = Dust.CloneDust(num1122);
					dust42.color = Color.White;
					Dust dust43 = dust42;
					dust43.scale /= 2f;
				}
				float hue = (x + Main.rand.NextFloat() * 0.4f) % 1f;
				Main.dust[num1122].color = Color.Lerp(color, Main.hslToRgb(hue, 1f, 0.75f), Projectile.scale / 1.4f);
			}
			if (Main.rand.NextBool(5))
			{
				Vector2 value35 = Projectile.velocity.RotatedBy(1.5707963705062866) * ((float)Main.rand.NextDouble() - 0.5f) * Projectile.width;
				int num1123 = Dust.NewDust(vector144 + value35 - Vector2.One * 4f, 8, 8, DustID.Smoke, 0f, 0f, 100, default(Color), 1.5f);
				Dust dust43 = Main.dust[num1123];
				dust43.velocity *= 0.5f;
				Main.dust[num1123].velocity.Y = 0f - Math.Abs(Main.dust[num1123].velocity.Y);
			}
			DelegateMethods.v3_1 = color.ToVector3() * 0.3f;
			float value36 = 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 20f);
			Vector2 size = new Vector2(Projectile.velocity.Length() * Projectile.localAI[1], (float)Projectile.width * Projectile.scale);
			float num1124 = Projectile.velocity.ToRotation();
			if (Main.netMode != NetmodeID.Server)
			{
				((WaterShaderData)Filters.Scene["WaterDistortion"].GetShader()).QueueRipple(Projectile.position + new Vector2(size.X * 0.5f, 0f).RotatedBy(num1124), new Color(0.5f, 0.1f * (float)Math.Sign(value36) + 0.5f, 0f, 1f) * Math.Abs(value36), size, RippleShape.Square, num1124);
			}
			Terraria.Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Projectile.localAI[1], (float)Projectile.width * Projectile.scale, DelegateMethods.CastLight);
			Vector2 vector140 = Projectile.Center + Projectile.velocity * (Projectile.localAI[1] - 8f);
			for (int num1108 = 0; num1108 < 2; num1108++)
			{
				float num1109 = Projectile.velocity.ToRotation() + ((Main.rand.NextBool(2)) ? (-1f) : 1f) * ((float)Math.PI / 2f);
				float num1110 = (float)Main.rand.NextDouble() * 0.8f + 1f;
				Vector2 vector141 = new Vector2((float)Math.Cos(num1109) * num1110, (float)Math.Sin(num1109) * num1110);
				int num1111 = Dust.NewDust(vector140, 0, 0, DustID.Electric, vector141.X, vector141.Y);
				Main.dust[num1111].noGravity = true;
				Main.dust[num1111].scale = 1.2f;
			}
			if (Main.rand.NextBool(5))
			{
				Vector2 value32 = Projectile.velocity.RotatedBy(1.5707963705062866) * ((float)Main.rand.NextDouble() - 0.5f) * Projectile.width;
				int num1112 = Dust.NewDust(vector140 + value32 - Vector2.One * 4f, 8, 8, DustID.Smoke, 0f, 0f, 100, default(Color), 1.5f);
				Dust dust43 = Main.dust[num1112];
				dust43.velocity *= 0.5f;
				Main.dust[num1112].velocity.Y = 0f - Math.Abs(Main.dust[num1112].velocity.Y);
			}
			DelegateMethods.v3_1 = new Vector3(0.4f, 0.85f, 0.9f);
			Terraria.Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Projectile.localAI[1], (float)Projectile.width * Projectile.scale, DelegateMethods.CastLight);
		}
	}
}

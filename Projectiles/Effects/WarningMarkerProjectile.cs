using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Projectiles.Effects
{
    public sealed class WarningMarkerProjectile : ModProjectile
    {
        private const int WarningDuration = 60;

        public override string Texture => $"{Mod.Name}/Assets/Textures/Effects/Warning";

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = WarningDuration;
            Projectile.aiStyle = -1;
            Projectile.hide = true;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            int elapsed = WarningDuration - Projectile.timeLeft;
            float progress = elapsed / (float)WarningDuration;

            Projectile.rotation += 0.08f + progress * 0.12f;
        }


        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

            int elapsed = WarningDuration - Projectile.timeLeft;
            float progress = elapsed / (float)WarningDuration;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            float pulse = 1f + 0.08f * MathF.Sin(Main.GlobalTimeWrappedHourly * 18f);
            float scale = MathHelper.Lerp(1.7f, 0.85f, progress) * pulse;

            float alpha = MathHelper.Lerp(0.25f, 0.9f, progress);

            if (progress > 0.75f)
            {
                float flash = MathF.Sin(Main.GlobalTimeWrappedHourly * 50f);
                alpha *= flash > 0f ? 1f : 0.45f;
            }

            Color color = Color.Lerp(Color.OrangeRed, Color.Red, progress) * alpha;

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                color,
                Projectile.rotation,
                origin,
                scale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}
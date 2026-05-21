using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Projectiles.NPCs
{
    public class LaserGridController : ModProjectile
    {
        public const int GridLineCount = 11;
        public const int HalfGridLineCount = GridLineCount / 2;

        public const int MaxOffsetTiles = 5;

        public const int LineSpacingTiles = 4;

        public const int TileSize = 16;
        public const int TelegraphTime = 300;
        public const int LaserLifetime = 180;

        public const float LaserSpeed = 18f;

        // 激光从阵列外侧进入，避免一生成就贴在目标身上。
        public const int EdgePaddingTiles = 8;

        private bool initialized;
        private bool fired;

        private int TargetPlayerIndex => (int)Projectile.ai[0];
        private int LaserDamage => (int)Projectile.ai[1];

        public override string Texture => "Terraria/Images/Projectile_0";

        public static void Spawn(IEntitySource source, Player targetPlayer, int damage, int owner = 255)
        {
            if (!targetPlayer.active || targetPlayer.dead)
            {
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Projectile.NewProjectile(
                source,
                targetPlayer.Center,
                Vector2.Zero,
                ModContent.ProjectileType<LaserGridController>(),
                0,
                0f,
                owner,
                targetPlayer.whoAmI,
                damage
            );
        }

        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TelegraphTime + 30;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void AI()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (!initialized)
            {
                initialized = true;
                InitializeGridCenter();
                SpawnTelegraphLines();
                Projectile.netUpdate = true;
            }

            Projectile.localAI[0]++;

            if (!fired && Projectile.localAI[0] >= TelegraphTime)
            {
                fired = true;
                FireEyeLaserGrid();
                SoundEngine.PlaySound(SoundID.Item33, Projectile.Center);
            }
        }

        private void InitializeGridCenter()
        {
            Player targetPlayer = Main.player[TargetPlayerIndex];

            int offsetX = Main.rand.Next(-MaxOffsetTiles, MaxOffsetTiles + 1);
            int offsetY = Main.rand.Next(-MaxOffsetTiles, MaxOffsetTiles + 1);

            Vector2 tileOffset = new Vector2(offsetX, offsetY) * TileSize;
            Projectile.Center = targetPlayer.Center + tileOffset;
        }

        private void SpawnTelegraphLines()
        {
            float spacing = LineSpacingTiles * TileSize;
            float lineLength = (GridLineCount - 1) * spacing + TileSize;

            for (int i = -HalfGridLineCount; i <= HalfGridLineCount; i++)
            {
                float offset = i * spacing;

                Vector2 rowCenter = Projectile.Center + new Vector2(0f, offset);
                SpawnWarningLine(rowCenter, LaserGridWarningLine.Horizontal, lineLength);

                Vector2 columnCenter = Projectile.Center + new Vector2(offset, 0f);
                SpawnWarningLine(columnCenter, LaserGridWarningLine.Vertical, lineLength);
            }
        }

        private void SpawnWarningLine(Vector2 center, float directionType, float lineLength)
        {
            int index = Projectile.NewProjectile(
                Projectile.GetSource_FromAI(),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<LaserGridWarningLine>(),
                0,
                0f,
                Projectile.owner,
                directionType,
                lineLength
            );

            Projectile warning = Main.projectile[index];
            warning.timeLeft = TelegraphTime;
            warning.netUpdate = true;
        }

        private void FireEyeLaserGrid()
        {
            float spacing = LineSpacingTiles * TileSize;
            float halfGridSize = HalfGridLineCount * spacing;
            float edgePadding = EdgePaddingTiles * TileSize;

            for (int i = -HalfGridLineCount; i <= HalfGridLineCount; i++)
            {
                float offset = i * spacing;

                Vector2 leftStart = new Vector2(
                    Projectile.Center.X - halfGridSize - edgePadding,
                    Projectile.Center.Y + offset
                );

                SpawnEyeLaser(leftStart, Vector2.UnitX);

                Vector2 topStart = new Vector2(
                    Projectile.Center.X + offset,
                    Projectile.Center.Y - halfGridSize - edgePadding
                );

                SpawnEyeLaser(topStart, Vector2.UnitY);
            }
        }

        private void SpawnEyeLaser(Vector2 position, Vector2 direction)
        {
            Projectile laser = Projectile.NewProjectileDirect(
                Projectile.GetSource_FromAI(),
                position,
                direction * LaserSpeed,
                ProjectileID.EyeLaser,
                LaserDamage,
                0f,
                Projectile.owner
            );

            laser.hostile = true;
            laser.friendly = false;
            laser.tileCollide = false;
            laser.ignoreWater = true;
            laser.timeLeft = LaserLifetime;
            laser.netUpdate = true;
        }
    }

    public class LaserGridWarningLine : ModProjectile
    {
        public const float Horizontal = 0f;
        public const float Vertical = 1f;

        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LaserGridController.TelegraphTime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.hide = true;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            bool isHorizontal = Math.Abs(Projectile.ai[0] - Horizontal) < 0.01f;
            float length = Projectile.ai[1];

            float progress = 1f - Projectile.timeLeft / (float)LaserGridController.TelegraphTime;
            float pulse = 0.65f + 0.35f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12f);

            Color warningColor = new Color(255, 90, 90) * (0.35f + 0.35f * progress) * pulse;

            Vector2 center = Projectile.Center - Main.screenPosition;

            Vector2 wideScale = isHorizontal
                ? new Vector2(length, 18f)
                : new Vector2(18f, length);

            Vector2 middleScale = isHorizontal
                ? new Vector2(length, 9f)
                : new Vector2(9f, length);

            Vector2 coreScale = isHorizontal
                ? new Vector2(length, 3f)
                : new Vector2(3f, length);

            Rectangle source = new Rectangle(0, 0, 1, 1);
            Vector2 origin = new Vector2(0.5f, 0.5f);

            Main.EntitySpriteDraw(
                pixel,
                center,
                source,
                warningColor * 0.18f,
                0f,
                origin,
                wideScale,
                SpriteEffects.None,
                0
            );

            Main.EntitySpriteDraw(
                pixel,
                center,
                source,
                warningColor * 0.32f,
                0f,
                origin,
                middleScale,
                SpriteEffects.None,
                0
            );

            Main.EntitySpriteDraw(
                pixel,
                center,
                source,
                warningColor * 0.75f,
                0f,
                origin,
                coreScale,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}

using AdvancedMod.DamageClasses.Element;
using AdvancedMod.DamageClasses.Element.Definitions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Projectiles.Weapons.Element
{
    /// <summary>
    /// 元素瓶触发的协同攻击光球。
    /// </summary>
    public sealed class AdvancedElementOrb : ModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.HallowStar}";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<ElementDamage>();
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.rotation += 0.2f * Projectile.direction;
            Lighting.AddLight(Projectile.Center, ResolveLightColor().ToVector3() * 0.65f);

            int dustType = ResolveDustType();
            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType, Projectile.velocity * -0.15f, 120, ResolveLightColor(), 1f);
                dust.noGravity = true;
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.Lerp(Color.White, ResolveLightColor(), 0.75f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = frame.Size() / 2f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                float opacity = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Color color = ResolveLightColor() * (0.35f * opacity);
                Main.EntitySpriteDraw(texture, drawPosition, frame, color, Projectile.rotation, origin, Projectile.scale * (0.9f - i * 0.04f), SpriteEffects.None);
            }

            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ElementCombatSystem.ProcessProjectileHit(Projectile, target, hit, damageDone, ResolveElement(), ResolveBuildUp(), AttackFlags.SuppressCoordinatedAttack);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            ElementCombatSystem.ModifyProjectileHit(Projectile, target, ref modifiers, ResolveElement(), ResolveBuildUp(), AttackFlags.SuppressCoordinatedAttack);
        }

        private ElementType ResolveElement()
        {
            return (ElementType)(int)Projectile.ai[0];
        }

        private int ResolveBuildUp()
        {
            return (int)MathHelper.Clamp(Projectile.ai[1], 1f, 999f);
        }

        private Color ResolveLightColor()
        {
            return ResolveElement() switch
            {
                ElementType.Thunder => new Color(160, 120, 255),
                ElementType.Wind => new Color(120, 255, 220),
                ElementType.Pyro => new Color(255, 170, 80),
                _ => Color.White,
            };
        }

        private int ResolveDustType()
        {
            return ResolveElement() switch
            {
                ElementType.Thunder => DustID.Electric,
                ElementType.Wind => DustID.GemEmerald,
                ElementType.Pyro => DustID.Torch,
                _ => DustID.GoldFlame,
            };
        }
    }
}

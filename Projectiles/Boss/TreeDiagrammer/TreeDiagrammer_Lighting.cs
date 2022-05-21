using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Projectiles.Boss.TreeDiagrammer
{
    public class TreeDiagrammer_Lighting : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("树状图设计者闪电");
        }

        public override string Texture => "Terraria/Images/Projectile_466";

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 20;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 600;
            Projectile.aiStyle = 88;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuff.ElectromagneticInduction>(), 300);
        }
    }
}

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

        public override void SetDefaults()
        {
            projectile.width = 28;
            projectile.height = 20;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.penetrate = 3;
            projectile.timeLeft = 600;
            //projectile.aiStyle = 0;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuff.ElectromagneticInduction>(), 300);
            projectile.Kill();
        }

        public override void AI()
        {

        }
    }
}

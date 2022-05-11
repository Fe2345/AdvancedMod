using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System;

namespace AdvancedMod.Projectiles.Boss.TreeDiagrammer
{
    public class TreeDiagrammer_Laser : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("树状图设计者激光");
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 600;
            //Projectile.aiStyle = 0;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuff.ElectromagneticInduction>(),300);
            Projectile.Kill();
        }

        public override void AI()
        {
            Projectile.rotation = (float)(Math.PI / 2 - Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X));
        }
    }
}

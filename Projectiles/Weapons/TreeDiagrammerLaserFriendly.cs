﻿using Terraria;
using Terraria.ModLoader;
using System;

namespace AdvancedMod.Projectiles.Weapons
{
    public class TreeDiagrammerLaserFriendly : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("树状图设计者激光");
        }

        public override string Texture => "Terraria/Images/Projectile_100";

        public override void SetDefaults()
        {
            Projectile.width = 5;
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.timeLeft = 200;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuff.ElectromagneticInduction>(), 10);
        }

        public override void AI()
        {
            Projectile.rotation = (float)(Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + Math.PI / 2);
        }
    }
}

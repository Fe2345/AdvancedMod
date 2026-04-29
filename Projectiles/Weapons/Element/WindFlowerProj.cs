using AdvancedMod.Buffs.DoT;
using AdvancedMod.Buffs.DoT.Data;
using AdvancedMod.Utils;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Projectiles.Weapons.DoT
{
    public class WindFlowerProj : ModProjectile
    {
        public override string Texture => "AdvancedMod/Assets/Textures/Projectiles/Weapon/DoT/WindFlowerProj";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20; // 调整为更合理的碰撞箱
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<global::AdvancedMod.DamageClasses.Element.ElementDamage>();
            Projectile.penetrate = 1; // 击中一次即销毁
            Projectile.timeLeft = 600;
            Projectile.tileCollide = true; // 碰到物块销毁（不可穿透物块）
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            var dotInstance = new WeatheringDoTData()
            {
                Behavior = Systems.DataStructures.DoTData.DoTBehavior.Default,
                BasicDMG = 0f,
                Attack = 1f,
                Multiplier = 1f,
                Stacks = 1,
                MaxStack = 30 // 层数上限30
            };

            target.Advanced().AddOrUpdateDoT(target, ModContent.BuffType<Weathering>(), 1800, dotInstance); // 持续1800帧（30秒）
        }

        public override void AI()
        {
            // 朝向始终指向速度方向
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }
    }
}

using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System;

namespace AdvancedMod.Projectiles
{
    public class PhantomLight_Projectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("幻影之光射线");
        }

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 600;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 120;
        }
        public override void AI()
        {
            
            // 如果玩家仍然在控制弹幕
            if (Main.player[Projectile.owner].channel)
            {
                // 获取弹幕持有者
                Player player = Main.player[Projectile.owner];
                // 从玩家到达鼠标位置的单位向量
                Vector2 unit = Vector2.Normalize(Main.MouseWorld - player.Center);
                // 随机角度
                float rotaion = unit.ToRotation();
                // 调整玩家转向以及手持物品的转动方向
                player.direction = Main.MouseWorld.X < player.Center.X ? -1 : 1;
                player.itemRotation = (float)Math.Atan2(rotaion.ToRotationVector2().Y * player.direction,
                    rotaion.ToRotationVector2().X * player.direction);
                // 玩家保持物品使用动画
                player.itemTime = 2;
                player.itemAnimation = 2;
                // 从弹幕到达鼠标位置的单位向量
                Vector2 unit2 = Vector2.Normalize(Main.MouseWorld - Projectile.Center);
                // 让弹幕缓慢朝鼠标方向移动
                //Projectile.velocity = unit2 * 5;
            }
            else
            {
                // 如果玩家放弃吟唱就慢慢消失
                if (Projectile.timeLeft > 30)
                    Projectile.timeLeft = 30;
                // 返回函数这样就不会执行下面的攻击代码
                return;
            }
        }
    }
}

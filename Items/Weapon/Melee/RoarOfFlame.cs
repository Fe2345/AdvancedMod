using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Items.Weapon.Melee
{
    public class RoarOfFlame : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("魔焰之怒");
            Tooltip.SetDefault("魔焰之神的力量结晶");
        }

        public override void SetDefaults()
        {
            item.useStyle = ItemUseStyleID.SwingThrow;  //枪支或法杖
            item.damage = 600;   //伤害
            item.useAnimation = 20; //使用动画时长
            item.useTime = 20;   //攻速
            item.knockBack = 9;  //击退
            item.width = 30;     //大小
            item.height = 30;    //大小
            item.scale = 1.25f;  //碰撞箱
            item.rare = ItemRarityID.Red;       //稀有度
            item.value = Item.sellPrice(gold: 44);
            item.crit = 30;       //暴击率
            item.autoReuse = true;   //自动挥舞
            item.useTurn = true;      //使用中可转身
            item.melee = true;        //魔法武器
            item.shootSpeed = 10f;    //射速
            item.channel = true;      //有特殊行为

            item.shoot = ModContent.ProjectileType<Projectiles.RoarOfFlame_Projectile>();
        }

        public override Vector2? HoldoutOrigin()
        {
            return new Vector2(1, 1);
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            target.AddBuff(BuffID.Bleeding, 10);
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            /*
			 ref 引用传递 传递实参
			 Vector2 表示一个具有两个单精度浮点值的向量。
			 */
            Vector2 target = Main.screenPosition + new Vector2((float)Main.mouseX, (float)Main.mouseY);
            /*
			 target 是一个坐标变量
			 Main.screenPosition 是屏幕的位置，在屏幕的左上角
			 new Vector2 是构造一个Vector2类型的变量
			 是让屏幕我坐标加上鼠标的坐标，所以target就是在屏幕中鼠标的位置
			 */
            float ceilingLimit = target.Y;
            if (ceilingLimit > player.Center.Y - 200f)
            {
                ceilingLimit = player.Center.Y - 200f;
            }
            for (int i = 0; i < 5; i++)//循环三次 代表一次挥动就生成三个弹幕
            {
                position = player.Center + new Vector2((-(float)Main.rand.Next(0, 401) * player.direction), -600f);//随机角度
                position.Y -= (100 * i);//塑造层次感
                Vector2 heading = target - position;
                if (heading.Y < 0f)
                {
                    heading.Y *= -1f;
                }
                if (heading.Y < 20f)
                {
                    heading.Y = 20f;
                }
                heading.Normalize();
                heading *= new Vector2(speedX, speedY).Length();
                speedX = heading.X;
                speedY = heading.Y + Main.rand.Next(-40, 41) * 0.02f;//随机下落速度
                Projectile.NewProjectile(position.X, position.Y, speedX, speedY, type, damage * 2, knockBack, player.whoAmI, 0f, ceilingLimit);
            }
            return false;
        }
    }
}

using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;

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
            Item.useStyle = ItemUseStyleID.Swing;  //枪支或法杖
            Item.damage = 600;   //伤害
            Item.useAnimation = 20; //使用动画时长
            Item.useTime = 20;   //攻速
            Item.knockBack = 9;  //击退
            Item.width = 30;     //大小
            Item.height = 30;    //大小
            Item.scale = 1.25f;  //碰撞箱
            Item.rare = ItemRarityID.Red;       //稀有度
            Item.value = Item.sellPrice(gold: 44);
            Item.crit = 30;       //暴击率
            Item.autoReuse = true;   //自动挥舞
            Item.useTurn = true;      //使用中可转身
            Item.DamageType = DamageClass.Melee;        //近战武器
            Item.shootSpeed = 10f;    //射速
            Item.channel = true;      //有特殊行为

            Item.shoot = ModContent.ProjectileType<Projectiles.RoarOfFlame_Projectile>();
        }

        public override Vector2? HoldoutOrigin()
        {
            return new Vector2(1, 1);
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            target.AddBuff(BuffID.Bleeding, 10);
        }

        //public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
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
                heading *= velocity.Length();
                velocity.X = heading.X;
                velocity.Y = heading.Y + Main.rand.Next(-40, 41) * 0.02f;//随机下落速度
                Projectile.NewProjectile(source,position.X, position.Y, velocity.X, velocity.Y, type, damage * 2, knockback, player.whoAmI, 0f, ceilingLimit);
            }
            return false;
        }
    }
}

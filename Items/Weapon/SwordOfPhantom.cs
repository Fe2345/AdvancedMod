using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System;
using AdvancedMod.Utils;

namespace AdvancedMod.Items.Weapon
{
    public class SwordOfPhantom : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("幻影刃");
            Tooltip.SetDefault("万物的幻影");
        }

        public override void SetDefaults()
        {
            item.useStyle = ItemUseStyleID.SwingThrow;  //枪支或法杖
            item.damage = 1000;   //伤害
            item.useAnimation = 10; //使用动画时长
            item.useTime = 10;   //攻速
            item.knockBack = 10;  //击退
            item.width = 30;     //大小
            item.height = 30;    //大小
            item.scale = 1.25f;  //碰撞箱
            item.rare = ItemRarityID.Red;       //稀有度
            item.value = Item.sellPrice(silver: 44);
            item.crit = 50;       //暴击率
            item.autoReuse = true;   //自动挥舞
            item.useTurn = true;      //使用中可转身
            item.melee = true;        //近战武器
            item.shootSpeed = 10f;    //射速
            item.channel = true;      //有特殊行为


            item.shoot = ModContent.ProjectileType<Projectiles.SwordOfPhantom_Projectile>();
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            for (int i = 0;i < 6; i++)
            {
                Vector2 speed = new Vector2(Main.screenPosition.X + Main.mouseX - player.Center.X, Main.screenPosition.X + Main.mouseY - player.Center.Y);
                //Vector2 shootVel = (Math.Atan2(Main.mouseX - player.Center.X,  Main.mouseY - player.Center.Y) + (i * Math.PI / 6)).ToRotationVector2() * 10;
                Vector2 shootVel = Tool.TurnVector(speed, (float)(i * Math.PI / 3));
                Projectile.NewProjectile(position,Vector2.Normalize(shootVel)*10,item.shoot,damage,knockBack,player.whoAmI,speed.X,speed.Y);
            }
            

            return false;
        }
    }
}

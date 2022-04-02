using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using AdvancedMod.Projectiles;
using Microsoft.Xna.Framework;
using System;

namespace AdvancedMod.Items.Weapon
{
    public class PhantomLight : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("幻影之光");
            Tooltip.SetDefault("幻影之神的力量结晶");
        }

        public override void SetDefaults()
        {
            item.useStyle = ItemUseStyleID.HoldingOut;  //枪支或法杖
            item.damage = 500;   //伤害
            item.useAnimation = 5; //使用动画时长
            item.useTime = 5;   //攻速
            item.knockBack = 10;  //击退
            item.width = 30;     //大小
            item.height = 30;    //大小
            item.scale = 1.25f;  //碰撞箱
            item.rare = ItemRarityID.Expert;       //稀有度
            item.value = Item.sellPrice(gold: 44);
            item.crit = 33;       //暴击率
            item.autoReuse = true;   //自动挥舞
            item.useTurn = true;      //使用中可转身
            item.magic = true;        //魔法武器
            item.shootSpeed = 10f;    //射速
            item.mana = 15;           //耗蓝
            item.channel = true;      //有特殊行为
            item.noMelee = true;      //无近战伤害


            item.shoot = ModContent.ProjectileType<PhantomLight_Projectile>();
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            Vector2 PlrToMouse = Main.MouseWorld - player.Center;
            float r = (float)Math.Atan2(PlrToMouse.Y,PlrToMouse.X);

            for (int i = -30;i < 30; i++)
            {
                float r2 = r + i * MathHelper.Pi / 90f;
                Vector2 shootVel = r2.ToRotationVector2() * 10;
                Projectile.NewProjectile(position, shootVel, type, 100, 10, player.whoAmI);
            }
            
            return true;
        }
    }
}

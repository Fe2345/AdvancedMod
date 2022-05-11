using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using AdvancedMod.Projectiles;
using Microsoft.Xna.Framework;
using System;
using Terraria.DataStructures;

namespace AdvancedMod.Items.Weapon.Magic
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
            Item.useStyle = ItemUseStyleID.Shoot;  //枪支或法杖
            Item.damage = 500;   //伤害
            Item.useAnimation = 5; //使用动画时长
            Item.useTime = 5;   //攻速
            Item.knockBack = 10;  //击退
            Item.width = 30;     //大小
            Item.height = 30;    //大小
            Item.scale = 1.25f;  //碰撞箱
            Item.rare = ItemRarityID.Expert;       //稀有度
            Item.value = Item.sellPrice(gold: 44);
            Item.crit = 33;       //暴击率
            Item.autoReuse = true;   //自动挥舞
            Item.useTurn = true;      //使用中可转身
            Item.DamageType = DamageClass.Magic;       //魔法武器
            Item.shootSpeed = 10f;    //射速
            Item.mana = 15;           //耗蓝
            Item.channel = true;      //有特殊行为
            Item.noMelee = true;      //无近战伤害


            Item.shoot = ModContent.ProjectileType<PhantomLight_Projectile>();
        }


        //public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 PlrToMouse = Main.MouseWorld - player.Center;
            float r = (float)Math.Atan2(PlrToMouse.Y,PlrToMouse.X);

            for (int i = -30;i < 30; i++)
            {
                float r2 = r + i * MathHelper.Pi / 90f;
                Vector2 shootVel = r2.ToRotationVector2() * 10;
                Projectile.NewProjectile(source,position, shootVel, type, 100, 10, player.whoAmI);
            }
            
            return true;
        }
    }
}

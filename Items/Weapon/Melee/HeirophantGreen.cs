using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using AdvancedMod.Utils;
using System;

namespace AdvancedMod.Items.Weapon.Melee
{
    public class HeirophantGreen : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("法皇之绿");
            Tooltip.SetDefault("左右键拥有不同的攻击方式");
        }

        public override void SetDefaults()
        {
            item.useStyle = ItemUseStyleID.SwingThrow;  //近战武器
            item.damage = 100;   //伤害
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
            item.melee = true;        //近战武器
            item.shootSpeed = 10f;    //射速
            item.channel = true;      //有特殊行为

            item.shoot = ModContent.ProjectileType<Projectiles.HeirophantGreen_Projectile>();
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            if (player.altFunctionUse == 2)
            {
                //右键
                for (int i = 0;i < 10; i++)
                {

                }
            }
            else
            {
                //左键
                for (int i = 0;i < 10; i++)
                {
                    Vector2 vel = Tool.TurnVector(Vector2.One, Main.rand.NextFloat((float)Math.PI * 2));
                    Projectile.NewProjectile(player.Center, Vector2.Normalize(vel) * 6, item.shoot, damage, knockBack, player.whoAmI, speedX, speedY); 
                }
            }

            return true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.ChlorophyteBar, 12);
            recipe.AddTile(ModContent.TileType<Tiles.AdvancedCraftTable>());
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}

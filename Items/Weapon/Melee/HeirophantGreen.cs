     using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using AdvancedMod.Utils;
using System;
using Terraria.DataStructures;

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
            Item.useStyle = ItemUseStyleID.Swing;  //近战武器
            Item.damage = 100;   //伤害
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
            Item.DamageType = DamageClass.Melee;     //近战武器
            Item.shootSpeed = 10f;    //射速
            Item.channel = true;      //有特殊行为

            Item.shoot = ModContent.ProjectileType<Projectiles.HeirophantGreen_Projectile>();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                //右键
                for (int i = 0; i < 10; i++)
                {

                }
            }
            else
            {
                //左键
                for (int i = 0; i < 10; i++)
                {
                    Vector2 vel = Tool.TurnVector(Vector2.One, Main.rand.NextFloat((float)Math.PI * 2));
                    Projectile.NewProjectile(source,player.Center, Vector2.Normalize(vel) * 6, Item.shoot, damage, knockback, player.whoAmI,vel.X,vel.Y);
                }
            }

            return true;
        }
        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.ChlorophyteBar, 12)
            .AddTile(ModContent.TileType<Tiles.ElectromagneticWorkStation>())
            .Register();
    }
}

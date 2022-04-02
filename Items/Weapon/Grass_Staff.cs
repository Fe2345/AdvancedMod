using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using AdvancedMod.Projectiles;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Items.Weapon
{
    public class Grass_Staff : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("草杖");
            Tooltip.SetDefault("用魔法发射丛林孢子");
        }

        public override void SetDefaults()
        {
            item.useStyle = ItemUseStyleID.HoldingOut;  //枪支或法杖
            item.damage = 47;   //伤害
            item.useAnimation = 20; //使用动画时长
            item.useTime = 30;   //攻速
            item.knockBack = 4;  //击退
            item.width = 30;     //大小
            item.height = 30;    //大小
            item.scale = 1.25f;  //碰撞箱
            item.rare = ItemRarityID.Pink;       //稀有度
            item.value = Item.sellPrice(silver: 44);
            item.crit = 15;       //暴击率
            item.autoReuse = true;   //自动挥舞
            item.useTurn = true;      //使用中可转身
            item.magic = true;        //魔法武器
            item.shootSpeed = 10f;    //射速
            item.mana = 10;           //耗蓝
            item.channel = true;      //有特殊行为
            item.noMelee = true;      //无近战伤害
            

            item.shoot = ModContent.ProjectileType<Grass_Staff_Projectiles>();
        }

        public override Vector2? HoldoutOrigin()
        {
            return new Vector2(1, 1);
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            target.AddBuff(BuffID.Bleeding, 10);
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.JungleSpores, 44);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

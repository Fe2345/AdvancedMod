using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory
{
    public class Core_Of_Life : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("生存之心");
            Tooltip.SetDefault("极大提升你的生存能力\n所有伤害提高到原来100倍\n生命上限提高到1000\n移速极大升高");
        }

        public override void SetDefaults()
        {
            item.width = 42;
            item.height = 42;
            item.accessory = true;
            item.rare = ItemRarityID.Red;
            item.value = Item.sellPrice(platinum: 11);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.magicDamage += 99.0f;
            player.rangedDamage += 99.0f;
            player.meleeDamage += 99.0f;
            player.minionDamage += 99.0f;
            player.statLifeMax2 += 1500;
            player.moveSpeed = 1.6f;
            item.defense = 100;
            item.expert = true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.LunarBar, 100);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 1;
            ascentWhenRising = 1;
            maxAscentMultiplier = 1;
            constantAscend = 1;
        }
        // 控制翅膀水平飞行的参数
        public override void HorizontalWingSpeeds(Player player, ref float speed, ref float acceleration)
        {
            speed = 60;
            acceleration = 10;
        }
    }
}

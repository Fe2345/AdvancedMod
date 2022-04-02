using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory
{
    public class Proof_Of_Storm : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("暴风之证");
            Tooltip.SetDefault("极大提升你的生存能力\n移动速度提高至150mph\n暴击率极大升高");
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
            player.meleeCrit += 30;
            player.rangedCrit += 30;
            player.magicCrit += 30;
            player.maxRunSpeed = 150;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.LunarBar, 100);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

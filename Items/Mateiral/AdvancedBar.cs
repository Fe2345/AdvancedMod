using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Mateiral
{
    public class AdvancedBar : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("先进锭");
            Tooltip.SetDefault("\"AdvancedMod的精华\"");
        }

        public override void SetDefaults()
        {
            Item.value = Item.sellPrice(platinum: 10);
            Item.rare = ItemRarityID.Orange;
            Item.maxStack = 999;
        }

        public override void AddRecipes()
        {

        }
    }
}
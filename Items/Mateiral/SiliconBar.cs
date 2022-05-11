using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Mateiral
{
    public class SiliconBar : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("精硅锭");
            Tooltip.SetDefault("\"树状图设计者的精华\"");
        }

        public override void SetDefaults()
        {
            Item.value = Item.sellPrice(platinum: 1);
            Item.rare = ItemRarityID.Orange;
            Item.maxStack = 999;
        }

        public override void AddRecipes()
        {

        }
    }
}

using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Mateiral
{
    public class Disable_Bar : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("禁忌锭");
            Tooltip.SetDefault("\"你不该拥有它\"");
        }

        public override void SetDefaults()
        {
            item.value = Item.sellPrice(platinum:44);
            item.rare = ItemRarityID.Red;
            item.maxStack = 999;
        }

        public override void AddRecipes()
        {
        
        }
    }
}

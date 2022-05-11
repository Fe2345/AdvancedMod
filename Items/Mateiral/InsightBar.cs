using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Mateiral
{
    public class InsightBar : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("洞察锭");
            Tooltip.SetDefault("\"魔眼之神的精华\"");
        }

        public override void SetDefaults()
        {
            Item.value = Item.sellPrice(platinum: 5);
            Item.rare = ItemRarityID.Orange;
            Item.maxStack = 999;
        }

        public override void AddRecipes()
        {

        }
    }
}

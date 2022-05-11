using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Mateiral
{
    public class TimePieces : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("时间碎块");
            Tooltip.SetDefault("\"被时间支配者斩碎的时间\"");
        }

        public override void SetDefaults()
        {
            Item.value = Item.sellPrice(gold:77);
            Item.rare = ItemRarityID.Expert;
            Item.maxStack = 999;
        }

        public override void AddRecipes()
        {

        }
    }
}

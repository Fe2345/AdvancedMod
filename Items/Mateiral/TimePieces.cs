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
            item.value = Item.sellPrice(gold:77);
            item.rare = ItemRarityID.Expert;
            item.maxStack = 999;
        }

        public override void AddRecipes()
        {

        }
    }
}

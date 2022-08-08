using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Mount
{
    public class FishOutOfWater : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("离水之鱼");
            Tooltip.SetDefault("召唤一个可骑乘的猪龙鱼公爵\n\"骑上小怪\"");
        }

        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: 1);
            Item.mountType = ModContent.MountType<FishOutOfWaterMount>();
        }
    }
}

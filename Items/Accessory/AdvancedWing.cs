using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory
{
    [AutoloadEquip(EquipType.Wings)]
    public class AdvancedWing : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("先进之翼");
            Tooltip.SetDefault("允许你无限飞行");
        }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 42;
            Item.height = 42;
            Item.rare = ItemRarityID.Expert;
            Item.value = Item.sellPrice(platinum: 11);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.wingTimeMax = 180;
            player.wingTime = player.wingTimeMax;
            Item.expert = true;
        }

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
        {
            ascentWhenFalling = 1;
            ascentWhenRising = 1;
            maxAscentMultiplier = 1;
        }

        public override void HorizontalWingSpeeds(Player player, ref float speed, ref float acceleration)
        {
            speed = 80;
            acceleration = 40;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ModContent.ItemType<Items.Mateiral.InsightBar>(), 5)
            .AddIngredient(ModContent.ItemType<Items.Mateiral.TimePieces>(), 1)
            .AddTile(ModContent.TileType<Tiles.ElectromagneticWorkStation>())
            .Register();
    }
}

using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Armor
{
    [AutoloadEquip(EquipType.Legs)]
    public class SiliconGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("精硅护胫");
            Tooltip.SetDefault("增加10%伤害减免");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.sellPrice(gold: 5);
            Item.defense = 12;
        }

        public override void UpdateEquip(Player player)
        {
            player.endurance += 0.1f;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ModContent.ItemType<Items.Mateiral.SiliconBar>(), 12)
            .AddTile(ModContent.TileType<Tiles.ElectromagneticWorkStation>())
            .Register();
    }
}

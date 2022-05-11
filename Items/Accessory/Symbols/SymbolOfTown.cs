using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Symbols
{
    public class SymbolOfTown : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("城镇之象");
            Tooltip.SetDefault("有五分之一概率将你所受伤害由任意城镇NPC承受");
        }

        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.accessory = true;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            AdvancedPlayer.SymbolOfTown = true;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.RedHat)
            .AddIngredient(ItemID.DyeTradersScimitar)
            .AddIngredient(ItemID.StylistKilLaKillScissorsIWish)
            .AddIngredient(ModContent.ItemType<Items.Weapon.Ranged.RabbitBomb>())
            .AddTile(TileID.Anvils)
            .Register();
    }
}

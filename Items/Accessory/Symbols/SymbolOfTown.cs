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
            item.width = 42;
            item.height = 42;
            item.accessory = true;
            item.rare = ItemRarityID.Green;
            item.value = Item.sellPrice(gold: 1);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            AdvancedPlayer.SymbolOfTown = true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.RedHat);
            recipe.AddIngredient(ItemID.DyeTradersScimitar);
            recipe.AddIngredient(ItemID.StylistKilLaKillScissorsIWish);
            recipe.AddIngredient(ModContent.ItemType<Items.Weapon.Thrown.RabbitBomb>());
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AdvancedMod.Utils;

namespace AdvancedMod.Items.Accessory
{
    /// <summary>
    /// Forces all prototype element input to wind so the input reinterpretation stage can be validated in game.
    /// </summary>
    public sealed class PrototypeWindEnchantAccessory : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.Shackle}";

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.accessory = true;
            Item.value = Item.buyPrice(silver: 30);
            Item.rare = ItemRarityID.Green;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.Advanced().PrototypeForceWindElement = true;
        }
    }
}

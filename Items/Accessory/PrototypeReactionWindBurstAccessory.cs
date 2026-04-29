using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AdvancedMod.Utils;

namespace AdvancedMod.Items.Accessory
{
    /// <summary>
    /// Adds one extra 10000 wind damage hit to every successful prototype reaction.
    /// </summary>
    public sealed class PrototypeReactionWindBurstAccessory : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.BandofRegeneration}";

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
            player.Advanced().PrototypeReactionExtraWindBurst = true;
        }
    }
}

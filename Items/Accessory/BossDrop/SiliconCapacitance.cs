using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.BossDrop
{
    public class SiliconCapacitance : ModItem
    {
        public override string Texture => "AdvancedMod/Assets/Textures/Items/SiliconCapacitance";

        public override void SetStaticDefaults()
        {
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
            if (hideVisual)
            {
                AdvancedPlayer.SiliconCapacotance = false;
            }
        }
    }
}

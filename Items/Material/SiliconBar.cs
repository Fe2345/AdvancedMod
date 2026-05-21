using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Material
{
    public class SiliconBar : ModItem
    {
	    public override string Texture => $"{Mod.Name}/Assets/Textures/Items/Material/SiliconBar";

        public override void SetDefaults()
        {
            Item.value = Item.sellPrice(platinum: 1);
            Item.rare = ItemRarityID.Orange;
            Item.maxStack = 999;
        }

        public override void AddRecipes()
        {

        }
    }
}

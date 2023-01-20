using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Curser
{
    public class PowerQuiver : ModItem
    {
        public override string Texture => "AdvancedMod/Assets/Textures/Items/PowerQuiver";

        public override void SetStaticDefaults()
        {
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
            player.GetDamage(DamageClass.Ranged) += 0.1f;
            player.GetDamage<DamageClasses.CurseDamage>() += 0.1f;
            player.ammoCost80 = true;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.MagicQuiver)
            .AddIngredient(ItemID.EndlessQuiver)
            .AddTile<Tiles.ElectromagneticWorkStation>()
            .Register();
    }
}

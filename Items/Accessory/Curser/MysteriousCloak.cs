using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Curser
{
    public class MysteriousCloak : ModItem
    {
        public override string Texture => "AdvancedMod/Assets/Textures/Items/MysteriousCloak";

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
            player.GetDamage<DamageClasses.CurseDamage>() += 0.2f;
            player.ammoCost75 = true;
            player.manaCost -= 0.2f;
            player.manaRegen = (int)(player.manaRegen * 1.2f);
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient<PowerQuiver>()
            .AddIngredient<ManaCloak>()
            .AddTile<Tiles.ElectromagneticWorkStation>()
            .Register();
    }
}

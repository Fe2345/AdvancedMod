using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Souls
{
    public class CurserSoul : ModItem
    {
        public override string Texture => "AdvancedMod/Assets/Textures/Items/CurserSoul";

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red ;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage<DamageClasses.CurseDamage>() += 0.66f;
            player.ammoCost75 = true;
            player.manaCost -= 0.5f;
            player.ammoBox = true;
            player.manaFlower = true;
            player.manaRegen = (int)(player.manaRegen * 1.5f);
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient<Items.Weapon.Curse.Laser>()
            .AddIngredient<Items.Accessory.Curser.CelestialCloak>()
            .AddTile<Tiles.ElectromagneticWorkStation>()
            .Register();
    }
}

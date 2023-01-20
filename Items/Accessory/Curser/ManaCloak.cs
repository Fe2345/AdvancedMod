using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Curser
{
    public class ManaCloak : ModItem
    {
        public override string Texture => "AdvancedMod/Assets/Textures/Items/ManaCloak";

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
            player.GetDamage(DamageClass.Magic) += 0.1f;
            player.GetDamage<DamageClasses.CurseDamage>() += 0.1f;
            player.manaCost -= 0.2f;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.ManaCrystal, 2)
            .AddIngredient(ItemID.Silk, 10)
            .AddTile(TileID.Anvils)
            .Register();
    }
}

using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Souls
{
    public class SorcererSoul : ModItem
    {
        public override string Texture => "AdvancedMod/Assets/Textures/Items/SorcererSoul";

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Magic) += 0.66f;
            player.manaCost -= 0.75f;
            player.manaFlower = true;
            //天界手铐！
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.DiamondStaff)
            .AddIngredient(ItemID.SpaceGun)
            .AddIngredient(ItemID.WaterBolt)
            .AddIngredient(ItemID.CrystalSerpent)
            .AddIngredient(ItemID.RainbowRod)
            .AddIngredient(ItemID.RazorbladeTyphoon)
            .AddIngredient(ItemID.LastPrism)
            .AddIngredient(ItemID.CelestialEmblem)
            .AddTile<Tiles.ElectromagneticWorkStation>()
            .Register();
    }
}

using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Souls
{
    public class SorcererSoul : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("巫士之魂");
            Tooltip.SetDefault("增加66%魔法伤害\n减少75%魔力消耗\n增加50%魔力回复\n附带魔力花和天界手铐效果");
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

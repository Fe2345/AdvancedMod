using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Curser
{
    public class CelestialCloak : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("天界斗篷");
            Tooltip.SetDefault("+33%诅咒与远程伤害\n减少25%弹药消耗\n减少25%魔力消耗\n增加25%魔力回复\n诅咒武器造成的减益时长增加一倍");
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
            player.GetDamage<DamageClasses.CurseDamage>() += 0.33f;
            player.ammoCost75 = true;
            player.manaCost -= 0.25f;
            player.manaRegen = (int)(player.manaRegen * 1.25f);
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient<Items.Accessory.Curser.MysteriousCloak>()
            .AddIngredient(ItemID.AvengerEmblem)
            .AddTile<Tiles.ElectromagneticWorkStation>()
            .Register();
    }
}

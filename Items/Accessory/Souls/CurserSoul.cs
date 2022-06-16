using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Souls
{
    public class CurserSoul : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("诅咒师之魂");
            Tooltip.SetDefault("增加66%诅咒伤害\n减少25%弹药消耗和50%魔力消耗\n增加50%魔力回复\n附带魔力花、弹药箱、天界斗篷效果");
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

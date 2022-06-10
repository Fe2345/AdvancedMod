using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Curser
{
    public class MysteriousCloak : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("神秘斗篷");
            Tooltip.SetDefault("+20%诅咒伤害\n减少25%弹药消耗\n减少20%魔力消耗\n增加20%魔力回复");
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

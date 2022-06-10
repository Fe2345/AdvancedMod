using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Curser
{
    public class ManaCloak : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("魔力斗篷");
            Tooltip.SetDefault("+10%诅咒与魔法伤害\n减少20%魔力消耗");
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

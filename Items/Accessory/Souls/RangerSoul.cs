using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Souls
{
    public class RangerSoul : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("游侠之魂");
            Tooltip.SetDefault("增加66%远程伤害\n减少25%弹药消耗\n附带狙击镜和弹药箱效果");
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
            player.GetDamage(DamageClass.Ranged) += 0.66f;
            player.ammoCost75 = true;
            player.ammoBox = true;
            player.scope = true;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.Minishark)
            .AddIngredient(ItemID.Boomstick)
            .AddIngredient(ItemID.Megashark)
            .AddIngredient(ItemID.VortexBeater)
            .AddIngredient(ItemID.SDMG)
            .AddIngredient(ItemID.Tsunami)
            .AddIngredient(ItemID.Celeb2)
            .AddIngredient(ItemID.SuperStarCannon)
            .AddTile<Tiles.ElectromagneticWorkStation>()
            .Register();
    }
}

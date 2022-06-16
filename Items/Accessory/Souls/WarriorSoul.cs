using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Souls
{
    public class WarriorSoul : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("战士之魂");
            Tooltip.SetDefault("增加66%近战伤害\n增加25%挥舞速度\n为所有近战武器启用自动挥舞\n附带烈火手套和悠悠球袋的效果");
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
            player.GetDamage(DamageClass.Melee) += 0.66f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.25f;
            player.GetKnockback(DamageClass.Melee) += 0.20f;
            player.autoReuseGlove = true;
            player.yoyoGlove = true;
            player.yoyoString = true;
            player.counterWeight = 1;
            player.magmaStone = true;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.CopperShortsword)
            .AddIngredient(ItemID.Starfury)
            .AddIngredient(ItemID.TerraBlade)
            .AddIngredient(ItemID.TheEyeOfCthulhu)
            .AddIngredient(ItemID.Terrarian)
            .AddIngredient(ItemID.NorthPole)
            .AddIngredient(ItemID.DayBreak)
            .AddIngredient(ItemID.PaladinsHammer)
            .AddIngredient(ItemID.Flairon)
            .AddIngredient(ItemID.ShadowFlameKnife)
            .AddIngredient(ItemID.FireGauntlet)
            .AddIngredient(ItemID.YoyoBag)
            .AddTile<Tiles.ElectromagneticWorkStation>()
            .Register();
    }
}

using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Souls
{
    public class SoulOfOverworld : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("人间之魂");
            Tooltip.SetDefault("+15防御力\n+15%伤害\n+15%移速\n+15%暴击率\n+80最大生命值\n在地表增益为以上所述三倍");
        }

        public override void SetDefaults()
        {
            item.width = 42;
            item.height = 42;
            item.accessory = true;
            item.rare = ItemRarityID.Green;
            item.value = Item.sellPrice(gold: 1);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!player.ZoneOverworldHeight)
            {
                player.magicDamage += 0.15f;
                player.rangedDamage += 0.15f;
                player.meleeDamage += 0.15f;
                player.minionDamage += 0.15f;
                player.magicCrit += 15;
                player.meleeCrit += 15;
                player.rangedCrit += 15;
                player.moveSpeed = 1.15f;
                item.defense = 15;
                player.statLifeMax2 += 80;
            }
            else
            {
                player.magicDamage += 0.50f;
                player.rangedDamage += 0.50f;
                player.meleeDamage += 0.50f;
                player.minionDamage += 0.50f;
                player.magicCrit += 50;
                player.meleeCrit += 50;
                player.rangedCrit += 50;
                player.moveSpeed = 1.5f;
                item.defense = 50;
                player.statLifeMax2 += 250;
            }
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<SoulOfForest>(),1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfDesert>(),1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfIceland>(),1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfJungle>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfOcean>(), 1);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

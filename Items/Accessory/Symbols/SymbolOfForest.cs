using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Symbols
{
    public class SymbolOfForest : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("森林之符");
            Tooltip.SetDefault("+3防御力\n+3%伤害\n+3%移速\n+3%暴击率\n在森林中增益为上述三倍");
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
            if ((!player.ZoneOverworldHeight) || player.ZoneJungle || player.ZoneDesert || player.ZoneCrimson || player.ZoneCorrupt || player.ZoneSnow || player.ZoneDungeon || player.ZoneHoly)
            {
                player.magicDamage += 0.03f;
                player.rangedDamage += 0.03f;
                player.meleeDamage += 0.03f;
                player.minionDamage += 0.03f;
                player.magicCrit += 3;
                player.meleeCrit += 3;
                player.rangedCrit += 3;
                player.moveSpeed = 1.03f;
                item.defense = 3;
            }
            else
            {
                player.magicDamage += 0.1f;
                player.rangedDamage += 0.1f;
                player.meleeDamage += 0.1f;
                player.minionDamage += 0.1f;
                player.magicCrit += 10;
                player.meleeCrit += 10;
                player.rangedCrit += 10;
                player.moveSpeed = 1.1f;
                item.defense = 10;
            }
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Wood, 44);
            recipe.AddIngredient(ItemID.Gel, 10);
            recipe.AddIngredient(ItemID.Bunny, 3);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

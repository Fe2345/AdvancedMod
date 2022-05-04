﻿using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Souls
{
    public class SoulOfJungle : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("丛林之魂");
            Tooltip.SetDefault("+3防御力\n+3%伤害\n+3%移速\n+3%暴击率\n在丛林中增益为上述三倍");
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
            if (!player.ZoneJungle) 
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
            recipe.AddIngredient(ItemID.RichMahogany, 44);
            recipe.AddIngredient(ItemID.Vine, 10);
            recipe.AddIngredient(ItemID.Frog, 3);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
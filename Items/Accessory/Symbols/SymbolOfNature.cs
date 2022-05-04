using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Symbols
{
    public class SymbolOfNature : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("自然之象");
            Tooltip.SetDefault("+100防御力\n+100%伤害\n+60%移速\n+100%暴击率\n+500最大生命值\n减免25%伤害");
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
            if (!Main.hardMode)
            {
                item.defense = 10;
            }
            else if (NPC.downedMechBossAny && !NPC.downedPlantBoss)
            {
                player.moveSpeed = 1.2f;
                item.defense = 25;
                player.statLifeMax2 += 100;
            }
            else if (NPC.downedPlantBoss && !NPC.downedMoonlord)
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
            else if (NPC.downedMoonlord)
            {
                player.magicDamage += 1.0f;
                player.rangedDamage += 1.0f;
                player.meleeDamage += 1.0f;
                player.minionDamage += 1.0f;
                player.magicCrit += 100;
                player.meleeCrit += 100;
                player.rangedCrit += 100;
                player.moveSpeed = 1.6f;
                item.defense = 100;
                player.statLifeMax2 += 500;
                player.lifeRegen += 40;
                player.endurance = 0.25f;
            }
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<Accessory.Souls.SoulOfOverworld>(), 1);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

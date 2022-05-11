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
            Item.width = 42;
            Item.height = 42;
            Item.accessory = true;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!Main.hardMode)
            {
                Item.defense = 10;
            }
            else if (NPC.downedMechBossAny && !NPC.downedPlantBoss)
            {
                player.moveSpeed = 1.2f;
                Item.defense = 25;
                player.statLifeMax2 += 100;
            }
            else if (NPC.downedPlantBoss && !NPC.downedMoonlord)
            {
                player.GetDamage(DamageClass.Melee) += 0.50f;
                player.GetDamage(DamageClass.Ranged) += 0.50f;
                player.GetDamage(DamageClass.Magic) += 0.50f;
                player.GetDamage(DamageClass.Summon) += 0.50f;
                player.GetCritChance(DamageClass.Melee) += 50;
                player.GetCritChance(DamageClass.Ranged) += 50;
                player.GetCritChance(DamageClass.Magic) += 50;
                player.moveSpeed = 1.5f;
                Item.defense = 50;
                player.statLifeMax2 += 250;
            } 
            else if (NPC.downedMoonlord)
            {
                player.GetDamage(DamageClass.Melee) += 1f;
                player.GetDamage(DamageClass.Ranged) += 1f;
                player.GetDamage(DamageClass.Magic) += 1f;
                player.GetDamage(DamageClass.Summon) += 1f;
                player.GetCritChance(DamageClass.Melee) += 100;
                player.GetCritChance(DamageClass.Ranged) += 100;
                player.GetCritChance(DamageClass.Magic) += 100;
                player.moveSpeed = 1.6f;
                Item.defense = 100;
                player.statLifeMax2 += 500;
                player.lifeRegen += 40;
                player.endurance = 0.25f;
            }
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ModContent.ItemType<Items.Accessory.Souls.SoulOfOverworld>())
            .AddTile(TileID.Anvils)
            .Register();
    }
}

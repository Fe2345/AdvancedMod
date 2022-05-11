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
            Item.width = 42;
            Item.height = 42;
            Item.accessory = true;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (!player.ZoneOverworldHeight)
            {
                player.GetDamage(DamageClass.Melee) += 0.15f;
                player.GetDamage(DamageClass.Ranged) += 0.15f;
                player.GetDamage(DamageClass.Magic) += 0.15f;
                player.GetDamage(DamageClass.Summon) += 0.15f;
                player.GetCritChance(DamageClass.Melee) += 15;
                player.GetCritChance(DamageClass.Ranged) += 15;
                player.GetCritChance(DamageClass.Magic) += 15;
                player.moveSpeed = 1.15f;
                Item.defense = 15;
                player.statLifeMax2 += 80;
            }
            else
            {
                player.GetDamage(DamageClass.Melee) += 0.5f;
                player.GetDamage(DamageClass.Ranged) += 0.5f;
                player.GetDamage(DamageClass.Magic) += 0.5f;
                player.GetDamage(DamageClass.Summon) += 0.5f;
                player.GetCritChance(DamageClass.Melee) += 50;
                player.GetCritChance(DamageClass.Ranged) += 50;
                player.GetCritChance(DamageClass.Magic) += 50;
                player.moveSpeed = 1.5f;
                Item.defense = 50;
                player.statLifeMax2 += 250;
            }
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ModContent.ItemType<SoulOfForest>(), 1)
            .AddIngredient(ModContent.ItemType<SoulOfDesert>(), 1)
            .AddIngredient(ModContent.ItemType<SoulOfIceland>(), 1)
            .AddIngredient(ModContent.ItemType<SoulOfJungle>(), 1)
            .AddIngredient(ModContent.ItemType<SoulOfOcean>(), 1)
            .AddTile(TileID.Anvils)
            .Register();
    }
}

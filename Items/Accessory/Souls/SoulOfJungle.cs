using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Souls
{
    public class SoulOfJungle : ModItem
    {
        public override string Texture => "AdvancedMod/Assets/Textures/Items/SoulOfJungle";

        public override void SetStaticDefaults()
        {
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
            if (!player.ZoneJungle) 
            {
                player.GetDamage(DamageClass.Melee) += 0.03f;
                player.GetDamage(DamageClass.Ranged) += 0.03f;
                player.GetDamage(DamageClass.Magic) += 0.03f;
                player.GetDamage(DamageClass.Summon) += 0.03f;
                player.GetCritChance(DamageClass.Melee) += 3;
                player.GetCritChance(DamageClass.Ranged) += 3;
                player.GetCritChance(DamageClass.Magic) += 3;
                player.moveSpeed = 1.03f;
                Item.defense = 3;
            }
            else
            {
                player.GetDamage(DamageClass.Melee) += 0.1f;
                player.GetDamage(DamageClass.Ranged) += 0.1f;
                player.GetDamage(DamageClass.Magic) += 0.1f;
                player.GetDamage(DamageClass.Summon) += 0.1f;
                player.GetCritChance(DamageClass.Melee) += 10;
                player.GetCritChance(DamageClass.Ranged) += 10;
                player.GetCritChance(DamageClass.Magic) += 10;
                player.moveSpeed = 1.1f;
                Item.defense = 10;
            }
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.RichMahogany, 44)
            .AddIngredient(ItemID.Vine, 10)
            .AddIngredient(ItemID.Frog, 3)
            .AddTile(TileID.Anvils)
            .Register();
    }
}

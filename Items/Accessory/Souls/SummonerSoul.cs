using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Accessory.Souls
{
    public class SummonerSoul : ModItem
    {
        public override string Texture => "AdvancedMod/Assets/Textures/Items/SummonerSoul";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("召唤师之魂");
            Tooltip.SetDefault("增加66%召唤伤害\n增加50%鞭子速度和长度\n+12召唤位");
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
            player.GetDamage(DamageClass.Summon) += 0.66f;
            player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) += 0.5f;
            player.maxMinions += 12;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.HornetStaff)
            .AddIngredient(ItemID.ImpStaff)
            .AddIngredient(ItemID.SanguineStaff)
            .AddIngredient(ItemID.StardustDragonStaff)
            .AddIngredient(ItemID.EmpressBlade)
            .AddIngredient(ItemID.BlandWhip)
            .AddIngredient(ItemID.ThornWhip)
            .AddIngredient(ItemID.BoneWhip)
            .AddIngredient(ItemID.FireWhip)
            .AddIngredient(ItemID.SwordWhip)
            .AddIngredient(ItemID.RainbowWhip)
            .AddIngredient(ItemID.PapyrusScarab)
            .AddTile<Tiles.ElectromagneticWorkStation>()
            .Register();
    }
}

using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Armor
{
    [AutoloadEquip(EquipType.Head)]
    public  class SiliconHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("精硅头盔");
            Tooltip.SetDefault("增加20%伤害\n减少20%弹药消耗\n增加100点最大魔力\n减少20%魔力消耗\n增加4个仆从召唤位");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.sellPrice(gold:5);
            Item.defense = 20;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += 0.2f;
            player.GetDamage(DamageClass.Ranged) += 0.2f;
            player.GetDamage(DamageClass.Magic) += 0.2f;
            player.GetDamage(DamageClass.Summon) += 0.2f;
            player.GetDamage(ModContent.GetInstance<DamageClasses.CurseDamage>()) += 0.2f;
            player.maxMinions += 4;
            player.manaCost -= 0.2f;
            player.ammoCost80 = true;
            player.statManaMax2 += 100;

        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ModContent.ItemType<Items.Mateiral.SiliconBar>(), 18)
            .AddTile(ModContent.TileType<Tiles.ElectromagneticWorkStation>())
            .Register();
    }
}

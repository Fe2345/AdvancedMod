using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using AdvancedMod.Items.Mateiral;

namespace AdvancedMod.Items.Weapon.Melee
{
    public class Gift_Of_Dungon : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("地牢的馈赠");
            Tooltip.SetDefault("一击秒杀地牢守卫");
        }

        public override void SetDefaults()
        {
            item.useStyle = ItemUseStyleID.Stabbing;
            item.damage = 114514;
            item.crit = 100;
            item.autoReuse = true;
            item.useTurn = true;
            item.width = 60;
            item.height = 60;
            item.scale = 1.25f;
            item.useAnimation = 10;
            item.useTime = 10;

            item.value = Item.sellPrice(platinum: 45);
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Bone, 44);
            recipe.AddIngredient(ModContent.ItemType<Disable_Bar>(), 1);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

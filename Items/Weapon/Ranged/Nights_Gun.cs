using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Weapon.Ranged
{
    public class Nights_Gun : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("永夜枪");
            Tooltip.SetDefault("45%几率不消耗弹药");
        }

        public override void SetDefaults()
        {
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.autoReuse = true;
            item.useTurn = true;
            item.ranged = true;
            item.noMelee = true;

            item.damage = 20;
            item.crit = 24;
            item.rare = ItemRarityID.Orange;
            item.value = Item.sellPrice(gold: 44);
            item.shoot = ProjectileID.Bullet;
            item.shootSpeed = 10f;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Minishark);
            recipe.AddIngredient(ItemID.Boomstick);
            recipe.AddIngredient(ItemID.Shotgun);
            recipe.AddIngredient(ItemID.FlintlockPistol);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

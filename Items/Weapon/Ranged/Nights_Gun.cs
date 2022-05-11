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
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.DamageType = DamageClass.Ranged;
            Item.noMelee = true;

            Item.damage = 20;
            Item.crit = 24;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 44);
            Item.shoot = ProjectileID.Bullet;
            Item.shootSpeed = 10f;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.Minishark)
            .AddIngredient(ItemID.Boomstick)
            .AddIngredient(ItemID.Shotgun)
            .AddIngredient(ItemID.FlintlockPistol)
            .AddTile(TileID.Anvils)
            .Register();
    }
}

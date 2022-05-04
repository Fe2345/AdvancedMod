using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Items.Weapon.Thrown
{
    public class RabbitBomb : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("兔兔炸弹");
            Tooltip.SetDefault("你可真是个坏东西");
        }

        public override void SetDefaults()
        {
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.autoReuse = false;
            item.useTurn = false;
            item.thrown = true;
            item.noMelee = true;
            item.damage = 60;
            item.crit = 24;
            item.rare = ItemRarityID.Orange;
            item.value = Item.sellPrice(gold: 44);
            item.shoot = ModContent.ProjectileType<Projectiles.RabbitBomb_Projectile>();
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {
            Vector2 speed = new Vector2(speedX, speedY);
            Projectile.NewProjectile(player.Center, 4 * speed / speed.Length(), item.shoot, damage, knockBack, player.whoAmI, speedX, speedY);

            return false;
        }
    }
}

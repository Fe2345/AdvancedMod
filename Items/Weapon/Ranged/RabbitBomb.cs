using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;

namespace AdvancedMod.Items.Weapon.Ranged
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
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = false;
            Item.useTurn = false;
            Item.DamageType = DamageClass.Ranged;
            Item.noMelee = true;
            Item.damage = 60;
            Item.crit = 24;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 44);
            Item.shoot = ModContent.ProjectileType<Projectiles.RabbitBomb_Projectile>();
        }

        //public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source,player.Center, 4 * velocity / velocity.Length(), Item.shoot, damage, knockback, player.whoAmI, velocity.X, velocity.Y);

            return false;
        }
    }
}

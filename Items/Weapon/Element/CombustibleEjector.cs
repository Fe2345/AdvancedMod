using AdvancedMod.DamageClasses.Element;
using AdvancedMod.Projectiles.Weapons.Element;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Items.Weapon.Element
{
    /// <summary>
    /// 可燃物发射器，使用凝胶发射火属性弹幕。
    /// </summary>
    public sealed class CombustibleEjector : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.GrenadeLauncher}";

        public override void SetDefaults()
        {
            Item.damage = 20;
            Item.DamageType = ModContent.GetInstance<ElementDamage>();
            Item.width = 44;
            Item.height = 20;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 4f;
            Item.value = Item.buyPrice(silver: 80);
            Item.rare = ItemRarityID.Green;
            Item.UseSound = SoundID.Item34;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BurnedGel>();
            Item.shootSpeed = 10f;
            Item.useAmmo = AmmoID.Gel;
            Item.consumeAmmoOnFirstShotOnly = false;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (velocity.LengthSquared() > 0f)
            {
                position += Vector2.Normalize(velocity) * 60f;
            }
        }
    }
}

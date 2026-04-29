using AdvancedMod.DamageClasses.Element;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Items.Weapon.Magic
{
    /// <summary>
    /// Minimal projectile staff prototype used to validate projectile-side element hits.
    /// </summary>
    public sealed class PrototypeThunderStaff : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.AmethystStaff}";

        public override void SetDefaults()
        {
            Item.damage = 18;
            Item.DamageType = ModContent.GetInstance<ElementDamage>();
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.mana = 5;
            Item.noMelee = true;
            Item.knockBack = 3f;
            Item.value = Item.buyPrice(silver: 80);
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item20;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.Weapons.PrototypeThunderBolt>();
            Item.shootSpeed = 10f;
            Item.staff[Type] = true;
        }
    }
}

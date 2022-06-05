using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Weapon.Melee
{
    public class Surge : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("电涌");
            Tooltip.SetDefault("\"和胶体没有任何关系\"");
        }

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.width = 24;
            Item.height = 24;
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item1;
            Item.DamageType = DamageClass.Melee;
            Item.channel = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.Weapons.SurgeYoyo>();
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.shootSpeed = 16f;
            Item.knockBack = 2.5f;
            Item.damage = 80;

            Item.value = Item.sellPrice(0, 25);
            Item.rare = ItemRarityID.Purple;
        }
    }
}

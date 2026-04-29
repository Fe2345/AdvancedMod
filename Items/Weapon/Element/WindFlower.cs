using Terraria.ModLoader;

namespace AdvancedMod.Items.Weapon.Element
{
    public class WindFlower : ModItem
    {
        public override string Texture => "AdvancedMod/Assets/Textures/Items/Weapon/DoT/WindFlower";

        public override void SetDefaults()
        {
            Item.damage = 1;
            Item.DamageType = ModContent.GetInstance<global::AdvancedMod.DamageClasses.Element.ElementDamage>();

            Item.mana = 6;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.useStyle = Terraria.ID.ItemUseStyleID.Shoot; // 像法杖一样举起
            Item.noMelee = true; // 不造成近战伤害
            Item.knockBack = 3f;
            Item.value = Terraria.Item.sellPrice(0, 1, 0, 0);
            Item.rare = Terraria.ID.ItemRarityID.Green;
            Item.UseSound = Terraria.ID.SoundID.Item43;
            Item.autoReuse = true; // 自动连续使用

            // 发射物
            Item.shoot = ModContent.ProjectileType<Projectiles.Weapons.DoT.WindFlowerProj>();
            Item.shootSpeed = 10f; // 发射速度
        } 
    }
}

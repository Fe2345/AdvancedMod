using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Weapon.Summon
{
    public class DiagrammerController : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("设计者遥控器");
            Tooltip.SetDefault("召唤迷你树状图设计者为你而战");
        }

        public override void SetDefaults()
        {
            Item.height = 32;
            Item.width = 32;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Lime;
            Item.damage = 100;
            Item.value = Item.buyPrice(0, 54, 0, 0);
            Item.noMelee = true;
            Item.useTime = 30;
            Item.knockBack = 1f;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.mana = 10;
            Item.crit = 10;
            Item.staff[Item.type] = true;
            Item.UseSound = SoundID.Item44;
            Item.DamageType = DamageClass.Summon;
            //Item.buffType = ModContent.BuffType<GliderBuff>();
            //Item.buffTime = 3600;
            Item.shoot = ModContent.ProjectileType<Projectiles.Weapons.MiniDiagrammerProj>();
            Item.shootSpeed = 10f;
        }
    }
}

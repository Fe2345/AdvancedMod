using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Items.Weapon.Curse
{
    public class SwordOfWar : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("战乱之剑");
            Tooltip.SetDefault("\"开发者之力归你所有\"\n左右键拥有不同的攻击方式\n开发者物品");
        }

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Swing;  //枪支或法杖
            Item.damage = 50731;   //伤害
            Item.useAnimation = 20; //使用动画时长
            Item.useTime = 20;   //攻速
            Item.knockBack = 9;  //击退
            Item.width = 30;     //大小
            Item.height = 30;    //大小
            Item.scale = 1.25f;  //碰撞箱
            Item.rare = ItemRarityID.Master;      //稀有度
            Item.value = Item.sellPrice(gold: 44);
            Item.autoReuse = true;   //自动挥舞
            Item.useTurn = true;      //使用中可转身
            Item.DamageType = ModContent.GetInstance<DamageClasses.CurseDamage>();     //诅咒武器
            Item.shootSpeed = 10f;    //射速
            Item.channel = true;      //有特殊行为
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                Item.shoot = ModContent.ProjectileType<Projectiles.Weapons.SwordOfWarBlade>();
                Projectile.NewProjectile(source,position, velocity*3, type, damage, knockback);
            }
            else
            {
                Item.shoot = ModContent.ProjectileType<Projectiles.Weapons.CursedBlade>();
                Projectile.NewProjectile(source,position,velocity,type,damage,knockback);
            }

            return false;
        }
    }
}

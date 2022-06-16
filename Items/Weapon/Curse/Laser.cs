using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using System;

namespace AdvancedMod.Items.Weapon.Curse
{
    public class Laser : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("激光器");
            Tooltip.SetDefault("\"释放树状图设计者真正的力量\"\n左右键具有不同的攻击方式");
        }

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.HoldUp;  //枪支或法杖
            Item.damage = 80;   //伤害
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

        public override bool? UseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.mana = 10;
                Item.ammo = 0;
                Item.shoot = ModContent.ProjectileType<Projectiles.Weapons.TreeDiagrammerDeathrayFriendly>();
            }
            else
            {
                Item.mana = 0;
                Item.ammo = AmmoID.Bullet;
                Item.shoot = ModContent.ProjectileType<Projectiles.Weapons.TreeDiagrammerLaserFriendly>();
            }

            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                Projectile.NewProjectile(source, position, velocity, type, Item.damage, knockback);
            }
            else
            {
                for (int i = -2; i < 3; i++)
                {
                    Projectile.NewProjectile(source, position, Utils.Tool.TurnVector(velocity,(float)Math.PI/12 * i), type, Item.damage, knockback);
                }
            }

            return false;
        }
    }
}

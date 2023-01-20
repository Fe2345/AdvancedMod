using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using AdvancedMod.Utils;
using System;

namespace AdvancedMod.Items.Weapon.Magic
{
    public class StreamerSingleRotaryGun : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("流光单旋机炮");
            Tooltip.SetDefault("如流星雨一般绚丽地划过天空");
        }

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Shoot;  //枪支或法杖
            Item.damage = 300;   //伤害
            Item.useAnimation = 2; //使用动画时长
            Item.useTime = 2;   //攻速
            Item.knockBack = 10;  //击退
            Item.width = 30;     //大小
            Item.height = 30;    //大小
            Item.scale = 1.25f;  //碰撞箱
            Item.rare = ItemRarityID.Expert;       //稀有度
            Item.value = Item.sellPrice(gold: 44);
            Item.crit = 7;       //暴击率
            Item.autoReuse = true;   //自动挥舞
            Item.useTurn = true;      //使用中可转身
            Item.DamageType = DamageClass.Magic;       //魔法武器
            Item.shootSpeed = 10f;    //射速
            Item.mana = 15;           //耗蓝
            Item.channel = true;      //有特殊行为
            Item.noMelee = true;      //无近战伤害

            Item.shoot = ModContent.ProjectileType<Projectiles.Weapons.StreamerSingleRotaryGunProj>();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            for (int i = 0;i < 3; i++)
            {
                Vector2 shootVel = Tool.TurnVector(velocity, (float)((-15+Main.rand.Next(30))*Math.PI/180));
                Projectile.NewProjectile(source, position, Vector2.Normalize(shootVel) * 250,type, damage, knockback, player.whoAmI, velocity.X, velocity.Y);
            }

            return true;
        }
    }
}

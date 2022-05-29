using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System;
using AdvancedMod.Utils;
using Terraria.DataStructures;

namespace AdvancedMod.Items.Weapon.Melee
{
    public class SwordOfPhantom : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("幻影刃");
            Tooltip.SetDefault("万物的幻影");
        }

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Swing;  //枪支或法杖
            Item.damage = 3000;   //伤害
            Item.useAnimation = 10; //使用动画时长
            Item.useTime = 5;   //攻速
            Item.knockBack = 10;  //击退
            Item.width = 30;     //大小
            Item.height = 30;    //大小
            Item.scale = 1.25f;  //碰撞箱
            Item.rare = ItemRarityID.Red;       //稀有度
            Item.value = Item.sellPrice(silver: 44);
            Item.crit = 50;       //暴击率
            Item.autoReuse = true;   //自动挥舞
            Item.useTurn = true;      //使用中可转身
            Item.DamageType = DamageClass.Melee;        //近战武器
            Item.shootSpeed = 10f;    //射速
            Item.channel = true;      //有特殊行为


            Item.shoot = ModContent.ProjectileType<Projectiles.SwordOfPhantom_Projectile>();
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            Mod fargoSouls = ModLoader.GetMod("FargowiltasSouls");
            if (fargoSouls == null)
            {
                target.AddBuff(ModContent.BuffType<Buffs.Debuff.TheWorld>(),5);
            }
        }

        //public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            for (int i = 0;i < 6; i++)
            {
                //Vector2 speed = new Vector2(Main.screenPosition.X + Main.mouseX - player.Center.X, Main.screenPosition.X + Main.mouseY - player.Center.Y);
                //Vector2 shootVel = (Math.Atan2(Main.mouseX - player.Center.X,  Main.mouseY - player.Center.Y) + (i * Math.PI / 6)).ToRotationVector2() * 10;
                Vector2 shootVel = Tool.TurnVector(velocity, (float)(i * 2* Math.PI / 3));
                Projectile.NewProjectile(source,position,Vector2.Normalize(shootVel)*20,Item.shoot,damage,knockback,player.whoAmI,velocity.X,velocity.Y);
            }
            return false;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.BeeKeeper)
            .AddIngredient(ItemID.BreakerBlade)
            .AddIngredient(ItemID.Seedler)
            .AddIngredient(ItemID.Meowmere)
            .AddIngredient(ModContent.ItemType<Items.Weapon.Melee.RoarOfFlame>())
            .AddTile(ModContent.TileType<Tiles.ElectromagneticWorkStation>())
            .Register();
    }
}

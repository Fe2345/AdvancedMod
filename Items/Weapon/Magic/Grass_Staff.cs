using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using AdvancedMod.Projectiles;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Items.Weapon.Magic
{
    public class Grass_Staff : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("草杖");
            Tooltip.SetDefault("用魔法发射丛林孢子");
        }

        public override void SetDefaults()
        {
            Item.useStyle = ItemUseStyleID.Shoot;  //枪支或法杖
            Item.damage = 47;   //伤害
            Item.useAnimation = 20; //使用动画时长
            Item.useTime = 30;   //攻速
            Item.knockBack = 4;  //击退
            Item.width = 30;     //大小
            Item.height = 30;    //大小
            Item.scale = 1.25f;  //碰撞箱
            Item.rare = ItemRarityID.Pink;       //稀有度
            Item.value = Item.sellPrice(silver: 44);
            Item.crit = 15;       //暴击率
            Item.autoReuse = true;   //自动挥舞
            Item.useTurn = true;      //使用中可转身
            Item.DamageType = DamageClass.Magic;        //魔法武器
            Item.shootSpeed = 10f;    //射速
            Item.mana = 10;           //耗蓝
            Item.channel = true;      //有特殊行为
            Item.noMelee = true;      //无近战伤害
            

            Item.shoot = ModContent.ProjectileType<Grass_Staff_Projectiles>();
        }

        public override Vector2? HoldoutOrigin()
        {
            return new Vector2(1, 1);
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit)
        {
            target.AddBuff(BuffID.Bleeding, 10);
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.JungleSpores, 44)
            .AddTile(TileID.Anvils)
            .Register();
        //public override void AddRecipes()
    }
}

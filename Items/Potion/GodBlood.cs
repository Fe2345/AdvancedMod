using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Potion
{
    public class GodBlood : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("神明之血");
            Tooltip.SetDefault("回复与最大生命值相当的血量");
        }

        public override void SetDefaults()
        {
            // 这部分就不说了
            Item.width = 14;
            Item.height = 24;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.maxStack = 30;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(0, 0, 50, 0);
            // 物品的使用方式，还记得2是什么吗
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            // 喝药的声音
            Item.UseSound = SoundID.Item3;
            // 决定这个物品使用以后会不会减少，true就是使用后物品会少一个，默认为false
            Item.consumable = true;
            // 决定使用动画出现后，玩家转身会不会影响动画的方向，true就是会，默认为false
            Item.useTurn = true;
            // 加buff的方法1：设置物品的buffType为buff的ID
            // 这里我设置了中毒debuff（2333
            Item.healLife = Main.LocalPlayer.statLifeMax2;

            // 用于在物品描述上显示buff持续时间
        }
    }
}

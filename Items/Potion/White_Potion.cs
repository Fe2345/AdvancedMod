using Terraria.ModLoader;
using Terraria;
using Terraria.ID;
using AdvancedMod.Buffs.Not_DeBuff;

namespace AdvancedMod.Items.Potion
{
    public class White_Potion : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("白药水");
            Tooltip.SetDefault("\"神圣的祝福\"");
        }

        public override void SetDefaults()
        {
            // 这部分就不说了
            item.width = 14;
            item.height = 24;
            item.useAnimation = 17;
            item.useTime = 17;
            item.maxStack = 30;
            item.rare = ItemRarityID.Pink;
            item.value = Item.sellPrice(0, 0, 50, 0);
            // 物品的使用方式，还记得2是什么吗
            item.useStyle = ItemUseStyleID.EatingUsing;
            // 喝药的声音
            item.UseSound = SoundID.Item3;
            // 决定这个物品使用以后会不会减少，true就是使用后物品会少一个，默认为false
            item.consumable = true;
            // 决定使用动画出现后，玩家转身会不会影响动画的方向，true就是会，默认为false
            item.useTurn = true;
            // 加buff的方法1：设置物品的buffType为buff的ID
            // 这里我设置了中毒debuff（2333
            item.buffType = ModContent.BuffType<Reward_Of_The_God_Of_Eye>();
            item.buffType = BuffID.Honey;

            // 用于在物品描述上显示buff持续时间
            item.buffTime = 60000;
        }
    }
}

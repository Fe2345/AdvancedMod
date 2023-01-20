using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Items.Potion
{
    public class Stardrop : ModItem
    {
        public override string Texture => "AdvancedMod/Assets/Textures/Items/Stardrop";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("星之果实");
            Tooltip.SetDefault("一种神秘莫测的水果，给予食用者力量。\n它有梦幻般的味道……给人留下十分强烈的回忆，然而只可意会不可言传。\n\n增加 30 最大生命值\n增加 20 最大魔力值");
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
        }

        public override bool? UseItem(Player player)
        {
            player.statLifeMax += 30;
            player.statManaMax += 20;

            AdvancedPlayer player1 = player.GetModPlayer<AdvancedPlayer>();
            player1.UsedStardropCount += 1;

            string Text1 = "很奇怪，但那个味道让你想起先进Mod。";
            string Text2 = "你脑中充满了各种关于先进Mod的想法。";
            string SpecialText_Latic7 = "你发现了一颗 星之果实！你的头脑中充满了各种想法……都是关于Latic7的想法？（非常感谢！)";
            string SpecialText_SwordOfWar = "你发现了一颗 星之果实！你的头脑中充满了各种想法……都是关于SwordOfWar的想法？（非常感谢！)";
            string SpecialText_Mod = "你发现了一个 星之果实！你感受到与先进Mod不可动摇的联系。AdvancedMod";
            string LevelUp = "你的最大生命值和魔力值提高了。";
            Color color = new Color(175, 75, 255);

            if (player.name.Equals("SwordOfWar"))
            {
                Main.NewText(SpecialText_SwordOfWar, color);
            }
            else if (player.name.Equals("Latic7"))
            {
                Main.NewText(SpecialText_Latic7, color);
            }
            else if (player.name.Equals("Advanced"))
            {
                Main.NewText(SpecialText_Mod, color);
            }
            else
            {
                if (Main.rand.NextBool(1))
                {
                    Main.NewText(Text1, color);
                }
                else
                {
                    Main.NewText(Text2, color);
                }
            }
            Main.NewText(LevelUp, color);

            return true; 
        }

        public override bool CanUseItem(Player player)
        {
            if (player.GetModPlayer<AdvancedPlayer>().UsedStardropCount >= 15) return false;
            return true;
        }
    }
}

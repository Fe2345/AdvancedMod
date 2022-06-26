using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Chat;
using Terraria.Localization;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Items.Misc
{
    public class DifficultySwitcher : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("难度切换器");
            Tooltip.SetDefault("\"做出你的选择\"\n右键来改变世界难度\n切换顺序为：旅途->经典->专家->大师\n切换至经典模式会导致异变模式/虚空模式被强制关闭\n来自开发者的警告：请勿使用此物品带来的旅行模式特性来刷物品！！！");
        }

        public override void SetDefaults()
        {
            Item.width = 42;
            Item.height = 42;
            Item.accessory = true;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override bool CanUseItem(Player player)
        {
            if (!Utils.Tool.CheckBossAlive()) return true;

            return false;
        }

        public override bool? UseItem(Player player)
        {
            string text = "";

            switch (Main.GameMode)
            {
                case 0:
                    Main.GameMode = 1;
                    player.difficulty = 0;
                    text = "世界难度已被设为专家模式！！！";
                    break;
                case 1:
                    Main.GameMode = 2;
                    player.difficulty = 0;
                    text = "世界难度已被设为大师模式！！！";
                    break;
                case 2:
                    Main.GameMode = 3;
                    player.difficulty = 3;
                    text = "世界难度已被设为旅途模式！！！";
                    break;
                case 3:
                    Main.GameMode = 0;
                    text = "世界难度已被设为经典模式！！！";
                    player.difficulty = 0;
                    break;
            }

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(text, 175, 75, 255);
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), new Color(175, 75, 255));
                NetMessage.SendData(MessageID.WorldData);
            }

            return true;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddTile(TileID.DemonAltar)
            .Register();
    }
}

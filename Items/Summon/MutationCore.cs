using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.Localization;

namespace AdvancedMod.Items.Summon
{
    public class MutationCore : ModItem
    {
        public override void SetStaticDefaults()
        {
            if (!Main.expertMode)
            {
                Tooltip.SetDefault(Language.GetTextValue("Mods.AdvancedMod.ItemTooltip.MutationCore_NMode"));
            }
            else
            {
                if (Main.masterMode)
                {
                    Tooltip.SetDefault(Language.GetTextValue("Mods.AdvancedMod.ItemTooltip.MutationCore_VMode"));
                }
                else
                {
                    Tooltip.SetDefault(Language.GetTextValue("Mods.AdvancedMod.ItemTooltip.MutationCore_MMode"));
                }
            }
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 18;
            Item.rare = ItemRarityID.Purple;
            Item.maxStack = 999;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = false;
            Item.value = Item.sellPrice(silver: 1);
        }

        public override bool CanUseItem(Player player)
        {
            if (Utils.Tool.CheckBossAlive())
            {
                Main.NewText($"你现在不能改变规则，周围有{Utils.Tool.GetClosestNPC(player.Center,true).FullName}在游荡");
                return false;  
            }
            
            if (!Main.expertMode && !Main.masterMode)
            {
                return false;
            }

            return true;
        }

        public override bool? UseItem(Player player)
        {
            Color color = new Color(175, 75, 255);
            if (!AdvancedWorld.MutationMode)
            {
                AdvancedWorld.MutationMode = true;
                Main.NewText(Main.masterMode ? "虚空模式已开启！！！" : "异变模式已开启！！！",color);
            }
            else
            {
                AdvancedWorld.MutationMode = false;
                Main.NewText(Main.masterMode ? "虚空模式已关闭！！！" : "异变模式已关闭！！！",color);
            }

            return true;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddTile(TileID.DemonAltar)
            .Register();
    }
}

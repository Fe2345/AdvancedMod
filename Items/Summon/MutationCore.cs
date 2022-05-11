using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Items.Summon
{
    public class MutationCore : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("异变核心");
            Tooltip.SetDefault("开启/关闭异变模式\n改变所有BOSS的AI\n所有Advanced饰品效果下降\n世界BOSS 和 异变、奇变、幻变BOSS将可以被挑战");
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
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Boss.TreeDiagrammer>()) | !Main.expertMode)
            {
                Main.NewText("异变阻止了你改变规则");
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
                Main.NewText("异变模式已开启！！！",color);
            }
            else
            {
                AdvancedWorld.MutationMode = false;
                AdvancedPlayer.RecievedInitBag = false;
                Main.NewText("异变模式已关闭！！！",color);
            }

            return true;
        }

        public override void AddRecipes() => CreateRecipe()
            .AddTile(TileID.DemonAltar)
            .Register();
    }
}

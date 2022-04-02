using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items.Misc
{
    public class TreeDiagrammerBag : ModItem
    {
        public override int BossBagNPC => ModContent.NPCType<NPCs.Boss.TreeDiagrammer>();

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("宝藏袋(树状图设计者)");
            Tooltip.SetDefault("右键打开");
        }

        public override void SetDefaults()
        {
            item.maxStack = 999;
            item.consumable = true;
            item.width = 16;
            item.height = 16;
            item.rare = ItemRarityID.Expert;
        }

        public override void OpenBossBag(Player player)
        {
            player.QuickSpawnItem(ModContent.ItemType<Items.Mateiral.SiliconBar>(),Main.rand.Next(12)+24);
            player.QuickSpawnItem(ItemID.IronBar, Main.rand.Next(5) + 5);
            player.QuickSpawnItem(ItemID.SoulofLight, Main.rand.Next(6) + 6);
            player.QuickSpawnItem(ItemID.Wire, Main.rand.Next(10) + 10);
        }
    }
}

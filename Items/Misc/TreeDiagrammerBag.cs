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
            Item.maxStack = 999;
            Item.consumable = true;
            Item.width = 16;
            Item.height = 16;
            Item.rare = ItemRarityID.Expert;
        }

        public override void OpenBossBag(Player player)
        {
            player.QuickSpawnItem(player.GetSource_OpenItem(ModContent.ItemType<Items.Mateiral.SiliconBar>()),ModContent.ItemType<Items.Mateiral.SiliconBar>(),Main.rand.Next(12)+24);
            player.QuickSpawnItem(player.GetSource_OpenItem(ItemID.IronBar),ItemID.IronBar, Main.rand.Next(5) + 5);
            player.QuickSpawnItem(player.GetSource_OpenItem(ItemID.SoulofLight),ItemID.SoulofLight, Main.rand.Next(6) + 6);
            player.QuickSpawnItem(player.GetSource_OpenItem(ItemID.Wire),ItemID.Wire, Main.rand.Next(10) + 10);
            switch (Main.rand.Next(5))
            {
                case 0:
                    player.QuickSpawnItem(player.GetSource_OpenItem(Type), ModContent.ItemType<Items.Weapon.Melee.Surge>());
                    break;
            }
            
        }
    }
}

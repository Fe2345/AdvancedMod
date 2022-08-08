using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.Items
{
    public class AdvancedGlobelItem : GlobalItem
    {
        public override bool InstancePerEntity => true;

        public override void OpenVanillaBag(string context, Player player, int arg)
        {
            if (context.Equals("bossBag") && arg == ItemID.WallOfFleshBossBag && Main.rand.NextBool(5))
            {
                Item.NewItem(player.GetSource_OpenItem(ItemID.WallOfFleshBossBag),player.Center,ModContent.ItemType<Accessory.BossDrop.CurserEmblem>());
            }
        }
    }
}

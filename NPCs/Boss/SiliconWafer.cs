using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace AdvancedMod.NPCs.Boss
{
    public class SiliconWafer : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("硅晶片");
            Main.npcFrameCount[npc.type] = 1;
        }

        public override void SetDefaults()
        {
            npc.lifeMax = 400;
            npc.defense = 20;
            npc.damage = 30;
            Main.npcFrameCount[npc.type] = 1;
            npc.noGravity = true;
            npc.noTileCollide = true;
            aiType = -1;
            npc.boss = false;
        }

        public override void AI()
        {
            Player player = Main.player[npc.target];
            Vector2 distance = player.Center - npc.Center;

            npc.velocity = 6 *distance/distance.Length();
        }

        public override void NPCLoot()
        {
            if (!AdvancedWorld.MutationMode) Item.NewItem(npc.Center, ItemID.Heart, 1);
        }
    }
}

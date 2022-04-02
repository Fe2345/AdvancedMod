using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

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
            aiType = 5;
            npc.boss = false;
        }
    }
}

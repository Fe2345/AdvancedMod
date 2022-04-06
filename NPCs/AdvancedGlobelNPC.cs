using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace AdvancedMod.NPCs
{
    public class AdvancedGlobelNPC : GlobalNPC
    {
        public override bool InstancePerEntity
        {
            get
            {
                return true;
            }
        }

        public bool TheWorld;
        public bool KingCrimson;

        public override void ResetEffects(NPC npc)
        {
            TheWorld = false;
            KingCrimson = false;
        }

        public override bool PreAI(NPC npc)
        {
            if (TheWorld)
            {
                npc.velocity = npc.oldVelocity;
                npc.position = npc.oldPosition;
                npc.frameCounter = 0;
            }

            if (KingCrimson)
            {
                npc.life = npc.lifeMax;
            }

            return true;
        }
    }
}

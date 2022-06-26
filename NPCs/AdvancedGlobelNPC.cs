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
            bool PreAI = base.PreAI(npc);

            if (TheWorld)
            {
                npc.velocity = npc.oldVelocity;
                npc.position = npc.oldPosition;
                npc.frameCounter = 0;
                PreAI = false;
            }

            if (KingCrimson)
            {
                npc.life = npc.lifeMax;
            }

            return PreAI;
        }

        public override bool CheckDead(NPC npc)
        {
            if (TheWorld)
            {
                npc.life = 1;
                return false;
            }

            return true;
        }
    }
}

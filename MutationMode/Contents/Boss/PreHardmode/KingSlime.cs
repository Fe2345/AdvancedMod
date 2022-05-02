using AdvancedMod.MutationMode.NPCMatching;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace AdvancedMod.MutationMode.Contents.Boss.PreHardmode
{
    public class KingSlime : MutationModeNPCBehaviour
    {
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchType(NPCID.KingSlime);

        public bool landed;
        public bool enteredPhase2;
        /*
        
        */

        public override bool PreAI(NPC npc)
        {
            Player player = Main.player[npc.target];
            Vector2 distance = player.Center - npc.Center;

            if (npc.velocity == Vector2.Zero)
            {
                landed = true;
            }
            else
            {
                landed = false;
            }

            if (landed)
            {
                npc.velocity = new Vector2(distance.X > 0 ? 1 : -1 * Main.rand.Next(5),Main.rand.Next(5));
            }

            return false;
        }
    }
}

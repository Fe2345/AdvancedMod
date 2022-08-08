using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.GameContent.ItemDropRules;

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

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            LeadingConditionRule VModeRule = new LeadingConditionRule(new ItemDropRules.VoidModeDropRule());
            //add npcloot
            switch (npc.type)
            {
                case NPCID.WallofFlesh:
                    npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(),ModContent.ItemType<Items.Accessory.BossDrop.CurserEmblem>(),4,1,1,1));
                    break;
                case NPCID.DukeFishron:
                    VModeRule.OnSuccess(Utils.Tool.BossBagDropCustom(ModContent.ItemType<Items.Mount.FishOutOfWater>()));
                    break;
            }
        }
    }
}

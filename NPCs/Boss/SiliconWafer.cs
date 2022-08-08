using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.GameContent.Bestiary;

namespace AdvancedMod.NPCs.Boss
{
    public class SiliconWafer : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("硅晶片");
            Main.npcFrameCount[NPC.type] = 1;

            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                Velocity = -1f,
                Direction = -1
            };
        }

        public override void SetDefaults()
        {
            NPC.lifeMax = 400;
            NPC.defense = 20;
            NPC.damage = 30;
            Main.npcFrameCount[NPC.type] = 1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            AIType = -1;
            NPC.boss = false;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.UIInfoProvider = new CommonEnemyUICollectionInfoProvider(
                ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[ModContent.NPCType<NPCs.Boss.TreeDiagrammer>()],
                quickUnlock: true);
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement("Mods.AdvancedMod.Bestiary.SiliconWafer")
            }) ;
        }

        public override void AI()
        {
            Player player = Main.player[NPC.target];
            Vector2 distance = player.Center - NPC.Center;

            NPC.velocity = 6 *distance/distance.Length();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            base.ModifyNPCLoot(npcLoot);

            if (!AdvancedWorld.MutationMode && !Main.masterMode) Item.NewItem(NPC.GetSource_Loot(), NPC.Center, ItemID.Heart, 1);
        }
    }
}

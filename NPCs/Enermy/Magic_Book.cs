using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.Bestiary;

namespace AdvancedMod.NPCs.Enermy
{
    public class Magic_Book : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("魔道书");
            Main.npcFrameCount[NPC.type] = 1;

            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                Velocity = -1f,
                Direction = -1
            };
        }

        public override void SetDefaults()
        {
            NPC.width = 30;
            NPC.height = 30;
            NPC.damage = 40;
            NPC.lifeMax = 300;
            NPC.defense = 55;
            NPC.knockBackResist = 0.5f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath9;
            NPC.value = Item.buyPrice(gold: 4);
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.buffImmune[BuffID.Bleeding] = true;
            NPC.buffImmune[BuffID.Burning] = true;
            NPC.aiStyle = 22;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement("Mods.AdvancedMod.Bestiary.MagicBook")
            });
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.manaSick)
            {
                return 0.1f;
            }
            return 0;
        }

        //public override void NPCLoot()
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            base.ModifyNPCLoot(npcLoot);

            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Mateiral.Disable_Bar>(), 1));
        }
    }
}

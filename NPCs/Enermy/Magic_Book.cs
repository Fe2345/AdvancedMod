using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using AdvancedMod.Items.Mateiral;

namespace AdvancedMod.NPCs.Enermy
{
    public class Magic_Book : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("魔道书");
            Main.npcFrameCount[npc.type] = 1;
        }

        public override void SetDefaults()
        {
            npc.width = 30;
            npc.height = 30;
            npc.damage = 40;
            npc.lifeMax = 300;
            npc.defense = 55;
            npc.knockBackResist = 0.5f;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath9;
            npc.value = Item.buyPrice(gold: 4);
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.buffImmune[BuffID.Bleeding] = true;
            npc.buffImmune[BuffID.Burning] = true;
            npc.aiStyle = 22;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.player.manaSick)
            {
                return 0.1f;
            }
            return 0;
        }

        public override void NPCLoot()
        {
            if (Main.rand.Next(1000000) == 0)
            {
                Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, ModContent.ItemType<Disable_Bar>());
            }
        }
    }
}

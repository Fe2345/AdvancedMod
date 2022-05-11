using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace AdvancedMod.NPCs.Boss
{
    public class MutationBoss : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("异变");
            Main.npcFrameCount[NPC.type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 150;
            NPC.height = 150;
            NPC.damage = 80;
            NPC.lifeMax = Main.expertMode? 1000000 : 2000000;
            NPC.defense = 30;
            NPC.knockBackResist = 0f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            Music = MusicID.Boss2;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[ModContent.BuffType<Buffs.Debuff.TheWorld>()] = true;
            Main.npcFrameCount[NPC.type] = 1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            AIType = -1;
            NPC.boss = true;

            if (AdvancedWorld.MutationMode)
            {
                NPC.lifeMax = 3000000;
            }

            if (AdvancedWorld.MutationMode && Main.masterMode)
            {
                NPC.lifeMax = 4500000;
            }
        }
        
        private enum MBStatus
        {
            Disappear,
            Search,
            Nature1,
            Enermy1,
            Town1,
            Nature2,
            Enermy2,
            Town2,
            Nature3,
            Enermy3,
            Town3
        }

        MBStatus status;
        int Time;
        public override void AI()
        {
            Player player = Main.player[NPC.target];

            if (Vector2.Distance(NPC.Center, player.Center) > 200)
            {
                status = MBStatus.Search;
            }
            else if (Time >= 0 && Time < 240)
            {
                status = MBStatus.Nature1; //爆炸兔
            }
            else if (Time >= 240 && Time < 600)
            {
                status = MBStatus.Enermy1; //史莱姆尖刺
            }
            else if (Time >= 600 && Time < 900)
            {
                status = MBStatus.Town1; //向导的木箭
            }
            else if (Time >= 900 && Time < 1800)
            {
                status = MBStatus.Nature2;
            }
            else if (Time >= 1800 && Time < 2700)
            {
                status = MBStatus.Enermy2;
            }
            else if (Time >= 2700 && Time < 3000)
            {
                status = MBStatus.Town2;
            }
            else if (Time == 3000)
            {
                Time = 0;
            }


        }
    }
}

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
            Main.npcFrameCount[npc.type] = 1;
        }

        public override void SetDefaults()
        {
            npc.width = 150;
            npc.height = 150;
            npc.damage = 80;
            npc.lifeMax = Main.expertMode? 1000000 : 2000000;
            npc.defense = 30;
            npc.knockBackResist = 0f;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath6;
            music = MusicID.Boss2;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[ModContent.BuffType<Buffs.Debuff.TheWorld>()] = true;
            Main.npcFrameCount[npc.type] = 1;
            npc.noGravity = true;
            npc.noTileCollide = true;
            aiType = -1;
            npc.boss = true;

            if (AdvancedWorld.MutationMode)
            {
                npc.lifeMax = 3000000;
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
            Player player = Main.player[npc.target];

            if (Vector2.Distance(npc.Center, player.Center) > 200)
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

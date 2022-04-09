using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using AdvancedMod.Projectiles;
using Microsoft.Xna.Framework;

namespace AdvancedMod.NPCs.Boss
{
    
    [AutoloadBossHead]
    public class TreeDiagrammer : ModNPC
    {
        public bool Chat1;
        public bool enterPhase2;
        public bool Chat3;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("树状图设计者");
            Main.npcFrameCount[npc.type] = 1;
        }

        public override void SetDefaults()
        {
            npc.width = 150;
            npc.height = 150;
            npc.damage = 80;
            npc.lifeMax = Main.expertMode ? 60000 : 30000;
            npc.defense = 30;
            npc.knockBackResist = 0f;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath6;
            music = MusicID.Boss2;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = true;
            Main.npcFrameCount[npc.type] = 1;
            npc.noGravity = true;
            npc.noTileCollide = true;
            aiType = -1;
            npc.boss = true;
            bossBag = ModContent.ItemType<Items.Misc.TreeDiagrammerBag>();
        }
        private enum TDStatus
        {
            Search,
            Attack,
            Summon,
            Disappear
        }

        TDStatus status;
        int Time = 0;
        Color color = new Color(255, 255, 255);
        public override void AI()
        {
            Player player = Main.player[npc.target];
            if (!player.active || player.dead)
            {
                status = TDStatus.Disappear;
            }
            if (Vector2.Distance(npc.Center,player.Center) >= 2500f)
            {
                status = TDStatus.Search;
            }
            else
            {
                Time++;
                if (Time >= 900)
                {
                    Time = 0;
                }
                else if (Time >=0 && Time < 600){
                    status = TDStatus.Attack;
                }
                else
                {
                    status = TDStatus.Summon;
                }
            }

            switch (status)
            {
                case TDStatus.Disappear:
                    npc.TargetClosest(false);
                    npc.velocity.X = 0;
                    npc.velocity.Y += 1;
                    return;
                case TDStatus.Search:
                    if (Time % 120 == 0)
                    {
                        npc.velocity = (player.Center - npc.Center) / Vector2.Distance(player.Center,npc.Center) * 6;
                    }
                    
                    break;
                case TDStatus.Attack:
                    if (Vector2.Distance(player.Center + new Vector2(-100, -100), npc.Center) < Vector2.Distance(player.Center + new Vector2(100, -100), npc.Center))
                    {
                        npc.velocity = (player.Center - npc.Center + new Vector2(-100,10)) / Vector2.Distance(player.Center, npc.Center) * 6;
                    }
                    else
                    {
                        npc.velocity = (player.Center - npc.Center + new Vector2(100, 100)) / Vector2.Distance(player.Center, npc.Center) * 6;
                    }
                    Vector2 npcToPlr = player.Center - npc.Center;
                    Projectile.NewProjectile(npc.Center, Vector2.Normalize(npcToPlr)*4, ModContent.ProjectileType<Projectiles.Boss.TreeDiagrammer.TreeDiagrammer_Laser>(), 0, 0f, Main.myPlayer, 0, 3);
                    break;
                case TDStatus.Summon:
                    if (Time % 60 == 0)
                    {
                        NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SiliconWafer>());
                    }
                    
                    break;
            }

            if (npc.life <= npc.lifeMax * 0.75 && !Chat1)
            {
                Main.NewText("虽然你仅仅刮掉了我的几根电线，但已经超过很多其他的挑战者了。", color);
                Chat1 = true;
            }
            else if (npc.life <= npc.lifeMax * 0.5 && !enterPhase2)
            {
                Main.NewText("前面的都是闹着玩的。我要拿出我的真实实力了。", color);
                enterPhase2 = true;
            }
            else if (npc.life <= npc.lifeMax * 0.25 && !Chat3){
                Main.NewText("你竟然认为可以杀死我？！？", color);
                Chat3 = true;
            }
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void NPCLoot()
        {
            if (Main.expertMode)
            {
                Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, ModContent.ItemType<Items.Misc.TreeDiagrammerBag>(), 1);
            }
            else
            {
                Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, ModContent.ItemType<Items.Mateiral.SiliconBar>(), Main.rand.Next(6) + 12);
                Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, ItemID.SoulofLight, Main.rand.Next(3)+6);
                Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, ItemID.IronBar, Main.rand.Next(3)+5);
                Item.NewItem((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height, ItemID.Wire, Main.rand.Next(5)+10);
            }

            AdvancedWorld.downedTreeDiagrammer = true;
        }
    }
}

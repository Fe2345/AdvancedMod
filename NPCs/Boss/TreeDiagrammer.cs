using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using AdvancedMod.Projectiles;
using Microsoft.Xna.Framework;
using AdvancedMod.Utils;
using System;

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
            Disappear,
            Search,
            Attack,
            Summon,
            Lighting
        }
        /*
        ai[0] 状态指示器（死亡，追击，攻击）
            -2 NPC死亡
            -1 玩家死亡
            0 攻击
            1 追击玩家
        ai[1] 计时器
        ai[2] 攻击方式指示器
            0 停止攻击
            1 一阶段攻击
            2 一阶段召唤
            3 异变模式旋转闪电
        */
        Color color = new Color(255, 255, 255); //状态讯息颜色
        int i = 0; //旋转的闪电弹幕计数器
        public override void AI()
        {
            Player player = Main.player[npc.target];
            if (!AdvancedWorld.MutationMode && npc.ai[1] >= 900)
            {
                npc.ai[1] = 0;
            }
            else if (AdvancedWorld.MutationMode && npc.ai[1] >= 1200)
            {
                npc.ai[1] = 0;
            }

            if (!player.active || player.dead)
            {
                npc.ai[0] = -1;
            }
            if (Vector2.Distance(npc.Center,player.Center) >= 2500f)
            {
                npc.ai[0] = 1;
            }
            else
            {
                npc.ai[1]++;
                if (npc.ai[1] >= 900)
                {
                    npc.ai[1] = 0;
                }
                else if (npc.ai[1] >= 0 && npc.ai[1] < 600){
                    npc.ai[2] = 1;
                }
                else if (npc.ai[1] >= 600 && npc.ai[1] < 900)
                {
                    npc.ai[2] = 2;
                }
                else if (AdvancedWorld.MutationMode && npc.ai[1] >= 900 && npc.ai[1] < 1200)
                {
                    npc.ai[2] = 3;
                }
            }

            switch ((int)npc.ai[0])
            {
                case -2:
                    break;
                case -1:
                    npc.velocity = new Vector2(0, 20);
                    break;
                case 0:
                    npc.ai[2] = 0;
                    break;
                case 1:
                    npc.position = player.position + new Vector2(10, 10);
                    break;
            }

            switch ((int)npc.ai[2])
            {
                case 1:
                    if (Vector2.Distance(player.Center + new Vector2(-100, -100), npc.Center) < Vector2.Distance(player.Center + new Vector2(100, -100), npc.Center))
                    {
                        npc.velocity = (player.Center - npc.Center + new Vector2(-100,10)) / Vector2.Distance(player.Center, npc.Center) * 6;
                    }
                    else
                    {
                        npc.velocity = (player.Center - npc.Center + new Vector2(100, 100)) / Vector2.Distance(player.Center, npc.Center) * 6;
                    }
                    Vector2 npcToPlr = player.Center - npc.Center;
                    Projectile.NewProjectile(npc.Center, Vector2.Normalize(npcToPlr)*40, ModContent.ProjectileType<Projectiles.Boss.TreeDiagrammer.TreeDiagrammer_Laser>(), 0, 0f, Main.myPlayer, 0, 3);
                    break;
                case 2:
                    if (npc.ai[1] % 60 == 0)
                    {
                        NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SiliconWafer>());
                    }

                    break;
                case 3:
                    Vector2 InitAngle = Tool.TurnVector((player.Center - npc.Center), (float)(-Math.PI / 6));
                    while (npc.ai[1] % 5 == 0)
                    {
                        Vector2 speed = Tool.TurnVector(InitAngle, (float)(i * Math.PI / 30));
                        Projectile.NewProjectile(npc.Center,speed,ModContent.ProjectileType<Projectiles.Boss.TreeDiagrammer.TreeDiagrammer_Lighting>(),npc.damage /3, 0f ,Main.myPlayer,0f ,npc.whoAmI);
                        i++;
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

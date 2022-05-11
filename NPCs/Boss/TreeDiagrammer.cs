using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Localization;
using AdvancedMod.Projectiles;
using Microsoft.Xna.Framework;
using AdvancedMod.Utils;
using System;
using System.Collections.Generic;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;

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
            //DisplayName.AddTranslation(GameCulture.Chinese, "树状图设计者");
            //DisplayName.AddTranslation(GameCulture.English, "Tree Diagrammer");
            Main.npcFrameCount[NPC.type] = 1;

            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                Velocity = -1f,
                Direction = -1
            };
        }

        public override void SetDefaults()
        {
            NPC.width = 150;
            NPC.height = 150;
            NPC.damage = 80;
            NPC.lifeMax = Main.expertMode ? 60000 : 30000;
            NPC.defense = 30;
            NPC.knockBackResist = 0f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            Music = MusicID.Boss2;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            Main.npcFrameCount[NPC.type] = 1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            AIType = -1;
            NPC.boss = true;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement("Mods.AdvancedMod.Bestiary.TreeDiagrammer")
            });
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
            /*
            Player player = Main.player[npc.target];
            if (!AdvancedWorld.MutationMode && npc.ai[1] >= 900)
            {
                NPC.ai[1] = 0;
            }
            else if (AdvancedWorld.MutationMode && npc.ai[1] >= 1200)
            {
                NPC.ai[1] = 0;
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
                if (npc.ai[1] >= 0 && npc.ai[1] < 600){
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
            */
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            base.ModifyNPCLoot(npcLoot);

            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.Misc.TreeDiagrammerBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ModContent.ItemType<Items.Mateiral.SiliconBar>(), 1, 12, 18));
            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ItemID.SoulofLight, 1, 6, 9));
            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ItemID.IronBar, 1, 5, 9));
            npcLoot.Add(ItemDropRule.ByCondition(new Conditions.NotExpert(), ItemID.Wire, 1, 10, 15));
        }
        /*
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
        */
    }
}

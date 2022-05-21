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
            NPCID.Sets.BossBestiaryPriority.Add(NPC.type);
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
        ai[3] 阶段指示器
            -1 狂暴
            0 无
            1 第一阶段
            2 第二阶段
        */
        public override void AI()
        {
            ++NPC.ai[1];
            Player player = Main.player[NPC.target];

            Vector2 NpcToplrTop = player.Center + new Vector2(0, -30) - NPC.Center;

            //处理BOSS阶段
            if (!Main.dayTime)
            {
                NPC.ai[3] = -1;
            }
            else if (NPC.life < 0.5 * NPC.lifeMax)
            {
                NPC.ai[3] = 2;
            }
            else if (NPC.ai[3] == 0)
            {
                NPC.ai[3] = 1;
            }

            //处理计时
            if (NPC.ai[1] > 1800 && !AdvancedWorld.MutationMode) NPC.ai[1] = 0;
            if (NPC.ai[1] > 3000 && AdvancedWorld.MutationMode && !Main.masterMode) NPC.ai[1] = 0;
            if (NPC.ai[1] > 3600 && AdvancedWorld.MutationMode && Main.masterMode) NPC.ai[1] = 0;

            if (!player.active | player.dead)
            {
                NPC.ai[0] = -1;
            }
            else if (NPC.Distance(player.Center) >= 2000)
            {
                NPC.ai[0] = 1;
            }
            else
            {
                NPC.ai[0] = 0;
            }

            if (NPC.ai[3] == -1) //angry
            {
                NPC.defense *= 2;
                NPC.damage *= 2;

                
                NPC.velocity = Vector2.Normalize(NpcToplrTop) * 10;
                if (NPC.ai[1] % 60 == 0)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ProjectileID.CultistBossLightningOrb,
                                            0, 0);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                                         Vector2.Normalize(player.Center - NPC.Center) * 2,
                                         ModContent.ProjectileType<Projectiles.Boss.TreeDiagrammer.TreeDiagrammer_Lighting>(),
                                         NPC.damage, 0);
                }
            }
            else if (NPC.ai[3] == 1)
            {
                if (NPC.ai[0] == -1)
                {
                    NPC.velocity.X = 0;
                    NPC.velocity.Y += 5;
                }
                else if (NPC.ai[0] == 1)
                {
                    NPC.velocity = Vector2.Normalize(player.Center - NPC.Center) * 10;
                }
                else if (NPC.ai[0] == 0)
                {
                    if (NPC.Center.Y >= player.Center.Y)
                    {
                        NPC.velocity = Vector2.Normalize(NpcToplrTop) * 5;
                    }
                    else
                    {
                        NPC.velocity = new Vector2(Main.rand.NextFloat(10), 0);
                    }

                    if (NPC.ai[1] < 1200)
                    {
                        if (NPC.ai[1] % 90 == 0) Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                            Vector2.Normalize(player.Center - NPC.Center) * 5,
                            ModContent.ProjectileType<Projectiles.Boss.TreeDiagrammer.TreeDiagrammer_Laser>(),
                            NPC.damage, 0);
                    }
                    else if (NPC.ai[1] >= 1200 && NPC.ai[1] < 1800)
                    {
                        if (NPC.ai[1] % 60 == 0) NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y,
                                                            ModContent.NPCType<SiliconWafer>()
                            );
                    }
                    else if (AdvancedWorld.MutationMode && NPC.ai[1] >=1800 && NPC.ai[1] < 3000)
                    {
                        if (NPC.ai[1] % 120 == 0)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ProjectileID.CultistBossLightningOrb,
                                            0, 0);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                                                 Vector2.Normalize(player.Center - NPC.Center) * 2,
                                                 ModContent.ProjectileType<Projectiles.Boss.TreeDiagrammer.TreeDiagrammer_Lighting>(),
                                                 NPC.damage, 0);
                        }
                    }
                    else if (AdvancedWorld.MutationMode && Main.masterMode && NPC.ai[1] >= 3000 && NPC.ai[1] < 3600)
                    {
                        //缺少判定线AI  虚空模式没有
                        if (NPC.ai[1] % 200 == 0)
                        {
                            Vector2 pos = player.Center;
                            for (int i = 0; i < 8; i++)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), 
                                    new Vector2(player.Center.X - 210 + 60*i,player.Center.Y - 140 + 40*i),
                                    new Vector2(1,0) * 4,ProjectileID.EyeLaser, NPC.damage, 0);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(),
                                    new Vector2(player.Center.X - 210 + 60 * i, player.Center.Y - 140 + 40 * i),
                                    new Vector2(0, 1) * 4, ProjectileID.EyeLaser, NPC.damage, 0);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(),
                                    new Vector2(player.Center.X + 210 - 60 * i, player.Center.Y + 140 - 40 * i),
                                    new Vector2(-1, 0) * 4, ProjectileID.EyeLaser, NPC.damage, 0);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(),
                                    new Vector2(player.Center.X + 210 - 60 * i, player.Center.Y + 140 - 40 * i),
                                    new Vector2(0, -1) * 4, ProjectileID.EyeLaser, NPC.damage, 0);
                            }
                        }
                    }
                }
            }
            else if (NPC.ai[3] == 2)
            {

            }
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

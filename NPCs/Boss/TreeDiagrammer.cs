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
            NPC.damage = 50;
            NPC.lifeMax = Main.expertMode ? 60000 : 30000;
            NPC.defense = 30;
            NPC.knockBackResist = 0f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            Music = MusicID.Boss2;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Electrified] = true;
            NPC.buffImmune[ModContent.BuffType<Buffs.Debuff.ElectromagneticInduction>()] = true;
            /*
            NPCID.Sets.DebuffImmunitySets.Add(NPC.type, new Terraria.DataStructures.NPCDebuffImmunityData
            {
                SpecificallyImmuneTo = new int[]
                {
                    BuffID.Confused,
                    BuffID.Poisoned,
                    BuffID.Electrified,
                    ModContent.BuffType<Buffs.Debuff.ElectromagneticInduction>()
                }
            }
            );
            */
            Main.npcFrameCount[NPC.type] = 1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            AIType = -1;
            NPC.boss = true;

            if (AdvancedWorld.MutationMode) NPC.lifeMax = 80000;
            if (AdvancedWorld.MutationMode && Main.masterMode) NPC.lifeMax = 100000;
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

        二阶段
        300 弧形激光
        600 大风车1
        300 大风车2
        180*n 死亡光
        300 成对激光
        300 限制下的激光
        */
        Color color = new Color(0, 0, 0);
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
            if (NPC.ai[3] == 1)
            {
                if (NPC.ai[1] > 1800 && !AdvancedWorld.MutationMode) NPC.ai[1] = 0;
                if (NPC.ai[1] > 3000 && AdvancedWorld.MutationMode && !Main.masterMode) NPC.ai[1] = 0;
                if (NPC.ai[1] > 3600 && AdvancedWorld.MutationMode && Main.masterMode) NPC.ai[1] = 0;
            }
            else if (NPC.ai[3] == 2)
            {
                if (NPC.ai[1] > 1080 && !AdvancedWorld.MutationMode) NPC.ai[1] = 0;
                if (NPC.ai[1] > 1860 && AdvancedWorld.MutationMode && !Main.masterMode) NPC.ai[1] = 0;
                if (NPC.ai[1] > 2340 && AdvancedWorld.MutationMode && Main.masterMode) NPC.ai[1] = 0;
            }

            

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
                if (NPC.ai[0] == -1)
                {
                    NPC.velocity.X = 0;
                    NPC.velocity.Y += 5;
                }
                else if (NPC.ai[0] == 0)
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
                        NPC.velocity = new Vector2(-10+Main.rand.NextFloat(20), 0);
                    }

                    if (NPC.ai[1] < 1200)
                    {
                        if (NPC.ai[1] % 90 == 0) Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                            Vector2.Normalize(player.Center - NPC.Center) * 10,
                            ProjectileID.DeathLaser,
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
                        if (NPC.ai[1] % 200 == 0)
                        {
                            Vector2 pos = player.Center;
                            for (int i = 0; i < 8; i++)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), 
                                    new Vector2(player.Center.X - 210 + 60*i,player.Center.Y - 140 + 40*i),
                                    new Vector2(1,0) * 4, ModContent.ProjectileType<Projectiles.Boss.TreeDiagrammer.TreeDiagrammer_Laser>(), NPC.damage, 0);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(),
                                    new Vector2(player.Center.X - 210 + 60 * i, player.Center.Y - 140 + 40 * i),
                                    new Vector2(0, 1) * 4, ModContent.ProjectileType<Projectiles.Boss.TreeDiagrammer.TreeDiagrammer_Laser>(), NPC.damage, 0);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(),
                                    new Vector2(player.Center.X + 210 - 60 * i, player.Center.Y + 140 - 40 * i),
                                    new Vector2(-1, 0) * 4, ModContent.ProjectileType<Projectiles.Boss.TreeDiagrammer.TreeDiagrammer_Laser>(), NPC.damage, 0);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(),
                                    new Vector2(player.Center.X + 210 - 60 * i, player.Center.Y + 140 - 40 * i),
                                    new Vector2(0, -1) * 4, ModContent.ProjectileType<Projectiles.Boss.TreeDiagrammer.TreeDiagrammer_Laser>(), NPC.damage, 0);
                            }
                        }
                    }
                }
            }
            else if (NPC.ai[3] == 2)
            {
                if (NPC.ai[0] == -1)
                {
                    NPC.velocity.X = 0;
                    NPC.velocity.Y = 5;
                }
                else if (NPC.ai[0] == 0)
                {
                    if (NPC.ai[1] < 300)
                    {
                        NPC.rotation += 0.1f;
                        NPC.velocity = Vector2.Normalize(player.Center - NPC.Center) * 4;
                        if (NPC.ai[1] % 60 == 0)
                        {
                            for (int i = -2;i < 3; i++)
                            {
                                Vector2 vel = Utils.Tool.TurnVector(player.Center - NPC.Center, (float)(i*Math.PI / 9));
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,Vector2.Normalize(vel)*10,
                                    ProjectileID.DeathLaser, NPC.damage, 0);
                            }
                        }
                        
                    }
                    else if (NPC.ai[1] > 300 && NPC.ai[1] < 900 && AdvancedWorld.MutationMode)
                    {
                        NPC.velocity = Vector2.Zero;
                        NPC.rotation += 0.1f;
                        if (NPC.ai[1] % 60 == 0)
                        {
                            Vector2 vel = new Vector2((float)Math.Sin(NPC.rotation),(float)Math.Cos(NPC.rotation));
                            for (int i = 0;i < 4; i++)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ProjectileID.CultistBossLightningOrb,
                                                                            0, 0);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                                                    Vector2.Normalize(Utils.Tool.TurnVector(vel,(float)(i*Math.PI/2))) * 2,
                                                    ModContent.ProjectileType<Projectiles.Boss.TreeDiagrammer.TreeDiagrammer_Lighting>(),
                                                    NPC.damage, 0);
                            }
                        }
                    }
                    else if ((NPC.ai[1] > 300 && NPC.ai[1] < 600 & !AdvancedWorld.MutationMode) || (NPC.ai[1] >= 900 && NPC.ai[1] < 1200 && AdvancedWorld.MutationMode))
                    {
                        NPC.rotation += 0.1f;
                        if (NPC.ai[1] % 60 == 0)
                        {
                            Vector2 vel = new Vector2((float)Math.Sin(NPC.rotation), (float)Math.Cos(NPC.rotation));
                            for (int i = 0; i < 8; i++)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                                                    Vector2.Normalize(Utils.Tool.TurnVector(vel, (float)(i*Math.PI / 4))) * 2,
                                                    ProjectileID.DeathLaser,
                                                    NPC.damage, 0);
                            }
                        }
                    }
                    else if (!AdvancedWorld.MutationMode && NPC.ai[1] >= 600 && NPC.ai[1] < 780)
                    {
                        Main.NewText("超电磁炮，准备发射！");
                        if (NPC.ai[1] >= 660 && NPC.ai[1] < 720) Projectile.NewProjectile(NPC.GetSource_FromThis(),NPC.Center,
                                Vector2.Normalize(player.Center-NPC.Center)*10,ModContent.ProjectileType<Projectiles.Boss.TreeDiagrammer.TreeDiagrammerDeathray>(),
                                NPC.damage*10,0
                            );
                    }
                    else if (AdvancedWorld.MutationMode && !Main.masterMode && NPC.ai[1] >= 1200 && NPC.ai[1] < 1560)
                    {
                        if (NPC.ai[1] == 1200 || NPC.ai[1] == 1380) Main.NewText("超电磁炮，准备发射！");
                        if ((NPC.ai[1] >= 1260 && NPC.ai[1] < 1320) || (NPC.ai[1] >= 1440 && NPC.ai[1] < 1500)) Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                                Vector2.Normalize(player.Center - NPC.Center) * 10, ModContent.ProjectileType<Projectiles.Boss.TreeDiagrammer.TreeDiagrammerDeathray>(),
                                NPC.damage * 10, 0
                            );
                    }
                    else if (AdvancedWorld.MutationMode && Main.masterMode && NPC.ai[1] >=1200 && NPC.ai[1] < 1740)
                    {
                        if (NPC.ai[1] == 1200 || NPC.ai[1] == 1380 || NPC.ai[1] == 1560) Main.NewText("超电磁炮，准备发射！");
                        if ((NPC.ai[1] >= 1260 && NPC.ai[1] < 1320) || (NPC.ai[1] >= 1440 && NPC.ai[1] < 1500) || (NPC.ai[1] >= 1620 && NPC.ai[1] < 1680)) Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                                Vector2.Normalize(player.Center - NPC.Center) * 10, ModContent.ProjectileType<Projectiles.Boss.TreeDiagrammer.TreeDiagrammerDeathray>(),
                                NPC.damage * 10, 0
                            );
                    }
                    else if ((NPC.ai[1] >= 780 && NPC.ai[1] < 1080 && AdvancedWorld.MutationMode) || (NPC.ai[1] >= 1560 && NPC.ai[1] < 1860 && AdvancedWorld.MutationMode && !Main.masterMode) || (NPC.ai[1] >= 1740 && NPC.ai[1] < 2040 && AdvancedWorld.MutationMode && Main.masterMode)){
                        if (NPC.ai[1] % 60 == 0)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(),new Vector2((float)(player.Center.X+Math.Cos(Math.PI/3))*40,(float)(player.Center.Y+Math.Sin(Math.PI/3)*40)),
                                                    new Vector2((float)(Math.Sin(Math.PI/6))*10,(float)(-Math.Cos(Math.PI/6))),ProjectileID.DeathLaser,
                                                    NPC.damage,0
                                );
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2((float)(player.Center.X + Math.Cos(2*Math.PI / 3)*40), (float)(player.Center.Y + Math.Sin(Math.PI / 3)*40)),
                                                    new Vector2((float)(Math.Sin(-Math.PI / 6))*10, (float)(-Math.Cos(Math.PI / 6))), ProjectileID.DeathLaser,
                                                    NPC.damage, 0
                                );
                        }
                    }
                    else if (NPC.ai[1] >= 2040 && NPC.ai[1] < 2340)
                    {
                        if (NPC.ai[1] % 60 == 0)
                        {
                            for (int i = -4;i < 4; i++)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), player.Center + new Vector2(40 * i, -150),
                                    Vector2.Zero, ProjectileID.CultistBossLightningOrb, NPC.damage, 0
                                    );
                                Projectile.NewProjectile(NPC.GetSource_FromThis(),player.Center + new Vector2(40 * i -150),
                                    new Vector2(0,2),ModContent.ProjectileType<Projectiles.Boss.TreeDiagrammer.TreeDiagrammer_Lighting>(),
                                    NPC.damage,0
                                    );
                            }
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Normalize(player.Center - NPC.Center) * 10,
                                ProjectileID.DeathLaser,NPC.damage, 0
                                );
                        }
                    }
                }
                
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

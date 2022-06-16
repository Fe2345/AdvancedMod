using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System.Collections.Generic;
using AdvancedMod.Utils;
using Terraria.GameContent.Personalities;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;

namespace AdvancedMod.NPCs.Town
{
    [AutoloadHead]
    public class Mutation : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("异变");
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.Angler];
            //NPC总共帧图数，一般为16+下面两种帧的帧数
            NPCID.Sets.ExtraFramesCount[NPC.type] = NPCID.Sets.ExtraFramesCount[NPCID.Angler];
            //额外活动帧，一般为5
            NPCID.Sets.AttackFrameCount[NPC.type] = NPCID.Sets.AttackFrameCount[NPCID.Angler];
            //攻击帧，这个帧数取决于你的NPC攻击类型，射手填5，战士和投掷填4，法师填2，当然，也可以多填，就是不知效果如何（这里直接引用商人的）
            NPCID.Sets.DangerDetectRange[NPC.type] = 1000;
            //巡敌范围，以像素为单位，这个似乎是半径
            NPCID.Sets.AttackType[NPC.type] = NPCID.Sets.AttackType[NPCID.Angler];
            //攻击类型，一般为0，想要模仿其他NPC就填他们的ID
            NPCID.Sets.AttackTime[NPC.type] = 20;
            //单次攻击持续时间，越短，则该NPC攻击越快（可以用来模拟长时间施法的NPC）
            NPCID.Sets.AttackAverageChance[NPC.type] = 3;
            //NPC遇敌的攻击优先度，该数值越大则NPC遇到敌怪时越会优先选择逃跑，反之则该NPC越好斗。
            //最小一般为1，你可以试试0或负数LOL~

            NPC.Happiness.SetNPCAffection(NPCID.Dryad, AffectionLevel.Love);
            NPC.Happiness.SetNPCAffection(NPCID.Princess, AffectionLevel.Love);
            NPC.Happiness.SetNPCAffection<Chemist>(AffectionLevel.Like);
            NPC.Happiness.SetNPCAffection(NPCID.Angler, AffectionLevel.Like);
            NPC.Happiness.SetNPCAffection(NPCID.DyeTrader, AffectionLevel.Dislike);
            NPC.Happiness.SetNPCAffection(NPCID.TaxCollector, AffectionLevel.Hate);

            NPC.Happiness.SetBiomeAffection<JungleBiome>(AffectionLevel.Love);
            NPC.Happiness.SetBiomeAffection<HallowBiome>(AffectionLevel.Like);
            NPC.Happiness.SetBiomeAffection<SnowBiome>(AffectionLevel.Dislike);
            NPC.Happiness.SetBiomeAffection<CrimsonBiome>(AffectionLevel.Hate);
            NPC.Happiness.SetBiomeAffection<CorruptionBiome>(AffectionLevel.Hate);
            NPC.Happiness.SetBiomeAffection<DungeonBiome>(AffectionLevel.Hate);

            NPCID.Sets.NPCBestiaryDrawModifiers drawModifiers = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                Velocity = -1f,
                Direction = -1
            };
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            //如果你想写敌对NPC也行
            NPC.width = 22;
            //碰撞箱宽
            NPC.height = 32;
            //碰撞箱高            
            NPC.aiStyle = 7;
            //必带项，如果你能自己写出城镇NPC的AI可以不带
            NPC.damage = 10;
            //碰撞伤害，由于城镇NPC没有碰撞伤害所以可以忽略
            NPC.defense = 150;
            //防御力
            NPC.lifeMax = Main.expertMode ? 5000 : 1500;
            if (Main.masterMode) NPC.lifeMax = 10000;
            //生命值
            NPC.HitSound = SoundID.NPCHit1;
            //受伤音效
            NPC.DeathSound = SoundID.NPCDeath1;
            //死亡音效
            NPC.knockBackResist = 0.1f;
            //抗击退性，数字越大抗性越低

            AnimationType = NPCID.Angler;

            if (NPC.downedMechBossAny)
            {
                NPC.lifeMax = Main.expertMode ? 10000 : 3000;
                if (Main.masterMode) NPC.lifeMax = 20000;
            }

            if (NPC.downedMoonlord)
            {
                NPC.lifeMax = Main.expertMode ? 15000 : 4500;
                if (Main.masterMode) NPC.lifeMax = 30000;
            }

            if (AdvancedWorld.downedMutationBosses)
            {
                NPC.lifeMax = Main.masterMode ? 4500000 : 3000000;
            }
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement("Mods.AdvancedMod.Bestiary.Mutation")
            }) ;
        }

        public override List<string> SetNPCNameList()
        {
            string[] names = { "Devi", "KingSlime", "Trump", "Clound", "SwordOfWar" };
            return new List<string>(names);
        }

        public override void FindFrame(int frameHeight)
        {
            base.FindFrame(frameHeight);
        }

        public override bool CanTownNPCSpawn(int numTownNPCs, int money)
        {
            return true;
        }

        public override string GetChat()
        {
            List<string> chat = new List<string>()
            {
                "我可以给你提供帮助，不过前提是你做了我给你的任务",
                "Oh My God!",
                "Holy S**T",
                "我有一个兄弟，但是我却不想见到他"
            };

            if (AdvancedWorld.MutationMode)
            {
                chat.Add("我觉得我还是要坚持我的初心，去找我那个大哥打一架.");
            }

            if (!AdvancedWorld.downedGodOfTime)
            {
                chat.Add("我有一个可以操纵时间的挚友……对了，SwordOfWar是不是还没把他做出来？");
            }

            if (Main.eclipse)
            {
                chat.Add("你需要泰拉马桶");
            }

            if (Main.bloodMoon && !Main.hardMode)
            {
                chat.Add("WDNMD");
            }

            if (Main.bloodMoon && Main.hardMode)
            {
                chat.Add("小丑很好玩，不是吗？");
            }

            return Main.rand.Next(chat);
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            //翻译“商店文本”
            button = "商店";
            button2 = "升级";
        }

        public override void OnChatButtonClicked(bool firstButton, ref bool shop)
        { 
            //如果按下第一个按钮，则开启商店
            if (firstButton)
            {
                shop = true;
            }
            else if (!firstButton)
            {
                if (!AdvancedPlayer.RecievedInitBag)
                {
                    AdvancedPlayer.RecievedInitBag = true;
                    Item.NewItem(NPC.GetSource_GiftOrReward(),Main.LocalPlayer.Center, ModContent.ItemType<Items.Summon.MutationCore>(), 1);
                    Item.NewItem(NPC.GetSource_GiftOrReward(),Main.LocalPlayer.Center, ModContent.ItemType<Items.Summon.ComplexBossSummons_PreHardmode>(), 1);
                    Item.NewItem(NPC.GetSource_GiftOrReward(),Main.LocalPlayer.Center, ItemID.PlatinumBroadsword, 1);
                    Item.NewItem(NPC.GetSource_GiftOrReward(),Main.LocalPlayer.Center, ItemID.IronBow, 1);
                    Item.NewItem(NPC.GetSource_GiftOrReward(),Main.LocalPlayer.Center, ItemID.RubyStaff, 1);
                    Item.NewItem(NPC.GetSource_GiftOrReward(),Main.LocalPlayer.Center, ItemID.SlimeStaff, 1);
                    Item.NewItem(NPC.GetSource_GiftOrReward(),Main.LocalPlayer.Center, ItemID.LifeCrystal, 3);
                    Item.NewItem(NPC.GetSource_GiftOrReward(),Main.LocalPlayer.Center, ItemID.ManaCrystal, 3);
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "StorageHeart");
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "CraftingAccess");
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "StorageUnit", 16);
                    return;
                }
                else if (!AdvancedPlayer.RecievedBoss1Bag && NPC.downedBoss1) //Eye Of Cthulhu
                {
                    AdvancedPlayer.RecievedBoss1Bag = true;
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.Amethyst, 10);
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.Topaz, 10);
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.GoblinBattleStandard);
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeCrimtane", 4);
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeDemonite", 4);
                }
                else if (!AdvancedPlayer.RecievedBoss2Bag && NPC.downedBoss2)
                {
                    AdvancedPlayer.RecievedBoss2Bag = true;
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.Sapphire, 10);
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.Emerald, 10);
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeCrimtane", 4);
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeDemonite", 4);
                }
                else if (!AdvancedPlayer.RecievedBoss3Bag && NPC.downedBoss3)
                {
                    AdvancedPlayer.RecievedBoss3Bag = true;
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.Ruby, 10);
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.Diamond, 10);
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.Amber, 10);
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeHellstone",16);
                }
                else if (!AdvancedPlayer.RecievedFleshWallBag && Main.hardMode)
                {
                    AdvancedPlayer.RecievedFleshWallBag = true;
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ModContent.ItemType<Items.Summon.ComplexBossSummons_Hardmode>(), 1);
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.PirateMap);
                }
                else if (!AdvancedPlayer.RecievedMechBossBag && NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
                {
                    AdvancedPlayer.RecievedMechBossBag = true;
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeHallowed", 16);
                }
                else if (!AdvancedPlayer.RecievedPlanteraBag && NPC.downedPlantBoss)
                {
                    AdvancedPlayer.RecievedPlanteraBag = true;
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.PumpkinMoonMedallion);
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.NaughtyPresent);
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeBlueChlorophyte", 16);
                }
                else if (!AdvancedPlayer.RecievedMoonlordBag && NPC.downedMoonlord)
                {
                    AdvancedPlayer.RecievedMoonlordBag = true;
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeLuminite", 16);
                }
                else if (!AdvancedPlayer.RecievedGodOfEyeBag && AdvancedWorld.downedGodOfEye)
                {
                    AdvancedPlayer.RecievedGodOfEyeBag = true;
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeTerra", 16);
                }
            }
        }

        public override void SetupShop(Chest shop, ref int nextSlot)
        {
            Tool.AddItem(ref shop, ref nextSlot, true, ModContent.ItemType<Items.Summon.MutationCore>(), 100);
            Tool.AddItem(ref shop, ref nextSlot, true, ModContent.ItemType<Items.Summon.ComplexBossSummons_PreHardmode>(), 100);
            Tool.AddItem(ref shop, ref nextSlot, Main.hardMode, ModContent.ItemType<Items.Summon.ComplexBossSummons_Hardmode>(), 100);
            Tool.AddItem(ref shop, ref nextSlot, NPC.downedBoss2, ItemID.FeralClaws, 10000);
            Tool.AddItem(ref shop, ref nextSlot, Main.hardMode, ItemID.TitanGlove, 10000);
            Tool.AddItem(ref shop, ref nextSlot, Main.hardMode, ItemID.MagmaStone, 10000);
            Tool.AddItem(ref shop, ref nextSlot, !Main.dayTime, ModContent.ItemType<Items.Potion.WonderPotion>(), 50000);
        }

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = 36;
            knockback = 3f;

            if (Main.hardMode)
            {
                damage = 80;
                knockback = 6f;
            }
            if (NPC.downedPlantBoss)
            {
                damage = 120;
                knockback = 8f;
            }
            if (NPC.downedMoonlord)
            {
                damage = 200;
                knockback = 15f;
            }
            if (AdvancedWorld.downedMutationBosses)
            {
                damage = 1000;
                knockback = 30f;
            }
        }

        //NPC攻击一次后的间隔
        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 3;
            randExtraCooldown = 2;
            //间隔的算法：实际间隔会大于或等于cooldown的值且总是小于cooldown+randExtraCooldown的总和（TR总整这些莫名其妙的玩意）
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            projType = ModContent.ProjectileType<Projectiles.RabbitBomb_Projectile>();
            //使用兔兔炸弹的弹幕
            attackDelay = 180;
            //NPC在出招后多长时间才会发射弹幕
        }
        
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            base.ModifyNPCLoot(npcLoot);

            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapon.Ranged.RabbitBomb>(), 1));
        }
        
    }
}

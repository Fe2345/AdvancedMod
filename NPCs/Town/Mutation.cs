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

            NPC.buffImmune[ModContent.BuffType<Buffs.Debuff.TheWorld>()] = true;
            if (ModLoader.TryGetMod("FargowiltasSouls",out Mod fargo))
            {
                NPC.buffImmune[Tool.GetModBuff("FargowiltasSouls", "TimeFrozen")] = true;
            }

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
            AdvancedPlayer player = Main.player[Main.myPlayer].GetModPlayer<AdvancedPlayer>();

            //如果按下第一个按钮，则开启商店
            if (firstButton)
            {
                shop = true;
            }
            else if (!firstButton)
            {
                if (!player.RecievedInitBag)
                {
                    player.RecievedInitBag = true;
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
                    Main.npcChatText = "我目前只能给你这些了……或许你可以去击败一只强大的怪物？";
                    return;
                }
                else if (!player.RecievedBoss1Bag && NPC.downedBoss1) //Eye Of Cthulhu
                {
                    player.RecievedBoss1Bag = true;
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.Amethyst, 10);
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.Topaz, 10);
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.GoblinBattleStandard);
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeCrimtane", 4);
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeDemonite", 4);
                    Main.npcChatText = "干的漂亮！如果你想要变得更强，你可以尝试踏上那片邪恶的土地！那里危机四伏，但是这并非是个坏主意！";
                    return;
                }
                else if (!player.RecievedBoss2Bag && NPC.downedBoss2)
                {
                    player.RecievedBoss2Bag = true;
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.Sapphire, 10);
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.Emerald, 10);
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeCrimtane", 4);
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeDemonite", 4);
                    Main.npcChatText = "你已经变得更强了，你不妨去一去丛林和地狱，或者尝试解开地牢门口的老人的诅咒。";
                    return;
                }
                else if (!player.RecievedBoss3Bag && NPC.downedBoss3)
                {
                    player.RecievedBoss3Bag = true;
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.Ruby, 10);
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.Diamond, 10);
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.Amber, 10);
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeHellstone",16);
                    Main.npcChatText = "那个邪恶的空骨架死了。现在你可以前往地狱迎接最后的挑战了。";
                    return;
                }
                else if (!player.RecievedFleshWallBag && Main.hardMode)
                {
                    player.RecievedFleshWallBag = true;
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ModContent.ItemType<Items.Summon.ComplexBossSummons_Hardmode>(), 1);
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.PirateMap);
                    Main.npcChatText = "你完成了向导给你的试炼……抱歉，现在又有新的威胁出现了，你不妨试试。";
                    return;

                }
                else if (!player.RecievedMechBossBag && NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
                {
                    player.RecievedMechBossBag = true;
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeHallowed", 16);
                    Main.npcChatText = "不知道你有没有注意到？丛林地下长出了一些花苞，千万不要轻易挖开它！当然，除非你想成为大南方植物终结者";
                    return;
                }
                else if (!player.RecievedPlanteraBag && NPC.downedPlantBoss)
                {
                    player.RecievedPlanteraBag = true;
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.PumpkinMoonMedallion);
                    Item.NewItem(NPC.GetSource_GiftOrReward(), Main.LocalPlayer.Center, ItemID.NaughtyPresent);
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeBlueChlorophyte", 16);
                    Main.npcChatText = "你杀死了一只花……你不妨去丛林深处的神庙看看，然后去地牢绕两圈。";
                    return;

                }
                else if (!player.RecievedMoonlordBag && NPC.downedMoonlord)
                {
                    player.RecievedMoonlordBag = true;
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeLuminite", 16);
                    Main.npcChatText = "嘘！我希望这些话不要被树妖听到！将有一些不速之客前来进攻！我说的可不是火星人！";
                    return;
                }
                else if (!player.RecievedGodOfEyeBag && AdvancedWorld.downedGodOfEye)
                {
                    player.RecievedGodOfEyeBag = true;
                    Utils.Tool.NewModItem(Main.LocalPlayer.Center, "MagicStorage", "UpgradeTerra", 16);
                    Main.npcChatText = "我感觉时间正在波动……看来'他'要来了。准备迎战吧……";
                    return;
                }
                else
                {
                    Main.npcChatText = "现在我没有什么可以给你的……";
                    return;
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
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.CopperBar), ItemID.CopperBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.TinBar), ItemID.TinBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.IronBar), ItemID.IronBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.LeadBar), ItemID.LeadBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.SilverBar), ItemID.SilverBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.TungstenBar), ItemID.TungstenBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.GoldBar), ItemID.GoldBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.PlatinumBar), ItemID.PlatinumBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.MeteoriteBar), ItemID.MeteoriteBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.DemoniteBar), ItemID.DemoniteBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.CrimtaneBar), ItemID.CrimtaneBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.HellstoneBar), ItemID.HellstoneBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.CobaltBar), ItemID.CobaltBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.PalladiumBar), ItemID.PalladiumBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.MythrilBar), ItemID.MythrilBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.OrichalcumBar), ItemID.OrichalcumBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.AdamantiteBar), ItemID.AdamantiteBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.TitaniumBar), ItemID.TitaniumBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ModContent.ItemType<Items.Mateiral.SiliconBar>()), ModContent.ItemType<Items.Mateiral.SiliconBar>(), 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.HallowedBar), ItemID.HallowedBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.ChlorophyteBar), ItemID.ChlorophyteBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.SpectreBar), ItemID.SpectreBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.ShroomiteBar), ItemID.ShroomiteBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.LunarBar), ItemID.LunarBar, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.KingSlimeBossBag), ItemID.KingSlimeBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.EyeOfCthulhuBossBag), ItemID.EyeOfCthulhuBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.EaterOfWorldsBossBag), ItemID.EaterOfWorldsBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.BrainOfCthulhuBossBag), ItemID.BrainOfCthulhuBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.QueenBeeBossBag), ItemID.QueenBeeBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.SkeletronBossBag), ItemID.SkeletronBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.DeerclopsBossBag), ItemID.DeerclopsBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.WallOfFleshBossBag), ItemID.WallOfFleshBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.QueenSlimeBossBag), ItemID.QueenSlimeBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.TwinsBossBag), ItemID.TwinsBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.DestroyerBossBag), ItemID.DestroyerBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.SkeletronPrimeBossBag), ItemID.SkeletronPrimeBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.PlanteraBossBag), ItemID.PlanteraBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.FairyQueenBossBag), ItemID.FairyQueenBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.GolemBossBag), ItemID.GolemBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.FishronBossBag), ItemID.FishronBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.CultistBossBag), ItemID.CultistBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.MoonLordBossBag), ItemID.MoonLordBossBag, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ItemID.BossBagBetsy), ItemID.BossBagBetsy, 2000);
            Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, ModContent.ItemType<Items.Misc.TreeDiagrammerBag>()), ModContent.ItemType<Items.Misc.TreeDiagrammerBag>(), 2000);
            if (ModLoader.TryGetMod("FargowiltasSouls",out Mod fargo))
            {
                Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, Tool.GetModItem("FargowiltasSouls","DeviBag")), Tool.GetModItem("FargowiltasSouls","DeviBag"), 2000);
                Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, Tool.GetModItem("FargowiltasSouls", "CosmosBag")), Tool.GetModItem("FargowiltasSouls", "CosmosBag"), 2000);
                Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, Tool.GetModItem("FargowiltasSouls", "AbomBag")), Tool.GetModItem("FargowiltasSouls", "AbomBag"), 2000);
                Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, Tool.GetModItem("FargowiltasSouls", "MutantBag")), Tool.GetModItem("FargowiltasSouls", "MutantBag"), 2000);
                Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.LocalPlayer, Tool.GetModItem("FargowiltasSouls", "TrojanSquirrelBag")), Tool.GetModItem("FargowiltasSouls", "TrojanSquirrelBag"), 2000);
            }

            if (ModLoader.TryGetMod("CalamityMod",out Mod calamity))
            {
                Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.player[Main.myPlayer], Tool.GetModItem("CalamityMod", "AerialiteBar")), Tool.GetModItem("CalamityMod", "AerialiteBar"), 2000);
                Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.player[Main.myPlayer], Tool.GetModItem("CalamityMod", "CrynoicBar")), Tool.GetModItem("CalamityMod", "CryonicBar"), 2000);
                Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.player[Main.myPlayer], Tool.GetModItem("CalamityMod", "PerennialBar")), Tool.GetModItem("CalamityMod", "PerennialBar"), 2000);
                Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.player[Main.myPlayer], Tool.GetModItem("CalamityMod", "ScoriaBar")), Tool.GetModItem("CalamityMod", "ScoriaBar"), 2000);
                Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.player[Main.myPlayer], Tool.GetModItem("CalamityMod", "AstralBar")), Tool.GetModItem("CalamityMod", "AstralBar"), 2000);
                Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.player[Main.myPlayer], Tool.GetModItem("CalamityMod", "UelibloomBar")), Tool.GetModItem("CalamityMod", "UelibloomBar"), 2000);
                Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.player[Main.myPlayer], Tool.GetModItem("CalamityMod", "CosmiliteBar")), Tool.GetModItem("CalamityMod", "CosmiliteBar"), 2000);
                Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.player[Main.myPlayer], Tool.GetModItem("CalamityMod", "AerialiteBar")), Tool.GetModItem("CalamityMod", "AerialiteBar"), 2000);
                Tool.AddItem(ref shop, ref nextSlot, Tool.HaveItem(Main.player[Main.myPlayer], Tool.GetModItem("CalamityMod", "ShadowspecBar")), Tool.GetModItem("CalamityMod", "ShadowspecBar"), 2000);
            }
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

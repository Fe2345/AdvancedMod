using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Localization;
using System.Collections.Generic;

namespace AdvancedMod.NPCs.Town
{
    [AutoloadHead]
    public class Mutation : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("异变");
            DisplayName.AddTranslation(GameCulture.English, "Mutation");
            Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.Angler];
            //NPC总共帧图数，一般为16+下面两种帧的帧数
            NPCID.Sets.ExtraFramesCount[npc.type] = NPCID.Sets.ExtraFramesCount[NPCID.Angler];
            //额外活动帧，一般为5
            NPCID.Sets.AttackFrameCount[npc.type] = NPCID.Sets.AttackFrameCount[NPCID.Angler];
            //攻击帧，这个帧数取决于你的NPC攻击类型，射手填5，战士和投掷填4，法师填2，当然，也可以多填，就是不知效果如何（这里直接引用商人的）
            NPCID.Sets.DangerDetectRange[npc.type] = 1000;
            //巡敌范围，以像素为单位，这个似乎是半径
            NPCID.Sets.AttackType[npc.type] = NPCID.Sets.AttackType[NPCID.Angler];
            //攻击类型，一般为0，想要模仿其他NPC就填他们的ID
            NPCID.Sets.AttackTime[npc.type] = 20;
            //单次攻击持续时间，越短，则该NPC攻击越快（可以用来模拟长时间施法的NPC）
            NPCID.Sets.AttackAverageChance[npc.type] = 3;
            //NPC遇敌的攻击优先度，该数值越大则NPC遇到敌怪时越会优先选择逃跑，反之则该NPC越好斗。
            //最小一般为1，你可以试试0或负数LOL~
        }

        public override void SetDefaults()
        {
            npc.townNPC = true;
            npc.friendly = true;
            //如果你想写敌对NPC也行
            npc.width = 22;
            //碰撞箱宽
            npc.height = 32;
            //碰撞箱高            
            npc.aiStyle = 7;
            //必带项，如果你能自己写出城镇NPC的AI可以不带
            npc.damage = 10;
            //碰撞伤害，由于城镇NPC没有碰撞伤害所以可以忽略
            npc.defense = 150;
            //防御力
            npc.lifeMax = Main.expertMode ? 5000 : 1500;
            //生命值
            npc.HitSound = SoundID.NPCHit1;
            //受伤音效
            npc.DeathSound = SoundID.NPCDeath1;
            //死亡音效
            npc.knockBackResist = 0.1f;
            //抗击退性，数字越大抗性越低
            animationType = NPCID.Angler;
            //如果你的NPC属于除投掷类NPC以外的其他攻击类型，请带上，值可以填对应NPC的ID

            if (NPC.downedMechBossAny)
            {
                npc.lifeMax = Main.expertMode ? 10000 : 3000;
            }

            if (NPC.downedMoonlord)
            {
                npc.lifeMax = Main.expertMode ? 15000 : 4500;
            }
        }

        public override string TownNPCName()
        {
            string[] names = { "Devi", "KingSlime", "Trump", "Clound", "SwordOfWar" };
            return Main.rand.Next(names);
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
            button2 = "帮助";
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
                    Item.NewItem(Main.LocalPlayer.Center, ModContent.ItemType<Items.Summon.MutationCore>(), 1);
                    Item.NewItem(Main.LocalPlayer.Center, ModContent.ItemType<Items.Summon.ComplexBossSummons_PreHardmode>(), 1);
                }
            }
        }

        public override void SetupShop(Chest shop, ref int nextSlot)
        {
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Potion.WonderPotion>());
            shop.item[nextSlot].value = 500;
        }
    }
}

﻿using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Utilities;
using AdvancedMod.Items.Weapon;
using AdvancedMod.Items.Potion;
using AdvancedMod.Items.Mateiral;
using AdvancedMod.Items.Accessory;
using AdvancedMod.Buffs.Not_DeBuff;
using AdvancedMod.Projectiles;
using AdvancedMod.Utils;

namespace AdvancedMod.NPCs.Town
{
    [AutoloadHead]
    public class Madman : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("疯子");
            //该NPC的游戏内显示名
            Main.npcFrameCount[npc.type] = 20;
            //NPC总共帧图数，一般为16+下面两种帧的帧数
            NPCID.Sets.ExtraFramesCount[npc.type] = 5;
            //额外活动帧，一般为5
            NPCID.Sets.AttackFrameCount[npc.type] = 0;
            //攻击帧，这个帧数取决于你的NPC攻击类型，射手填5，战士和投掷填4，法师填2，当然，也可以多填，就是不知效果如何（这里直接引用商人的）
            NPCID.Sets.DangerDetectRange[npc.type] = 1000;
            //巡敌范围，以像素为单位，这个似乎是半径
            NPCID.Sets.AttackType[npc.type] = NPCID.Sets.AttackType[NPCID.Guide];
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
            npc.lifeMax = Main.expertMode ? 1500 : 750;
            //生命值
            npc.HitSound = SoundID.NPCHit1;
            //受伤音效
            npc.DeathSound = SoundID.NPCDeath1;
            //死亡音效
            npc.knockBackResist = 0.3f;
            //抗击退性，数字越大抗性越低
        }

        public override string TownNPCName()
        {
            string[] names = { "qwertyuiop", "asdfhjkl", "zxcvbnm" };
            return Main.rand.Next(names);
        }

        public override void FindFrame(int frameHeight)
        {
            base.FindFrame(frameHeight);
        }

        public override bool CanTownNPCSpawn(int numTownNPCs, int money)
        {
            //该入住条件为：已拥有三个或以上的城镇NPC，玩家拥有钱数大于等于一银，且击败克苏鲁之眼
            if (numTownNPCs >= 4)
            {
                return true;
            }
            return false;
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            {
                chat.Add("#$%&&&*()E$R%%DGJHW#$");
                return chat;
            }
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            //翻译“商店文本”
            button = "商店";
            button2 = "BUFF";
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
                int[] BuffList = Tool.DifferentArray(6, 205);
                for (int i = 0; i < 6; i++)
                {
                    Main.LocalPlayer.AddBuff(BuffList[i], 36000);
                }
            }
        }

        public override void SetupShop(Chest shop, ref int nextSlot)
        {
            int[] ItemList = Tool.DifferentArray(10, 3929);
            for (int i = 0; i < 10; i++)
            {
                shop.item[nextSlot].SetDefaults(ItemList[i]);
                shop.item[nextSlot].value = 5000;
                nextSlot++;
            }
        }
    }
}

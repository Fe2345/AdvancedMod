using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using AdvancedMod.Items.Potion;
using AdvancedMod.Items.Mateiral;
using AdvancedMod.Items.Accessory;
using AdvancedMod.Buffs.Not_DeBuff;
using AdvancedMod.Projectiles;
using System.Collections.Generic;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Personalities;

namespace AdvancedMod.NPCs.Town
{
    [AutoloadHead]
    public class Watcher : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("观察者");
            //该NPC的游戏内显示名
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.Cyborg];
            //NPC总共帧图数，一般为16+下面两种帧的帧数
            NPCID.Sets.ExtraFramesCount[NPC.type] = NPCID.Sets.ExtraFramesCount[NPCID.Cyborg];
            //额外活动帧，一般为5
            NPCID.Sets.AttackFrameCount[NPC.type] = NPCID.Sets.AttackFrameCount[NPCID.Cyborg];
            //攻击帧，这个帧数取决于你的NPC攻击类型，射手填5，战士和投掷填4，法师填2，当然，也可以多填，就是不知效果如何（这里直接引用商人的）
            NPCID.Sets.DangerDetectRange[NPC.type] = 1000;
            //巡敌范围，以像素为单位，这个似乎是半径
            NPCID.Sets.AttackType[NPC.type] = NPCID.Sets.AttackType[NPCID.Cyborg];
            //攻击类型，一般为0，想要模仿其他NPC就填他们的ID
            NPCID.Sets.AttackTime[NPC.type] = 20;
            //单次攻击持续时间，越短，则该NPC攻击越快（可以用来模拟长时间施法的NPC）
            NPCID.Sets.AttackAverageChance[NPC.type] = 3;
            //NPC遇敌的攻击优先度，该数值越大则NPC遇到敌怪时越会优先选择逃跑，反之则该NPC越好斗。
            //最小一般为1，你可以试试0或负数LOL~

            NPC.Happiness.SetNPCAffection<Chemist>(AffectionLevel.Love);
            NPC.Happiness.SetNPCAffection(NPCID.Cyborg, AffectionLevel.Like);
            NPC.Happiness.SetNPCAffection(NPCID.TaxCollector, AffectionLevel.Dislike);
            NPC.Happiness.SetNPCAffection<Madman>(AffectionLevel.Hate);

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
            NPC.lifeMax = Main.expertMode ? 1500 : 750;
            //生命值
            NPC.HitSound = SoundID.NPCHit1;
            //受伤音效
            NPC.DeathSound = SoundID.NPCDeath1;
            //死亡音效
            NPC.knockBackResist = 0.3f;
            //抗击退性，数字越大抗性越低
        }

        public override List<string> SetNPCNameList()
        {
            string[] names = { "Redstone","Minecraft","Giovana","Phantom","Advanced","Diagram","Bizzare","TieGaoMC","Mutant","Brando"};
            return new List<string>(names);
        }

        public override void FindFrame(int frameHeight)
        {
            base.FindFrame(frameHeight);
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement("Mods.AdvancedMod.Bestiary.Watcher")
            });
        }

        public override bool CanTownNPCSpawn(int numTownNPCs, int money)
        {
            //该入住条件为：已拥有三个或以上的城镇NPC，玩家拥有钱数大于等于一银，且击败克苏鲁之眼
            if (NPC.downedMoonlord)
            {
                return true;
            }
            return false;
        }

        public override string GetChat()
        {
            List<string> chat = new List<string>()
            {
                "魔眼之神已经陨落了……",
                "我怎么感觉月亮领主像是个擦玻璃的一样？",
                "这个世界难道是有Bug的吗？",
                "吾心吾行澄如明镜，所作所为皆为『正义』"
            };

            if (NPC.homeless)
            {
                chat.Add("世间最荒唐的事情，无过于我可以洞察万物但连个家都没有");
            }

            if (Main.eclipse)
            {
                chat.Add("太阳的消失因一些机械而起，但那些机械不过是尘埃而已");
            }

            if (Main.bloodMoon)
            {
                chat.Add("让我想想,这红色的月亮让我想到了什么陈旧的往事？");
            }

            if (Main.raining)
            {
                chat.Add("通过我的眼睛，我可以看到雨水的美好");
            }

            return Main.rand.Next(chat);
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            //翻译“商店文本”
            button = "商店";
            button2 = "祝福";
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
                if (Utils.Tool.CheckBossAlive())
                {
                    Main.LocalPlayer.AddBuff(ModContent.BuffType<Buffs.Not_DeBuff.Fate>(), 54000);
                }
                else
                {
                    Main.LocalPlayer.AddBuff(ModContent.BuffType<Reward_Of_The_God_Of_Eye>(),54000);
                }
            }
        }

        public override void SetupShop(Chest shop, ref int nextSlot)
        {
            //设置商品
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapon.Magic.PhantomLight>());
            //设置价格
            shop.item[nextSlot].value = 5000000;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<SiliconBar>());
            shop.item[nextSlot].value = 500000;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<InsightBar>());
            shop.item[nextSlot].value = 1000000;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<AdvancedBar>());
            shop.item[nextSlot].value = 3000000;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<AdvancedWing>());
            shop.item[nextSlot].value = 15000000;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<White_Potion>());
            shop.item[nextSlot].value = 1000000;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<TimePieces>());
            shop.item[nextSlot].value = 1000000;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Accessory.Symbols.SymbolOfNature>());
            shop.item[nextSlot].value = 10000;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapon.Melee.RoarOfFlame>());
            shop.item[nextSlot].value = 50000;
        }

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = 60;
            knockback = 3f;
        }
        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 1;
            randExtraCooldown = 1;
            //间隔的算法：实际间隔会大于或等于cooldown的值且总是小于cooldown+randExtraCooldown的总和（TR总整这些莫名其妙的玩意）
        }
        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            projType = ModContent.ProjectileType<Grass_Staff_Projectiles>();
            //使用艹杖的弹幕
            attackDelay = 120;
            //NPC在出招后多长时间才会发射弹幕
        }
        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            multiplier = 15f;
            //弹幕射速
            gravityCorrection = 0f;
            //重力修正，描述作用是抛射物在发射时向上偏移的程度，但我感觉影响不大
            randomOffset = 1f;
            //这个是NPC的攻击精准度，数值越低瞄的越准，最低为0
        }
    }
}

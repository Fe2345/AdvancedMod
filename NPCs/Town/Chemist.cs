using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Utilities;

namespace AdvancedMod.NPCs.Town
{
    [AutoloadHead]
    public class Chemist : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("化学家");
            //该NPC的游戏内显示名
            Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.Guide];
            //NPC总共帧图数，一般为16+下面两种帧的帧数
            NPCID.Sets.ExtraFramesCount[npc.type] = NPCID.Sets.ExtraFramesCount[NPCID.Guide];
            //额外活动帧，一般为5
            NPCID.Sets.AttackFrameCount[npc.type] = NPCID.Sets.AttackFrameCount[NPCID.Guide];
            //攻击帧，这个帧数取决于你的NPC攻击类型，射手填5，战士和投掷填4，法师填2，当然，也可以多填，就是不知效果如何（这里直接引用商人的）
            NPCID.Sets.DangerDetectRange[npc.type] = 1000;
            //巡敌范围，以像素为单位，这个似乎是半径
            NPCID.Sets.AttackType[npc.type] = NPCID.Sets.AttackType[NPCID.Guide];
            //攻击类型，一般为0，想要模仿其他NPC就填他们的ID
            NPCID.Sets.AttackTime[npc.type] = 20;
            //单次攻击持续时间，越短，则该NPC攻击越快（可以用来模拟长时间施法的NPC）
            NPCID.Sets.AttackAverageChance[npc.type] = 2;
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
            npc.lifeMax = Main.expertMode ? 500 : 150;
            //生命值
            npc.HitSound = SoundID.NPCHit1;
            //受伤音效
            npc.DeathSound = SoundID.NPCDeath1;
            //死亡音效
            npc.knockBackResist = 0.3f;
            //抗击退性，数字越大抗性越低
            animationType = NPCID.Guide;
            //如果你的NPC属于除投掷类NPC以外的其他攻击类型，请带上，值可以填对应NPC的ID

            if (NPC.downedMechBossAny)
            {
                npc.lifeMax = Main.expertMode ? 1000 : 300;
            }

            if (NPC.downedMoonlord)
            {
                npc.lifeMax = Main.expertMode ? 1500 : 450;
            }
        }

        public override string TownNPCName()
        {
            string[] names = { "Lavoisier", "CH3COOH", "Oxygen","CH3COOCH2CH3","Side" };
            return Main.rand.Next(names);
        }

        public override void FindFrame(int frameHeight)
        {
            base.FindFrame(frameHeight);
        }

        public override bool CanTownNPCSpawn(int numTownNPCs, int money)
        {
            //该入住条件为：护士存在且有至少10个NPC
            if (NPC.AnyNPCs(NPCID.Nurse) && numTownNPCs >= 10)
            {
                return true;
            }
            return false;
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            {
                if (!Main.bloodMoon && !Main.eclipse)
                {
                    //无家可归时
                    if (npc.homeless)
                    {
                        chat.Add("我已经设计好了一个绝妙的实验，但是我的实验室离我太远了。");
                    }
                    else
                    {
                        chat.Add($"{Main.LocalPlayer.name}，你知道吗？我有几乎所有的药剂！");
                    }
                }
                //日食时
                if (Main.eclipse)
                {
                    chat.Add("太阳去哪里了？？？这样我的反应就无法进行了！！！");
                }
                //血月时
                if (Main.bloodMoon)
                {
                    chat.Add("这红色的月亮让水中掺杂了杂质，我的实验又失败了。");
                }
                if (Main.raining)
                {
                    chat.Add("雨天的雨水，正是最好的溶剂！");
                }
                if (NPC.FindFirstNPC(NPCID.Nurse) >= 0 )
                {
                    chat.Add($"{Main.npc[NPCID.Nurse].GivenName}和我都一直为泰拉世界的医药学而努力。");
                }
                return chat;
            }
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
             //翻译“商店文本”
            button = "商店";
            button2 = "移除减益";
        }

        public override void OnChatButtonClicked(bool firstButton, ref bool shop)
        {
            int[] debuffs = {30,20,24,70,22,80,35,23,31,32,197,33,36,195,196,37,38,39,69,46,47,103,149,156,164,163,144,148,145,94,21,88,68,67,25,119,120,86,194,199 };
            //如果按下第一个按钮，则开启商店
            if (firstButton)
            {
                shop = true;
            }
            else if (!firstButton)
            {
                for (int i = 0;i < debuffs.Length; i++)
                {
                    Main.LocalPlayer.DelBuff(debuffs[i]);
                }
            }
        }

        public override void SetupShop(Chest shop, ref int nextSlot)
        {
            if (Main.dayTime)
            {
                shop.item[nextSlot].SetDefaults(ItemID.AmmoReservationPotion);
                //设置价格
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.ArcheryPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.BattlePotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.BuilderPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.CalmingPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.CratePotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.EndurancePotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.FeatherfallPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.FishingPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(2329);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.FlipperPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.GillsPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.GravitationPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.HunterPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.InfernoPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.HeartreachPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.InvisibilityPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.IronskinPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.LifeforcePotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.LovePotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.MagicPowerPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.ManaRegenerationPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.MiningPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.NightOwlPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.ObsidianSkinPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.RagePotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.RegenerationPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.ShinePotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.SonarPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.SpelunkerPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.Diamond);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.Topaz);
                shop.item[nextSlot].value = 500;
                nextSlot++;
            }
            else if (!Main.dayTime)
            {
                shop.item[nextSlot].SetDefaults(ItemID.StinkPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.SummoningPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.SwiftnessPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.ThornsPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.TitanPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.WarmthPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.WaterWalkingPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.WrathPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.WormholePotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.RecallPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.TeleportationPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.Mushroom);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.BottledHoney);
                shop.item[nextSlot].value = 500;
                shop.item[nextSlot].SetDefaults(ItemID.BottledWater);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.LesserHealingPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.HealingPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.GreaterHealingPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.LesserManaPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.ManaPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.GreaterManaPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.Honeyfin);
                shop.item[nextSlot].value = 500;
                nextSlot++;
            }
            if (NPC.downedMoonlord)
            {
                shop.item[nextSlot].SetDefaults(ItemID.SuperManaPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.SuperHealingPotion);
                shop.item[nextSlot].value = 500;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Potion.GodBlood>());
                shop.item[nextSlot].value = 10000;
            }
            if (NPC.AnyNPCs(NPCID.SantaClaus))
            {
                shop.item[nextSlot].SetDefaults(ItemID.Eggnog);
                shop.item[nextSlot].value = 500;
                nextSlot++;
            }
            if (NPC.AnyNPCs(NPCID.SkeletonMerchant))
            {
                shop.item[nextSlot].SetDefaults(ItemID.StrangeBrew);
                shop.item[nextSlot].value = 500;
                nextSlot++;
            }
            //设置商品
            
        }
        /*
        //设置该NPC的近战/抛射物伤害和击退（取决于NPC攻击类型）
        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = 55;
            knockback = 3f;
        }
        */
        //NPC攻击一次后的间隔
        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 1;
            randExtraCooldown = 1;
            //间隔的算法：实际间隔会大于或等于cooldown的值且总是小于cooldown+randExtraCooldown的总和（TR总整这些莫名其妙的玩意）
        }

        //弹幕设置
        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            if (!Main.hardMode)
            {
                projType = ProjectileID.FlamingArrow;
                //使用烈焰箭的弹幕
                attackDelay = 120;
                //NPC在出招后多长时间才会发射弹幕
            }
            else
            {
                projType = ProjectileID.IchorArrow;
                //使用灵液箭的弹幕
                attackDelay = 60;
                //NPC在出招后多长时间才会发射弹幕
            }
        }

        //射手NPC专属：手持武器（选带项）
        public override void DrawTownAttackGun(ref float scale, ref int item, ref int closeness)
        {
            if (!Main.hardMode)
            {
                scale = 1f;
                //大小
                item = ItemID.PlatinumBow;
                //手持武器类型，铂金弓
                closeness = 18;
                //武器更接近NPC(以像素为单位, 数字越大离NPC越近, 当武器位置不对时调整)
            }
            else
            {
                scale = 1f;
                //大小
                item = ItemID.Tsunami;
                //手持武器类型，海啸
                closeness = 18;
                //武器更接近NPC(以像素为单位, 数字越大离NPC越近, 当武器位置不对时调整)
            }
        }
    }
}

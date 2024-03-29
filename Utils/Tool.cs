﻿using System;
using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Utils
{
    public class Tool
    {
        public static List<int> debuffs = new List<int>{BuffID.Poisoned,BuffID.Bleeding,BuffID.OnFire,70,BuffID.Darkness,
                                          BuffID.Blackout,BuffID.Silenced,BuffID.Cursed,BuffID.Confused,
                                          BuffID.Slow,197,BuffID.Weak,BuffID.BrokenArmor,BuffID.WitheredArmor,
                                          BuffID.Horrified,BuffID.TheTongue,BuffID.CursedInferno,BuffID.Ichor,
                                          BuffID.Frostburn,BuffID.Chilled,BuffID.Frozen,BuffID.Webbed,BuffID.Stoned,
                                          164,BuffID.Obstructed,BuffID.Electrified,148,BuffID.MoonLeech,BuffID.ManaSickness,
                                          BuffID.PotionSickness,BuffID.ChaosState,BuffID.Suffocation,BuffID.Burning,
                                          BuffID.Tipsy,BuffID.Lovestruck,BuffID.Stinky,BuffID.WaterCandle,194,
                                          199,332,BuffID.Hunger,BuffID.Starving,BuffID.Midas,BuffID.Oiled,BuffID.Wet,
                                          BuffID.Slimed,320,BuffID.ShadowFlame,BuffID.BetsysCurse,169,BuffID.Daybreak,
                                          183,186,BuffID.Dazed,ModContent.BuffType<Buffs.Debuff.ElectromagneticInduction>(),
                                          ModContent.BuffType<Buffs.Debuff.TheWorld>(),ModContent.BuffType<Buffs.Debuff.VoidPressure>()
        };

        public static int[] DifferentArray(int n, int max)
        {
            int[] l = { 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};
            for (int i = 0;i < n; i++)
            {
            a: l[i] = Main.rand.Next(max);
                if (i > 0)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (l[j] == l[i])
                        {
                            j = 0;
                            goto a;
                        }
                    }
                }
            }
            return l;
        }

        public static Vector2 TurnVector(Vector2 PreVector,float angle)
        {
            Vector2 TurnedVector = new Vector2((float)(PreVector.X * Math.Cos(angle) - PreVector.Y * Math.Sin(angle)), (float)(PreVector.Y * Math.Cos(angle) + PreVector.X * Math.Sin(angle)));

            return TurnedVector;
        }

        public static void AddItem(ref Chest shop, ref int nextSlot, bool Check, int type, int value)
        {
            if (!Check) return;
            shop.item[nextSlot].SetDefaults(type);
            shop.item[nextSlot].value = value;
            nextSlot++;
        }

        public static bool CheckBossAlive()
        {
            foreach (var npc in Main.npc)
            {
                foreach (var boss in AdvancedMod.Bosses)
                {
                    if (npc.type == boss) return true;
                }
            }

            return false;
        }

        public static NPC GetClosestNPC(Vector2 position,bool boss)
        {
            int NPCIndex = 0;
            if (!boss)
            {
                for (int i = 0; i < Main.npc.Length; i++)
                {
                    if (Vector2.Distance(position,Main.npc[i].Center) < Vector2.Distance(position, Main.npc[NPCIndex].Center) && !Main.npc[i].friendly)
                    {
                        NPCIndex = i;
                    }
                }
            }
            else
            {
                for (int i = 0; i < Main.npc.Length; i++)
                {
                    if (Vector2.Distance(position, Main.npc[i].Center) < Vector2.Distance(position, Main.npc[NPCIndex].Center) && !Main.npc[i].friendly && Main.npc[i].boss)
                    {
                        NPCIndex = i;
                    }
                }
            }

            return Main.npc[NPCIndex];
        }

        public static NPC FindClosestEnermy(Vector2 position)
        {
            int NPCIndex = 0;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Vector2.Distance(position, Main.npc[i].Center) < Vector2.Distance(position, Main.npc[NPCIndex].Center) && !Main.npc[i].friendly)
                {
                    NPCIndex = i;
                }
            }

            return Main.npc[NPCIndex];
        }

        public static bool AccessoryEquiped(int type,Player player)
        {
            foreach (var Item in player.armor)
            {
                if (type == Item.type) return true;
            }

            return false;
        }

        public static IItemDropRule BossBagDropCustom(int itemType, int amount = 1)
        {
            return new DropLocalPerClientAndResetsNPCMoneyTo0(itemType, 1, amount, amount, null);
        }

        public static void NewModItem(Vector2 spawnPos,string ModName,string ItemName,int amount = 1)
        {
            if (ModContent.TryFind(ModName,ItemName,out ModItem item))
            {
                Item.NewItem(null, spawnPos, item.Type, amount);
            }
        }

        public static int GetModItem(string modName,string itemName)
        {
            if (ModContent.TryFind(modName,itemName,out ModItem item))
            {
                return item.Type;
            }

            return 0;
        }

        public static int GetModNPC(string modName,string npcName)
        {
            if (ModContent.TryFind(modName,npcName,out ModNPC npc))
            {
                return npc.Type;
            }

            return 0;
        }

        public static int GetModBuff(string modName,string buffName)
        {
            if (ModContent.TryFind(modName,buffName,out ModBuff buff))
            {
                return buff.Type;
            }
            return 0;
        }

        public static bool HaveItem(Player player,int type)
        {
            foreach(Item item in player.inventory)
            {
                if (item.type == type) return true;
            }

            return false;
        }

        //环绕式追逐算法（返回速度）
        //参数： vel 自身速度 self 自身位置 target 目标位置 r 环绕半径 p 平滑度
        //平滑度：越大越突变，越小越平滑
        public static Vector2 ChaseAround(Vector2 vel,Vector2 self,Vector2 target,float r,float p)
        {
            Vector2 diff = target - self;
            Vector2 des = target + new Vector2((float)(r / Math.Pow(1 + Math.Pow(diff.X / diff.Y, 2), 0.5)), (float)(-1 * r * diff.X / (diff.Y * Math.Pow(1 + Math.Pow(diff.X / diff.Y, 2), 0.5))));

            return (vel + des * p) / (1 + p);
        }

        //圆锥曲线式环绕
        public static Vector2 CircleAround(Vector2 vel,Vector2 self,Vector2 target,float r)
        {
            float v = (float)(Math.Pow((float)vel.Length(), 2) / r);
            Vector2 diff = target - self;
            float cos = (float)(diff.X / Math.Pow(1 + Math.Pow(diff.Y / diff.X, 2), 0.5));
            float sin = (float)(diff.Y / Math.Pow(1+ Math.Pow(diff.Y/diff.X,2),0.5));
            return vel + new Vector2(v*cos,v*sin);
        }
    }
}

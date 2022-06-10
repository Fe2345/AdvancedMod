using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                                          183,186,ModContent.BuffType<Buffs.Debuff.ElectromagneticInduction>(),
                                          ModContent.BuffType<Buffs.Debuff.TheWorld>()
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
            for (int i = 0;i < Main.npc.Length; i++)
            {
                if (Main.npc[i].boss)
                {
                    return true;
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

        public static bool AccessoryEquiped(int type,Player player)
        {
            foreach (var Item in player.armor)
            {
                if (type == Item.type) return true;
            }

            return false;
        }
    }
}

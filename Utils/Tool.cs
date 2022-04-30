using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Microsoft.Xna.Framework;

namespace AdvancedMod.Utils
{
    public class Tool
    {
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
    }
}

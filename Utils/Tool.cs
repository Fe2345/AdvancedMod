using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

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
    }
}

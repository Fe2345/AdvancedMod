using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AdvancedMod
{
    public class AdvancedPlayer : ModPlayer
    {
        public static int Energy;
        public static int UsedSiliconHeartCount;
        public static bool RecievedInitBag;

        public static bool SymbolOfTown;

        public override void Initialize()
        {
            Energy = 2048;
            UsedSiliconHeartCount = 0;
            RecievedInitBag = false;
        }

        public override TagCompound Save()
        {
            return new TagCompound
            {
                {"Energy",Energy },
                {"UsedSiliconHeartCount",UsedSiliconHeartCount },
                {"RecievedInitBag",RecievedInitBag }
            };
        }

        public override void Load(TagCompound tag)
        {
            Energy = tag.GetInt("Energy");
            UsedSiliconHeartCount = tag.GetInt("UsedSiliconHeartCount");
            RecievedInitBag = tag.GetBool("RecievedInitBag");
        }

        public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit)
        {
            if (SymbolOfTown)
            {
                if (Main.rand.Next(5) == 1)
                {
                    for (int i = 0;i < Main.npc.Length; i++)
                    {
                        if (Main.npc[i].townNPC)
                        {
                            Main.npc[i].life -= (int)damage;
                        }
                    }
                }
                
                damage = 0;
            }
        }
    }
}

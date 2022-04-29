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
    }
}

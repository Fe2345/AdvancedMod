using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AdvancedMod
{
    public class AdvancedPlayer : ModPlayer
    {
        public int Energy;
        public int UsedSiliconHeartCount;

        public override TagCompound Save()
        {
            return new TagCompound
            {
                {"Energy",Energy },
                {"UsedSiliconHeartCount",UsedSiliconHeartCount }
            };
        }

        public override void Load(TagCompound tag)
        {
            Energy = tag.GetInt("Energy");
            UsedSiliconHeartCount = tag.GetInt("UsedSiliconHeartCount");
        }
    }
}

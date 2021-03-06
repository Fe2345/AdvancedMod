using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.ModLoader.IO;
using System.Collections.Generic;

namespace AdvancedMod
{
    public class AdvancedWorld : ModSystem
    {
        public static bool MutationMode;
        public static bool downedPolarMessager;
        public static bool downedTreeDiagrammer;
        public static bool downedTriangle;
        public static bool downedGodOfEye;
        public static bool downedThrougher;
        public static bool downedGodOfTime;
        public static bool downedTheWorld;
        public static bool downedMutationBosses;

        public override void  SaveWorldData(TagCompound tag)
        {
            List<string> data = new List<string>();
            if (MutationMode) data.Add("MutationMode");
            if (downedPolarMessager) data.Add("PolarMessager");
            if (downedTreeDiagrammer) data.Add("TreeDiagrammer");
            if (downedTriangle) data.Add("Triangle");
            if (downedGodOfEye) data.Add("GodOfEye");
            if (downedThrougher) data.Add("Througher");
            if (downedGodOfTime) data.Add("GodOfTime");
            if (downedTheWorld) data.Add("TheWorld");
            if (downedMutationBosses) data.Add("MutationBosses");

            tag.Add("data", data);
        }

        public override void LoadWorldData(TagCompound tag)
        {
            IList<string> data = tag.GetList<string>("data");
            MutationMode = data.Contains("MutationMode");
            downedPolarMessager = data.Contains("PolarMessager");
            downedTreeDiagrammer = data.Contains("TreeDiagrammer");
            downedTriangle = data.Contains("Triangle");
            downedGodOfEye = data.Contains("GodOfEye");
            downedThrougher = data.Contains("Througher");
            downedGodOfTime = data.Contains("GodOfTime");
            downedTheWorld = data.Contains("TheWorld");
            downedMutationBosses = data.Contains("MutationBosses");
        }
    }
}

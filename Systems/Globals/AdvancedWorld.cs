using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI.Chat;
using Terraria.ModLoader.IO;
using ReLogic.Content;
using MonoMod.RuntimeDetour.HookGen;
using System.Collections.Generic;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Reflection;

namespace AdvancedMod
{
    public class AdvancedWorld : ModSystem
    {
        public static bool MutationMode;
		public static bool EndMode;

        //Boss击败记录
        public static bool downedTreeDiagrammer;
		public static bool downedTheEnd; //作为最终boss击败的保留值，用于设计一些需要击败最终boss才会解禁的效果

        public override void  SaveWorldData(TagCompound tag)
        {
            List<string> data = new List<string>();
            if (MutationMode) data.Add("MutationMode");
			if (EndMode) data.Add("EndMode");
            if (downedTreeDiagrammer) data.Add("downedTreeDiagrammer");

            tag.Add("data", data);
        }

        public override void LoadWorldData(TagCompound tag)
        {
            IList<string> data = tag.GetList<string>("data");
            MutationMode = data.Contains("MutationMode");
			EndMode = data.Contains("EndMode");
            downedTreeDiagrammer = data.Contains("downedTreeDiagrammer");
        }

        public override void PostUpdateWorld()
        {
            
        }

        public override void PostWorldGen()
        {

            
        }
	}
}

using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System;

namespace AdvancedMod.ModCompatibilities
{
    public class BossChecklistCompatibility : ModCompatibility
    {
        public BossChecklistCompatibility(Mod callerMod) : base(callerMod, "BossChecklist")
        {
        }

        private void InitializeBosses()
        {
            Mod BossChecklist = ModLoader.GetMod("BossCheckLiat");
            if (BossChecklist != null)
            {
                BossChecklist.Call("AddBossWithInfo", "树状图设计者", 9, 50f, (Func<bool>)(() => AdvancedWorld.downedTreeDiagrammer), "使用[i:{ModContent.ItemType<Items.Summon.DiagrammerWreckage>}]召唤");
            }
        }
    }
}

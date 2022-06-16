using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using Terraria;

namespace AdvancedMod.ItemDropRules
{
    public class VoidModeDropRule : IItemDropRuleCondition
    {
        public bool CanDrop(DropAttemptInfo info)
        {
            if (info.IsInSimulation) return false;

            return AdvancedWorld.MutationMode && info.IsMasterMode;
        }

        public bool CanShowItemDropInUI()
        {
            return AdvancedWorld.MutationMode && Main.masterMode;
        }

        public string GetConditionDescription()
        {
            return $"这是[i:{ModContent.BuffType<Buffs.Debuff.VoidPressure>()}]虚空模式下的掉率";
        }
    }
}

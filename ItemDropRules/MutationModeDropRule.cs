using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;

namespace AdvancedMod.ItemDropRules
{
    public class MutationModeDropRule : IItemDropRuleCondition
    {
        public bool CanDrop(DropAttemptInfo info)
        {
            if (info.IsInSimulation) return false;

            return AdvancedWorld.MutationMode;
        }

        public bool CanShowItemDropInUI()
        {
            return AdvancedWorld.MutationMode;
        }

        public string GetConditionDescription()
        {
            return $"这是[i:{ModContent.ItemType<Items.Summon.MutationCore>()}]异变模式下的掉率";
        }
    }
}

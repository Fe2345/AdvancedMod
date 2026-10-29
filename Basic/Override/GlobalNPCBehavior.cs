using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Basic.Override;

/// <summary>按实体类型挂载的行为覆盖入口；具体内容和状态由派生类负责。</summary>
public abstract class GlobalNPCBehavior : GlobalNPC
{
    public sealed override bool InstancePerEntity => true;
    protected abstract bool MatchesType(int type);
    protected abstract bool OverridesAI(NPC npc);
    protected abstract void BehaviorAI(NPC npc);

    public sealed override bool AppliesToEntity(NPC entity, bool lateInstantiation) => MatchesType(entity.type);

    public sealed override bool PreAI(NPC npc)
    {
        if (!OverridesAI(npc)) return true;
        BehaviorAI(npc);
        return false;
    }
}

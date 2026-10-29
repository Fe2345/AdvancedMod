using Terraria;
using Terraria.ID;

namespace AdvancedMod.Contents.Hallow.NPCs;

public sealed class SkeletronPrimeBehavior : HallowMechanicalBehavior
{
    protected override bool MatchesType(int type) => type is NPCID.SkeletronPrime or NPCID.PrimeCannon or NPCID.PrimeSaw or NPCID.PrimeVice or NPCID.PrimeLaser;
    // 自定义机械骷髅王行为留待后续实现；神圣版本明确不运行原版 AI。
    protected override void BehaviorAI(NPC npc) { }
}

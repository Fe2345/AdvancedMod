using Terraria;
using Terraria.ID;

namespace AdvancedMod.Contents.Hallow.NPCs;

public sealed class TwinsBehavior : HallowMechanicalBehavior
{
    protected override bool MatchesType(int type) => type is NPCID.Retinazer or NPCID.Spazmatism;
    // 自定义双子行为留待后续实现；神圣版本明确不运行原版 AI。
    protected override void BehaviorAI(NPC npc) { }
}

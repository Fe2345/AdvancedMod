using Terraria;
using Terraria.ID;

namespace AdvancedMod.Contents.Hallow.NPCs;

public sealed partial class DestroyerBehavior : HallowMechanicalBehavior
{
    protected override bool MatchesType(int type) => type is NPCID.TheDestroyer or NPCID.TheDestroyerBody or NPCID.TheDestroyerTail;
    public override void SetDefaults(NPC npc)
    {
        base.SetDefaults(npc);
        State = MovementState.Burrowing;
    }
    protected override void BehaviorAI(NPC npc) => TickStateMachine(npc);
}

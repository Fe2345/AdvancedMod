using System.IO;
using AdvancedMod.Basic.Override;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader.IO;

namespace AdvancedMod.Contents.Hallow.NPCs;

/// <summary>仅挂在三王及其对应部件上的召唤版本标记，不随当前环境切换。</summary>
public abstract class HallowMechanicalBehavior : GlobalNPCBehavior
{
    public bool Hallow { get; private set; }
    protected sealed override bool OverridesAI(NPC npc) => Hallow;
    public override void SetDefaults(NPC npc) => Hallow = false;

    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        if (source is EntitySource_BossSpawn { Target: Player player })
            Hallow = player.GetModPlayer<HallowMechanicalSummonPlayer>().Consume(npc.type);
        else if (source is EntitySource_Parent { Entity: NPC parent } &&
                 TryGet(parent, out var behavior) && behavior.GetType() == GetType())
            Hallow = behavior.Hallow;
    }

    internal void SetHallow(NPC npc, bool value)
    {
        if (Hallow == value) return;
        Hallow = value;
        npc.netUpdate = true;
    }

    public override void SendExtraAI(NPC npc, BitWriter bits, BinaryWriter writer) => bits.WriteBit(Hallow);
    public override void ReceiveExtraAI(NPC npc, BitReader bits, BinaryReader reader) => Hallow = bits.ReadBit();

    internal static bool TryGet(NPC npc, out HallowMechanicalBehavior behavior)
    {
        if (npc.TryGetGlobalNPC<DestroyerBehavior>(out var destroyer)) behavior = destroyer;
        else if (npc.TryGetGlobalNPC<TwinsBehavior>(out var twins)) behavior = twins;
        else if (npc.TryGetGlobalNPC<SkeletronPrimeBehavior>(out var prime)) behavior = prime;
        else { behavior = null; return false; }
        return true;
    }
}

using System;
using System.IO;
using AdvancedMod.Basic.Graphics.BossBars;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AdvancedMod.Contents.Hallow.NPCs;

/// <summary>普通／神圣毁灭者的探测怪账本；释放不扣除，只有所属探测怪死亡才扣除。</summary>
public sealed class DestroyerBarState : GlobalNPC
{
    private static int nextEncounter;
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.type is NPCID.TheDestroyer or NPCID.Probe;
    public int Encounter { get; private set; }
    public int Owner { get; private set; } = -1;
    public int Maximum { get; private set; }
    public int Defeated { get; private set; }
    public int Remaining => Math.Max(0, Maximum - Defeated);
    public float RedRatio { get; private set; }
    public float YellowRatio { get; private set; }
    private int delay;
    private bool visualInitialized;
    public const int DamageHoldTicks = 24;
    public const float DamageCatchupPerTick = 0.012f;

    public override void SetDefaults(NPC npc)
    {
        Encounter = Maximum = Defeated = delay = 0; Owner = -1;
        RedRatio = YellowRatio = 0; visualInitialized = false;
        if (npc.type == NPCID.TheDestroyer) npc.BossBar = ModContent.GetInstance<DestroyerBossBar>();
    }
    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        if (npc.type == NPCID.TheDestroyer) { Encounter = ++nextEncounter; Owner = npc.whoAmI; return; }
        if (source is not EntitySource_Parent { Entity: NPC parent }) return;
        int root = parent.type == NPCID.TheDestroyer ? parent.whoAmI : parent.realLife;
        if (root < 0 || root >= Main.maxNPCs) return;
        NPC head = Main.npc[root];
        if (!head.active || head.type != NPCID.TheDestroyer) return;
        Owner = root; Encounter = head.GetGlobalNPC<DestroyerBarState>().Encounter;
    }
    public override void PostAI(NPC npc)
    {
        if (npc.type != NPCID.TheDestroyer) return;
        if (Main.netMode != NetmodeID.MultiplayerClient && Maximum == 0 && npc.ai[0] != 0)
        {
            foreach (NPC part in Main.ActiveNPCs)
                if (part.type == NPCID.TheDestroyerBody && part.realLife == npc.whoAmI) Maximum++;
                else if (part.type == NPCID.Probe && part.GetGlobalNPC<DestroyerBarState>() is var probe && probe.Owner == npc.whoAmI && probe.Encounter == Encounter) Maximum++;
            if (Maximum > 0) npc.netUpdate = true;
        }
        if (Main.dedServ) return;
        float ratio = npc.lifeMax > 0 ? Math.Clamp((float)npc.life / npc.lifeMax, 0, 1) : 0;
        if (!visualInitialized || ratio > RedRatio) { YellowRatio = ratio; delay = 0; visualInitialized = true; }
        else if (ratio < RedRatio) delay = DamageHoldTicks;
        RedRatio = ratio;
        if (delay > 0) delay--;
        else YellowRatio = Math.Max(RedRatio, YellowRatio - DamageCatchupPerTick);
    }
    public override void OnKill(NPC npc)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || npc.type != NPCID.Probe || Owner < 0 || Owner >= Main.maxNPCs) return;
        NPC head = Main.npc[Owner];
        if (!head.active || head.type != NPCID.TheDestroyer) return;
        var state = head.GetGlobalNPC<DestroyerBarState>();
        if (state.Encounter != Encounter) return;
        state.Defeated = Math.Min(state.Maximum, state.Defeated + 1); head.netUpdate = true;
    }
    public override void SendExtraAI(NPC npc, BitWriter bits, BinaryWriter writer)
    { writer.Write(Encounter); writer.Write(Owner); writer.Write(Maximum); writer.Write(Defeated); }
    public override void ReceiveExtraAI(NPC npc, BitReader bits, BinaryReader reader)
    { Encounter = reader.ReadInt32(); Owner = reader.ReadInt32(); Maximum = reader.ReadInt32(); Defeated = reader.ReadInt32(); }
}

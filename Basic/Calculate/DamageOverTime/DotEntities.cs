#nullable enable
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.IO;

namespace AdvancedMod.Basic.Calculate.DamageOverTime;

internal sealed class DotTargetState(Entity target)
{
    internal readonly Entity Target = target;
    internal readonly DotLedger Ledger = new();
    internal readonly DotFrameText Text = new();
    internal Rectangle Bounds;
    internal bool NeedsLifeSync;
    internal int NativeTextDamage;
    internal bool IsAlive => Target switch
    {
        NPC n => n.active && n.life > 0,
        Player p => p.active && !p.dead,
        _ => false
    };
    internal void Reset()
    {
        Ledger.Clear(); Text.Clear(); NativeTextDamage = 0; NeedsLifeSync = false;
        DotSystem.Forget(this);
    }
    internal void Apply(ref int lifeRegen, int count)
    {
        if (!Ledger.Apply(ref lifeRegen, count)) return;
        Bounds = Target.Hitbox;
        NeedsLifeSync = true;
    }
}

/// <summary>NPC 伤害在单人／服务器一次写入；实例重置时丢弃旧来源账本。</summary>
public sealed class DotNpc : GlobalNPC
{
    public override bool InstancePerEntity => true;
    internal DotTargetState? Current { get; private set; }
    internal bool ManagedText { get; private set; }
    internal DotTargetState GetState(NPC npc) => Current ??= new(npc);
    internal void EnableText(NPC npc)
    {
        if (ManagedText) return;
        ManagedText = true;
        npc.netUpdate = true;
        // 先同步标记再发生扣血，远端关闭自己的合并数字，避免与权威拆分结果重复。
        if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
    }
    public override void SendExtraAI(NPC npc, BitWriter bits, BinaryWriter writer) => bits.WriteBit(ManagedText);
    public override void ReceiveExtraAI(NPC npc, BitReader bits, BinaryReader reader)
    {
        bool managed = bits.ReadBit();
        if (Main.netMode == NetmodeID.MultiplayerClient) ManagedText = managed;
    }
    public override void SetDefaults(NPC npc) { Current?.Reset(); Current = null; ManagedText = false; }
    public override void UpdateLifeRegen(NPC npc, ref int damage)
    {
        if (Current is null || Main.netMode == NetmodeID.MultiplayerClient) return;
        if (!npc.active || npc.life <= 0 || npc.dontTakeDamage || npc.immortal) { Current.Reset(); return; }
        Current.Apply(ref npc.lifeRegen, npc.lifeRegenCount);
        // 不改 damage；保留原版实际分组规则，由引擎跳字确认本次分配量。
    }
}

/// <summary>使用较晚的恢复 Hook，避免一次性请求被前面的饥饿赋值覆盖。</summary>
public sealed class DotPlayer : ModPlayer
{
    private DotTargetState? state;
    internal DotTargetState State => state ??= new(Player);
    internal DotTargetState? Current => state;
    internal bool ManagedText { get; set; }
    internal void EnableText()
    {
        if (ManagedText) return;
        ManagedText = true;
        if (Main.netMode == NetmodeID.MultiplayerClient) DotNetwork.SendPlayerOwnership(Player.whoAmI, true);
    }
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        if (Main.netMode == NetmodeID.Server) DotNetwork.SendPlayerOwnership(Player.whoAmI, ManagedText, toWho);
    }
    public override void UpdateLifeRegen()
    {
        if (state is null || Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer) return;
        if (!Player.active || Player.dead || Player.ghost) { state.Reset(); return; }
        state.Apply(ref Player.lifeRegen, Player.lifeRegenCount);
    }
    private void Reset() { state?.Reset(); state = null; }
    public override void Initialize() { Reset(); ManagedText = false; }
    public override void UpdateDead() => Reset();
    public override void OnRespawn() => Reset();
    public override void OnEnterWorld() { Reset(); ManagedText = false; }
    public override void PlayerDisconnect() { Reset(); ManagedText = false; }
}

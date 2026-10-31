using System;
using System.IO;
using System.Collections.Generic;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Element;
using AdvancedMod.Basic.Element.Tml;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Hallow.NPCs;

/// <summary>只观察已经提交的神圣反应；玩家受伤仍沿用现有所属客户端权威。</summary>
public sealed class DestroyerReactionCounter : ModSystem
{
    internal const byte PacketKind = 131;
    private const int ReceiptWindow = 256;
    private static readonly Dictionary<int, (HashSet<long> Seen, Queue<long> Order)> receipts = new();
    public override void Load() => ElementHitSystem.Resolved += Resolved;
    public override void Unload() => ElementHitSystem.Resolved -= Resolved;
    public override void OnWorldLoad() => receipts.Clear();
    public override void OnWorldUnload() => receipts.Clear();
    internal static void ResetConnection(int player) => receipts.Remove(player);
    internal static bool AcceptReceipt(int player, long hit)
    {
        if (hit < 0) return false;
        if (!receipts.TryGetValue(player, out var receipt)) receipts[player] = receipt = (new(), new());
        if (!receipt.Seen.Add(hit)) return false;
        receipt.Order.Enqueue(hit);
        if (receipt.Order.Count > ReceiptWindow) receipt.Seen.Remove(receipt.Order.Dequeue());
        return true;
    }
    private static void Resolved(ElementResolution result)
    {
        if (!result.Committed || result.Outcome == null || !result.Plan.IsHallowed) return;
        var hit = result.Plan.Hit;
        if (hit.Target?.Kind != SourceKind.Player || hit.Source == null) return;
        int root = -1, encounter = -1;
        if (hit.Source.Kind == SourceKind.Npc && hit.Source.EntityIndex is int index && index >= 0 && index < Main.maxNPCs)
        {
            var npc = Main.npc[index];
            if (npc.active && npc.TryGetGlobalNPC<DestroyerBehavior>(out var behavior) && behavior.Hallow)
            { root = npc.type == NPCID.TheDestroyer ? index : npc.realLife; if (ValidHead(root)) encounter = Main.npc[root].GetGlobalNPC<DestroyerBarState>().Encounter; }
        }
        else if (hit.Source.Kind == SourceKind.Projectile && hit.Source.EntityIndex is int slot && slot >= 0 && slot < Main.maxProjectiles)
        {
            var projectile = Main.projectile[slot];
            if (projectile.identity == hit.Source.ProjectileIdentity && projectile.TryGetGlobalProjectile<DestroyerAttackOwner>(out var owner))
            { root = owner.Head; encounter = owner.Encounter; }
        }
        if (!ValidHead(root) || Main.npc[root].GetGlobalNPC<DestroyerBarState>().Encounter != encounter) return;
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            if (hit.Target.PlayerIndex != Main.myPlayer) return;
            var packet = ModContent.GetInstance<global::AdvancedMod.AdvancedMod>().GetPacket();
            packet.Write(PacketKind); packet.Write((short)root); packet.Write(encounter); packet.Write(hit.HitId); packet.Send();
        }
        else Main.npc[root].GetGlobalNPC<DestroyerBehavior>().RecordReaction(Main.npc[root]);
    }
    private static bool ValidHead(int index) => index >= 0 && index < Main.maxNPCs && Main.npc[index].active && Main.npc[index].type == NPCID.TheDestroyer;
    internal static void Receive(BinaryReader reader, int sender)
    {
        int headIndex = reader.ReadInt16(), encounter = reader.ReadInt32(); long hit = reader.ReadInt64();
        if (Main.netMode != NetmodeID.Server || sender < 0 || sender >= Main.maxPlayers || !Main.player[sender].active ||
            !ValidHead(headIndex) || Main.npc[headIndex].GetGlobalNPC<DestroyerBarState>().Encounter != encounter || !AcceptReceipt(sender, hit)) return;
        NPC head = Main.npc[headIndex]; head.GetGlobalNPC<DestroyerBehavior>().RecordReaction(head);
        NetMessage.SendData(MessageID.SyncNPC, number: headIndex);
    }
}

public sealed class DestroyerReactionPlayer : ModPlayer
{
    public override void PlayerConnect() => DestroyerReactionCounter.ResetConnection(Player.whoAmI);
    public override void PlayerDisconnect() => DestroyerReactionCounter.ResetConnection(Player.whoAmI);
}

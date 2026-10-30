#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdvancedMod.Basic.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Basic.Element.Tml;

/// <summary>服务器发整表快照，客户端只维护表现，不创建结算用结界。</summary>
public sealed class HolyFieldVisuals : ModSystem
{
    internal const byte PacketKind = 129;
    internal const int SnapshotIntervalTicks = 6;
    internal readonly record struct Snapshot(int Owner, long Identity, int Revision, Vector2 Center, float Radius, int Life, int Charge, int Remaining, int Age);
    internal static readonly Dictionary<int, Snapshot> Mirrors = new();
    private static readonly Dictionary<int, HolyFieldEffect> effects = new();
    internal static int Opens, Closes, Teleports;
    internal static int Seed
    {
        get { unchecked { int hash = 17; foreach (char c in Main.ActiveWorldFileData.SeedText + "HolyField") hash = hash * 31 + c; return hash; } }
    }
    internal static void Publish(int to = -1)
    {
        var states = HolyFieldSystem.Fields.Values.Where(f => f.IsActive).Select(f => new Snapshot(f.Owner.whoAmI,
            f.Identity, f.Revision, f.Center, f.Radius, f.Life.Total, f.Charge,
            (int)(f.Expires - HolyFieldSystem.Tick), (int)(HolyFieldSystem.Tick - f.Created))).ToArray();
        if (Main.netMode == NetmodeID.SinglePlayer) { Apply(states); return; }
        if (Main.netMode != NetmodeID.Server) return;
        var packet = ModContent.GetInstance<AdvancedMod>().GetPacket();
        packet.Write(PacketKind); packet.Write((ushort)states.Length);
        foreach (var s in states)
        {
            packet.Write((byte)s.Owner); packet.Write(s.Identity); packet.Write(s.Revision);
            packet.Write(s.Center.X); packet.Write(s.Center.Y); packet.Write(s.Radius);
            packet.Write(s.Life); packet.Write(s.Charge); packet.Write(s.Remaining); packet.Write(s.Age);
        }
        packet.Send(to);
    }
    internal static void Receive(BinaryReader reader, int sender)
    {
        // 客户端只能请求整表，不能提交结界状态。
        if (Main.netMode == NetmodeID.Server) { Publish(sender); return; }
        if (Main.netMode != NetmodeID.MultiplayerClient) return;
        int count = reader.ReadUInt16();
        if (count > Main.maxPlayers) return;
        var states = new Snapshot[count];
        for (int i = 0; i < count; i++) states[i] = new(reader.ReadByte(), reader.ReadInt64(), reader.ReadInt32(),
            new(reader.ReadSingle(), reader.ReadSingle()), reader.ReadSingle(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        Apply(states);
    }
    private static void Apply(Snapshot[] states)
    {
        foreach (int owner in Mirrors.Keys.Except(states.Select(s => s.Owner)).ToArray())
        { effects[owner].Stop(); effects.Remove(owner); Mirrors.Remove(owner); Closes++; }
        foreach (var s in states)
        {
            bool had = Mirrors.TryGetValue(s.Owner, out var previous);
            if (!had || previous.Identity != s.Identity || previous.Revision != s.Revision)
            {
                if (had) { effects[s.Owner].Stop(); Closes++; if (previous.Identity == s.Identity) Teleports++; }
                var effect = new HolyFieldEffect(s.Center, s.Radius, Seed, !had && s.Age >= HolyFieldEffect.OpenSeconds / WorldVfxSystem.TickSeconds);
                effects[s.Owner] = effect; Vfx.Create(effect); Opens++;
            }
            else
            {
                effects[s.Owner].MoveTo(s.Center, WorldVfxSystem.TickSeconds * (Main.netMode == NetmodeID.SinglePlayer ? 1 : SnapshotIntervalTicks));
                effects[s.Owner].Radius = s.Radius;
            }
            Mirrors[s.Owner] = s;
        }
    }
    public override void PostUpdateEverything()
    {
        if (Main.gameMenu) return;
        if (Main.netMode == NetmodeID.SinglePlayer || Main.netMode == NetmodeID.Server && HolyFieldSystem.Tick % SnapshotIntervalTicks == 0) Publish();
    }
    private static void Clear() { foreach (var effect in effects.Values) effect.Stop(); effects.Clear(); Mirrors.Clear(); Opens = Closes = Teleports = 0; }
    public override void OnWorldUnload() => Clear();
    public override void OnWorldLoad() => Clear();
    public override void Unload() => Clear();
}

public sealed class HolyFieldJoinPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (Main.netMode != NetmodeID.MultiplayerClient) return;
        var packet = Mod.GetPacket(); packet.Write(HolyFieldVisuals.PacketKind); packet.Send();
    }
}

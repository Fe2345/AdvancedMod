#nullable enable
using System;
using System.IO;
using AdvancedMod.Basic.Element;
using AdvancedMod.Basic.Element.Tml;
using AdvancedMod.Basic.Tag;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Basic.Calculate.DamageOverTime;

/// <summary>仅传已结算的显示结果；不通过文字包接收伤害请求或修改生命。</summary>
internal static class DotNetwork
{
    internal const byte PacketKind = 133;
    private const float PlayerPositionTolerance = 512;
    private enum Message : byte { Text, PlayerOwnership }
    internal static void Publish(Rectangle bounds, DotText text, int playerIndex)
    {
        if (Main.netMode != NetmodeID.Server) Show(bounds, text.Damage, text.Element, text.Critical, playerIndex, false);
        if (Main.netMode != NetmodeID.SinglePlayer)
            Send(bounds, text.Damage, text.Element, text.Critical, playerIndex, false);
    }
    internal static void PublishNative(Rectangle bounds, int damage, int playerIndex)
    {
        if (Main.netMode != NetmodeID.Server) Show(bounds, damage, null, false, playerIndex, true);
        if (Main.netMode != NetmodeID.SinglePlayer) Send(bounds, damage, null, false, playerIndex, true);
    }
    private static void Show(Rectangle bounds, int damage, TagEnum? element, bool critical, int playerIndex, bool native)
    {
        // CombatText 自身处理无界面环境；调用参数也可由引擎验收观察。
        var color = native ? playerIndex >= 0 ? CombatText.LifeRegen : CombatText.LifeRegenNegative
            : element.HasValue ? ElementTextSystem.ColorFor(element.Value) : Color.White;
        // 此调用不位于被改写的原生方法中，不会再次进入分配逻辑。
        CombatText.NewText(bounds, color, damage, critical, dot: true);
    }
    private static void Send(Rectangle bounds, int damage, TagEnum? element, bool critical, int playerIndex, bool native, int ignore = -1)
    {
        var packet = ModContent.GetInstance<DotSystem>().Mod.GetPacket();
        packet.Write(PacketKind); packet.Write((byte)Message.Text); packet.Write((short)playerIndex);
        packet.Write(bounds.X); packet.Write(bounds.Y); packet.Write(bounds.Width); packet.Write(bounds.Height);
        packet.Write(damage); packet.Write(element.HasValue ? (int)element.Value : -1); packet.Write(critical); packet.Write(native);
        packet.Send(ignoreClient: ignore);
    }
    internal static void SendPlayerOwnership(int playerIndex, bool managed, int to = -1, int ignore = -1)
    {
        var packet = ModContent.GetInstance<DotSystem>().Mod.GetPacket();
        packet.Write(PacketKind); packet.Write((byte)Message.PlayerOwnership);
        packet.Write((short)playerIndex); packet.Write(managed); packet.Send(to, ignore);
    }
    internal static void Receive(BinaryReader reader, int sender)
    {
        var message = (Message)reader.ReadByte();
        if (message == Message.PlayerOwnership)
        {
            int index = reader.ReadInt16(); bool managed = reader.ReadBoolean();
            if (index < 0 || index >= Main.maxPlayers) throw new InvalidDataException("持续伤害显示玩家索引非法。");
            if (Main.netMode == NetmodeID.Server && (index != sender || !Main.player[index].active)) return;
            if (Main.netMode is NetmodeID.Server or NetmodeID.MultiplayerClient)
                Main.player[index].GetModPlayer<DotPlayer>().ManagedText = managed;
            if (Main.netMode == NetmodeID.Server) SendPlayerOwnership(index, managed, ignore: sender);
            return;
        }
        if (message != Message.Text) throw new InvalidDataException("未知持续伤害数据包。");
        int playerIndex = reader.ReadInt16();
        var bounds = new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        int damage = reader.ReadInt32(), elementId = reader.ReadInt32();
        bool critical = reader.ReadBoolean(), native = reader.ReadBoolean();
        TagEnum? element = elementId == -1 ? null : (TagEnum)elementId;
        if (damage <= 0 || playerIndex < -1 || playerIndex >= Main.maxPlayers ||
            bounds.Width < 0 || bounds.Width > short.MaxValue || bounds.Height < 0 || bounds.Height > short.MaxValue ||
            element.HasValue && !ElementSlots.FromTag(element.Value).HasValue)
            throw new InvalidDataException("持续伤害文字包参数非法。");
        if (Main.netMode == NetmodeID.Server)
        {
            // NPC 文字只由服务器生成；客户端只可发布自己的玩家伤害文字。
            if (sender < 0 || sender >= Main.maxPlayers || playerIndex != sender || !Main.player[sender].active) return;
            // 使用浮点中心避免恶意整型坐标溢出后绕过距离判断。
            var center = new Vector2((float)bounds.X + bounds.Width * 0.5f, (float)bounds.Y + bounds.Height * 0.5f);
            if (Vector2.DistanceSquared(center, Main.player[sender].Center) > PlayerPositionTolerance * PlayerPositionTolerance) return;
            if (!Main.player[sender].GetModPlayer<DotPlayer>().ManagedText) return;
            Send(bounds, damage, element, critical, playerIndex, native, sender);
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient)
            Show(bounds, damage, element, critical, playerIndex, native);
    }
}

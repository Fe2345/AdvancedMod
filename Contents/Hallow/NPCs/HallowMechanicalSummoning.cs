using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Hallow.NPCs;

/// <summary>在原版 ApplyItemTime 调用中记录意图，随后仍由原版生成、消耗和同步 Boss。</summary>
public sealed class HallowMechanicalSummonItem : GlobalItem
{
    internal const byte PacketKind = 128;
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) => MaskForItem(entity.type) != 0;
    internal static int MaskForItem(int type) => type switch
    {
        ItemID.MechanicalWorm => 1,
        ItemID.MechanicalEye => 2 | 4,
        ItemID.MechanicalSkull => 8,
        _ => 0
    };
    internal static int MaskForNPC(int type) => type switch
    {
        NPCID.TheDestroyer => 1,
        NPCID.Retinazer => 2,
        NPCID.Spazmatism => 4,
        NPCID.SkeletronPrime => 8,
        _ => 0
    };

    public override bool? UseItem(Item item, Player player)
    {
        if (player.whoAmI != Main.myPlayer || Main.IsItDay() || !player.ZoneHallow) return null;
        int mask = MaskForItem(item.type);
        if (mask == 0 || !player.SummonItemCheck(item)) return null;
        if (Main.netMode == NetmodeID.SinglePlayer)
            player.GetModPlayer<HallowMechanicalSummonPlayer>().Arm(mask);
        else if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            // 与随后原版消息 61 共用有序连接，先告知服务器本次物品使用。
            var packet = Mod.GetPacket();
            packet.Write(PacketKind);
            packet.Write((short)item.type);
            packet.Send();
        }
        return null;
    }

    internal static void Receive(BinaryReader reader, int sender)
    {
        int itemType = reader.ReadInt16();
        if (Main.netMode != NetmodeID.Server || sender < 0 || sender >= Main.maxPlayers) return;
        var player = Main.player[sender];
        int mask = MaskForItem(itemType);
        if (mask == 0 || !player.active || player.dead || Main.IsItDay() || !player.ZoneHallow ||
            player.HeldItem.type != itemType || !player.SummonItemCheck(player.HeldItem)) return;
        player.GetModPlayer<HallowMechanicalSummonPlayer>().Arm(mask);
    }
}

/// <summary>按玩家保存仅供紧随其后的原版召唤消费的一次性标记。</summary>
public sealed class HallowMechanicalSummonPlayer : ModPlayer
{
    private const ulong PendingLifetimeTicks = 2;
    private int pending;
    private ulong armedAt;
    internal void Arm(int mask) { pending = mask; armedAt = Main.GameUpdateCount; }
    internal bool Consume(int npcType)
    {
        int bit = HallowMechanicalSummonItem.MaskForNPC(npcType);
        if (Main.GameUpdateCount - armedAt > PendingLifetimeTicks) pending = 0;
        if (bit == 0 || (pending & bit) == 0) return false;
        pending &= ~bit;
        return true;
    }
    public override void PostItemCheck()
    {
        // 单人同步调用结束后立即清理；服务器为网络读取跨刻保留短窗口。
        if (Main.netMode == NetmodeID.SinglePlayer) pending = 0;
    }
    public override void Initialize() => pending = 0;
}

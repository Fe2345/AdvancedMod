using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>每位主人独立计时；同一NPC可同时有多个玩家的标记，不互相覆盖或共享。</summary>
public sealed class MetalWormMark : GlobalNPC
{
    public override bool InstancePerEntity => true;
    private Dictionary<int, ulong> owners = new();
    public override void SetDefaults(NPC npc) => owners = new();
    internal bool HasOwner(int owner) => owners.TryGetValue(owner, out ulong until) && Main.GameUpdateCount < until;
    internal void Mark(NPC npc, int owner)
    {
        if (owner < 0 || owner >= Main.maxPlayers) return;
        owners[owner] = Main.GameUpdateCount + MetalWormRules.TagTicks;
        npc.AddBuff(ModContent.BuffType<MetalWormTag>(), MetalWormRules.TagTicks);
        npc.netUpdate = true;
    }
    public override void PostAI(NPC npc)
    {
        foreach (int owner in owners.Keys.ToArray())
            if (!HasOwner(owner) || !Main.player[owner].active)
            { owners.Remove(owner); if (Main.netMode != NetmodeID.MultiplayerClient) npc.netUpdate = true; }
    }
    public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
    {
        if (!HasOwner(projectile.owner) || projectile.ModProjectile is MetalWormProbeShot ||
            !(projectile.minion || projectile.sentry || ProjectileID.Sets.MinionShot[projectile.type] || ProjectileID.Sets.SentryShot[projectile.type])) return;
        modifiers.FlatBonusDamage += (int)(MetalWormRules.TagDamage * ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type]);
    }
    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
    {
        var active = owners.Where(p => p.Value > Main.GameUpdateCount).ToArray();
        writer.Write((byte)active.Length);
        foreach (var entry in active) { writer.Write((byte)entry.Key); writer.Write((ushort)(entry.Value - Main.GameUpdateCount)); }
    }
    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
    {
        int count = reader.ReadByte(); var received = new Dictionary<int, ulong>();
        for (int i = 0; i < count; i++)
        { int owner = reader.ReadByte(); int remaining = reader.ReadUInt16(); if (owner < Main.maxPlayers) received[owner] = Main.GameUpdateCount + (ulong)System.Math.Min(remaining, MetalWormRules.TagTicks); }
        if (Main.netMode == NetmodeID.MultiplayerClient) owners = received;
    }
}

public sealed class MetalWormTag : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.SwordWhipNPCDebuff;
    public override void SetStaticDefaults()
    { Main.debuff[Type] = true; Main.buffNoSave[Type] = true; BuffID.Sets.IsATagBuff[Type] = true; }
}

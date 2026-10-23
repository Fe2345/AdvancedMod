using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;
using System.IO;
using AdvancedMod.Basic.Element.Tml;
using Terraria;
using AdvancedMod.Basic.Calculate.Tml;
using AdvancedMod.Basic.Element;

namespace AdvancedMod.Basic.Global;

/// <summary>
/// NPC 自身攻击描述存储，以及玩家攻击 NPC 的唯一管线写入入口。
/// </summary>
public sealed class CombatGlobalNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public AttackProfile Profile { get; internal set; } = AttackProfile.Unknown;
    public double ElementAmount { get; internal set; }
    public ElementTarget Elements { get; private set; } = new(ElementTarget.DefaultOperationLimit);

    private static long nextIdentity;
    public long NetworkIdentity { get; private set; }
    private bool hadState;
    public override void SetDefaults(NPC npc)
    {
        Elements = new(ElementTarget.DefaultOperationLimit);
        NetworkIdentity = ++nextIdentity;
        Profile = AttackProfile.Unknown;
        ElementAmount = 0;
        hadState = false;
    }
    public override void PostAI(NPC npc)
    {
        if (Main.netMode != NetmodeID.Server || Main.GameUpdateCount % ElementNetwork.SyncInterval != 0) return;
        if (hadState || Elements.HasState) npc.netUpdate = true;
        hadState = Elements.HasState;
    }
    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
    {
        writer.Write(NetworkIdentity);
        ElementNetwork.WriteAttack(writer, Profile, ElementAmount, 1, 1);
        Elements.WriteState(writer);
    }
    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
    {
        long identity = reader.ReadInt64();
        var attack = ElementNetwork.ReadAttack(reader);
        var state = new ElementTarget(ElementTarget.DefaultOperationLimit);
        state.ReadState(reader);
        if (Main.netMode != NetmodeID.MultiplayerClient) return;
        NetworkIdentity = identity;
        Profile = attack.Profile;
        ElementAmount = attack.Amount;
        Elements = state;
    }

    public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
    {
        var context = CombatContext.ForItemHit(CalculateSystem.NextHitId(), player, item, npc,
            item.GetGlobalItem<CombatGlobalItem>().Profile);
        context = Element.Tml.ElementHitSystem.PrepareNpc(context, ref modifiers);
        var source = modifiers.SourceDamage;
        var result = CalculateSystem.Pipeline.ModifyNpcHit(in context, ref modifiers);
        Element.Tml.AmplificationEffects.CaptureSource(context, source, result);
    }

    public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
    {
        // 首版只处理玩家弹幕攻击 NPC，不把 NPC 间的伤害纳入这一入口。
        if (!projectile.friendly || projectile.hostile || projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            return;
        var context = CombatContext.ForProjectileHit(CalculateSystem.NextHitId(), projectile, npc,
            projectile.GetGlobalProjectile<CombatGlobalProjectile>().Profile);
        context = Element.Tml.ElementHitSystem.PrepareNpc(context, ref modifiers);
        var source = modifiers.SourceDamage;
        var result = CalculateSystem.Pipeline.ModifyNpcHit(in context, ref modifiers);
        Element.Tml.AmplificationEffects.CaptureSource(context, source, result);
    }

    public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile) =>
        Element.Tml.ElementHitSystem.CanHitNpc(npc, projectile) ? null : false;
}

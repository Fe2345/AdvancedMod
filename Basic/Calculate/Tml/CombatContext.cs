#nullable enable
using System;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Tag;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Basic.Calculate.Tml;

/// <summary>上游提供的攻击语义；未知攻击不推断标签、身份或来源物品。</summary>
public sealed class AttackProfile
{
    public static AttackProfile Unknown { get; } = new(TagSet.Empty);
    public TagSet Tags { get; }
    public string? AttackId { get; }
    public bool IsManagedAttack { get; }
    public string? SourceItemId { get; }

    public AttackProfile(TagSet tags, string? attackId = null, bool isManagedAttack = false, string? sourceItemId = null)
    {
        ArgumentNullException.ThrowIfNull(tags);
        Tags = tags;
        AttackId = attackId;
        IsManagedAttack = isManagedAttack;
        SourceItemId = sourceItemId;
    }
}

/// <summary>单次 Hook 的实体读取视图；只读属性不授权贡献者修改实体或保存上下文。</summary>
public sealed class CombatContext
{
    public long HitId { get; }
    public HitKind Kind { get; }
    public Player? AttackerPlayer { get; }
    public NPC? AttackerNpc { get; }
    public Item? SourceItem { get; }
    public Projectile? SourceProjectile { get; }
    public NPC? TargetNpc { get; }
    public Player? TargetPlayer { get; }
    public AttackProfile Profile { get; }
    public DamageClass? NativeDamageClass { get; }
    public ExecutionSide ExecutionSide { get; }
    public bool IsEnvironmental { get; }

    private CombatContext(long hitId, HitKind kind, AttackProfile profile, Player? attackerPlayer = null,
        NPC? attackerNpc = null, Item? item = null, Projectile? projectile = null, NPC? targetNpc = null,
        Player? targetPlayer = null, DamageClass? damageClass = null, bool isEnvironmental = false)
    {
        ArgumentNullException.ThrowIfNull(profile);
        HitId = hitId;
        Kind = kind;
        Profile = profile;
        AttackerPlayer = attackerPlayer;
        AttackerNpc = attackerNpc;
        SourceItem = item;
        SourceProjectile = projectile;
        TargetNpc = targetNpc;
        TargetPlayer = targetPlayer;
        NativeDamageClass = damageClass;
        IsEnvironmental = isEnvironmental;
        ExecutionSide = Main.netMode switch
        {
            NetmodeID.SinglePlayer => ExecutionSide.SinglePlayer,
            NetmodeID.MultiplayerClient => ExecutionSide.Client,
            NetmodeID.Server => ExecutionSide.Server,
            _ => throw new InvalidOperationException("TML 网络执行侧未定义。")
        };
    }

    public static CombatContext ForItemHit(long hitId, Player attacker, Item item, NPC target, AttackProfile profile)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(target);
        return new(hitId, HitKind.ItemToNpc, profile, attackerPlayer: attacker, item: item,
            targetNpc: target, damageClass: item.DamageType);
    }

    public static CombatContext ForProjectileHit(long hitId, Projectile projectile, NPC target, AttackProfile profile)
    {
        ArgumentNullException.ThrowIfNull(projectile);
        ArgumentNullException.ThrowIfNull(target);
        Player? owner = null;
        if (projectile.friendly && !projectile.hostile && projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
        {
            Player? candidate = Main.player[projectile.owner];
            if (candidate is { active: true }) owner = candidate;
        }
        return new(hitId, HitKind.ProjectileToNpc, profile, attackerPlayer: owner, projectile: projectile,
            targetNpc: target, damageClass: projectile.DamageType);
    }

    public static CombatContext ForNpcHurt(long hitId, NPC attacker, Player target, AttackProfile profile)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(target);
        return new(hitId, HitKind.NpcToPlayer, profile, attackerNpc: attacker, targetPlayer: target);
    }

    public static CombatContext ForProjectileHurt(long hitId, Projectile projectile, Player target, AttackProfile profile)
    {
        ArgumentNullException.ThrowIfNull(projectile);
        ArgumentNullException.ThrowIfNull(target);
        // 敌对弹幕的 owner 不作为玩家攻击者；历史 NPC 来源待上游提供证据。
        return new(hitId, HitKind.ProjectileToPlayer, profile, projectile: projectile,
            targetPlayer: target, damageClass: projectile.DamageType);
    }

    public HitSnapshot CreateSnapshot()
    {
        SourceInfo? attacker = AttackerPlayer is not null ? Describe(AttackerPlayer)
            : AttackerNpc is not null ? Describe(AttackerNpc) : null;
        SourceInfo? source = SourceItem is not null ? Describe(SourceItem, AttackerPlayer?.whoAmI)
            : SourceProjectile is not null ? Describe(SourceProjectile, AttackerPlayer?.whoAmI) : attacker;
        if (IsEnvironmental) source = new(SourceKind.Environment, "AdvancedMod:IdleReaction");
        SourceInfo? target = TargetNpc is not null ? Describe(TargetNpc)
            : TargetPlayer is not null ? Describe(TargetPlayer) : null;
        return new(HitId, Kind, ExecutionSide, Profile.Tags, Profile.AttackId, Profile.IsManagedAttack,
            NativeDamageClass?.FullName, attacker, source, target);
    }

    public CombatContext WithProfile(AttackProfile profile) => new(HitId, Kind, profile,
        AttackerPlayer, AttackerNpc, SourceItem, SourceProjectile, TargetNpc, TargetPlayer, NativeDamageClass, IsEnvironmental);

    internal CombatContext ForChain(NPC target, AttackProfile profile) => new(CalculateSystem.NextHitId(),
        HitKind.ProjectileToNpc, profile, AttackerPlayer, AttackerNpc, SourceItem, SourceProjectile,
        target, damageClass: DamageClass.Generic, isEnvironmental: IsEnvironmental);

    public static CombatContext ForEnvironment(long hitId, NPC target, AttackProfile profile) =>
        new(hitId, HitKind.ProjectileToNpc, profile, targetNpc: target, damageClass: DamageClass.Generic, isEnvironmental: true);

    public static CombatContext ForEnvironment(long hitId, Player target, AttackProfile profile) =>
        new(hitId, HitKind.ProjectileToPlayer, profile, targetPlayer: target, damageClass: DamageClass.Generic, isEnvironmental: true);

    private static SourceInfo Describe(Player player) => new(SourceKind.Player, "Terraria:Player", PlayerIndex: player.whoAmI);
    private static SourceInfo Describe(NPC npc) => new(SourceKind.Npc,
        npc.ModNPC?.FullName ?? $"Terraria:{NPCID.Search.GetName(npc.type)}", EntityIndex: npc.whoAmI);
    private static SourceInfo Describe(Item item, int? owner) => new(SourceKind.Item,
        item.ModItem?.FullName ?? $"Terraria:{ItemID.Search.GetName(item.type)}", PlayerIndex: owner);
    private static SourceInfo Describe(Projectile projectile, int? owner) => new(SourceKind.Projectile,
        projectile.ModProjectile?.FullName ?? $"Terraria:{ProjectileID.Search.GetName(projectile.type)}",
        PlayerIndex: owner, EntityIndex: projectile.whoAmI, ProjectileIdentity: projectile.identity);
}

public interface ICombatContributor
{
    HitKinds SupportedHits { get; }
    bool IsActive(in CombatContext context);
    void Collect(in CombatContext context, ContributionWriter writer);
}

#nullable enable
using System;
using AdvancedMod.Basic.Calculate.Tml;
using AdvancedMod.Basic.Element;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace AdvancedMod.Basic.Utils;

/// <summary>武器命中配置及带标签的弹幕生成入口；不推断或继承来源修正。</summary>
public static class ElementAttacks
{
    // 生成调用深度阻止同源嵌套弹幕误用父级配置；不替换原始 IEntitySource。
    internal static int SpawnDepth;
    private sealed record SpawnSetup(IEntitySource Source, TagSet Tags, double Amount, string? AttackId, int Depth);
    private static SpawnSetup? pendingSpawn;
    internal static void ApplySpawn(Projectile projectile, IEntitySource source)
    {
        if (pendingSpawn is not { } setup || setup.Depth != SpawnDepth || !ReferenceEquals(setup.Source, source)) return;
        var data = projectile.GetGlobalProjectile<CombatGlobalProjectile>();
        data.Profile = new(setup.Tags, setup.AttackId, isManagedAttack: true);
        data.ElementAmount = setup.Amount;
    }
    public static void ConfigureWeapon(Item item, TagSet tags, double elementAmount, string? attackId = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        Validate(tags, elementAmount);
        var data = item.GetGlobalItem<CombatGlobalItem>();
        data.Profile = new(tags, attackId, isManagedAttack: true);
        data.ElementAmount = elementAmount;
        item.NetStateChanged();
    }

    public static void ConfigureNpcContact(NPC npc, TagSet tags, double elementAmount, string? attackId = null)
    {
        Validate(tags, elementAmount);
        var data = npc.GetGlobalNPC<CombatGlobalNPC>();
        data.Profile = new(tags, attackId, isManagedAttack: true);
        data.ElementAmount = elementAmount;
        npc.netUpdate = true;
    }

    /// <summary>通过 GlobalProjectile.OnSpawn 在首次网络发包前写入标签，不改变 IEntitySource 的原版语义。</summary>
    public static int NewProjectile(IEntitySource source, Vector2 position, Vector2 velocity,
        int type, int damage, float knockback, TagSet tags, double elementAmount, int owner = 255,
        float ai0 = 0, float ai1 = 0, float ai2 = 0, string? attackId = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        Validate(tags, elementAmount);
        var previous = pendingSpawn;
        pendingSpawn = new(source, tags, elementAmount, attackId, SpawnDepth + 1);
        try { return Projectile.NewProjectile(source, position, velocity, type, damage, knockback, owner, ai0, ai1, ai2); }
        finally { pendingSpawn = previous; }
    }

    private static void Validate(TagSet tags, double amount)
    {
        _ = new ElementRequest(new(0, Calculate.Core.HitKind.ProjectileToNpc,
            Calculate.Core.ExecutionSide.SinglePlayer, tags), 0, amount);
    }
}

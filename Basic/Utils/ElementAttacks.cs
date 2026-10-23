#nullable enable
using System;
using AdvancedMod.Basic.Calculate.Tml;
using AdvancedMod.Basic.Element;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace AdvancedMod.Basic.Utils;

/// <summary>武器命中配置及带标签的弹幕生成入口；不推断或继承来源修正。</summary>
public static class ElementAttacks
{
    public static void ConfigureWeapon(Item item, TagSet tags, double elementAmount, string? attackId = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        Validate(tags, elementAmount);
        var data = item.GetGlobalItem<CombatGlobalItem>();
        data.Profile = new(tags, attackId, isManagedAttack: true);
        data.ElementAmount = elementAmount;
    }

    public static void ConfigureNpcContact(NPC npc, TagSet tags, double elementAmount, string? attackId = null)
    {
        Validate(tags, elementAmount);
        var data = npc.GetGlobalNPC<CombatGlobalNPC>();
        data.Profile = new(tags, attackId, isManagedAttack: true);
        data.ElementAmount = elementAmount;
    }

    /// <summary>在原版 NewProjectile 完成后、首次 AI 前写入标签；不改变 IEntitySource 的原版语义。</summary>
    public static int NewProjectile(IEntitySource source, Vector2 position, Vector2 velocity,
        int type, int damage, float knockback, TagSet tags, double elementAmount, int owner = 255,
        float ai0 = 0, float ai1 = 0, float ai2 = 0, string? attackId = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        Validate(tags, elementAmount);
        int index = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, owner, ai0, ai1, ai2);
        if (index < 0 || index >= Main.maxProjectiles || !Main.projectile[index].active) return index;
        var data = Main.projectile[index].GetGlobalProjectile<CombatGlobalProjectile>();
        data.Profile = new(tags, attackId, isManagedAttack: true);
        data.ElementAmount = elementAmount;
        // 修正保留弹幕 SetDefaults 定义的自身值，绝不复制武器或父弹幕的值。
        return index;
    }

    private static void Validate(TagSet tags, double amount)
    {
        _ = new ElementRequest(new(0, Calculate.Core.HitKind.ProjectileToNpc,
            Calculate.Core.ExecutionSide.SinglePlayer, tags), 0, amount);
    }
}

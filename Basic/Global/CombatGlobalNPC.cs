using Terraria.ModLoader;
using Terraria;
using AdvancedMod.Basic.Calculate.Tml;

namespace AdvancedMod.Basic.Global;

/// <summary>
/// NPC 自身攻击描述存储，以及玩家攻击 NPC 的唯一管线写入入口。
/// </summary>
public sealed class CombatGlobalNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public AttackProfile Profile { get; internal set; } = AttackProfile.Unknown;

    public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
    {
        var context = CombatContext.ForItemHit(CalculateSystem.NextHitId(), player, item, npc,
            item.GetGlobalItem<CombatGlobalItem>().Profile);
        CalculateSystem.Pipeline.ModifyNpcHit(in context, ref modifiers);
    }

    public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
    {
        // 首版只处理玩家弹幕攻击 NPC，不把 NPC 间的伤害纳入这一入口。
        if (!projectile.friendly || projectile.hostile || projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            return;
        var context = CombatContext.ForProjectileHit(CalculateSystem.NextHitId(), projectile, npc,
            projectile.GetGlobalProjectile<CombatGlobalProjectile>().Profile);
        CalculateSystem.Pipeline.ModifyNpcHit(in context, ref modifiers);
    }
}

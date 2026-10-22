using Terraria;
using Terraria.ModLoader;
using AdvancedMod.Basic.Calculate.Tml;

namespace AdvancedMod.Basic.Global;

/// <summary>玩家受伤的唯一管线写入入口，不处理 PvP 或持续伤害。</summary>
public sealed class CombatPlayer : ModPlayer
{
    public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
    {
        var context = CombatContext.ForNpcHurt(CalculateSystem.NextHitId(), npc, Player,
            npc.GetGlobalNPC<CombatGlobalNPC>().Profile);
        CalculateSystem.Pipeline.ModifyPlayerHurt(in context, ref modifiers);
    }

    public override void ModifyHitByProjectile(Projectile projectile, ref Player.HurtModifiers modifiers)
    {
        if (!projectile.hostile) return;
        var context = CombatContext.ForProjectileHurt(CalculateSystem.NextHitId(), projectile, Player,
            projectile.GetGlobalProjectile<CombatGlobalProjectile>().Profile);
        CalculateSystem.Pipeline.ModifyPlayerHurt(in context, ref modifiers);
    }
}

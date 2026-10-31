using Terraria;
using Terraria.ID;

namespace AdvancedMod.Contents.Hallow.NPCs;

/// <summary>原版96号魔焰的粒子尾迹；独立于运动，供两阶段共用。</summary>
internal static class DestroyerFlameVisuals
{
    internal static void Emit(Projectile projectile)
    {
        if (Main.dedServ) return;
        int index = Dust.NewDust(projectile.position + projectile.velocity, projectile.width, projectile.height,
            DustID.CursedTorch, projectile.velocity.X, projectile.velocity.Y, 100, default,
            DestroyerAttackRules.FlameDustScale * DestroyerAttackRules.FlameScale);
        Main.dust[index].noGravity = true;
    }
}

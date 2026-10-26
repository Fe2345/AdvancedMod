using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>完美格挡派生的简约剑形弹幕；元素只决定标签与颜色，附着量由生成入口明确设为0。</summary>
public sealed class HallowCounterSword : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12; Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = 1;
        Projectile.timeLeft = HallowActionRules.CounterLifetime; Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void AI()
    {
        if (!float.IsFinite(Projectile.ai[1]) || Projectile.ai[1] <= 0 || Projectile.ai[1] > 10) { Projectile.Kill(); return; }
        // 512只限制反击触发，生成后每把剑独立追踪最近敌人，不再加一道搜索半径门槛。
        NPC closest = null; float distance = float.MaxValue;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            float d = Vector2.DistanceSquared(Projectile.Center, npc.Center);
            if (npc.CanBeChasedBy() && d < distance) { closest = npc; distance = d; }
        }
        if (closest != null)
        {
            Vector2 desired = (closest.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired * HallowActionRules.CounterSpeed, HallowActionRules.CounterTurn);
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 delta = Projectile.rotation.ToRotationVector2() * HallowBladeRules.BladeLength * Projectile.ai[1] / 2;
        float collision = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - delta, Projectile.Center + delta,
            HallowSwordVisuals.BladeThickness * Projectile.ai[1], ref collision);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float radius = HallowBladeRules.BladeLength * Projectile.ai[1];
        HallowSwordVisuals.DrawSimple(Projectile.Center - Projectile.rotation.ToRotationVector2() * radius / 2, Projectile.rotation, radius,
            HallowSwordVisuals.ElementColor(HallowActionRules.Decode((int)Projectile.ai[0])), 1);
        return false;
    }
}

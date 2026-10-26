using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>移植愤怒云雨怪的逐轴追踪；目标选择与生成雨滴由所属客户端负责。</summary>
public sealed class RainCloud : ModProjectile
{
    internal const int BuffDuration = 18000;
    private const float HoverHeight = 200, Speed = 4, Acceleration = 0.25f, DriftRadius = 20;
    private const float SearchRange = 800, RecallRange = 2000;
    private const int RainInterval = 9, AnimationInterval = 9, FrameCount = 6;
    public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.RainCloudRaining}";
    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = FrameCount;
        Main.projPet[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = 54;
        Projectile.height = 28;
        Projectile.friendly = true;
        Projectile.minion = true;
        Projectile.minionSlots = 1;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        int buff = ModContent.BuffType<RainCloudBuff>();
        if (!owner.active || owner.dead)
        {
            owner.ClearBuff(buff);
            Projectile.Kill();
            return;
        }
        if (owner.HasBuff(buff)) Projectile.timeLeft = 2;
        if (++Projectile.frameCounter >= AnimationInterval)
        {
            Projectile.frameCounter = 0;
            Projectile.frame = (Projectile.frame + 1) % FrameCount;
        }
        if (Projectile.owner == Main.myPlayer)
        {
            if (Vector2.DistanceSquared(Projectile.Center, owner.Center) > RecallRange * RecallRange)
            {
                Projectile.Center = owner.Center - new Vector2(0, HoverHeight);
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
            }
            int selected = FindTarget(owner);
            if (Projectile.ai[1] != selected + 1)
            {
                Projectile.ai[1] = selected + 1;
                Projectile.netUpdate = true;
            }
        }
        int targetIndex = (int)Projectile.ai[1] - 1;
        NPC target = targetIndex >= 0 && targetIndex < Main.maxNPCs ? Main.npc[targetIndex] : null;
        if (target != null && !target.CanBeChasedBy(Projectile)) target = null;
        Vector2 destination = (target?.Center ?? owner.Center) - new Vector2(0, HoverHeight);
        Vector2 delta = destination - Projectile.Center;
        Vector2 desired = delta.LengthSquared() < DriftRadius * DriftRadius
            ? Projectile.velocity : delta.SafeNormalize(Vector2.Zero) * Speed;
        Projectile.velocity.X = Approach(Projectile.velocity.X, desired.X);
        Projectile.velocity.Y = Approach(Projectile.velocity.Y, desired.Y);

        // 条件不满足时暂停计时，与原版一致；云穿墙，雨滴与攻击视线不穿墙。
        if (Projectile.owner != Main.myPlayer || target == null ||
            Projectile.Right.X <= target.Left.X || Projectile.Left.X >= target.Right.X ||
            Projectile.Bottom.Y >= target.Top.Y ||
            !Collision.CanHit(Projectile.position, Projectile.width, Projectile.height, target.position, target.width, target.height)) return;
        if (++Projectile.ai[0] < RainInterval) return;
        Projectile.ai[0] = 0;
        Vector2 spawn = new(Projectile.Center.X + Main.rand.Next(-14, 15), Projectile.Bottom.Y + 4);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, new Vector2(0, 5),
            ModContent.ProjectileType<RainCloudDrop>(), Projectile.damage, 0, Projectile.owner);
    }
    private int FindTarget(Player owner)
    {
        if (owner.HasMinionAttackTargetNPC)
        {
            NPC chosen = Main.npc[owner.MinionAttackTargetNPC];
            if (chosen.CanBeChasedBy(Projectile) && Vector2.DistanceSquared(owner.Center, chosen.Center) <= SearchRange * SearchRange)
                return chosen.whoAmI;
        }
        int result = -1;
        float best = SearchRange * SearchRange;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            float distance = Vector2.DistanceSquared(owner.Center, npc.Center);
            if (distance < best && npc.CanBeChasedBy(Projectile)) { best = distance; result = npc.whoAmI; }
        }
        return result;
    }
    private static float Approach(float current, float desired)
    {
        if (current < desired)
        {
            current += Acceleration;
            if (current < 0 && desired > 0) current += Acceleration * 2;
        }
        else if (current > desired)
        {
            current -= Acceleration;
            if (current > 0 && desired < 0) current -= Acceleration * 2;
        }
        return current;
    }
}

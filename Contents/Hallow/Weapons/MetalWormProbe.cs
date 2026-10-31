using System;
using System.IO;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Graphics;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Contents.Hallow.NPCs;
using AdvancedMod.Contents.Vfx;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Visuals = AdvancedMod.Basic.Graphics.Vfx;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>临时探测怪绕敌飞行，第180刻发射，第240刻消失；不占召唤位。</summary>
public sealed class MetalWormProbe : ModProjectile
{
    private long targetIdentity;
    private float orbitAngle;
    private bool fired;
    public override string Texture => "Terraria/Images/NPC_" + NPCID.Probe;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 30;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.timeLeft = MetalWormRules.ProbeLifetime;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override void OnSpawn(IEntitySource source)
    {
        int slot = (int)Projectile.ai[0] - 1;
        if (slot >= 0 && slot < Main.maxNPCs)
        {
            targetIdentity = Main.npc[slot].GetGlobalNPC<CombatGlobalNPC>().NetworkIdentity;
            orbitAngle = (Projectile.Center - Main.npc[slot].Center).ToRotation();
        }
    }
    public override void AI()
    {
        Projectile.ai[2]++;
        if (Projectile.ai[2] >= MetalWormRules.ProbeLifetime) { Projectile.Kill(); return; }
        int slot = (int)Projectile.ai[0] - 1;
        NPC target = slot >= 0 && slot < Main.maxNPCs ? Main.npc[slot] : null;
        if (target == null || !target.active || target.friendly || target.life <= 0 ||
            target.GetGlobalNPC<CombatGlobalNPC>().NetworkIdentity != targetIdentity) target = null;
        if (target == null && Projectile.owner == Main.myPlayer)
        {
            float distance = MetalWormRules.ProbeSearchRadius * MetalWormRules.ProbeSearchRadius;
            foreach (NPC candidate in Main.ActiveNPCs)
            {
                float next = Vector2.DistanceSquared(Projectile.Center, candidate.Center);
                if (!candidate.CanBeChasedBy(Projectile) || next >= distance) continue;
                target = candidate; distance = next;
            }
            if (target != null)
            {
                Projectile.ai[0] = target.whoAmI + 1;
                targetIdentity = target.GetGlobalNPC<CombatGlobalNPC>().NetworkIdentity;
                orbitAngle = (Projectile.Center - target.Center).ToRotation();
                Projectile.netUpdate = true;
            }
        }
        // 用环绕目的地与有限加速度表现探测怪的游走；不移植以玩家为目标的敌对NPC AI。
        orbitAngle += MetalWormRules.ProbeOrbitSpeed * (Projectile.identity % 2 == 0 ? 1 : -1);
        Vector2 direction = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
        Vector2 desired = Vector2.Zero;
        if (target != null)
        {
            float radius = MetalWormRules.ProbeOrbitRadius + target.Size.Length() * .5f;
            Vector2 destination = target.Center + orbitAngle.ToRotationVector2() * radius;
            desired = (destination - Projectile.Center) / MetalWormRules.ProbeApproachTicks;
            if (desired.LengthSquared() > MetalWormRules.ProbeMoveSpeed * MetalWormRules.ProbeMoveSpeed)
                desired = desired.SafeNormalize(Vector2.UnitY) * MetalWormRules.ProbeMoveSpeed;
            direction = (target.Center - Projectile.Center).SafeNormalize(direction);
        }
        Projectile.velocity = Projectile.velocity.MoveTowards(desired, MetalWormRules.ProbeAcceleration);
        Projectile.rotation = direction.ToRotation() + MathHelper.PiOver2;
        if (Projectile.owner != Main.myPlayer) return;
        if ((int)Projectile.ai[2] % MetalWormRules.ProbeSyncTicks == 0) Projectile.netUpdate = true;
        if (fired || Projectile.ai[2] < MetalWormRules.ProbeFireTick) return;
        fired = true;
        if (target != null)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, direction * MetalWormRules.ShotSpeed,
                ModContent.ProjectileType<MetalWormProbeShot>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.ai[1]);
        Projectile.netUpdate = true;
    }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(targetIdentity); writer.Write(orbitAngle); writer.Write(fired); }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        targetIdentity = reader.ReadInt64(); orbitAngle = reader.ReadSingle(); fired |= reader.ReadBoolean();
        Projectile.timeLeft = Math.Max(1, MetalWormRules.ProbeLifetime - (int)Projectile.ai[2]);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Main.instance.LoadNPC(NPCID.Probe);
        var texture = TextureAssets.Npc[NPCID.Probe].Value;
        var frame = texture.Frame(1, Main.npcFrameCount[NPCID.Probe]);
        Color color = DestroyerPhaseOneRules.Color((ProbeElement)(int)Projectile.ai[1]);
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.Lerp(Color.White, color, .35f),
            Projectile.rotation, frame.Size() * .5f, Projectile.scale, SpriteEffects.None);
        return false;
    }
}

/// <summary>友方派生元素攻击；独立配置零附着与神圣资格，不复用敌对弹幕的伤害归属。</summary>
public sealed class MetalWormProbeShot : ModProjectile
{
    private IdyllicEffect wind;
    private EffectHandle windHandle;
    private int age;
    internal ProbeElement Element => (ProbeElement)(int)Projectile.ai[0];
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.PinkLaser;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = 1; Projectile.timeLeft = MetalWormRules.ShotLifetime;
        Projectile.ignoreWater = true; Projectile.aiStyle = -1;
    }
    public override void OnSpawn(IEntitySource source)
    {
        var element = DestroyerPhaseOneRules.Decode((byte)Projectile.ai[0]);
        if (element == ProbeElement.None) { Projectile.Kill(); return; }
        var data = Projectile.GetGlobalProjectile<CombatGlobalProjectile>();
        data.Profile = new(new TagSet(new[] { new TagEntry(false, DestroyerPhaseOneRules.Tag(element)), new TagEntry(false, TagEnum.Hallow) }), "Hallow.MetalWormProbeShot", true);
        data.ElementAmount = 0;
    }
    public override void AI()
    {
        age++;
        Projectile.tileCollide = Element is ProbeElement.Fire or ProbeElement.Ice or ProbeElement.Lightning;
        if (Element == ProbeElement.Water)
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * (Projectile.velocity.Length() + DestroyerPhaseOneRules.WaterAcceleration);
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        Lighting.AddLight(Projectile.Center, DestroyerPhaseOneRules.Color(Element).ToVector3() * .3f);
        if (Element == ProbeElement.Fire) DestroyerFlameVisuals.Emit(Projectile);
        if (Element != ProbeElement.Wind || Main.dedServ) return;
        if (wind == null)
        {
            wind = new IdyllicEffect(Projectile.Center, new IdyllicSettings { Size = DestroyerPhaseOneRules.WindWidth, Thickness = DestroyerPhaseOneRules.WindThickness }, .3f, true);
            windHandle = Visuals.Create(wind);
        }
        wind.Center = Projectile.Center;
        wind.Settings = wind.Settings with { Rotation = MathHelper.ToDegrees(Projectile.rotation), Opacity = .7f * Math.Min(1, Projectile.timeLeft / 12f) };
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Element == ProbeElement.Wind
        ? HallowProbeShot.WindCollides(Projectile.Center, Projectile.rotation, targetHitbox) : null;
    public override bool PreDraw(ref Color lightColor)
    {
        if (Element == ProbeElement.Wind) return false;
        int type = Element switch { ProbeElement.Water => ProjectileID.SharknadoBolt,
            ProbeElement.Fire => ProjectileID.CursedFlameHostile, ProbeElement.Ice => ProjectileID.IceBolt, _ => ProjectileID.PinkLaser };
        Main.instance.LoadProjectile(type);
        var texture = TextureAssets.Projectile[type].Value;
        int frames = Math.Max(1, Main.projFrames[type]);
        var frame = texture.Frame(1, frames, 0, age / 5 % frames);
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation,
            frame.Size() * .5f, Projectile.scale, SpriteEffects.None);
        return false;
    }
    public override void OnKill(int timeLeft) { if (wind != null) Visuals.Kill(windHandle); }
}

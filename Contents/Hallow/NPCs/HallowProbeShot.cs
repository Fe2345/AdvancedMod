using System;
using System.IO;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Graphics;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Contents.Vfx;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Visuals = AdvancedMod.Basic.Graphics.Vfx;

namespace AdvancedMod.Contents.Hallow.NPCs;

/// <summary>只接管神圣探测怪原生射出的 PinkLaser 实例；保留原 AI 的计时、受击重置和视线判定。</summary>
public sealed class HallowProbeShot : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.type == ProjectileID.PinkLaser;
    public ProbeElement Element { get; private set; }
    public bool Returning { get; private set; }
    internal int SourceSlot = -1;
    private long sourceIdentity;
    private int age;
    private bool configured;
    private IdyllicEffect wind;
    private EffectHandle windHandle;

    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || source is not EntitySource_Parent { Entity: NPC parent } ||
            !parent.TryGetGlobalNPC<HallowProbeBehavior>(out var probe) || probe.Element == ProbeElement.None) return;
        Element = probe.Element; SourceSlot = parent.whoAmI;
        sourceIdentity = parent.GetGlobalNPC<CombatGlobalNPC>().NetworkIdentity;
        Configure(projectile);
        projectile.netUpdate = true;
    }

    private void Configure(Projectile projectile)
    {
        if (configured || Element == ProbeElement.None) return;
        configured = true;
        projectile.netImportant = true;
        var combat = projectile.GetGlobalProjectile<CombatGlobalProjectile>();
        combat.Profile = new(new TagSet(new[] { new TagEntry(false, DestroyerPhaseOneRules.Tag(Element)) }), "Hallow.Probe." + Element, isManagedAttack: true);
        combat.ElementAmount = DestroyerPhaseOneRules.ShotAttachment;
        // 雷保留原生激光完整行为；其他种类沿用本次发射方向和伤害，使用独立表现／运动。
        if (Element == ProbeElement.Lightning) return;
        Vector2 center = projectile.Center;
        projectile.width = projectile.height = Element == ProbeElement.Water ? 24 : Element == ProbeElement.Wind ? 16 : 12;
        projectile.Center = center;
        projectile.extraUpdates = 0;
        projectile.timeLeft = DestroyerPhaseOneRules.ShotLifetime;
        projectile.penetrate = Element == ProbeElement.Water ? -1 : 1;
        projectile.tileCollide = Element is ProbeElement.Fire or ProbeElement.Ice;
        projectile.alpha = 0;
    }

    public override bool PreAI(Projectile projectile)
    {
        if (Element is ProbeElement.None or ProbeElement.Lightning) return true;
        age++;
        if (Returning)
        {
            if (SourceSlot < 0 || SourceSlot >= Main.maxNPCs || !Main.npc[SourceSlot].active ||
                Main.npc[SourceSlot].type != NPCID.Probe || Main.npc[SourceSlot].GetGlobalNPC<CombatGlobalNPC>().NetworkIdentity != sourceIdentity)
            { projectile.Kill(); return false; }
            NPC source = Main.npc[SourceSlot];
            if (projectile.Distance(source.Center) < DestroyerPhaseOneRules.ReturnCatchDistance) { projectile.Kill(); return false; }
            Vector2 desired = (source.Center - projectile.Center).SafeNormalize(Vector2.UnitY) * DestroyerPhaseOneRules.ReturnSpeed;
            projectile.velocity = projectile.velocity.MoveTowards(desired, DestroyerPhaseOneRules.ReturnAcceleration);
        }
        projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
        Lighting.AddLight(projectile.Center, DestroyerPhaseOneRules.Color(Element).ToVector3() * 0.3f);
        if (Element == ProbeElement.Fire) DestroyerFlameVisuals.Emit(projectile);
        if (Element == ProbeElement.Wind && !Main.dedServ)
        {
            if (wind == null)
            {
                wind = new IdyllicEffect(projectile.Center, new IdyllicSettings { Size = DestroyerPhaseOneRules.WindWidth, Thickness = 6 }, .3f, true);
                windHandle = Visuals.Create(wind);
            }
            wind.Center = projectile.Center;
            wind.Settings = wind.Settings with { Rotation = MathHelper.ToDegrees(projectile.rotation),
                Opacity = .7f * Math.Min(1, age / 6f) * Math.Min(1, projectile.timeLeft / 12f) };
        }
        return false;
    }

    public override void OnHitPlayer(Projectile projectile, Player target, Player.HurtInfo info)
    {
        if (Element != ProbeElement.Water || Returning) return;
        Returning = true; projectile.timeLeft = DestroyerPhaseOneRules.ShotLifetime;
        // 敌对弹幕命中在被击中的客户端判定；借已有模组连接通知权威端返程。
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = Mod.GetPacket(); packet.Write(ReturnPacketKind);
            packet.Write((short)projectile.identity); packet.Write((byte)projectile.owner); packet.Send();
        }
        projectile.netUpdate = true;
    }
    internal const byte ReturnPacketKind = 130;
    internal static void ReceiveReturn(BinaryReader reader, int sender)
    {
        int identity = reader.ReadInt16(), owner = reader.ReadByte();
        if (Main.netMode != NetmodeID.Server || sender < 0 || sender >= Main.maxPlayers || !Main.player[sender].active) return;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.identity != identity || projectile.owner != owner ||
                !projectile.TryGetGlobalProjectile<HallowProbeShot>(out var shot) || shot.Element != ProbeElement.Water || shot.Returning) continue;
            // 为网络延迟留出有限余量，不允许远端修改任意水弹。
            if (projectile.Distance(Main.player[sender].Center) > 480f) return;
            shot.Returning = true; projectile.timeLeft = DestroyerPhaseOneRules.ShotLifetime;
            // 网络接收不在弹幕 AI 内；下一次 Update 开头会清 netUpdate，因此此处直接同步。
            NetMessage.SendData(MessageID.SyncProjectile, number: projectile.whoAmI);
            return;
        }
    }

    public override bool? Colliding(Projectile projectile, Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (Element != ProbeElement.Wind) return null;
        return WindCollides(projectile.Center, projectile.rotation, targetHitbox);
    }
    internal static bool WindCollides(Vector2 center, float rotation, Rectangle targetHitbox)
    {
        var geometry = new IdyllicArcGeometry(60, DestroyerPhaseOneRules.WindWidth);
        Vector2 Point(float u)
        {
            Vector2 point = geometry.Point(u) - new Vector2(0, geometry.Sagitta / 2);
            point = IdyllicEffect.Perspective(point / 640) * 481 + new Vector2(0, -10);
            return center + point.RotatedBy(rotation);
        }
        // 使用同一圆弧与伪透视变换；不以未经投影的弦长充当伤害盒。
        Vector2 previous = Point(.1f);
        for (int i = 1; i <= 12; i++)
        {
            Vector2 next = Point(.1f + .8f * i / 12); float collision = 0;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), previous, next,
                DestroyerPhaseOneRules.WindThickness, ref collision)) return true;
            previous = next;
        }
        return false;
    }
    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (Element is ProbeElement.None or ProbeElement.Lightning) return true;
        if (Element == ProbeElement.Wind) return false;
        int visual = Element switch { ProbeElement.Water => ProjectileID.SharknadoBolt,
            ProbeElement.Fire => ProjectileID.CursedFlameHostile, _ => ProjectileID.IceBolt };
        Main.instance.LoadProjectile(visual);
        var texture = TextureAssets.Projectile[visual].Value;
        int count = Math.Max(1, Main.projFrames[visual]);
        var frame = texture.Frame(1, count, 0, age / 5 % count);
        Main.EntitySpriteDraw(texture, projectile.Center - Main.screenPosition, frame, Color.White,
            projectile.rotation, frame.Size() / 2, projectile.scale, SpriteEffects.None);
        return false;
    }
    public override void OnKill(Projectile projectile, int timeLeft) { if (wind != null) Visuals.Kill(windHandle); }
    public override void SendExtraAI(Projectile projectile, BitWriter bits, BinaryWriter writer)
    {
        writer.Write((byte)Element);
        if (Element == ProbeElement.None) return;
        writer.Write((short)SourceSlot); writer.Write(sourceIdentity); writer.Write(Returning); writer.Write(age); writer.Write(projectile.timeLeft);
    }
    public override void ReceiveExtraAI(Projectile projectile, BitReader bits, BinaryReader reader)
    {
        Element = DestroyerPhaseOneRules.Decode(reader.ReadByte());
        if (Element == ProbeElement.None) return;
        SourceSlot = reader.ReadInt16(); sourceIdentity = reader.ReadInt64(); Returning = reader.ReadBoolean(); age = reader.ReadInt32();
        int lifetime = reader.ReadInt32();
        // 同步包给的是权威端左上角；首次配置尺寸时不能再次做服务器端的中心保持偏移。
        Vector2 position = projectile.position; Configure(projectile); projectile.position = position; projectile.timeLeft = lifetime;
    }
}

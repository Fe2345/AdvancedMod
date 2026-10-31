using System;
using System.Collections.Generic;
using System.IO;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>保留原版附魔飞刀AI；储量耗尽才接管无伤返程，服务器负责扣量与回充。</summary>
public sealed class BladeStaffWater : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.type == ProjectileID.Smolstar;
    internal int Remaining { get; private set; } = MetalWormRules.BladeCapacity;
    internal bool Returning => Remaining == 0;
    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        Remaining = MetalWormRules.BladeCapacity;
        var data = projectile.GetGlobalProjectile<CombatGlobalProjectile>();
        data.Profile = new(new TagSet(new[] { new TagEntry(false, TagEnum.Hydro) }), "Hallow.BladeStaffWater", true);
        data.ElementAmount = MetalWormRules.BladeAttachment;
    }
    public override bool? CanDamage(Projectile projectile) => Returning ? false : null;
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || damageDone <= 0 || Returning) return;
        int cost = target.GetGlobalNPC<MetalWormMark>().HasOwner(projectile.owner) ? MetalWormRules.BladeMarkedCost : MetalWormRules.BladeNormalCost;
        // 混合命中后不足一次普通费用时耗尽剩余储量；本击附着仍为约定的0.25。
        Remaining = Math.Max(0, Remaining - cost);
        if (Returning) { projectile.ai[0] = projectile.ai[1] = 0; }
        projectile.netUpdate = true;
        if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncProjectile, number: projectile.whoAmI);
    }
    public override bool PreAI(Projectile projectile)
    {
        if (!Returning) return true;
        Player owner = Main.player[projectile.owner];
        if (!owner.active || owner.dead) { projectile.Kill(); return false; }
        if (owner.smolstar) projectile.timeLeft = 2;
        Vector2 destination = owner.Center;
        Vector2 delta = destination - projectile.Center;
        if (delta.Length() > MetalWormRules.BladeRecallDistance)
        { projectile.Center = destination; delta = Vector2.Zero; }
        if (delta.Length() <= MetalWormRules.BladeCatchRadius)
        {
            projectile.velocity = Vector2.Zero;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Remaining = MetalWormRules.BladeCapacity;
                projectile.ai[0] = projectile.ai[1] = 0;
                projectile.netUpdate = true;
                if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncProjectile, number: projectile.whoAmI);
            }
            return false;
        }
        Vector2 desired = delta.SafeNormalize(Vector2.UnitY) * Math.Min(MetalWormRules.BladeReturnSpeed, delta.Length());
        projectile.velocity = projectile.velocity.MoveTowards(desired, MetalWormRules.BladeReturnAcceleration);
        projectile.rotation = projectile.rotation.AngleLerp(projectile.velocity.ToRotation() + MathHelper.PiOver2, .2f);
        return false;
    }
    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter writer) => writer.Write((byte)Remaining);
    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader reader)
    {
        int remaining = reader.ReadByte();
        // 原版仆从仍由owner发送运动；不能让客户端旧镜像回写服务器储量。
        if (Main.netMode != NetmodeID.Server) Remaining = Math.Clamp(remaining, 0, MetalWormRules.BladeCapacity);
    }
}

public sealed class BladeStaffWaterItem : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.type == ItemID.Smolstar;
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) => tooltips.Add(new TooltipLine(Mod, "WaterReservoir",
        Terraria.Localization.Language.GetTextValue("Mods.AdvancedMod.BladeStaffWaterTooltip")));
}

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using AdvancedMod.Basic.Graphics;
using Visuals = AdvancedMod.Basic.Graphics.Vfx;

namespace AdvancedMod.Contents.Hallow.Weapons;

public sealed class HeavensJudgmentSwing : ModProjectile
{
    private EffectHandle trail;
    private Item sourceItem;
    private float itemScale = 1;
    private int selectedSlot, facing = 1, hitCycle;
    private bool validSpawn, initialized, completed;
    private Vector2 previousHilt;
    private float previousAngle;
    public int Stage => (int)Projectile.ai[0];
    public float StartAngle => Projectile.ai[1];
    public int Duration => (int)Projectile.ai[2];
    public float Progress => Projectile.localAI[0] / (Duration * Projectile.MaxUpdates);
    public Vector2 Hilt => Projectile.Center;
    public Vector2 Tip => Hilt + Projectile.rotation.ToRotationVector2() * HallowBladeRules.BladeLength * itemScale;
    private Player Owner => Main.player[Projectile.owner];
    public override string Texture => $"Terraria/Images/Item_{ItemID.Excalibur}";
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
        ProjectileID.Sets.AllowsContactDamageFromJellyfish[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.ownerHitCheck = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.extraUpdates = HallowBladeRules.Subupdates - 1;
        Projectile.noEnchantmentVisuals = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void OnSpawn(IEntitySource source)
    {
        if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) return;
        selectedSlot = Owner.selectedItem;
        if (source is EntitySource_ItemUse itemSource && itemSource.Item.ModItem is HeavensJudgment)
        {
            sourceItem = itemSource.Item;
            itemScale = Owner.GetAdjustedItemScale(sourceItem);
            facing = Projectile.velocity.X == 0 ? Owner.direction : Math.Sign(Projectile.velocity.X);
            validSpawn = true;
        }
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(itemScale); writer.Write((short)selectedSlot); writer.Write((sbyte)facing);
        writer.Write(Projectile.localAI[0]);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        itemScale = reader.ReadSingle(); selectedSlot = reader.ReadInt16(); facing = reader.ReadSByte();
        Projectile.localAI[0] = reader.ReadSingle();
        validSpawn = float.IsFinite(itemScale) && itemScale > 0 && itemScale <= 10 && selectedSlot >= 0 && selectedSlot < 58 &&
            (facing == 1 || facing == -1) && float.IsFinite(Projectile.localAI[0]) && Projectile.localAI[0] >= 0;
    }
    public override void AI()
    {
        if (!validSpawn || Stage < 0 || Stage >= HallowBladeRules.StageCount || !float.IsFinite(StartAngle) || Duration < 1 || Duration > 3600 ||
            !Owner.active || Owner.dead || Owner.CCed || Owner.noItems || Owner.selectedItem != selectedSlot || Owner.HeldItem.type != ModContent.ItemType<HeavensJudgment>() ||
            (Projectile.owner == Main.myPlayer && !ReferenceEquals(sourceItem, Owner.HeldItem)))
        { Projectile.Kill(); return; }

        int totalSteps = Duration * Projectile.MaxUpdates;
        if (!initialized)
        {
            initialized = true;
            Projectile.timeLeft = Math.Max(1, totalSteps - (int)Projectile.localAI[0]);
            hitCycle = HallowBladeRules.HitCycle(Stage, Progress);
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item1, Owner.Center);
                trail = Visuals.Create(new HallowSlashEffect());
            }
        }

        previousAngle = HallowBladeRules.Angle(Stage, StartAngle, Progress);
        previousHilt = Projectile.localAI[0] == 0 ? Owner.MountedCenter : Hilt;
        Projectile.localAI[0] = Math.Min(totalSteps, Projectile.localAI[0] + 1);
        float angle = HallowBladeRules.Angle(Stage, StartAngle, Progress);
        int cycle = HallowBladeRules.HitCycle(Stage, Progress);
        if (cycle != hitCycle)
        {
            hitCycle = cycle;
            Projectile.ResetLocalNPCHitImmunity();
            // 第二圈仅扫描本圈，避免把跨圈前的旧姿态再次计为命中。
            previousAngle = StartAngle + MathHelper.TwoPi;
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item1, Owner.Center);
        }
        Owner.ChangeDir(facing);
        // 原生道具计时保持到本段结束，自动连发由物品系统发起下一段。
        Owner.SetDummyItemTime(2);
        Owner.heldProj = Projectile.whoAmI;
        Projectile.rotation = angle;
        float armAngle = angle - MathHelper.PiOver2;
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armAngle);
        Vector2 hand = Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armAngle);
        if (Owner.gravDir == -1) hand.Y = Owner.Bottom.Y + Owner.position.Y - hand.Y;
        hand.Y += Owner.gfxOffY;
        Projectile.Center = hand;
        Owner.itemRotation = MathHelper.WrapAngle(angle - (facing < 0 ? MathHelper.Pi : 0));
        if (!Main.dedServ)
        {
            Visuals.SetTarget(trail, new HallowSlashPose(Hilt, HallowBladeRules.BladeLength * itemScale, angle, angle - StartAngle,
                Projectile.localAI[0] / Projectile.MaxUpdates / 60f, Duration / 60f / Math.Max(1, MathF.Abs(HallowBladeRules.Sweep(Stage)) / MathHelper.TwoPi)));
            if (Projectile.numUpdates == 0)
                Projectile.EmitEnchantmentVisualsAt(Vector2.Lerp(Hilt, Tip, 0.5f) - new Vector2(8), 16, 16);
        }
        completed = Projectile.localAI[0] >= totalSteps;
    }
    public override bool? CanDamage() => initialized && validSpawn ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float radius = HallowBladeRules.BladeLength * itemScale;
        float width = HallowBladeRules.CollisionWidth * itemScale;
        // 按剑尖路程补判扫掠，防止扩大半径和高速挥动后跨过小目标。
        int steps = Math.Max(1, (int)MathF.Ceiling((MathF.Abs(Projectile.rotation - previousAngle) * radius + Vector2.Distance(previousHilt, Hilt)) / (width * 0.5f)));
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps, collisionPoint = 0;
            Vector2 hilt = Vector2.Lerp(previousHilt, Hilt, t);
            Vector2 tip = hilt + MathHelper.Lerp(previousAngle, Projectile.rotation, t).ToRotationVector2() * radius;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), hilt, tip, width, ref collisionPoint)) return true;
        }
        return false;
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.SourceDamage *= HallowBladeRules.DamageMultiplier(Stage);
        modifiers.HitDirectionOverride = target.Center.X >= Owner.Center.X ? 1 : -1;
    }
    public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) => modifiers.SourceDamage *= HallowBladeRules.DamageMultiplier(Stage);
    public override void CutTiles() => Utils.PlotTileLine(Hilt, Tip, HallowBladeRules.CollisionWidth * itemScale, DelegateMethods.CutTiles);
    public override void OnKill(int timeLeft)
    {
        Visuals.Stop(trail);
        if (Projectile.owner != Main.myPlayer || sourceItem?.ModItem is not HeavensJudgment item) return;
        if (completed) item.Combo.Complete(Stage, HallowBladeRules.Angle(Stage, StartAngle, 1), Main.GameUpdateCount);
        else item.Combo.Reset();
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (!initialized) return false;
        var texture = TextureAssets.Projectile[Type].Value;
        float scale = HallowBladeRules.BladeLength * itemScale / texture.Size().Length();
        Main.EntitySpriteDraw(texture, Hilt - Main.screenPosition, null, lightColor, Projectile.rotation + MathHelper.PiOver4,
            new Vector2(0, texture.Height), scale, SpriteEffects.None);
        return false;
    }
}

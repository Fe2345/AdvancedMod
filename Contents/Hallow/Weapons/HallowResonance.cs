using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>共鸣仅在动画中点提交物品数据，六剑与本体始终读取同一个物品元素。</summary>
public sealed class HallowResonance : ModProjectile
{
    private Item sourceItem;
    private int slot = -1;
    private float itemScale = 1;
    private bool committed;
    public int Age => (int)Projectile.ai[1];
    public bool Committed => committed;
    private Player Owner => Main.player[Projectile.owner];
    public override string Texture => "AdvancedMod/Assets/Textures/Hallow/HeavensJudgmentBlade";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16; Projectile.friendly = false; Projectile.hostile = false;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.timeLeft = HallowActionRules.ResonanceTicks + 2;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void OnSpawn(IEntitySource source)
    {
        if (source is EntitySource_ItemUse use && use.Item.ModItem is HeavensJudgment)
        { sourceItem = use.Item; slot = Owner.selectedItem; itemScale = Owner.GetAdjustedItemScale(sourceItem); }
    }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write((short)slot); writer.Write(itemScale); writer.Write(committed); }
    public override void ReceiveExtraAI(BinaryReader reader) { slot = reader.ReadInt16(); itemScale = reader.ReadSingle(); committed = reader.ReadBoolean(); }
    public override void AI()
    {
        if (slot < 0 || slot >= 58 || !Owner.active || Owner.dead || Owner.CCed || Owner.noItems || Owner.selectedItem != slot ||
            Owner.HeldItem.ModItem is not HeavensJudgment sword || !float.IsFinite(itemScale) || itemScale <= 0 || itemScale > 10 ||
            HallowActionRules.Decode((int)Projectile.ai[0]) is not { } target ||
            Projectile.owner == Main.myPlayer && !ReferenceEquals(sourceItem, Owner.HeldItem))
        { Projectile.Kill(); return; }
        Projectile.ai[1]++;
        if (Age == HallowActionRules.CommitTick && Projectile.owner == Main.myPlayer)
        {
            sword.CommitResonance(Owner, target); committed = true; Projectile.netUpdate = true;
        }
        Owner.SetDummyItemTime(2); Owner.heldProj = Projectile.whoAmI;
        Projectile.Center = Owner.MountedCenter;
        if (Age >= HallowActionRules.ResonanceTicks) Projectile.Kill();
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Owner.HeldItem.ModItem is not HeavensJudgment sword) return false;
        float phase = Age <= HallowActionRules.CommitTick ? Age : HallowActionRules.ResonanceTicks - Age;
        float lift = MathHelper.SmoothStep(0, 1, Math.Clamp(phase / HallowActionRules.LiftTicks, 0, 1));
        float unfold = MathHelper.SmoothStep(0, 1, Math.Clamp((phase - HallowActionRules.LiftTicks) / HallowActionRules.LiftTicks, 0, 1));
        float radius = HallowBladeRules.BladeLength * itemScale;
        Vector2 grip = Owner.MountedCenter + new Vector2(0, -radius * 0.65f * lift);
        float angle = MathHelper.Lerp(Projectile.velocity.ToRotation(), -MathHelper.PiOver2, lift);
        HallowSwordVisuals.DrawBody(grip, angle, radius, sword.Resonance, lightColor);
        Color color = HallowSwordVisuals.ElementColor(sword.Resonance);
        for (int i = 0; i < HallowActionRules.PhantomCount && unfold > 0; i++)
        {
            float orbit = MathHelper.TwoPi * i / HallowActionRules.PhantomCount + unfold * MathHelper.PiOver2;
            Vector2 offset = orbit.ToRotationVector2() * radius * 0.6f * unfold;
            HallowSwordVisuals.DrawSimple(grip + offset, angle + unfold * (orbit + MathHelper.PiOver2), radius, color, unfold * 0.65f);
        }
        return false;
    }
}

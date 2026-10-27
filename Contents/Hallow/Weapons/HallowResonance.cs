using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>共鸣仅在展开停顿结束时提交物品数据，六剑与本体始终读取同一个物品元素。</summary>
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
        var pose = HallowActionRules.ResonancePose(Age);
        float radius = HallowBladeRules.BladeLength * itemScale;
        Vector2 grip = Owner.MountedCenter + new Vector2(0, -radius * 0.65f * pose.Lift);
        float angle = MathHelper.Lerp(Projectile.velocity.ToRotation(), -MathHelper.PiOver2, pose.Lift);
        Color color = HallowSwordVisuals.ElementColor(sword.Resonance);
        // 深度由远到近排序，把本体放在环的中间；仅使用栈内存。
        Span<Vector3> orbit = stackalloc Vector3[HallowActionRules.PhantomCount];
        for (int i = 0; i < orbit.Length; i++)
        {
            float turn = MathHelper.TwoPi * i / orbit.Length + pose.Turn;
            orbit[i] = new Vector3(MathF.Cos(turn), MathF.Sin(turn), turn);
        }
        for (int i = 1; i < orbit.Length; i++)
        {
            var value = orbit[i]; int j = i - 1;
            while (j >= 0 && orbit[j].Y > value.Y) { orbit[j + 1] = orbit[j]; j--; }
            orbit[j + 1] = value;
        }
        bool bodyDrawn = false;
        for (int i = 0; i < orbit.Length; i++)
        {
            var point = orbit[i];
            if (!bodyDrawn && point.Y >= 0)
            {
                HallowSwordVisuals.DrawBody(grip, angle, radius, sword.Resonance, lightColor);
                bodyDrawn = true;
            }
            float depth = point.Y * pose.Spread;
            float perspective = 1 / (1 - depth * HallowActionRules.OrbitPerspective);
            Vector2 offset = new Vector2(point.X, point.Y * HallowActionRules.OrbitFlattening)
                * radius * HallowActionRules.OrbitRadius * pose.Spread * perspective;
            // 围绕剑身中点投影，近剑变大且偏下，所有虚影保持竖直。
            Vector2 phantomGrip = grip + offset + new Vector2(0, radius * (perspective - 1) / 2);
            HallowSwordVisuals.DrawPhantom(phantomGrip, -MathHelper.PiOver2, radius * perspective,
                color, pose.Spread * HallowActionRules.PhantomOpacity, point.Z);
        }
        if (!bodyDrawn) HallowSwordVisuals.DrawBody(grip, angle, radius, sword.Resonance, lightColor);
        return false;
    }
}

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using AdvancedMod.Basic.Utils;
using AdvancedMod.Basic.Tag;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>无伤害持续格挡；一次成功即收剑，冷却从起手计时。</summary>
public sealed class HallowGuard : ModProjectile
{
    private Item sourceItem;
    private int slot = -1;
    private float itemScale = 1;
    private bool succeeded;
    public int Age => (int)Projectile.ai[0];
    private Player Owner => Main.player[Projectile.owner];
    public Vector2 Direction => Projectile.velocity.SafeNormalize(Vector2.UnitX);
    private Vector2 Side => new(-Direction.Y, Direction.X);
    public float Radius => HallowBladeRules.BladeLength * itemScale;
    public Vector2 Grip => Owner.MountedCenter + Direction * HallowActionRules.GuardOffset - Side * Radius / 2;
    public override string Texture => "AdvancedMod/Assets/Textures/Hallow/HeavensJudgmentBlade";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16; Projectile.friendly = false; Projectile.hostile = false;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.timeLeft = 2;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void OnSpawn(IEntitySource source)
    {
        if (source is EntitySource_ItemUse use && use.Item.ModItem is HeavensJudgment)
        {
            sourceItem = use.Item; slot = Owner.selectedItem; itemScale = Owner.GetAdjustedItemScale(sourceItem);
            Owner.GetModPlayer<HallowGuardPlayer>().Begin();
        }
    }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write((short)slot); writer.Write(itemScale); }
    public override void ReceiveExtraAI(BinaryReader reader) { slot = reader.ReadInt16(); itemScale = reader.ReadSingle(); }
    internal bool Intersects(Rectangle rectangle)
    {
        float collision = 0;
        return Collision.CheckAABBvLineCollision(rectangle.TopLeft(), rectangle.Size(), Grip, Grip + Side * Radius,
            HallowActionRules.GuardThickness * itemScale, ref collision);
    }
    public override void AI()
    {
        if (slot < 0 || slot >= 58 || !Owner.active || Owner.dead || Owner.CCed || Owner.noItems || Owner.selectedItem != slot ||
            Owner.HeldItem.ModItem is not HeavensJudgment || !float.IsFinite(itemScale) || itemScale <= 0 || itemScale > 10 ||
            Projectile.owner == Main.myPlayer && (!ReferenceEquals(sourceItem, Owner.HeldItem) || !Owner.controlUseTile))
        { Projectile.Kill(); return; }
        Projectile.timeLeft = 2; Projectile.Center = Owner.MountedCenter;
        Owner.SetDummyItemTime(2); Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Direction.X < 0 ? -1 : 1);
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Direction.ToRotation() - MathHelper.PiOver2);
        if (Projectile.owner == Main.myPlayer && !succeeded)
        {
            bool perfect = HallowActionRules.Perfect(Age);
            foreach (Projectile incoming in Main.ActiveProjectiles)
            {
                if (!HallowActionRules.EligibleProjectile(incoming.hostile, incoming.damage, incoming.velocity.Length(), incoming.MaxUpdates, incoming.Size.Length()) || !Intersects(incoming.Hitbox)) continue;
                incoming.GetGlobalProjectile<HallowParryProjectile>().Marks.Mark(Owner.whoAmI, perfect);
                Success(perfect); return;
            }
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.friendly || npc.dontTakeDamage || npc.immortal || !npc.CanBeChasedBy() || !Intersects(npc.Hitbox)) continue;
                npc.GetGlobalNPC<HallowParryNPC>().Marks.Mark(Owner.whoAmI, perfect);
                Success(perfect); return;
            }
        }
        Projectile.ai[0]++;
    }
    private void Success(bool perfect)
    {
        succeeded = true;
        var state = Owner.GetModPlayer<HallowGuardPlayer>(); state.LastPerfect = perfect; state.SuccessCount++;
        if (perfect && sourceItem.ModItem is HeavensJudgment sword)
        {
            sword.Combo.Refresh(Main.GameUpdateCount);
            bool found = false;
            foreach (NPC npc in Main.ActiveNPCs)
                if (npc.CanBeChasedBy() && HallowActionRules.InFront(Owner.Center, Direction, npc.Center)) { found = true; break; }
            if (found)
                for (int sign = -1; sign <= 1; sign += 2)
                    ElementAttacks.NewProjectile(Projectile.GetSource_FromThis(), Owner.Center + Side * (HallowActionRules.CounterOffset * sign),
                        Direction * HallowActionRules.CounterSpeed, ModContent.ProjectileType<HallowCounterSword>(), Owner.GetWeaponDamage(sourceItem),
                        Owner.GetWeaponKnockback(sourceItem), HallowActionRules.Tags(sword.Resonance), 0, Owner.whoAmI,
                        HallowActionRules.Encode(sword.Resonance), itemScale, attackId: "Hallow.PerfectCounter");
        }
        Projectile.Kill();
    }
    public override void OnKill(int timeLeft)
    {
        if (Projectile.owner == Main.myPlayer) { Owner.itemTime = 0; Owner.itemAnimation = 0; }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Owner.HeldItem.ModItem is HeavensJudgment sword)
            HallowSwordVisuals.DrawBody(Grip, Direction.ToRotation() + MathHelper.PiOver2, Radius, sword.Resonance, lightColor);
        return false;
    }
}

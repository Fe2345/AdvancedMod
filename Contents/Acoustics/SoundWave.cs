using System;
using System.Collections.Generic;
using System.IO;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Visuals = AdvancedMod.Basic.Graphics.Vfx;

namespace AdvancedMod.Contents.Acoustics;

public sealed class SoundWave : ModProjectile
{
    public ulong Id;
    public Vector2 Origin;
    public float Phase, Energy = 1, Coherence = 1, Distance;
    public int Reflections, FusionDepth, Age;
    public float Wavelength = SoundRules.Wavelength;
    public readonly HashSet<ulong> Receivers = new();
    private EffectHandle visual;
    private bool initialized;
    private bool announced;
    public Vector2 Direction => Projectile.velocity.SafeNormalize(Vector2.UnitX);
    public SoundPose Pose => new(Projectile.Center, Direction, Phase, Wavelength, Energy, Coherence, FusionDepth > 0);
    public override string Texture => "Terraria/Images/Projectile_1";
    public override void SetDefaults()
    {
        // 原生宽高覆盖旋转后的包络，精确范围由 Colliding 的有向矩形裁定。
        Projectile.width = Projectile.height = (int)(2 * MathF.Sqrt(2) * SoundRules.HalfWidth);
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = SoundRules.Lifetime;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
        Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Id != 0 && Energy >= SoundRules.MinimumEnergy ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        SoundRules.Overlap(Projectile.Center, Direction, new(SoundRules.HalfLength, SoundRules.HalfWidth),
            targetHitbox.Center.ToVector2(), Vector2.UnitX, new(targetHitbox.Width * 0.5f, targetHitbox.Height * 0.5f));
    public void Advance()
    {
        Age++;
        Projectile.Center += Projectile.velocity;
        Distance += Projectile.velocity.Length();
        Energy *= SoundRules.Decay;
        if (Energy < SoundRules.MinimumEnergy || Projectile.timeLeft <= 1) { Projectile.Kill(); return; }
        UpdateAttack();
    }
    public void UpdateAttack()
    {
        Projectile.damage = Math.Max(1, (int)MathF.Round(SoundRules.BaseDamage * Energy * (FusionDepth > 0 ? SoundRules.DamageFactor(Coherence) : 1)));
        Projectile.originalDamage = Projectile.damage;
        Projectile.GetGlobalProjectile<CombatGlobalProjectile>().ElementAmount = SoundRules.BaseElement * Energy *
            (FusionDepth > 0 ? SoundRules.ElementFactor(Coherence) : 1);
    }
    public override void AI()
    {
        if (Id == 0) return;
        // 等反射／融合继承参数全部写入后只广播一次；服务器不是玩家 owner，不能依赖 netUpdate。
        if (Main.netMode == NetmodeID.Server && !announced)
        { announced = true; NetMessage.SendData(MessageID.SyncProjectile, number: Projectile.whoAmI); }
        if (Main.netMode == NetmodeID.MultiplayerClient) Advance();
        if (!Projectile.active || Main.dedServ) return;
        if (!initialized)
        {
            initialized = true;
            visual = Visuals.Create(new SoundVisual(Pose));
            // 只有刚生成的子波显示事件，晚加入不补播过时的融合闪光。
            if (Age <= 1 && (FusionDepth > 0 || Reflections > 0))
                Visuals.Create(new SoundVisual(Pose with { Center = Origin }, true));
        }
        Visuals.SetTarget(visual, Pose);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (SoundDemoSystem.Debug && Id != 0)
            Utils.DrawBorderString(Main.spriteBatch, $"#{Id} φ={Phase:F2} E={Energy:F2}\n反射={Reflections} {(FusionDepth > 0 ? SoundRules.Kind(Coherence) : "原波")}",
                Projectile.Center - Main.screenPosition, Color.White, SoundRules.DebugScale);
        return false;
    }
    public override void OnKill(int timeLeft) => Visuals.Kill(visual);
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(Id); w.Write(Origin.X); w.Write(Origin.Y); w.Write(Phase); w.Write(Energy); w.Write(Coherence);
        w.Write(Distance); w.Write(Wavelength); w.Write(Reflections); w.Write(FusionDepth); w.Write(Projectile.timeLeft); w.Write(Age);
        w.Write(Receivers.Count); foreach (ulong id in Receivers) w.Write(id);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        Id = r.ReadUInt64(); Origin = new(r.ReadSingle(), r.ReadSingle()); Phase = r.ReadSingle(); Energy = r.ReadSingle();
        Coherence = r.ReadSingle(); Distance = r.ReadSingle(); Wavelength = r.ReadSingle(); Reflections = r.ReadInt32();
        FusionDepth = r.ReadInt32(); Projectile.timeLeft = r.ReadInt32(); Age = r.ReadInt32();
        int count = r.ReadInt32();
        if (count < 0 || count > (SoundRules.MaxReflections + 1) * (1 << SoundRules.MaxFusionDepth)) throw new IOException("声波反射历史长度非法。");
        Receivers.Clear(); for (int i = 0; i < count; i++) Receivers.Add(r.ReadUInt64());
        if (!float.IsFinite(Energy) || Energy < 0 || !float.IsFinite(Phase) || !float.IsFinite(Coherence) || Coherence < 0 || Coherence > 1 ||
            !float.IsFinite(Wavelength) || Wavelength <= 0 || Reflections < 0 || Reflections > SoundRules.MaxReflections ||
            FusionDepth < 0 || FusionDepth > SoundRules.MaxFusionDepth) Id = 0;
    }
}

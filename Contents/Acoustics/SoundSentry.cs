using System.IO;
using AdvancedMod.Basic.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Visuals = AdvancedMod.Basic.Graphics.Vfx;

namespace AdvancedMod.Contents.Acoustics;

/// <summary>开发哨兵：按设定朝向发射一次，随后作为同类镜面接收器。</summary>
public sealed class SoundSentry : ModProjectile
{
    public ulong Id;
    private EffectHandle visual;
    private bool initialized;
    public Vector2 Normal => Projectile.ai[0].ToRotationVector2();
    public override string Texture => "Terraria/Images/Projectile_1";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = (int)(SoundRules.SentryRadius * 2);
        Projectile.sentry = true; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = SoundRules.SentryLifetime;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (Id == 0) return;
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            if (Projectile.owner >= Main.maxPlayers || !Main.player[Projectile.owner].active || Main.player[Projectile.owner].dead)
            { Projectile.Kill(); return; }
            if (Projectile.ai[2] > 0 && --Projectile.ai[2] == 0)
            {
                var wave = SoundDemoSystem.Spawn(Projectile.GetSource_FromAI(), Projectile.Center, Normal, Projectile.owner, Projectile.ai[1]);
                wave?.Receivers.Add(Id);
                Projectile.netUpdate = true;
            }
        }
        if (!Main.dedServ && !initialized)
        {
            initialized = true;
            visual = Visuals.Create(new SoundVisual(new(Projectile.Center, Normal, 0, SoundRules.Wavelength, 1, 1, false, true)));
        }
        if (!Main.dedServ) Visuals.SetTarget(visual, new SoundPose(Projectile.Center, Normal, 0, SoundRules.Wavelength, 1, 1, false, true));
    }
    public override bool PreDraw(ref Color lightColor) => false;
    public override void OnKill(int timeLeft) => Visuals.Kill(visual);
    public override void SendExtraAI(BinaryWriter writer) => writer.Write(Id);
    public override void ReceiveExtraAI(BinaryReader reader) => Id = reader.ReadUInt64();
}

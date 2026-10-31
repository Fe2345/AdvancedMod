using System;
using System.IO;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Basic.Graphics;
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
using R = AdvancedMod.Contents.Hallow.NPCs.DestroyerAttackRules;

namespace AdvancedMod.Contents.Hallow.Projectiles;

public enum DestroyerAttackKind { Fire, Wind, Laser, Water, Frost }

/// <summary>五种独立攻击实例，共用来源与元素入口；不占原版AI槽。</summary>
public sealed class DestroyerCrashProjectile : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_96";
    public DestroyerAttackKind Kind => (DestroyerAttackKind)(int)Projectile.ai[0];
    public int Age { get; private set; }
    public Vector2 Anchor { get; private set; }
    private IdyllicEffect wind;
    private EffectHandle windHandle;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2200;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20; Projectile.hostile = true; Projectile.friendly = false;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        Projectile.timeLeft = R.ShotLifetime; Projectile.netImportant = true;
    }
    public override void OnSpawn(IEntitySource source)
    {
        Anchor = Projectile.Center;
        Configure();
    }
    private void Configure()
    {
        TagEnum tag = Kind switch { DestroyerAttackKind.Fire => TagEnum.Pyro, DestroyerAttackKind.Wind => TagEnum.Anemo,
            DestroyerAttackKind.Laser => TagEnum.Electro, DestroyerAttackKind.Water => TagEnum.Hydro, _ => TagEnum.Cryo };
        var combat = Projectile.GetGlobalProjectile<CombatGlobalProjectile>();
        combat.Profile = new(new TagSet(new[] { new TagEntry(false, tag), new TagEntry(false, TagEnum.Hallow) }), "Hallow.Destroyer.Crash." + Kind, isManagedAttack: true);
        combat.ElementAmount = R.ElementAmount;
    }
    public override bool ShouldUpdatePosition() => Kind is not (DestroyerAttackKind.Water or DestroyerAttackKind.Frost);
    public override void AI()
    {
        Age++;
        var owner = Projectile.GetGlobalProjectile<DestroyerAttackOwner>();
        if (owner.Head < 0 || owner.Head >= Main.maxNPCs || !Main.npc[owner.Head].active ||
            Main.npc[owner.Head].type != NPCID.TheDestroyer || Main.npc[owner.Head].GetGlobalNPC<DestroyerBarState>().Encounter != owner.Encounter)
        { Projectile.Kill(); return; }
        Projectile.rotation = Projectile.velocity.ToRotation();
        switch (Kind)
        {
            case DestroyerAttackKind.Fire:
                DestroyerFlameVisuals.Emit(Projectile); break;
            case DestroyerAttackKind.Wind:
                if (!Main.dedServ)
                {
                    if (wind == null)
                    {
                        wind = new IdyllicEffect(Projectile.Center, new IdyllicSettings { Size = DestroyerPhaseOneRules.WindWidth, Thickness = DestroyerPhaseOneRules.WindThickness }, .3f, true);
                        windHandle = Visuals.Create(wind);
                    }
                    wind.Center = Projectile.Center; wind.Settings = wind.Settings with { Rotation = MathHelper.ToDegrees(Projectile.rotation + MathHelper.PiOver2), Opacity = .7f };
                }
                break;
            case DestroyerAttackKind.Water:
                float progress = Math.Clamp(Age / (float)R.WaterLifetime, 0, 1);
                float angle = Projectile.ai[1] + MathHelper.TwoPi * R.WaterTurns * progress;
                Projectile.Center = Anchor + angle.ToRotationVector2() * R.WaterRadius * (1 - progress);
                Projectile.rotation = angle; if (Age >= R.WaterLifetime) Projectile.Kill(); break;
            case DestroyerAttackKind.Frost:
                Projectile.Center = Main.npc[owner.Head].Center;
                if (Age >= R.FrostLifetime) Projectile.Kill(); break;
        }
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (Kind == DestroyerAttackKind.Frost)
        {
            float collision = 0;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * R.FrostLength, R.FrostWidth, ref collision);
        }
        if (Kind == DestroyerAttackKind.Wind)
            return HallowProbeShot.WindCollides(Projectile.Center, Projectile.rotation + MathHelper.PiOver2, targetHitbox);
        return null;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Kind == DestroyerAttackKind.Wind) return false;
        if (Kind == DestroyerAttackKind.Frost)
        {
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * R.FrostLength;
            float fade = Math.Min(1, (R.FrostLifetime - Age) / 8f);
            DestroyerUltimatePreview.Line(Projectile.Center, end, Color.Cyan * fade, R.FrostWidth);
            DestroyerUltimatePreview.Line(Projectile.Center, end, Color.White * fade, R.FrostWidth * .35f);
            return false;
        }
        int type = Kind switch { DestroyerAttackKind.Fire => ProjectileID.CursedFlameHostile,
            DestroyerAttackKind.Water => ProjectileID.SharknadoBolt, _ => ProjectileID.PinkLaser };
        Main.instance.LoadProjectile(type); var texture = TextureAssets.Projectile[type].Value;
        int frames = Math.Max(1, Main.projFrames[type]); var frame = texture.Frame(1, frames, 0, Age / 5 % frames);
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White,
            Projectile.rotation + MathHelper.PiOver2, frame.Size() / 2, 1, SpriteEffects.None);
        return false;
    }
    public override void OnKill(int timeLeft) { if (wind != null) Visuals.Kill(windHandle); }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(Age); writer.Write(Anchor.X); writer.Write(Anchor.Y); writer.Write(Projectile.timeLeft); }
    public override void ReceiveExtraAI(BinaryReader reader) { Age = reader.ReadInt32(); Anchor = new(reader.ReadSingle(), reader.ReadSingle()); Projectile.timeLeft = reader.ReadInt32(); Configure(); }
}

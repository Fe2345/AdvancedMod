using System;
using System.IO;
using AdvancedMod.Contents.Hallow.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using R = AdvancedMod.Contents.Hallow.NPCs.DestroyerAttackRules;

namespace AdvancedMod.Contents.Hallow.Projectiles;

/// <summary>每鞭独立锁定的无伤害预警／鞭形；角度按时钟扫180度，不持续追踪目标。</summary>
public sealed class DestroyerUltimatePreview : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public int Age { get; private set; }
    public int Encounter { get; private set; }
    public float StartAngle => Projectile.ai[1];
    public float Sweep => MathHelper.Pi * Projectile.ai[2];
    public float WarningAngle => StartAngle + Sweep * Math.Clamp(Age / (float)R.WarningSweepTicks, 0, 1);
    public bool Warning => Age < DestroyerPhaseTwoRules.UltimateWarningTicks;
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2500;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.netImportant = true;
        Projectile.timeLeft = DestroyerPhaseTwoRules.UltimateWarningTicks + DestroyerPhaseTwoRules.UltimateStrikeTicks;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void OnSpawn(IEntitySource source)
    {
        if (source is EntitySource_Parent { Entity: NPC head }) Encounter = head.GetGlobalNPC<DestroyerBarState>().Encounter;
    }
    public override void AI()
    {
        int index = (int)Projectile.ai[0];
        if (index < 0 || index >= Main.maxNPCs || !Main.npc[index].active || Main.npc[index].type != NPCID.TheDestroyer ||
            Main.npc[index].GetGlobalNPC<DestroyerBarState>().Encounter != Encounter) { Projectile.Kill(); return; }
        Age++;
    }
    internal Vector2 WhipPoint(float along, float progress)
    {
        float sweepProgress = MathHelper.SmoothStep(0, 1, Math.Clamp(progress, 0, 1));
        float bend = R.WhipBend * MathF.Sin(along * MathHelper.Pi) * MathF.Sin(sweepProgress * MathHelper.Pi);
        float relative = Math.Clamp(MathHelper.Pi * sweepProgress - bend, 0, MathHelper.Pi);
        return Projectile.Center + (StartAngle + Math.Sign(Sweep) * relative).ToRotationVector2() * (R.WhipLength * along);
    }
    internal static void Line(Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 offset = end - start;
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, start - Main.screenPosition, new Rectangle(0, 0, 1, 1), color,
            offset.ToRotation(), new Vector2(0, .5f), new Vector2(offset.Length(), width), SpriteEffects.None);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 root = Projectile.Center;
        Color gold = new(255, 215, 100);
        if (Warning)
        {
            float fade = Math.Min(1, Age / 6f);
            Vector2 start = root + StartAngle.ToRotationVector2() * R.WhipLength;
            Vector2 end = root + (StartAngle + Sweep).ToRotationVector2() * R.WhipLength;
            Line(root, start, gold * (.25f * fade), 2); Line(root, end, gold * (.25f * fade), 2);
            // 保留已扫过的淡扇区与外缘，完整显示需要离开的半圆，而非只闪过一根线。
            int samples = 60;
            Vector2 previous = start;
            float fraction = Math.Clamp(Age / (float)R.WarningSweepTicks, 0, 1);
            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector2 point = root + (StartAngle + Sweep * t).ToRotationVector2() * R.WhipLength;
                Line(previous, point, gold * (.18f * fade), 2);
                if (t <= fraction) Line(root, point, gold * (.035f * fade), 6);
                previous = point;
            }
            Vector2 tip = root + WarningAngle.ToRotationVector2() * R.WhipLength;
            Line(root, tip, gold * (.2f * fade), 18); Line(root, tip, gold * fade, 3);
        }
        else
        {
            float progress = (Age - DestroyerPhaseTwoRules.UltimateWarningTicks) / (float)DestroyerPhaseTwoRules.UltimateStrikeTicks;
            // 原本实体保持隐藏且无接触伤害；用原版节段贴图形成鞭形预览，不额外生成NPC。
            for (int i = 0; i <= R.WhipSamples; i++)
            {
                float u = i / (float)R.WhipSamples;
                int type = i == 0 ? NPCID.TheDestroyerTail : i == R.WhipSamples ? NPCID.TheDestroyer : NPCID.TheDestroyerBody;
                Main.instance.LoadNPC(type); var texture = TextureAssets.Npc[type].Value;
                var frame = texture.Frame(1, Main.npcFrameCount[type], 0, type == NPCID.TheDestroyerBody ? 1 : 0);
                Vector2 point = WhipPoint(u, progress);
                Vector2 tangent = WhipPoint(Math.Min(1, u + .01f), progress) - WhipPoint(Math.Max(0, u - .01f), progress);
                Main.EntitySpriteDraw(texture, point - Main.screenPosition, frame, Color.White, tangent.ToRotation() + MathHelper.PiOver2,
                    frame.Size() * .5f, R.WhipScale, SpriteEffects.None);
            }
        }
        return false;
    }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(Age); writer.Write(Encounter); writer.Write(Projectile.timeLeft); }
    public override void ReceiveExtraAI(BinaryReader reader) { Age = reader.ReadInt32(); Encounter = reader.ReadInt32(); Projectile.timeLeft = reader.ReadInt32(); }
}

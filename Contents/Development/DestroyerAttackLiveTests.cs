using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AdvancedMod.Basic.Element;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Contents.Hallow.NPCs;
using AdvancedMod.Contents.Hallow.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using R = AdvancedMod.Contents.Hallow.NPCs.DestroyerAttackRules;

namespace AdvancedMod.Contents.Development;

/// <summary>隔离客户端的攻击几何、实际伤害／反应与真实绘制验收，不修改正式充能数。</summary>
public sealed class DestroyerAttackLiveTests : ModSystem
{
    internal static bool Enabled => Environment.GetEnvironmentVariable("ADVANCEDMOD_DESTROYER_ATTACK_TEST") == "1";
    private readonly List<object> results = new();
    private readonly HashSet<int> captures = new();
    private NPC head;
    private int tick;
    private bool finished;
    private Vector2 anchor;
    private static Player P => Main.LocalPlayer;
    private string Output => Path.Combine(Main.SavePath, "AttackCaptures");
    public override void Load() { if (Enabled && !Main.dedServ) On_Main.Draw += Draw; }
    public override void Unload() => On_Main.Draw -= Draw;
    private void Check(string name, bool pass, object detail = null) => results.Add(new { name, status = pass ? "PASS" : "FAIL", detail });
    private void ClearShots() { foreach (Projectile p in Main.ActiveProjectiles) p.Kill(); }
    private void Enter(DestroyerBehavior.PhaseTwoState state) => typeof(DestroyerBehavior).GetMethod("Enter", BindingFlags.Instance | BindingFlags.NonPublic)
        .Invoke(head.GetGlobalNPC<DestroyerBehavior>(), new object[] { head, state });
    public override void PostUpdateEverything()
    {
        if (!Enabled || Main.gameMenu || finished) return;
        tick++; Main.autoSave = false; Main.dayTime = false; Main.time = 18000;
        P.statLife = P.statLifeMax2 = 5000; P.dead = false; P.immune = true; P.immuneTime = 600;
        Main.hideUI = true; Main.mapStyle = 0; Main.playerInventory = false;
        if (tick == 10)
        {
            anchor = new(Main.maxTilesX * 8, (float)Main.worldSurface * 16 - 1200);
            foreach (NPC npc in Main.ActiveNPCs) npc.active = false;
            int slot = NPC.NewNPC(new EntitySource_Misc("毁灭者弹幕隔离验收"), (int)anchor.X - 280, (int)anchor.Y - 120, NPCID.TheDestroyer);
            head = Main.npc[slot]; head.GetGlobalNPC<DestroyerBehavior>().SetHallow(head, true);
        }
        if (tick < 11) return;
        P.Center = anchor; P.velocity = Vector2.Zero;
        head.Center = anchor + new Vector2(-280, -120); head.velocity = Vector2.Zero;
        foreach (NPC part in Main.npc.Where(n => n.active && n.realLife == head.whoAmI && n.whoAmI != head.whoAmI)) part.alpha = 255;
        try
        {
            if (tick == 20) { Enter(DestroyerBehavior.PhaseTwoState.Idle); Run(); }
            if (tick is 40 or 110 or 180 or 250 or 320)
            {
                ClearShots(); Enter(DestroyerBehavior.PhaseTwoState.Idle);
                DestroyerBehavior.SpawnCrashAttack(head, (DestroyerAttackKind)((tick - 40) / 70));
            }
            // 展示夹具保持Idle，不影响正式攻击间隔或自然状态机。
            if (tick >= 20) typeof(DestroyerBehavior).GetProperty(nameof(DestroyerBehavior.PhaseTimer)).SetValue(head.GetGlobalNPC<DestroyerBehavior>(), 0);
            if (tick == 390)
            {
                ClearShots(); int id = Projectile.NewProjectile(head.GetSource_FromAI(), anchor - new Vector2(700, 0), Vector2.Zero,
                    ModContent.ProjectileType<DestroyerUltimatePreview>(), 0, 0, Main.myPlayer, head.whoAmI, -MathHelper.PiOver2, 1);
                CaptureUltimateGallery((DestroyerUltimatePreview)Main.projectile[id].ModProjectile);
            }
            if (tick > 455) Finish();
        }
        catch (Exception ex) { Check("运行异常", false, ex.ToString()); Finish(); }
    }
    private void Run()
    {
        var b = head.GetGlobalNPC<DestroyerBehavior>(); int[] counts = { 12, 6, 8, 4, 1 };
        TagEnum[] tags = { TagEnum.Pyro, TagEnum.Anemo, TagEnum.Electro, TagEnum.Hydro, TagEnum.Cryo };
        int[] incoming = { 1, 6, 3, 2, 4 }, complement = { 4, 2, 2, 3, 1 };
        int baseline = head.GetAttackDamage_ForProjectiles(25, 22);
        for (int kind = 0; kind < 5; kind++)
        {
            ClearShots(); DestroyerBehavior.SpawnCrashAttack(head, (DestroyerAttackKind)kind);
            var shots = Main.projectile.Where(p => p.active && p.ModProjectile is DestroyerCrashProjectile).ToArray();
            Check("发射数量-" + (DestroyerAttackKind)kind, shots.Length == counts[kind]);
            Check("神圣元素与来源-" + kind, shots.All(p => p.GetGlobalProjectile<CombatGlobalProjectile>().Profile.Tags.Contains(tags[kind]) &&
                p.GetGlobalProjectile<CombatGlobalProjectile>().Profile.Tags.Contains(TagEnum.Hallow) && p.GetGlobalProjectile<DestroyerAttackOwner>().Head == head.whoAmI));
            float multiplier = kind == 3 ? R.WaterMultiplier : kind == 4 ? R.FrostMultiplier : 1;
            Check("伤害倍率仅应用一次-" + kind, shots.All(p => p.damage == Math.Max(1, (int)(baseline * multiplier))));
            if (kind == 0)
            {
                var angles = shots.Select(p => p.velocity.ToRotation()).OrderBy(x => x).ToArray();
                Check("12向等角火焰", Enumerable.Range(1, 11).All(i => Math.Abs(angles[i] - angles[i - 1] - MathHelper.TwoPi / 12) < .001));
                int before = Main.dust.Count(d => d.active && d.type == DustID.CursedTorch && d.scale >= 3);
                foreach (var p in shots) p.ModProjectile.AI();
                Check("二阶段大尺寸无重力魔焰尾迹", Main.dust.Count(d => d.active && d.type == DustID.CursedTorch && d.scale >= 3 && d.noGravity) > before);
            }
            if (kind == 1) Check("六风刃从圆外指向锁定圆心", shots.All(p => Math.Abs(p.Distance(P.Center) - R.WindSpawnRadius) < .1 && Vector2.Dot(p.velocity.SafeNormalize(Vector2.Zero), (P.Center - p.Center).SafeNormalize(Vector2.Zero)) > .999));
            if (kind == 2) Check("八体节各自瞄准", shots.All(p => Vector2.Dot(p.velocity.SafeNormalize(Vector2.Zero), (P.Center - p.Center).SafeNormalize(Vector2.Zero)) > .999));
            var shot = shots[0]; var data = (DestroyerCrashProjectile)shot.ModProjectile;
            if (kind == 3)
            {
                for (int i = 0; i < 30; i++) data.AI();
                Check("旋风绕锁定圆心缩圈", Math.Abs(shot.Distance(data.Anchor) - R.WaterRadius * .75f) < .1);
            }
            if (kind == 4)
            {
                Vector2 velocity = shot.velocity; P.Center += new Vector2(200, 0); data.AI(); P.Center = anchor;
                Check("寒霜不重新跟踪玩家", shot.velocity == velocity);
                Vector2 direction = velocity.SafeNormalize(Vector2.UnitY);
                var inside = new Rectangle((int)(shot.Center.X + direction.X * 300) - 5, (int)(shot.Center.Y + direction.Y * 300) - 5, 10, 10);
                Check("寒霜光束沿线判定", data.Colliding(shot.Hitbox, inside) == true);
                Check("寒霜远离射线不命中", data.Colliding(shot.Hitbox, new Rectangle((int)shot.Center.X, (int)shot.Center.Y + 500, 10, 10)) == false);
            }
            foreach (var other in shots.Skip(1)) other.Kill();
            P.GetModPlayer<CombatPlayer>().SetNetworkState(new ElementTarget(ElementTarget.DefaultOperationLimit));
            P.GetModPlayer<CombatPlayer>().Elements.Attach(new(complement[kind], 2, 20));
            P.GetModPlayer<CombatPlayer>().Elements.Attach(new(incoming[kind], .5, 20));
            P.immune = false; P.immuneTime = 0; P.hurtCooldowns[0] = P.hurtCooldowns[1] = 0;
            shot.Center = P.Center; int life = P.statLife, charge = b.ReactionCount;
            shot.Damage();
            Check("真实伤害与神圣充能-" + kind, P.statLife < life && b.ReactionCount == charge + 1, new { life, after = P.statLife, charge, actual = b.ReactionCount });
            P.immune = true; P.immuneTime = 600;
        }
        ClearShots();
        // 一阶段通过原生探测怪来源验证共用粒子路径，不替换其射击AI。
        NPC body = Main.npc.First(n => n.active && n.type == NPCID.TheDestroyerBody && n.realLife == head.whoAmI);
        body.GetGlobalNPC<DestroyerBehavior>().SegmentElement = ProbeElement.Fire;
        int probeIndex = NPC.NewNPC(body.GetSource_FromAI(), (int)anchor.X, (int)anchor.Y, NPCID.Probe);
        NPC probe = Main.npc[probeIndex];
        int fireIndex = Projectile.NewProjectile(probe.GetSource_FromAI(), anchor, Vector2.UnitX * 9, ProjectileID.PinkLaser, 20, 0, Main.myPlayer);
        var fire = Main.projectile[fireIndex]; int dustBefore = Main.dust.Count(d => d.active && d.type == DustID.CursedTorch && d.scale >= 3);
        for (int i = 0; i < 8; i++) fire.GetGlobalProjectile<HallowProbeShot>().PreAI(fire);
        int dustAfter = Main.dust.Count(d => d.active && d.type == DustID.CursedTorch && d.scale >= 3 && d.noGravity);
        Check("一阶段魔焰同样恢复尾迹", dustAfter > dustBefore,
            new { dustBefore, dustAfter, probeElement = probe.GetGlobalNPC<HallowProbeBehavior>().Element, shotElement = fire.GetGlobalProjectile<HallowProbeShot>().Element });
        probe.active = false; ClearShots();
        int previewId = Projectile.NewProjectile(head.GetSource_FromAI(), anchor - new Vector2(700, 0), Vector2.Zero, ModContent.ProjectileType<DestroyerUltimatePreview>(), 0, 0, Main.myPlayer, head.whoAmI, -.7f, 1);
        var preview = (DestroyerUltimatePreview)Main.projectile[previewId].ModProjectile;
        for (int i = 0; i < R.WarningSweepTicks; i++) preview.AI();
        Check("预警精确旋转180度", Math.Abs(preview.WarningAngle - preview.StartAngle - MathHelper.Pi) < .0001);
        float stop = preview.WarningAngle; for (int i = 0; i < R.WarningHoldTicks - 1; i++) preview.AI();
        Check("终止线停留且预警无伤害", preview.Warning && preview.WarningAngle == stop && preview.CanDamage() == false);
        bool contained = true;
        for (int t = 0; t <= 18; t++) for (int i = 1; i <= 80; i++)
        {
            Vector2 offset = preview.WhipPoint(i / 80f, t / 18f) - preview.Projectile.Center;
            float angle = MathHelper.WrapAngle(offset.ToRotation() - preview.StartAngle);
            if (angle < -.001f) angle += MathHelper.TwoPi;
            contained &= angle <= MathHelper.Pi + .001f && offset.Length() <= R.WhipLength + .01f;
        }
        Check("鞭形全程位于已提示半圆内", contained);
        ClearShots();
    }
    private void CaptureUltimateGallery(DestroyerUltimatePreview preview)
    {
        var device = Main.instance.GraphicsDevice; var previous = device.GetRenderTargets(); var viewport = device.Viewport;
        Vector2 oldScreen = Main.screenPosition; int oldAge = preview.Age;
        using var target = new RenderTarget2D(device, 1800, 1800);
        int[] ages = { 1, 15, 30, 44, 53, 62 };
        try
        {
            device.SetRenderTarget(target); device.Clear(new Color(18, 22, 30));
            Main.screenPosition = preview.Projectile.Center - new Vector2(200, 1400);
            for (int i = 0; i < ages.Length; i++)
            {
                typeof(DestroyerUltimatePreview).GetProperty(nameof(DestroyerUltimatePreview.Age)).SetValue(preview, ages[i]);
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null,
                    Matrix.CreateScale(.3f) * Matrix.CreateTranslation((i % 3) * 600, (i / 3) * 900, 0));
                Color color = Color.White; preview.PreDraw(ref color); Main.spriteBatch.End();
                Main.spriteBatch.Begin(); Utils.DrawBorderString(Main.spriteBatch, "Tick " + ages[i] + (preview.Warning ? " - warning" : " - whip"), new Vector2(i % 3 * 600 + 30, i / 3 * 900 + 20), Color.White); Main.spriteBatch.End();
            }
        }
        finally { Main.screenPosition = oldScreen; typeof(DestroyerUltimatePreview).GetProperty(nameof(DestroyerUltimatePreview.Age)).SetValue(preview, oldAge); device.SetRenderTargets(previous); device.Viewport = viewport; }
        Directory.CreateDirectory(Output); using var file = File.Create(Path.Combine(Output, "ultimate-sequence.png")); target.SaveAsPng(file, target.Width, target.Height);
    }
    private void Draw(On_Main.orig_Draw orig, Main self, GameTime time)
    {
        orig(self, time);
        int[] moments = { 55, 140, 188, 280, 335 };
        int index = Array.FindIndex(moments, t => tick >= t && tick <= t + 4);
        if (index < 0 || !captures.Add(index)) return;
        Directory.CreateDirectory(Output);
        var pixels = new Color[Main.screenWidth * Main.screenHeight]; self.GraphicsDevice.GetBackBufferData(pixels);
        using var image = new Texture2D(self.GraphicsDevice, Main.screenWidth, Main.screenHeight); image.SetData(pixels);
        using var file = File.Create(Path.Combine(Output, ((DestroyerAttackKind)index) + ".png")); image.SaveAsPng(file, image.Width, image.Height);
    }
    private void Finish()
    {
        finished = true; Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, "report.json"), JsonSerializer.Serialize(new { results }, new JsonSerializerOptions { WriteIndented = true })); Main.instance.Exit();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AdvancedMod.Basic.Global;
using AdvancedMod.Contents.Acoustics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

/// <summary>仅隔离测试环境启用：真实 AI、Damage、元素提交及完整 Draw 截图。</summary>
public sealed class SoundLiveTests : ModSystem
{
    private static bool Enabled => Environment.GetEnvironmentVariable("ADVANCEDMOD_SOUND_LIVE_TEST") == "1";
    private readonly List<object> results = new(), captures = new();
    private readonly List<Hit> hits = new();
    private readonly HashSet<ulong> ids = new();
    private readonly string[] names = { "single", "reflect", "same", "opposite", "partial", "stress" };
    private record Hit(ulong Id, int Damage, int ExpectedDamage, double Element, int Reflections, int FusionDepth, float Coherence);
    private NPC target;
    private Vector2 anchor;
    private int stage = -1, tick;
    private bool running, captured;
    private float fusedCoherence = -1;
    private int maxReflections, maxFusion;
    private string DirectoryPath => Path.Combine(Main.SavePath, "SoundCaptures");
    public override void Load() { if (Enabled && !Main.dedServ) On_Main.Draw += Capture; }
    public override void Unload() { On_Main.Draw -= Capture; if (Enabled) ElementProbeNPC.AfterProjectileHit = null; }
    private void Result(string name, bool pass, object detail = null) => results.Add(new { name, status = pass ? "PASS" : "FAIL", detail });
    public override void PostUpdateEverything()
    {
        if (!Enabled || Main.dedServ || Main.gameMenu) return;
        if (!running)
        {
            running = true; Main.autoSave = false;
            anchor = Main.LocalPlayer.Center - new Vector2(0, SoundRules.DemoHeight);
            ElementProbeNPC.AfterProjectileHit = Record;
            Next();
        }
        Main.hideUI = true;
        Main.LocalPlayer.Center = anchor + new Vector2(0, SoundRules.DemoHeight);
        Main.LocalPlayer.velocity = Vector2.Zero;
        Main.LocalPlayer.statLife = Main.LocalPlayer.statLifeMax2;
        if (target is { active: true }) target.velocity = Vector2.Zero;
        foreach (var p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is not SoundWave w) continue;
            ids.Add(w.Id); maxReflections = Math.Max(maxReflections, w.Reflections); maxFusion = Math.Max(maxFusion, w.FusionDepth);
            if (w.FusionDepth > 0) fusedCoherence = w.Coherence;
        }
        if (++tick < 240) return;
        string name = names[stage];
        Result(name + "：波包寿命终点清理", !Main.projectile.Any(p => p.active && p.ModProjectile is SoundWave));
        Result(name + "：每个波包对靶标最多一次命中", hits.GroupBy(h => h.Id).All(g => g.Count() == 1), hits.ToArray());
        Result(name + "：反射与融合深度有界", maxReflections <= SoundRules.MaxReflections && maxFusion <= SoundRules.MaxFusionDepth);
        if (stage < 5)
        {
            Result(name + "：真实命中且伤害等于当前白值", hits.Count == 1 && hits.All(h => h.Damage == h.ExpectedDamage), hits.ToArray());
            double total = target.GetGlobalNPC<CombatGlobalNPC>().Elements.Snapshot.Elements[6].Total;
            Result(name + "：风附着提交", hits.Count > 0 && Math.Abs(total - hits.Sum(h => h.Element)) < 0.0001, new { total });
        }
        if (stage == 1) Result("反射子波转向后命中", hits.Any(h => h.Reflections == 1) && ids.Count >= 2);
        if (stage is >= 2 and <= 4)
        {
            float expected = stage == 2 ? 1 : stage == 3 ? 0 : 0.5f;
            Result(name + "：交汇相位驱动融合", Math.Abs(fusedCoherence - expected) < 0.001 && hits.Any(h => h.FusionDepth == 1), new { fusedCoherence, ids = ids.Count });
        }
        SoundDemoSystem.Clear(Main.myPlayer);
        if (target is { active: true }) target.active = false;
        if (++stage < names.Length) Begin(); else Finish();
    }
    private void Next() { stage = 0; Begin(); }
    private void Begin()
    {
        tick = 0; captured = false; hits.Clear(); ids.Clear(); maxFusion = maxReflections = 0; fusedCoherence = -1;
        Player player = Main.LocalPlayer;
        player.GetDamage(DamageClass.Generic) = StatModifier.Default;
        player.GetDamage(DamageClass.Summon) = StatModifier.Default;
        Vector2 targetPosition = stage == 1 ? anchor - new Vector2(0, 180) : stage >= 2 ? anchor + new Vector2(360, 0) : anchor;
        int index = NPC.NewNPC(player.GetSource_Misc("SoundLiveTest"), (int)targetPosition.X, (int)targetPosition.Y, ModContent.NPCType<ElementProbeNPC>());
        target = Main.npc[index]; target.Center = targetPosition; target.damage = 0;
        SoundDemoSystem.Sentry(player, anchor - new Vector2(SoundRules.DemoSeparation, 0), Vector2.UnitX, 0, true);
        if (stage >= 2)
            SoundDemoSystem.Sentry(player, anchor + new Vector2(SoundRules.DemoSeparation, 0), -Vector2.UnitX,
                stage == 3 ? MathHelper.Pi : stage == 4 ? MathHelper.PiOver2 : 0, true);
        if (stage is 1 or 5)
            SoundDemoSystem.Sentry(player, anchor, Vector2.Normalize(new(1, 1)), 0, false);
        if (stage == 5)
        {
            SoundDemoSystem.Sentry(player, anchor - new Vector2(0, SoundRules.DemoSeparation), Vector2.UnitY, MathHelper.PiOver2, true);
            SoundDemoSystem.Sentry(player, anchor + new Vector2(0, SoundRules.DemoSeparation), -Vector2.UnitY, MathHelper.Pi, true);
        }
        Mod.Logger.Info($"声波实机开始：{names[stage]}");
    }
    private void Record(Projectile projectile, NPC npc)
    {
        if (npc != target || projectile.ModProjectile is not SoundWave w) return;
        int previousDamage = hits.Sum(h => h.Damage);
        hits.Add(new(w.Id, target.lifeMax - target.life - previousDamage, projectile.damage,
            projectile.GetGlobalProjectile<CombatGlobalProjectile>().ElementAmount, w.Reflections, w.FusionDepth, w.Coherence));
    }
    private void Capture(On_Main.orig_Draw orig, Main self, GameTime gameTime)
    {
        orig(self, gameTime);
        if (!running || stage < 0 || stage >= names.Length || tick < 65 || captured) return;
        captured = true;
        Directory.CreateDirectory(DirectoryPath);
        try
        {
            var device = self.GraphicsDevice;
            int width = device.PresentationParameters.BackBufferWidth, height = device.PresentationParameters.BackBufferHeight;
            var data = new Color[width * height]; device.GetBackBufferData(data);
            using var image = new Texture2D(device, width, height); image.SetData(data);
            string file = names[stage] + ".png";
            using (var stream = File.Create(Path.Combine(DirectoryPath, file))) image.SaveAsPng(stream, width, height);
            captures.Add(new { file, tick, status = "CAPTURED" });
        }
        catch (Exception e) { captures.Add(new { file = names[stage], status = "BLOCKED", error = e.Message }); }
    }
    private void Finish()
    {
        ElementProbeNPC.AfterProjectileHit = null;
        Result("清场后无哨兵和波包", !Main.projectile.Any(p => p.active && p.ModProjectile is SoundWave or SoundSentry));
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(Path.Combine(DirectoryPath, "report.json"), JsonSerializer.Serialize(new { results, captures }, new JsonSerializerOptions { WriteIndented = true }));
        Main.instance.Exit();
    }
}


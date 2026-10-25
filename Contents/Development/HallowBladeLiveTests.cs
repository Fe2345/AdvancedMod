using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using AdvancedMod.Contents.Hallow.Weapons;

namespace AdvancedMod.Contents.Development;

/// <summary>隔离存档专用：驱动原生 ItemCheck 发起攻击，不直接调用武器 Shoot 或伪造伤害。</summary>
public sealed class HallowBladeLiveTests : ModSystem
{
    internal static bool Enabled => Environment.GetEnvironmentVariable("ADVANCEDMOD_HALLOW_LIVE_TEST") == "1";
    internal static bool Running;
    private static readonly List<SwingRecord> swings = new();
    private static readonly List<object> results = new();
    private static readonly HashSet<string> captured = new();
    private static NPC dummy;
    private static Vector2 anchor;
    private static int ticks, round, wait;
    private static string pendingCapture;
    private sealed class SwingRecord
    {
        public int Stage { get; set; }
        public int Duration { get; set; }
        public float StartAngle { get; set; }
        public int Identity { get; set; }
        public ulong StartTick { get; set; }
        public ulong EndTick { get; set; }
        public List<int> Hits { get; } = new();
    }
    public override void Load() { if (Enabled && !Main.dedServ) On_Main.Draw += Capture; }
    public override void Unload() { On_Main.Draw -= Capture; Running = false; }
    internal static void Start()
    {
        if (!Enabled || Running || Main.netMode != NetmodeID.SinglePlayer) return;
        Main.autoSave = false;
        Running = true; ticks = round = wait = 0; swings.Clear(); results.Clear(); captured.Clear(); pendingCapture = null;
        var p = Main.LocalPlayer;
        p.inventory[0] = new Item(ModContent.ItemType<HeavensJudgment>());
        p.selectedItem = 0;
        anchor = p.Center - new Vector2(0, 180);
        int id = NPC.NewNPC(p.GetSource_Misc("HallowLiveTest"), (int)(anchor.X + 160), (int)anchor.Y, ModContent.NPCType<ElementProbeNPC>());
        dummy = Main.npc[id]; dummy.Center = anchor + new Vector2(160, 0); dummy.damage = 0;
        ModContent.GetInstance<HallowBladeLiveTests>().Mod.Logger.Info("天裁实机测试开始：原生物品使用、1倍和2倍攻速四段连招。");
    }
    internal static void Drive(Player p)
    {
        if (!Running || p.whoAmI != Main.myPlayer) return;
        // 仅隔离验收隐藏界面，避免地图及首次进入世界的成就通知遮挡剑气。
        Main.hideUI = true;
        p.Center = anchor; p.velocity = Vector2.Zero; p.selectedItem = 0;
        p.statLife = p.statLifeMax2;
        p.GetDamage(DamageClass.Generic) = StatModifier.Default;
        p.GetDamage(DamageClass.Melee) = StatModifier.Default;
        p.GetAttackSpeed(DamageClass.Generic) = 1;
        p.GetAttackSpeed(DamageClass.Melee) = round == 0 ? 1 : 2;
        p.GetCritChance(DamageClass.Generic) = 4;
        p.GetCritChance(DamageClass.Melee) = 0;
        p.controlUseItem = swings.Count < (round + 1) * 4 && wait == 0;
        p.releaseUseItem = true;
        Main.mouseX = (int)(anchor.X + 300 - Main.screenPosition.X);
        Main.mouseY = (int)(anchor.Y - Main.screenPosition.Y);
        if (dummy is { active: true }) { dummy.Center = anchor + new Vector2(160, 0); dummy.velocity = Vector2.Zero; }
    }
    internal static void Spawn(Projectile projectile)
    {
        if (!Running || projectile.ModProjectile is not HeavensJudgmentSwing swing) return;
        swings.Add(new() { Stage = swing.Stage, Duration = swing.Duration, StartAngle = swing.StartAngle,
            Identity = projectile.identity, StartTick = Main.GameUpdateCount });
    }
    internal static void End(Projectile projectile)
    {
        if (!Running || projectile.ModProjectile is not HeavensJudgmentSwing) return;
        var record = swings.FindLast(x => x.Identity == projectile.identity);
        if (record != null) record.EndTick = Main.GameUpdateCount;
    }
    internal static void RecordHit(Projectile projectile, NPC npc, int damageDone)
    {
        if (!Running || npc != dummy || projectile.ModProjectile is not HeavensJudgmentSwing) return;
        var record = swings.FindLast(x => x.Identity == projectile.identity);
        if (record != null) record.Hits.Add(damageDone);
    }
    public override void PostUpdateEverything()
    {
        if (!Running) return;
        ticks++;
        foreach (var p in Main.ActiveProjectiles)
            if (p.ModProjectile is HeavensJudgmentSwing swing && swing.Progress >= 0.45f && swing.Progress <= 0.65f)
            {
                string key = $"speed-{round + 1}-stage-{swing.Stage + 1}";
                if (!captured.Contains(key)) pendingCapture = key;
            }
        if (ticks > 1200) { results.Add(new { name = "完整运行", status = "FAIL", error = "超过1200游戏刻未完成" }); Finish(); return; }
        if (swings.Count < (round + 1) * 4 || Main.LocalPlayer.ownedProjectileCounts[ModContent.ProjectileType<HeavensJudgmentSwing>()] > 0) return;
        if (++wait < 25) return;
        int expectedDuration = round == 0 ? 40 : 20;
        int[] expectedHits = { 1, 1, 2, 1 }, expectedDamage = { 135, 135, 101, 371 };
        for (int stage = 0; stage < 4; stage++)
        {
            var s = swings[round * 4 + stage];
            bool pass = s.Stage == stage && s.Duration == expectedDuration && s.Hits.Count == expectedHits[stage] && s.Hits.All(d => d == expectedDamage[stage]);
            results.Add(new { name = $"{round + 1}倍攻速第{stage + 1}段时长与命中", status = pass ? "PASS" : "FAIL", actual = s });
            if (stage > 0)
            {
                var previous = swings[round * 4 + stage - 1];
                float expected = HallowBladeRules.Angle(stage - 1, previous.StartAngle, 1);
                results.Add(new { name = $"{round + 1}倍攻速第{stage + 1}段角度衔接", status = MathF.Abs(s.StartAngle - expected) < 0.0001f ? "PASS" : "FAIL" });
            }
        }
        if (round++ == 0) { wait = 0; return; }
        results.Add(new { name = "基础暴击面板8%", status = Main.LocalPlayer.GetWeaponCrit(Main.LocalPlayer.HeldItem) == 8 ? "PASS" : "FAIL" });
        Finish();
    }
    private static void Finish()
    {
        Running = false;
        if (dummy != null) dummy.active = false;
        string directory = Path.Combine(Main.SavePath, "HallowCaptures"); Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "report.json"), JsonSerializer.Serialize(new { utc = DateTimeOffset.UtcNow, results, captures = captured }, new JsonSerializerOptions { WriteIndented = true }));
        ModContent.GetInstance<HallowBladeLiveTests>().Mod.Logger.Info($"天裁实机测试结束：{results.Count}项，截图{captured.Count}张。");
        Main.instance.Exit();
    }
    private void Capture(On_Main.orig_Draw orig, Main self, GameTime gameTime)
    {
        orig(self, gameTime);
        if (pendingCapture == null) return;
        string name = pendingCapture; pendingCapture = null;
        try
        {
            var device = self.GraphicsDevice;
            int width = device.PresentationParameters.BackBufferWidth, height = device.PresentationParameters.BackBufferHeight;
            var pixels = new Color[width * height]; device.GetBackBufferData(pixels);
            using var texture = new Texture2D(device, width, height); texture.SetData(pixels);
            string directory = Path.Combine(Main.SavePath, "HallowCaptures"); Directory.CreateDirectory(directory);
            using var output = File.Create(Path.Combine(directory, name + ".png")); texture.SaveAsPng(output, width, height);
            captured.Add(name);
        }
        catch (Exception e) { captured.Add(name); results.Add(new { name = "截图" + name, status = "BLOCKED", error = e.Message }); }
    }
}

public sealed class HallowBladeTestPlayer : ModPlayer
{
    public override void OnEnterWorld() { if (HallowBladeLiveTests.Enabled) Main.QueueMainThreadAction(HallowBladeLiveTests.Start); }
    public override bool PreItemCheck() { HallowBladeLiveTests.Drive(Player); return true; }
}
public sealed class HallowBladeTestProjectile : GlobalProjectile
{
    public override void OnSpawn(Projectile projectile, IEntitySource source) => HallowBladeLiveTests.Spawn(projectile);
    public override void OnKill(Projectile projectile, int timeLeft) => HallowBladeLiveTests.End(projectile);
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone) => HallowBladeLiveTests.RecordHit(projectile, target, damageDone);
}

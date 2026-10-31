using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Contents.Hallow.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

/// <summary>真实召唤、原生受击释放、实际弹幕命中与绘制；只在独立测试进程启用。</summary>
public sealed class DestroyerPhaseOneLiveTests : ModSystem
{
    internal static bool Enabled => Environment.GetEnvironmentVariable("ADVANCEDMOD_DESTROYER_PHASE1_TEST") == "1";
    internal static int WaterHits;
    private int tick;
    private bool prepared, captured, done;
    private readonly List<object> results = new();
    private NPC head;
    private NPC[] bodies;
    private readonly List<NPC> probes = new();
    private Projectile water;
    private readonly List<Projectile> galleryShots = new();
    private Vector2 Anchor => new(Main.maxTilesX / 2 * 16, (float)Main.worldSurface * 16 - 200);
    private string DirectoryPath => Path.Combine(Main.SavePath, "PhaseOneCaptures");
    public override void Load() { if (Enabled && !Main.dedServ) On_Main.Draw += Draw; }
    public override void Unload() => On_Main.Draw -= Draw;
    internal static void Drive(Player player)
    {
        if (!Enabled || Main.gameMenu) return;
        var test = ModContent.GetInstance<DestroyerPhaseOneLiveTests>();
        if (!test.prepared)
        {
            test.prepared = true; Main.autoSave = false;
            foreach (NPC npc in Main.ActiveNPCs) npc.active = false;
            int cx = (int)test.Anchor.X / 16, cy = (int)test.Anchor.Y / 16;
            for (int x = cx - 85; x <= cx + 85; x++)
            for (int y = cy - 65; y <= cy + 65; y++)
            {
                var tile = Main.tile[x, y]; tile.ClearEverything();
                if (y >= cy + 12) { tile.HasTile = true; tile.TileType = Math.Abs(x - cx) <= 20 && y < cy + 32 ? TileID.Pearlstone : TileID.Stone; }
            }
            player.inventory[0] = new Item(ItemID.MechanicalWorm, 3);
        }
        player.Center = test.Anchor; player.velocity = Vector2.Zero; player.selectedItem = 0;
        player.dead = false; player.respawnTimer = 0; player.statLifeMax2 = player.statLife = 5000;
        player.immune = test.tick is not (205 or 215); player.immuneTime = player.immune ? 600 : 0;
        if (!player.immune) { player.hurtCooldowns[0] = player.hurtCooldowns[1] = 0; }
        player.controlUseItem = test.tick == 120; player.releaseUseItem = true;
        Main.dayTime = false; Main.time = 18000; Main.hideUI = true; Main.mapStyle = 0; Main.playerInventory = false;
    }
    private void Check(string name, bool pass, object detail = null) => results.Add(new { name, status = pass ? "PASS" : "FAIL", detail });
    public override void PostUpdateEverything()
    {
        if (!Enabled || !prepared || Main.gameMenu || done) return;
        tick++;
        try
        {
            if (tick == 170)
            {
                head = Main.npc.First(n => n.active && n.type == NPCID.TheDestroyer);
                bodies = Main.npc.Where(n => n.active && n.type == NPCID.TheDestroyerBody && n.realLife == head.whoAmI).ToArray();
                Check("真实神圣召唤和一次消耗", Main.LocalPlayer.ZoneHallow && Main.LocalPlayer.inventory[0].stack == 2 && head.GetGlobalNPC<DestroyerBehavior>().Hallow);
                Check("所有身体已分配元素", bodies.Length == NPC.GetDestroyerSegmentsCount() && bodies.All(n => n.GetGlobalNPC<DestroyerBehavior>().SegmentElement != ProbeElement.None));
                var bar = head.GetGlobalNPC<DestroyerBarState>();
                Check("未释放探测怪计入总数", bar.Remaining == bodies.Length && !DestroyerBehavior.IsPhaseTwoReady(head));
                Check("初始头部减伤80%", Math.Abs(head.GetIncomingStrikeModifiers(DamageClass.Generic, 0).FinalDamage.ApplyTo(100) - 20) < .01);
                Check("身体和尾部共用减伤", bodies.All(n => Math.Abs(n.GetIncomingStrikeModifiers(DamageClass.Generic, 0).FinalDamage.ApplyTo(100) - 20) < .01));
                for (int i = 0; i < 5; i++)
                {
                    bodies[i].GetGlobalNPC<DestroyerBehavior>().SegmentElement = (ProbeElement)(i + 1);
                    var before = Main.npc.Where(n => n.active && n.type == NPCID.Probe).Select(n => n.whoAmI).ToHashSet();
                    Release(bodies[i]);
                    NPC probe = Main.npc.First(n => n.active && n.type == NPCID.Probe && !before.Contains(n.whoAmI));
                    probes.Add(probe);
                    Check("受击释放继承-" + (ProbeElement)(i + 1), probe.GetGlobalNPC<HallowProbeBehavior>().Element == (ProbeElement)(i + 1) &&
                        probe.GetGlobalNPC<DestroyerBarState>().Owner == head.whoAmI);
                }
                Check("释放不扣数量", bar.Remaining == bodies.Length);
            }
            if (tick == 190)
            {
                foreach (Projectile p in Main.ActiveProjectiles) p.Kill();
                for (int i = 0; i < probes.Count; i++)
                {
                    NPC probe = probes[i]; probe.Center = Anchor + new Vector2(-250 + i * 100, -150);
                    probe.target = Main.myPlayer; probe.justHit = false; probe.localAI[0] = 118;
                    probe.VanillaAI();
                    Check("射击阈值前不提前发射-" + i, probe.localAI[0] == 119 && Main.projectile.Count(p => p.active && p.type == ProjectileID.PinkLaser) == i);
                    probe.VanillaAI();
                    Projectile shot = Main.projectile.First(p => p.active && p.TryGetGlobalProjectile<HallowProbeShot>(out var s) && s.Element == (ProbeElement)(i + 1));
                    var data = shot.GetGlobalProjectile<CombatGlobalProjectile>();
                    Check("原版时机射击及元素-" + (ProbeElement)(i + 1), probe.localAI[0] == 0 && data.Profile.Tags.Contains(DestroyerPhaseOneRules.Tag((ProbeElement)(i + 1))) && !data.Profile.Tags.Contains(TagEnum.Hallow) && data.ElementAmount == 1);
                    if (i == 0)
                    {
                        water = shot;
                        Check("水弹未命中三秒寿命且每刻一次更新", shot.timeLeft == 180 && shot.extraUpdates == 0);
                    }
                    probe.localAI[0] = 119; probe.justHit = true; probe.VanillaAI();
                    Check("受击重置射击计时-" + i, probe.localAI[0] == 0); probe.justHit = false;
                }
                var ordinary = new NPC(); ordinary.SetDefaults(NPCID.Probe);
                Check("普通探测怪未接管", ordinary.GetGlobalNPC<HallowProbeBehavior>().Element == ProbeElement.None);
            }
            if (tick == 205) { water.Center = Anchor; water.velocity = Vector2.Zero; }
            if (tick == 210) Check("水弹实际命中后返航且保留伤害", water.active && water.GetGlobalProjectile<HallowProbeShot>().Returning && water.hostile && water.penetrate == -1);
            if (tick == 215) { water.Center = Anchor; water.velocity = Vector2.Zero; }
            if (tick == 220) Check("返程实际再次命中玩家", WaterHits >= 2, WaterHits);
            if (tick == 225)
            {
                foreach (Projectile shot in Main.ActiveProjectiles) shot.Kill();
                foreach (NPC probe in probes)
                {
                    probe.justHit = false; probe.localAI[0] = 119; probe.VanillaAI();
                    var element = probe.GetGlobalNPC<HallowProbeBehavior>().Element;
                    galleryShots.Add(Main.projectile.First(p => p.active && p.TryGetGlobalProjectile<HallowProbeShot>(out var state) && state.Element == element));
                }
            }
            if (tick >= 225 && tick < 245)
            {
                for (int i = 0; i < 5; i++)
                {
                    NPC loaded = bodies[i + 5]; loaded.GetGlobalNPC<DestroyerBehavior>().SegmentElement = (ProbeElement)(i + 1);
                    loaded.Center = Anchor + new Vector2(-240 + i * 120, -120); loaded.alpha = 0; loaded.rotation = 0;
                    bodies[i].Center = loaded.Center + new Vector2(0, 60); bodies[i].alpha = 0; bodies[i].rotation = 0;
                    probes[i].Center = loaded.Center + new Vector2(0, -70); probes[i].alpha = 0; probes[i].rotation = 0;
                    galleryShots[i].Center = loaded.Center + new Vector2(0, 200); galleryShots[i].velocity = new Vector2(0, -0.01f);
                    Lighting.AddLight(loaded.Center, Vector3.One);
                }
            }
            if (tick == 250)
            {
                foreach (NPC body in bodies) Release(body);
                var owned = Main.npc.Where(n => n.active && n.type == NPCID.Probe && n.GetGlobalNPC<DestroyerBarState>().Owner == head.whoAmI).ToArray();
                Check("全部体节可原生释放", owned.Length == bodies.Length);
                owned[0].SimpleStrikeNPC(100000, 0);
                Check("击败一只扣一点并减伤下降", head.GetGlobalNPC<DestroyerBarState>().Remaining == bodies.Length - 1 && Math.Abs(head.GetIncomingStrikeModifiers(DamageClass.Generic, 0).FinalDamage.ApplyTo(100) - 21) < .01);
                foreach (NPC probe in owned.Skip(1)) probe.SimpleStrikeNPC(100000, 0);
                Check("全部击败才满足二阶段", DestroyerBehavior.IsPhaseTwoReady(head));
                Check("归零恢复完整伤害", Math.Abs(head.GetIncomingStrikeModifiers(DamageClass.Generic, 0).FinalDamage.ApplyTo(100) - 100) < .01);
                Check("本体和探测怪接触不带元素", head.GetGlobalNPC<CombatGlobalNPC>().Profile.Tags.Count == 0 && probes[0].GetGlobalNPC<CombatGlobalNPC>().Profile.Tags.Count == 0);
            }
            if (tick >= 265 && captured) Finish();
        }
        catch (Exception error) { Check("运行异常", false, error.ToString()); Finish(); }
    }
    private static void Release(NPC body)
    {
        for (int i = 0; i < 2000 && body.ai[2] == 0; i++) body.HitEffect();
    }
    private void Finish()
    {
        done = true; Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(Path.Combine(DirectoryPath, "report.json"), JsonSerializer.Serialize(new { results }, new JsonSerializerOptions { WriteIndented = true }));
        Main.instance.Exit();
    }
    private void Draw(On_Main.orig_Draw orig, Main self, GameTime time)
    {
        orig(self, time);
        if (captured || tick < 230 || tick > 244) return;
        captured = true; Directory.CreateDirectory(DirectoryPath);
        var device = self.GraphicsDevice;
        var pixels = new Color[Main.screenWidth * Main.screenHeight]; device.GetBackBufferData(pixels);
        using var texture = new Texture2D(device, Main.screenWidth, Main.screenHeight); texture.SetData(pixels);
        using (var file = File.Create(Path.Combine(DirectoryPath, "phase1-world.png"))) texture.SaveAsPng(file, texture.Width, texture.Height);
        CaptureGallery(device);
        // 导出实际游戏资产及重染结果，便于逐像素验证只修改红色区域。
        foreach (int type in new[] { NPCID.TheDestroyerBody, NPCID.Probe })
        {
            Main.instance.LoadNPC(type);
            var original = TextureAssets.Npc[type].Value;
            using (var file = File.Create(Path.Combine(DirectoryPath, type + "-original.png"))) original.SaveAsPng(file, original.Width, original.Height);
            var source = new Color[original.Width * original.Height]; original.GetData(source);
            foreach (ProbeElement element in Enum.GetValues<ProbeElement>().Skip(1))
            {
                var colored = DestroyerElementDrawing.Texture(type, element, false);
                var actual = new Color[source.Length]; colored.GetData(actual);
                bool unchanged = true; int changed = 0;
                for (int i = 0; i < source.Length; i++)
                { var p = source[i]; bool red = p.R > p.G * 1.25f && p.R > p.B * 1.25f; if (!red && p != actual[i]) unchanged = false; if (p != actual[i]) changed++; }
                Check($"金属不染色且灯芯变色-{type}-{element}", unchanged && changed > 0);
                using var file = File.Create(Path.Combine(DirectoryPath, type + "-" + element + ".png")); colored.SaveAsPng(file, colored.Width, colored.Height);
            }
        }
    }

    private void CaptureGallery(GraphicsDevice device)
    {
        var prior = device.GetRenderTargets(); var viewport = device.Viewport;
        using var target = new RenderTarget2D(device, 1000, 430);
        using var batch = new SpriteBatch(device);
        try
        {
            device.SetRenderTarget(target); device.Clear(new Color(22, 26, 34));
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            for (int i = 0; i < 5; i++)
            {
                var element = (ProbeElement)(i + 1);
                Utils.DrawBorderString(batch, element.ToString(), new Vector2(85 + i * 180, 20), DestroyerPhaseOneRules.Color(element));
                var npc = new NPC(); npc.SetDefaults(NPCID.TheDestroyerBody); npc.scale = 2; npc.alpha = 0;
                var bodyTexture = TextureAssets.Npc[NPCID.TheDestroyerBody].Value;
                npc.frame = bodyTexture.Frame(1, Main.npcFrameCount[NPCID.TheDestroyerBody]);
                npc.Center = new Vector2(130 + i * 180, 165);
                DestroyerElementDrawing.Draw(npc, element, batch, Vector2.Zero, Color.White);
                var origin = npc.frame.Size() / 2;
                var released = bodyTexture.Frame(1, Main.npcFrameCount[NPCID.TheDestroyerBody], 0, 1);
                batch.Draw(bodyTexture, new Vector2(npc.Center.X, 300), released, Color.White, 0, origin, npc.scale, SpriteEffects.None, 0);
                npc.SetDefaults(NPCID.Probe); npc.scale = 2; npc.alpha = 0;
                npc.frame = TextureAssets.Npc[NPCID.Probe].Value.Frame(1, Main.npcFrameCount[NPCID.Probe]);
                npc.Center = new Vector2(130 + i * 180, 80);
                DestroyerElementDrawing.Draw(npc, element, batch, Vector2.Zero, Color.White);
            }
            Utils.DrawBorderString(batch, "Probe / Loaded segment / Released segment (vanilla)", new Vector2(100, 385), Color.White, .8f);
            batch.End();
        }
        finally { device.SetRenderTargets(prior); device.Viewport = viewport; }
        using var output = File.Create(Path.Combine(DirectoryPath, "phase1-colors.png")); target.SaveAsPng(output, target.Width, target.Height);
    }
}

public sealed class DestroyerPhaseOneTestPlayer : ModPlayer
{
    public override bool PreItemCheck() { DestroyerPhaseOneLiveTests.Drive(Player); return true; }
}

public sealed class DestroyerPhaseOneShotObserver : GlobalProjectile
{
    public override bool IsLoadingEnabled(Mod mod) => DestroyerPhaseOneLiveTests.Enabled;
    public override void OnHitPlayer(Projectile projectile, Player target, Player.HurtInfo info)
    {
        if (projectile.TryGetGlobalProjectile<HallowProbeShot>(out var shot) && shot.Element == ProbeElement.Water)
            DestroyerPhaseOneLiveTests.WaterHits++;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AdvancedMod.Basic.Graphics.BossBars;
using AdvancedMod.Contents.Hallow.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.UI;

namespace AdvancedMod.Contents.Development;

/// <summary>仅隔离进程启用的血条外观、真实探针释放与 TCP 同步验收。</summary>
public sealed class BossBarLiveTests : ModSystem
{
    internal static string Mode => Environment.GetEnvironmentVariable("ADVANCEDMOD_BOSSBAR_TEST");
    internal static bool Enabled => Mode is "preview" or "live" or "server" or "client1" or "client2";
    internal static bool Network => Mode is "server" or "client1" or "client2";
    internal int Tick;
    private int phaseTick, stableTicks, headIndex = -1;
    private bool initial, damaged, trail, caught, finished;
    private string capture;

    private readonly List<object> results = new();
    private string DirectoryPath => Path.Combine(Main.SavePath, "BossBarCaptures");
    private Vector2 Anchor => new(Main.maxTilesX / 2 * 16, (float)Main.worldSurface * 16 - 200);
    internal static NPC Head => Main.npc.FirstOrDefault(n => n.active && n.type == NPCID.TheDestroyer);
    public override void Load()
    {
        if (!Enabled) return;
        if (!Main.dedServ) On_Main.Draw += Draw;
        if (Network) On_Player.LoadPlayer += Rename;
    }
    public override void Unload() { On_Main.Draw -= Draw; On_Player.LoadPlayer -= Rename; }
    private PlayerFileData Rename(On_Player.orig_LoadPlayer orig, string path, bool cloud)
    { var data = orig(path, cloud); data.Player.name = "BossBar-" + Mode; return data; }
    internal static void Drive(Player player)
    {
        if (!Enabled || Main.gameMenu || Main.dedServ || player.whoAmI != Main.myPlayer) return;
        var test = ModContent.GetInstance<BossBarLiveTests>();
        player.Center = test.Anchor; player.velocity = Vector2.Zero;
        player.immune = true; player.immuneTime = 600; player.statLife = player.statLifeMax2;
        Main.hideUI = false; Main.mapStyle = 0; Main.playerInventory = false;
    }
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (Mode != "preview") return;
        layers.Add(new LegacyGameInterfaceLayer("AdvancedMod: 血条实验预览", () =>
        {
            Main.instance.LoadNPC(NPCID.Probe);
            var icon = TextureAssets.Npc[NPCID.Probe].Value;
            DestroyerBossBar.DrawBar(Main.spriteBatch, "毁灭者", "神圣金属铸造的钢铁蠕虫", 0.62f, 0.8f, 56, 80, icon, icon.Frame(1, Main.npcFrameCount[NPCID.Probe]));
            return true;
        }, InterfaceScaleType.UI));
    }
    public override void PostUpdateEverything()
    {
        if (!Enabled || Main.gameMenu || finished) return;
        Main.autoSave = false; Main.dayTime = false; Main.time = 18000;
        if (Main.dedServ && !Main.player.Any(p => p.active)) return;
        Tick++;
        if (Mode == "preview")
        {
            if (Tick == 120) capture = "preview";
            if (Tick == 180) { Check("预览截图已生成", File.Exists(Path.Combine(DirectoryPath, "preview.png"))); Finish(); }
            return;
        }
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            if (Tick == 60)
            {
                foreach (NPC n in Main.ActiveNPCs) n.active = false;
                headIndex = NPC.NewNPC(new EntitySource_Misc("血条实机验收"), (int)Anchor.X + 150, (int)Anchor.Y, NPCID.TheDestroyer, Start: 1);
                Main.npc[headIndex].target = Main.player.First(p => p.active).whoAmI;
            }
            NPC boss = Head;
            if (boss == null) { if (Tick > 1200) { Check("毁灭者存活", false); Finish(); } return; }
            if (Network && !File.Exists(Path.Combine(Environment.GetEnvironmentVariable("ADVANCEDMOD_BOSSBAR_TEST_RUN"), "client2", "BossBarCaptures", "ready.txt"))) return;
            if (++phaseTick == 180)
            {
                var state = boss.GetGlobalNPC<DestroyerBarState>();
                Check("初始探测怪上限来自身体数量", state.Maximum == NPC.GetDestroyerSegmentsCount() && state.Remaining == state.Maximum);
                var bodies = Main.npc.Where(n => n.active && n.type == NPCID.TheDestroyerBody && n.realLife == boss.whoAmI).Take(10).ToArray();
                foreach (NPC body in bodies) for (int i = 0; i < 3000 && body.ai[2] == 0; i++) body.HitEffect();
                Check("真实HitEffect释放十只", bodies.Length == 10 && bodies.All(n => n.ai[2] == 1));
                Check("释放不扣能量", state.Remaining == state.Maximum);
            }
            if (phaseTick == 240)
            {
                var probes = Main.npc.Where(n => n.active && n.type == NPCID.Probe).ToArray();
                Check("释放归属记录", probes.Length == 10 && probes.All(n => n.GetGlobalNPC<DestroyerBarState>().Owner == boss.whoAmI));
                foreach (NPC probe in probes.Take(5)) probe.SimpleStrikeNPC(100000, 1, noPlayerInteraction: true);
                Check("击败五只扣五点", boss.GetGlobalNPC<DestroyerBarState>().Remaining == boss.GetGlobalNPC<DestroyerBarState>().Maximum - 5);
                boss.life = boss.lifeMax / 2; boss.netUpdate = true;
                if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: boss.whoAmI);
            }
            if (phaseTick == 400 && !Network)
            {
                var body = Main.npc.First(n => n.active && n.type == NPCID.TheDestroyerBody && n.realLife == boss.whoAmI);
                int previous = boss.life; body.SimpleStrikeNPC(1000, 1, noPlayerInteraction: true);
                Check("身体受击扣除头部共享生命", boss.life < previous);
            }
            if (phaseTick == 500 && Main.dedServ) { Check("服务端最终生命", boss.life == boss.lifeMax / 2); Finish(); }
        }
        if (Main.dedServ) return;
        NPC head = Head;
        if (head == null) return;
        var data = head.GetGlobalNPC<DestroyerBarState>();
        if (!initial && data.Maximum > 0 && head.life == head.lifeMax)
        {
            initial = true;
            Check("初始生命及能量同步", data.Maximum == NPC.GetDestroyerSegmentsCount() && data.Remaining == data.Maximum);
            Check("实例挂接ModBossBar", head.BossBar is DestroyerBossBar);
            Directory.CreateDirectory(DirectoryPath); File.WriteAllText(Path.Combine(DirectoryPath, "ready.txt"), "初始状态已接收");
            capture = "initial";
        }
        if (head.life <= head.lifeMax / 2 && data.Remaining == data.Maximum - 5)
        {
            damaged = true;
            if (data.YellowRatio > data.RedRatio + 0.1f) { trail = true; capture ??= "damage-trail"; }
            if (Math.Abs(data.YellowRatio - data.RedRatio) < 0.001f) caught = true;
        }
        if (caught) stableTicks++;
        if ((Network && stableTicks >= 90 && Tick > 300) || (!Network && phaseTick == 550))
        {
            Check("确实观察初始满血及能量", initial);
            Check("半血与五只击败同步", damaged);
            Check("红条及时追随生命", Math.Abs(data.RedRatio - (float)head.life / head.lifeMax) < 0.001f);
            Check("黄色伤害残留出现", trail); Check("黄色残留追上红条", caught);
            Check("正式PreDraw已执行", DestroyerBossBar.DrawCount > 0);
            capture = "settled"; finished = true;
        }
        if (Tick > 3600) { Check("阶段未超时", false); Finish(); }
    }
    private void Check(string name, bool passed) => results.Add(new { name, status = passed ? "PASS" : "FAIL" });
    private void Finish()
    {
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(Path.Combine(DirectoryPath, "report.json"), JsonSerializer.Serialize(new { mode = Mode, results }, new JsonSerializerOptions { WriteIndented = true }));
        finished = true;
        if (!Network) Main.instance.Exit();
    }
    private void Draw(On_Main.orig_Draw orig, Main self, GameTime time)
    {
        int drawsBefore = DestroyerBossBar.DrawCount;
        orig(self, time);
        if (capture == null) return;
        // 失焦／跳帧时 Draw 可能未提交本帧界面；不读取此前已呈现的弃置缓冲。
        if (Mode != "preview" && DestroyerBossBar.DrawCount == drawsBefore) return;
        string name = capture; capture = null;
        var device = self.GraphicsDevice;
        int width = device.PresentationParameters.BackBufferWidth, height = device.PresentationParameters.BackBufferHeight;
        var pixels = new Color[width * height]; device.GetBackBufferData(pixels);
        using var texture = new Texture2D(device, width, height); texture.SetData(pixels);
        Directory.CreateDirectory(DirectoryPath);
        using (var output = File.Create(Path.Combine(DirectoryPath, name + ".png"))) texture.SaveAsPng(output, width, height);
        File.WriteAllText(Path.Combine(DirectoryPath, name + "-pixels.json"), JsonSerializer.Serialize(new { finalRed = pixels.Count(p => p.R == 239 && p.G == 76 && p.B == 71), drawCount = DestroyerBossBar.DrawCount }, new JsonSerializerOptions { WriteIndented = true }));
        if (finished) Finish();
    }
}
public sealed class BossBarTestPlayer : ModPlayer
{
    public override bool PreItemCheck() { BossBarLiveTests.Drive(Player); return true; }
}
public sealed class BossBarTestFreeze : GlobalNPC
{
    public override bool IsLoadingEnabled(Mod mod) => BossBarLiveTests.Enabled;
    public override bool PreAI(NPC npc)
    {
        if (npc.type is not (NPCID.TheDestroyer or NPCID.TheDestroyerBody or NPCID.TheDestroyerTail or NPCID.Probe)) return true;
        if (ModContent.GetInstance<BossBarLiveTests>().Tick < 70) return true;
        npc.velocity = Vector2.Zero; npc.timeLeft = 750; npc.alpha = 0; return false;
    }
}

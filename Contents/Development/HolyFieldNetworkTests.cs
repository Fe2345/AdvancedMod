#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AdvancedMod.Basic.Element.Tml;
using AdvancedMod.Basic.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

/// <summary>隔离存档双客户端验收；共享文件仅编排阶段，结界数据必须经过真实TCP包。</summary>
public sealed class HolyFieldNetworkTests : ModSystem
{
    private string? role, run;
    private int stage = -1, ticks, observed = -1;
    private bool finished;
    private Vector2 origin;
    private Player? owner;
    private long identity;
    private string? capture;
    private int matchingTicks;
    private Vector2? camera;
    private readonly List<object> results = new();
    private string Output => Path.Combine(Main.SavePath, "HolyFieldCaptures");
    private record Expected(int Stage, string Name, bool Active, int Owner, float X, float Y, int Life, int Charge, int Revision);
    public override void Load()
    {
        role = Environment.GetEnvironmentVariable("ADVANCEDMOD_HOLY_NET_TEST");
        run = Environment.GetEnvironmentVariable("ADVANCEDMOD_HOLY_NET_RUN");
        if (role is null) return;
        On_Player.LoadPlayer += Rename;
        if (!Main.dedServ) On_Main.Draw += Draw;
    }
    public override void Unload() { On_Player.LoadPlayer -= Rename; On_Main.Draw -= Draw; }
    private PlayerFileData Rename(On_Player.orig_LoadPlayer orig, string path, bool cloud)
    { var data = orig(path, cloud); data.Player.name = "HolyNet-" + role; return data; }
    private void Check(string name, bool pass) => results.Add(new { name, status = pass ? "PASS" : "FAIL" });
    private bool Ack(string client) => File.Exists(Path.Combine(run!, client, "HolyFieldCaptures", $"ack-{stage}.txt"));
    public override void PostUpdateEverything()
    {
        if (role is null || run is null || Main.gameMenu || finished) return;
        Main.autoSave = false;
        if (Main.netMode == NetmodeID.Server) Server();
        else Client();
    }
    private void Server()
    {
        if (stage < 0)
        {
            owner = Main.player.FirstOrDefault(p => p.active);
            if (owner is null) return;
            origin = owner.Center + new Vector2(0, -200);
            var field = HolyFieldSystem.Activate(owner, origin);
            field.Charge = 7; identity = field.Identity;
            stage = 0; SendExpected("首次展开与晚加入", true); return;
        }
        ticks++;
        if (ticks % 60 == 0)
            File.WriteAllText(Path.Combine(run!, "server-state.json"), JsonSerializer.Serialize(new
            {
                stage, ticks, fields = HolyFieldSystem.Fields.Values.Select(f => new { f.Identity, owner = f.Owner.whoAmI, f.Owner.active, f.Owner.dead, x = f.Center.X, y = f.Center.Y, remaining = f.Expires - HolyFieldSystem.Tick, life = f.Life.Total, f.Charge }),
                players = Main.player.Where(p => p.active).Select(p => new { p.whoAmI, p.name, p.dead, p.statLife })
            }));
        // 阶段等待期间保活，避免客户端截图／加载耗时消耗验收窗口；到期用例单独放行。
        if (stage is 0 or 1 or 2 or 3 or 5 or 7 && HolyFieldSystem.Fields.TryGetValue(owner!.whoAmI, out var waiting)) waiting.Expires = HolyFieldSystem.Tick + 1200;
        if (!Ack("client1") || !Ack("client2")) return;
        Check($"阶段{stage}双客户端确认", true);
        stage++; ticks = 0;
        switch (stage)
        {
            case 1:
                HolyFieldSystem.Activate(owner!, origin + new Vector2(500, 0));
                Check("内部反应不更新移动目标", HolyFieldSystem.Fields[owner!.whoAmI].NextDestination == origin);
                HolyFieldSystem.Activate(owner!, origin + new Vector2(1500, 0));
                SendExpected("移动到外部反应目标", true, origin + new Vector2(1500, 0)); break;
            case 2:
                HolyFieldSystem.Activate(owner!, origin + new Vector2(6000, 0));
                Check("超距提交立即迁移", HolyFieldSystem.Fields[owner!.whoAmI].Center == origin + new Vector2(6000, 0));
                SendExpected("传送旧关新开", true); break;
            case 3:
                var f = HolyFieldSystem.Fields[owner!.whoAmI];
                HolyFieldSystem.RecordLife(f, 150); f.Charge = 12;
                Check("服务器减抗即时且仅范围内", Math.Abs(HolyFieldSystem.Reduction(owner, new NPC { Center = f.Center }) - .03f) < .0001f && HolyFieldSystem.Reduction(owner, new NPC { Center = origin }) == 0);
                SendExpected("生命与充能更新", true); break;
            case 4: HolyFieldSystem.Close(owner!); SendExpected("显式关闭", false); break;
            case 5:
                var renewed = HolyFieldSystem.Activate(owner!, origin);
                Check("重建实例身份更新", renewed.Identity != identity);
                SendExpected("重新展开", true); break;
            case 6:
                HolyFieldSystem.Fields[owner!.whoAmI].Expires = HolyFieldSystem.Tick + 60;
                SendExpected("倒计时结束关闭", false); break;
            case 7:
                HolyFieldSystem.Activate(owner!, origin);
                SendExpected("死亡前展开", true); break;
            case 8:
                owner!.dead = true;
                SendExpected("主人死亡关闭", false); break;
            default:
                Check("两个真实客户端接入", Main.player.Count(p => p.active) == 2);
                Check("权威结界已清理", HolyFieldSystem.Fields.Count == 0);
                Finish(); break;
        }
    }
    private void SendExpected(string name, bool active, Vector2? destination = null)
    {
        HolyFieldSystem.Fields.TryGetValue(owner!.whoAmI, out var field);
        var point = destination ?? field?.Center ?? origin;
        var state = new Expected(stage, name, active, owner.whoAmI, point.X, point.Y, field?.Life.Total ?? 0, field?.Charge ?? 0, field?.Revision ?? 0);
        File.WriteAllText(Path.Combine(run!, "stage.json"), JsonSerializer.Serialize(state));
    }
    private void Client()
    {
        Main.LocalPlayer.creativeGodMode = true;
        // 普通世界不保证创造模式开关持续生效，显式隔离环境伤害，死亡阶段由服务器驱动。
        Main.LocalPlayer.immune = true; Main.LocalPlayer.immuneTime = 60;
        Main.LocalPlayer.statLife = Main.LocalPlayer.statLifeMax2;
        if (++ticks % 6 != 0) return;
        string path = Path.Combine(run!, "stage.json");
        if (!File.Exists(path)) return;
        Expected? expected;
        try { expected = JsonSerializer.Deserialize<Expected>(File.ReadAllText(path)); }
        catch (IOException) { return; }
        catch (JsonException) { return; }
        if (expected is null || expected.Stage == observed) return;
        bool exists = HolyFieldVisuals.Mirrors.TryGetValue(expected.Owner, out var snapshot);
        if (ticks % 60 == 0)
        {
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, "received.json"), JsonSerializer.Serialize(new { expected, exists, snapshot, x = snapshot.Center.X, y = snapshot.Center.Y, ticks, observed, matchingTicks, Main.myPlayer }));
            File.AppendAllText(Path.Combine(Output, "history.jsonl"), JsonSerializer.Serialize(new { stage = expected.Stage, exists, x = snapshot.Center.X, y = snapshot.Center.Y, snapshot.Life, snapshot.Charge, snapshot.Revision, ticks, matchingTicks }) + Environment.NewLine);
        }
        bool match = expected.Active ? exists && Vector2.Distance(snapshot.Center, new(expected.X, expected.Y)) < 1 && snapshot.Life == expected.Life && snapshot.Charge == expected.Charge && snapshot.Revision == expected.Revision : !exists;
        if (!match) { matchingTicks = 0; return; }
        if (expected.Active) camera = snapshot.Center;
        matchingTicks++;
        if (matchingTicks is 1 or 2 or 3) capture = $"stage-{expected.Stage}-frame-{matchingTicks * 6}";
        if (matchingTicks < 8) return;
        matchingTicks = 0;
        observed = expected.Stage;
        Check(expected.Name, true);
        Check($"阶段{observed}客户端无结算结界", HolyFieldSystem.Fields.Count == 0);
        if (observed == 2) Check("传送触发旧关新开", HolyFieldVisuals.Teleports == 1);
        Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, $"ack-{observed}.txt"), JsonSerializer.Serialize(snapshot));
        if (observed == 0) File.WriteAllText(Path.Combine(Output, "ready.txt"), "已收到实际网络快照");
        if (expected.Active) { Main.LocalPlayer.Center = snapshot.Center; Main.LocalPlayer.velocity = Vector2.Zero; }
        capture = $"stage-{observed}";
        if (observed == 8)
        {
            Check("开关次数一致", HolyFieldVisuals.Opens == 4 && HolyFieldVisuals.Closes == 4);
            Check("关闭尾迹释放完成", ModContent.GetInstance<WorldVfxSystem>().Manager.Count == 0);
            Check("实际提交结界绘制", HolyFieldEffect.DrawCount > 0);
            Finish();
        }
    }
    private void Finish()
    {
        finished = true; Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, "report.json"), JsonSerializer.Serialize(new { role, results }, new JsonSerializerOptions { WriteIndented = true }));
    }
    public override void ModifyScreenPosition()
    {
        if (role is not null && camera is { } center) Main.screenPosition = center - new Vector2(Main.screenWidth, Main.screenHeight) / 2;
    }
    private void Draw(On_Main.orig_Draw orig, Main self, GameTime time)
    {
        long before = HolyFieldEffect.DrawCount;
        orig(self, time);
        if (capture is null || Main.gameMenu) return;
        if (HolyFieldVisuals.Mirrors.Count > 0 && before == HolyFieldEffect.DrawCount) return;
        string name = capture; capture = null;
        var device = self.GraphicsDevice;
        int width = device.PresentationParameters.BackBufferWidth, height = device.PresentationParameters.BackBufferHeight;
        var pixels = new Color[width * height]; device.GetBackBufferData(pixels);
        using var texture = new Texture2D(device, width, height); texture.SetData(pixels);
        Directory.CreateDirectory(Output);
        using var output = File.Create(Path.Combine(Output, name + ".png")); texture.SaveAsPng(output, width, height);
    }
}

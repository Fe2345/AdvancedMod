using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AdvancedMod.Basic.Graphics;
using AdvancedMod.Basic.Graphics.Geometry;
using Visuals = AdvancedMod.Basic.Graphics.Vfx;

namespace AdvancedMod.Contents.Vfx;

/// <summary>显式触发的本地视觉演示，不创建弹幕、不造成伤害，也不保存世界数据。</summary>
public sealed class VfxExamples : ModSystem
{
    private EffectHandle ward, slash;
    private readonly List<EnvironmentHandle> skyRequests = new();
    private Vector2 slashCenter;
    private int swingTick = -1, demoTick = -1;
    private bool automaticStarted;
    private int nextCapture;
    private static readonly int[] CaptureTicks = { 6, 18, 90, 245, 365, 615, 660, 850 };
    private readonly List<object> captures = new();
    private const int SwingTicks = 24;
    public override void Load()
    {
        if (!Main.dedServ && Environment.GetEnvironmentVariable("ADVANCEDMOD_VFX_LIVE_TEST") == "1") On_Main.Draw += CaptureDraw;
    }
    public override void Unload() => On_Main.Draw -= CaptureDraw;

    private void CaptureDraw(On_Main.orig_Draw orig, Main self, GameTime gameTime)
    {
        orig(self, gameTime);
        if (!automaticStarted || demoTick < 0 || nextCapture >= CaptureTicks.Length || demoTick < CaptureTicks[nextCapture]) return;
        int requestedTick = CaptureTicks[nextCapture++];
        string directory = Path.Combine(Main.SavePath, "VfxCaptures");
        Directory.CreateDirectory(directory);
        try
        {
            // 仅自动验收模式读回完整后缓冲，正常游玩不注册这个钩子。
            var device = self.GraphicsDevice;
            int width = device.PresentationParameters.BackBufferWidth, height = device.PresentationParameters.BackBufferHeight;
            var pixels = new Color[width * height];
            device.GetBackBufferData(pixels);
            using var texture = new Texture2D(device, width, height);
            texture.SetData(pixels);
            string name = $"frame-{requestedTick:D4}.png";
            using (var output = File.Create(Path.Combine(directory, name))) texture.SaveAsPng(output, width, height);
            var manager = ModContent.GetInstance<WorldVfxSystem>().Manager;
            captures.Add(new { requestedTick, actualTick = demoTick, name, width, height, instances = manager.Count,
                skyRequests = manager.Environment.RequestCount, skyIntensity = manager.Environment.Get(GoldenSky.Key).Intensity, status = "CAPTURED" });
            Mod.Logger.Info($"VFX 截图已保存：{name}，演示刻 {demoTick}");
        }
        catch (Exception e)
        {
            captures.Add(new { requestedTick, status = "BLOCKED", error = e.ToString() });
            Mod.Logger.Warn("VFX 后缓冲截图失败，需手工验收。", e);
        }
        File.WriteAllText(Path.Combine(directory, "capture-report.json"), JsonSerializer.Serialize(new { utc = DateTimeOffset.UtcNow, captures }, new JsonSerializerOptions { WriteIndented = true }));
    }
    public override void PostSetupContent()
    {
        if (!Main.dedServ)
        {
            var system = ModContent.GetInstance<WorldVfxSystem>();
            system.RegisterSky(GoldenSky.Key, 1.2f, layer => new GoldenSky(layer, () => system.Manager.Environment.ResetStyle(GoldenSky.Key)));
        }
    }
    public void Ward(float delay = 0)
    {
        Visuals.Stop(ward);
        ward = Visuals.Create(new GoldenWard(new(Main.LocalPlayer.Center, 190)), delay);
    }
    public void StopWard() { Visuals.Stop(ward); }
    public void KillWard() { Visuals.Kill(ward); ward = default; }
    public void MoveWard() => Visuals.SetTarget(ward, new WardVisualState(Main.MouseWorld, 240));
    public void Sky() => skyRequests.Add(Visuals.AcquireEnvironment(GoldenSky.Key, new(0.85f)));
    public void ReleaseSky()
    {
        if (skyRequests.Count == 0) return;
        Visuals.ReleaseEnvironment(skyRequests[^1]); skyRequests.RemoveAt(skyRequests.Count - 1);
    }
    public void Slash()
    {
        Visuals.Stop(slash);
        slash = Visuals.Create(new SlashTrailEffect());
        slashCenter = Main.LocalPlayer.Center; swingTick = 0;
    }
    public void Demo() { demoTick = 0; Ward(); Sky(); Slash(); }
    public void StopAll()
    {
        Visuals.Stop(ward); Visuals.Stop(slash); swingTick = demoTick = -1;
        while (skyRequests.Count > 0) ReleaseSky();
    }
    public override void PostUpdatePlayers()
    {
        if (Main.dedServ || Main.gameMenu || Main.gamePaused) return;
        if (!automaticStarted && Environment.GetEnvironmentVariable("ADVANCEDMOD_VFX_LIVE_TEST") == "1")
        {
            automaticStarted = true; Main.autoSave = false; Demo();
            Mod.Logger.Info($"VFX 演示开始：tML={Main.versionNumber}，Luminance={ModLoader.GetMod("Luminance").Version}");
        }
        if (swingTick >= 0)
        {
            // 已知挥动曲线的独立播放示例；真实武器接入时替换为实际刀刃双端快照。
            const int Subsamples = 3;
            for (int i = 0; i < Subsamples; i++)
            {
                float progress = (swingTick + (float)i / Subsamples) / SwingTicks;
                Vector2 direction = Paths.Direction(MathHelper.Lerp(-2.5f, 0.8f, progress));
                double sampleTime = Visuals.Time + (double)i / Subsamples * WorldVfxSystem.TickSeconds;
                Visuals.AppendSample(slash, new SlashPoseSample(slashCenter + direction * 32, slashCenter + direction * 145, sampleTime));
            }
            if (++swingTick >= SwingTicks) { Visuals.Stop(slash); swingTick = -1; }
        }
        if (demoTick < 0) return;
        demoTick++;
        if (demoTick % 120 == 0 && demoTick < 600) Slash();
        if (demoTick == 120) Sky();
        if (demoTick == 240) ReleaseSky();
        if (demoTick == 360) { StopWard(); Ward(); }
        if (demoTick == 600) { StopWard(); ReleaseSky(); }
        if (demoTick == 630) Sky();
        if (demoTick == 780) ReleaseSky();
        if (demoTick >= 900)
        {
            demoTick = -1;
            Mod.Logger.Info("VFX 演示时序结束；视觉结果需检查截图，不自动判定通过。");
            if (automaticStarted) Main.instance.Exit();
        }
    }
    public override void ClearWorld() => Reset();
    public override void OnWorldUnload() => Reset();
    private void Reset() { ward = slash = default; skyRequests.Clear(); swingTick = demoTick = -1; automaticStarted = false; nextCapture = 0; captures.Clear(); }
}

public sealed class VfxExampleCommand : ModCommand
{
    public override string Command => "vfx";
    public override CommandType Type => CommandType.Chat;
    public override string Usage => "/vfx demo|ward|delay|move|stop|kill|slash|sky|release|clear";
    public override string Description => "本地视觉框架验收，不产生伤害或新增弹幕";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (Main.netMode == NetmodeID.Server) return;
        var examples = ModContent.GetInstance<VfxExamples>();
        switch (args.Length == 0 ? "demo" : args[0].ToLowerInvariant())
        {
            case "demo": examples.Demo(); break;
            case "ward": examples.Ward(); break;
            case "delay": examples.Ward(2); break;
            case "move": examples.MoveWard(); break;
            case "stop": examples.StopWard(); break;
            case "kill": examples.KillWard(); break;
            case "slash": examples.Slash(); break;
            case "sky": examples.Sky(); break;
            case "release": examples.ReleaseSky(); break;
            case "clear": examples.StopAll(); break;
            default: caller.Reply(Usage); return;
        }
        caller.Reply("已提交本地视觉演示指令。");
    }
}

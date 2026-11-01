using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Graphics.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace AdvancedMod.Contents.Development;

// 仅隔离客户端启用；数值／布局断言和真实输入验收分开记录。
public sealed class LunarLiveTests : ModSystem
{
    private static bool Enabled => Environment.GetEnvironmentVariable("ADVANCEDMOD_LUNAR_TEST") == "1";
    private readonly List<object> results = new();
    private int ticks;
    private bool ran, captured;
    private string Output => Path.Combine(Main.SavePath, "LunarCaptures");
    public override void Load() { if (Enabled && !Main.dedServ) On_Main.Draw += Draw; }
    public override void Unload() => On_Main.Draw -= Draw;
    private void Check(string name, bool ok, object detail = null) => results.Add(new { name, status = ok ? "PASS" : "FAIL", detail });
    public override void PostUpdateEverything()
    {
        if (!Enabled || Main.gameMenu) return;
        Main.autoSave = false;
        Main.LocalPlayer.statLife = Main.LocalPlayer.statLifeMax2;
        Main.LocalPlayer.immune = true; Main.LocalPlayer.immuneTime = 60;
        ticks++;
        if (!ran && ticks > 60)
        {
            ran = true;
            Directory.CreateDirectory(Output);
            try { Run(); } catch (Exception ex) { Check("运行异常", false, ex.ToString()); }
        }
        if (ran && ticks % 30 == 0)
        {
            var ui = ModContent.GetInstance<LunarEnergyUI>();
            var icon = ui.State.Icon.GetDimensions();
            File.WriteAllText(Path.Combine(Output, "live-state.json"), JsonSerializer.Serialize(new {
                rate = Main.LocalPlayer.GetModPlayer<PlayerSpecialState>().LunarRate,
                visible = LunarEnergyUI.Visible, panelOpen = ui.State.PanelOpen, inventory = Main.playerInventory,
                selected = Main.LocalPlayer.selectedItem, heldItem = Main.mouseItem.type,
                mouseInterface = Main.LocalPlayer.mouseInterface, map = Main.mapStyle, fullscreen = Main.mapFullscreen,
                uiScale = Main.UIScale, mapScale = Main.MapScale,
                icon = new { icon.X, icon.Y, icon.Width, icon.Height }
            }, new JsonSerializerOptions { WriteIndented = true }));
            if (File.Exists(Path.Combine(Output, "exit"))) Main.instance.Exit();
        }
    }

    private void Run()
    {
        var energy = Main.LocalPlayer.GetModPlayer<PlayerSpecialState>();
        var state = ModContent.GetInstance<LunarEnergyUI>().State;
        Main.hideUI = false; Main.mapStyle = 1; Main.mapFullscreen = false; Main.playerInventory = false;
        foreach (float input in new[] { -1f, 0f, 1f, 180f, 360f, 540f, 720f, 721f, float.MaxValue, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            energy.LunarRate = input;
            float expected = float.IsNaN(input) ? 0f : Math.Clamp(input, 0f, 720f);
            Check($"能量写入边界 {input}", energy.LunarRate == expected);
            Check($"零值隐藏／正值显现 {input}", LunarEnergyUI.Visible == (expected > 0f));
        }
        foreach (float fraction in new[] { .01f, .1f, .25f, .5f, .75f, 1f })
        {
            int total = 0, lit = 0;
            Vector2 centroid = Vector2.Zero;
            for (int y = -200; y <= 200; y++)
            for (int x = -200; x <= 200; x++)
            {
                var p = new Vector2(x / 200f, y / 200f);
                if (p.LengthSquared() > 1f) continue;
                total++;
                if (LunarEnergyUI.MoonIcon.IsLit(p, fraction)) { lit++; centroid += p; }
            }
            Check($"月相面积 {fraction:P0}", Math.Abs((float)lit / total - fraction) < .003f, new { fraction, area = (float)lit / total });
            if (fraction < 1f) Check($"从左下起盈 {fraction:P0}", centroid.X < 0 && centroid.Y > 0);
        }
        energy.LunarRate = 180f;
        state.Layout();
        state.Icon.LeftClick(new UIMouseEvent(state.Icon, state.Icon.GetDimensions().Center()));
        Check("正式图标点击事件打开", state.PanelOpen);
        Main.playerInventory = true; state.Update(new GameTime());
        Check("背包打开不关闭面板", state.PanelOpen);
        Main.playerInventory = false; Main.mapStyle = 2; state.Update(new GameTime());
        Check("背包关闭／地图切换不关闭面板", state.PanelOpen);
        energy.LunarRate = 0;
        Check("归零暂隐藏且保留开关", !LunarEnergyUI.Visible && state.PanelOpen);
        energy.LunarRate = 180;
        state.Icon.LeftClick(new UIMouseEvent(state.Icon, state.Icon.GetDimensions().Center()));
        Check("正式图标点击事件关闭", !state.PanelOpen);
        foreach (float scale in new[] { .5f, .75f, 1f })
        {
            Main.MapScale = scale; state.Layout();
            float safeLeft = Terraria.GameInput.PlayerInput.RealScreenWidth / Main.UIScale - LunarEnergyUI.MapRight - (LunarEnergyUI.MapSize + LunarEnergyUI.MapFrameClearance) * scale;
            Check($"小地图缩放{scale}左侧间距", state.Icon.GetDimensions().X + state.Icon.GetDimensions().Width <= safeLeft - LunarEnergyUI.Gap + .01f);
        }
        Main.MapScale = 1; Main.mapStyle = 1; state.Layout();
        Main.LocalPlayer.inventory[0].SetDefaults(Terraria.ID.ItemID.CopperShortsword);
        Main.LocalPlayer.inventory[1].SetDefaults(Terraria.ID.ItemID.DirtBlock);
        Main.LocalPlayer.inventory[1].stack = 10;
        Main.LocalPlayer.selectedItem = 0;
    }

    private void Draw(On_Main.orig_Draw orig, Main self, GameTime time)
    {
        orig(self, time);
        if (!Enabled || !ran || Main.gameMenu || ticks < 100 || captured) return;
        captured = true;
        var device = self.GraphicsDevice;
        var pixels = new Color[Main.screenWidth * Main.screenHeight];
        device.GetBackBufferData(pixels);
        using (var screenshot = new Texture2D(device, Main.screenWidth, Main.screenHeight))
        {
            screenshot.SetData(pixels);
            using var file = File.Create(Path.Combine(Output, "in-game.png"));
            screenshot.SaveAsPng(file, screenshot.Width, screenshot.Height);
        }
        Check("真实绘制读取180／720", ModContent.GetInstance<LunarEnergyUI>().State.Icon.DisplayedFraction == .25f);
        var data = new Color[LunarEnergyUI.MoonIcon.TextureSize * LunarEnergyUI.MoonIcon.TextureSize];
        foreach (int rate in new[] { 0, 1, 72, 180, 360, 540, 720 })
        {
            LunarEnergyUI.MoonIcon.RenderPixels(data, rate / 720f);
            using var texture = new Texture2D(device, LunarEnergyUI.MoonIcon.TextureSize, LunarEnergyUI.MoonIcon.TextureSize);
            texture.SetData(data);
            using var file = File.Create(Path.Combine(Output, $"moon-{rate}.png"));
            texture.SaveAsPng(file, texture.Width, texture.Height);
        }
        File.WriteAllText(Path.Combine(Output, "report.json"), JsonSerializer.Serialize(new { results }, new JsonSerializerOptions { WriteIndented = true }));
    }
}

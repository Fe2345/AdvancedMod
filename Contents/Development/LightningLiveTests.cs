using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AdvancedMod.Basic.Graphics;
using AdvancedMod.Basic.Element.Tml;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Basic.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AdvancedMod.Contents.Development;

/// <summary>隔离客户端中的两点闪电样例与截图；正常游戏不启用。</summary>
public sealed class LightningLiveTests : ModSystem
{
    private static bool Enabled => Environment.GetEnvironmentVariable("ADVANCEDMOD_LIGHTNING_TEST") == "1";
    private readonly List<object> results = new();
    private int ticks;
    private string capture;
    private readonly List<NPC> targets = new();
    public override void Load() { if (Enabled && !Main.dedServ) On_Main.Draw += Draw; }
    public override void Unload() => On_Main.Draw -= Draw;
    public override void PostUpdateEverything()
    {
        if (!Enabled || Main.gameMenu || Main.netMode != NetmodeID.SinglePlayer) return;
        Main.autoSave = false; Main.playerInventory = false; Main.mapStyle = 0; Main.hideUI = true;
        Main.dayTime = false; Main.time = 18000; Main.raining = false;
        Main.LocalPlayer.immune = true; Main.LocalPlayer.immuneTime = 600;
        if (++ticks == 60)
        {
            Vector2 origin = Main.screenPosition + new Vector2(Main.screenWidth * 0.5f - 200, 120);
            var color = new Color(185, 110, 255);
            global::AdvancedMod.Basic.Graphics.Vfx.Create(new LightningEffect(origin, origin + new Vector2(40, 0), color, 11));
            global::AdvancedMod.Basic.Graphics.Vfx.Create(new LightningEffect(origin + new Vector2(0, 70), origin + new Vector2(80, 70), color, 12));
            global::AdvancedMod.Basic.Graphics.Vfx.Create(new LightningEffect(origin + new Vector2(0, 140), origin + new Vector2(240, 140), color, 13));
            global::AdvancedMod.Basic.Graphics.Vfx.Create(new LightningEffect(origin + new Vector2(0, 210), origin + new Vector2(180, 270), color, 14));
            global::AdvancedMod.Basic.Graphics.Vfx.Create(new LightningEffect(origin + new Vector2(320, 0), origin + new Vector2(320, 240), Color.White, 15));
            results.Add(new { name = "独立五种方向长度样例创建", status = ModContent.GetInstance<WorldVfxSystem>().Manager.Count == 5 ? "PASS" : "FAIL" });
        }
        if (ticks == 64) capture = "standalone-peak";
        if (ticks == 70) capture = "standalone-fade";
        if (ticks == 82) capture = "standalone-ended";
        if (ticks == 90)
        {
            results.Add(new { name = "实际客户端寿命结束清理", status = ModContent.GetInstance<WorldVfxSystem>().Manager.Count == 0 ? "PASS" : "FAIL" });
            try { Chain(); }
            catch (Exception error) { results.Add(new { name = "导电真实链伤", status = "FAIL", detail = error.ToString() }); }
        }
        if (ticks == 94) capture = "reaction-chain-peak";
        if (ticks == 100) capture = "reaction-chain-fade";
        if (ticks == 115)
        {
            results.Add(new { name = "导电链段视觉到期清理", status = ModContent.GetInstance<WorldVfxSystem>().Manager.Count == 0 ? "PASS" : "FAIL" });
            foreach (var npc in targets) npc.active = false;
            // 复用现有完整单人验收，保留独立报告，不将视觉创建等同伤害回归通过。
            ElementLiveTests.Run();
        }
        if (ticks == 125)
        {
            string dir = Path.Combine(Main.SavePath, "LightningCaptures"); Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "report.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            Main.instance.Exit();
        }
    }
    private void Chain()
    {
        Vector2 start = Main.screenPosition + new Vector2(220, 170);
        int[] defenses = { 0, 20, 40, 1000, 0 };
        int[] expectedDamage = { 150, 95, 53, 0, 0 };
        for (int i = 0; i < defenses.Length; i++)
        {
            int id = NPC.NewNPC(new EntitySource_Misc("闪电链段实机验收"), (int)start.X, (int)start.Y, ModContent.NPCType<ElementProbeNPC>());
            var npc = Main.npc[id]; npc.Center = start + new Vector2(i * 64, 0); npc.defense = defenses[i];
            targets.Add(npc);
        }
        targets[0].GetGlobalNPC<CombatGlobalNPC>().Elements.Attach(new(3, 1.5, 100));
        int shot = ElementAttacks.NewProjectile(new EntitySource_Misc("闪电链段实机验收"), targets[0].Center, Vector2.Zero,
            ModContent.ProjectileType<ElementProbeProjectile>(), 100, 0,
            new TagSet(new[] { new TagEntry(false, TagEnum.Hydro) }), 1, Main.myPlayer);
        try { Main.projectile[shot].CritChance = 0; Main.projectile[shot].Damage(); }
        finally { Main.projectile[shot].active = false; }
        bool damage = true, noAttachment = true;
        for (int i = 0; i < targets.Count; i++)
        {
            damage &= targets[i].lifeMax - targets[i].life == expectedDamage[i];
            if (i > 0) noAttachment &= !targets[i].GetGlobalNPC<CombatGlobalNPC>().Elements.HasState;
        }
        results.Add(new { name = "真实导电150→95→53并在高防御目标前终止", status = damage ? "PASS" : "FAIL" });
        results.Add(new { name = "派生电弧不附着", status = noAttachment ? "PASS" : "FAIL" });
        results.Add(new { name = "仅两个有效链段创建闪电", status = ModContent.GetInstance<WorldVfxSystem>().Manager.Count == 2 ? "PASS" : "FAIL" });
    }
    private void Draw(On_Main.orig_Draw orig, Main self, GameTime time)
    {
        orig(self, time);
        if (capture is null) return;
        string name = capture; capture = null;
        var device = self.GraphicsDevice;
        int width = device.PresentationParameters.BackBufferWidth, height = device.PresentationParameters.BackBufferHeight;
        var pixels = new Color[width * height]; device.GetBackBufferData(pixels);
        using var texture = new Texture2D(device, width, height); texture.SetData(pixels);
        string dir = Path.Combine(Main.SavePath, "LightningCaptures"); Directory.CreateDirectory(dir);
        using var output = File.Create(Path.Combine(dir, name + ".png")); texture.SaveAsPng(output, width, height);
        results.Add(new { name, status = "PASS", detail = "客户端回读截图成功；观感需人工检查" });
    }
}



// 2026-10-26：隔离客户端验证元素飘字、原生命中接入和截图；仅环境变量启用。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AdvancedMod.Basic.Element.Presentation;
using AdvancedMod.Basic.Element.Tml;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Basic.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

public sealed class ElementTextLiveTests : ModSystem
{
    private static bool Enabled => Environment.GetEnvironmentVariable("ADVANCEDMOD_ELEMENT_TEXT_TEST") == "1";
    private readonly List<object> results = new();
    private int ticks;
    private bool capture;
    private string captureName = "palette";
    public override void Load() { if (Enabled && !Main.dedServ) On_Main.Draw += Draw; }
    public override void Unload() => On_Main.Draw -= Draw;
    private static void Check(bool value) { if (!value) throw new Exception("断言失败"); }
    private void Test(string name, Action test)
    {
        CombatText.clearAll();
        HolyFieldSystem.Clear();
        try { test(); results.Add(new { name, status = "PASS" }); }
        catch (Exception e) { results.Add(new { name, status = "FAIL", error = e.ToString() }); }
    }
    public override void PostUpdateEverything()
    {
        if (!Enabled || Main.gameMenu || Main.netMode != NetmodeID.SinglePlayer) return;
        Main.autoSave = false;
        Main.playerInventory = false; Main.mapStyle = 0; Main.hideUI = false;
        Main.LocalPlayer.immune = true; Main.LocalPlayer.immuneTime = 600;
        if (++ticks == 30)
        {
            Test("中文本地化", () => Check(Language.GetTextValue(ElementTextStyle.ReactionKey(TagEnum.HydroAnemo)) == "汽化"));
            Test("真实火元素命中只有一份深红数字", () => Hit(false, false));
            Test("真实融化命中具有深红数字和名称", () => Hit(true, false));
            Test("真实神圣融化仅反应名前有图标", () => Hit(true, false, true));
            Test("仅神圣特性无反应不加图标", () => Hit(false, false, true));
            Test("图标贴图为透明64像素且无彩色光晕", () =>
            {
                var icon = ModContent.Request<Texture2D>(ElementTextSystem.HolyIconAsset).Value;
                Check(icon.Width == 64 && icon.Height == 64);
                var pixels = new Color[icon.Width * icon.Height]; icon.GetData(pixels);
                Check(pixels.Any(p => p.A == 0) && pixels.Any(p => p.A == 255));
                Check(pixels.All(p => p.R == p.G && p.G == p.B));
            });
            Test("无敌目标不生成反应名", () => Hit(true, true));
            Test("静置融化采用火色", Idle);
            Test("玩家受伤原生数字着色", PlayerHit);
            Test("暴击数字保留原版强调", () =>
            {
                ElementTextSystem.Show(Main.LocalPlayer.Hitbox, 200, true, TagEnum.Electro, Array.Empty<TagEnum>());
                Check(Main.combatText.Single(t => t.active).crit);
            });
            Test("固定倾角与150刻寿命", () =>
            {
                ElementTextSystem.Show(Main.LocalPlayer.Hitbox, 0, false, TagEnum.Hydro, new[] { TagEnum.HydroAnemo }, true);
                var text = Main.combatText.Single(t => t.active);
                float angle = text.rotation;
                Check(Math.Abs(angle) <= MathHelper.ToRadians(10) && text.lifeTime == 150);
                for (int i = 0; i < 149; i++) text.Update();
                Check(text.active && text.lifeTime == 1 && text.rotation == angle);
                text.Update(); Check(!text.active && !ElementTextSystem.HasHolyIcon(text));
            });
            Test("同位置跨反应抑制与清池恢复", () =>
            {
                var bounds = Main.LocalPlayer.Hitbox;
                ElementTextSystem.Show(bounds, 0, false, TagEnum.Hydro, new[] { TagEnum.HydroAnemo });
                ElementTextSystem.Show(bounds, 0, false, TagEnum.Pyro, new[] { TagEnum.PyroCryo });
                Check(Main.combatText.Count(t => t.active) == 1);
                CombatText.clearAll();
                ElementTextSystem.Show(bounds, 0, false, TagEnum.Pyro, new[] { TagEnum.PyroCryo });
                Check(Main.combatText.Single(t => t.active).text == "融化");
            });
            Test("池复用不污染普通数字", () =>
            {
                ElementTextSystem.Show(Main.LocalPlayer.Hitbox, 0, false, TagEnum.Hydro, new[] { TagEnum.HydroAnemo });
                CombatText.clearAll();
                int id = CombatText.NewText(Main.LocalPlayer.Hitbox, Color.White, 999);
                var text = Main.combatText[id]; text.Update();
                Check(text.lifeTime == 59 && text.text == "999" && !ElementTextSystem.HasHolyIcon(text));
            });
            CombatText.clearAll();
            for (int i = 0; i < 6; i++)
            {
                var bounds = new Rectangle((int)Main.screenPosition.X + Main.screenWidth / 2 - 60,
                    (int)Main.screenPosition.Y + 120 + i * 65, 28, 40);
                var reaction = i switch { 0 or 3 => TagEnum.PyroCryo, 1 or 5 => TagEnum.HydroAnemo, _ => TagEnum.HydroElectro };
                ElementTextSystem.Show(bounds, 123 + i, false, (TagEnum)i, new[] { reaction }, i % 2 == 0);
            }
        }
        if (ticks == 38) capture = true;
        if (ticks == 48) { Main.UIScale = 1.25f; captureName = "ui-125"; }
        if (ticks == 56) capture = true;
        if (ticks == 64) { Main.GameZoomTarget = 1.5f; captureName = "zoom-150"; }
        if (ticks == 76) capture = true;
        if (ticks == 86)
        {
            string dir = Path.Combine(Main.SavePath, "ElementTextCaptures"); Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "report.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
            Main.instance.Exit();
        }
    }
    private static void Hit(bool reaction, bool immune, bool holy = false)
    {
        Vector2 position = Main.LocalPlayer.Center + new Vector2(300, -300);
        int id = NPC.NewNPC(new EntitySource_Misc("元素文字验收"), (int)position.X, (int)position.Y, ModContent.NPCType<ElementProbeNPC>());
        var npc = Main.npc[id]; npc.dontTakeDamage = immune;
        if (reaction) npc.GetGlobalNPC<CombatGlobalNPC>().Elements.Attach(new(4, 1.5, 100));
        int projectileId = ElementAttacks.NewProjectile(new EntitySource_Misc("元素文字验收"), npc.Center, Vector2.Zero,
            ModContent.ProjectileType<ElementProbeProjectile>(), 100, 0, new TagSet(holy ? new[] { new TagEntry(false, TagEnum.Pyro), new TagEntry(false, TagEnum.Hallow) } : new[] { new TagEntry(false, TagEnum.Pyro) }), reaction ? 1.5 : 0, Main.myPlayer);
        var projectile = Main.projectile[projectileId]; projectile.CritChance = 0;
        try
        {
            projectile.Damage();
            var texts = Main.combatText.Where(t => t.active).ToArray();
            if (immune) { Check(texts.Length == 0); return; }
            Check(texts.Length == (reaction ? 2 : 1));
            Check(texts.All(t => t.color == ElementTextSystem.ColorFor(TagEnum.Pyro)));
            Check(texts.Any(t => t.text == (reaction ? "175" : "100")));
            if (reaction) Check(texts.Any(t => t.text == "融化"));
            Check(texts.Count(ElementTextSystem.HasHolyIcon) == (reaction && holy ? 1 : 0));
        }
        finally { projectile.active = false; npc.active = false; }
    }
    private static void Idle()
    {
        Vector2 position = Main.LocalPlayer.Center + new Vector2(300, -300);
        int id = NPC.NewNPC(new EntitySource_Misc("静置文字验收"), (int)position.X, (int)position.Y, ModContent.NPCType<ElementProbeNPC>());
        var npc = Main.npc[id]; var state = npc.GetGlobalNPC<CombatGlobalNPC>().Elements;
        state.Attach(new(1, 1.5, 100)); state.Attach(new(4, 1.5, 100));
        try
        {
            ModContent.GetInstance<ElementHitSystem>().PostUpdateEverything();
            Check(Main.combatText.Any(t => t.active && t.text == "融化" && t.color == ElementTextSystem.ColorFor(TagEnum.Pyro)));
        }
        finally { npc.active = false; }
    }
    private static void PlayerHit()
    {
        var player = Main.LocalPlayer;
        int id = ElementAttacks.NewProjectile(new EntitySource_Misc("玩家文字验收"), player.Center + new Vector2(500, 0), Vector2.Zero,
            ModContent.ProjectileType<ElementProbeProjectile>(), 100, 0, new TagSet(new[] { new TagEntry(false, TagEnum.Pyro) }), 0, Main.myPlayer);
        var projectile = Main.projectile[id]; projectile.friendly = false; projectile.hostile = true;
        player.statLife = player.statLifeMax2; player.immune = false; player.immuneTime = 0;
        Array.Clear(player.hurtCooldowns); player.statDefense = default; player.endurance = 0;
        try
        {
            player.Hurt(PlayerDeathReason.ByProjectile(Main.myPlayer, id), 20, 0, dodgeable: false);
            Check(Main.combatText.Any(t => t.active && t.color == ElementTextSystem.ColorFor(TagEnum.Pyro)));
        }
        finally { projectile.active = false; player.immune = true; }
    }
    private void Draw(On_Main.orig_Draw orig, Main self, GameTime time)
    {
        orig(self, time);
        if (!capture) return;
        capture = false;
        var device = self.GraphicsDevice;
        int width = device.PresentationParameters.BackBufferWidth, height = device.PresentationParameters.BackBufferHeight;
        var pixels = new Color[width * height]; device.GetBackBufferData(pixels);
        using var texture = new Texture2D(device, width, height); texture.SetData(pixels);
        string dir = Path.Combine(Main.SavePath, "ElementTextCaptures"); Directory.CreateDirectory(dir);
        using var output = File.Create(Path.Combine(dir, captureName + ".png")); texture.SaveAsPng(output, width, height);
        File.WriteAllText(Path.Combine(dir, captureName + "-scale.json"), JsonSerializer.Serialize(new { ui = Main.UIScale, zoom = Main.GameViewMatrix.Zoom.X }));
    }
}

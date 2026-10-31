using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Contents.Hallow.Weapons;
using AdvancedMod.Contents.Hallow.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

/// <summary>正式碰撞伤害与可控边界并用；只在隔离测试进程执行。</summary>
public sealed class MetalWormLiveTests : ModSystem
{
    private bool ran, captured;
    private int ticks;
    private NPC marked;
    private readonly List<object> results = new();
    private static Player P => Main.LocalPlayer;
    private static Vector2 Origin => new(Main.spawnTileX * 16, 1000);
    private string Output => Path.Combine(Main.SavePath, "MetalWormCaptures");
    public override void Load() { if (Environment.GetEnvironmentVariable("ADVANCEDMOD_METAL_TEST") == "1") On_Main.Draw += Draw; }
    public override void Unload() => On_Main.Draw -= Draw;
    private void Check(string name, bool ok, object detail = null) => results.Add(new { name, status = ok ? "PASS" : "FAIL", detail });
    private static NPC N(Vector2 center)
    {
        int slot = NPC.NewNPC(new EntitySource_Misc("蠕虫鞭专项"), (int)center.X, (int)center.Y, ModContent.NPCType<ElementProbeNPC>());
        var n = Main.npc[slot]; n.Center = center; n.damage = 0; return n;
    }
    internal static Projectile Blade(Player owner, Vector2 position)
    {
        int index = Projectile.NewProjectile(owner.GetSource_Misc("刃杖专项"), position, Vector2.UnitX, ProjectileID.Smolstar, 100, 0, owner.whoAmI);
        return Main.projectile[index];
    }
    internal static void Hit(Projectile p, NPC target)
    {
        p.Center = target.Center; p.ai[0] = 60; p.ai[1] = target.whoAmI;
        p.localNPCImmunity[target.whoAmI] = 0; target.immune[p.owner] = 0;
        p.Damage();
    }
    public override void PostUpdateEverything()
    {
        if (Main.gameMenu || Main.netMode != NetmodeID.SinglePlayer || Environment.GetEnvironmentVariable("ADVANCEDMOD_METAL_TEST") != "1") return;
        Main.autoSave = false; P.statLife = P.statLifeMax2; P.immune = true; P.immuneTime = 60;
        if (ran)
        {
            if (++ticks >= MetalWormRules.TagTicks + 2 && captured)
            {
                Check("标记实际240刻到期", !marked.GetGlobalNPC<MetalWormMark>().HasOwner(P.whoAmI));
                Directory.CreateDirectory(Output);
                File.WriteAllText(Path.Combine(Output, "report.json"), JsonSerializer.Serialize(new { results }, new JsonSerializerOptions { WriteIndented = true }));
                Main.instance.Exit();
            }
            return;
        }
        ran = true;
        try { Run(); } catch (Exception error) { Check("运行异常", false, error.ToString()); marked ??= N(Origin); }
    }
    private void Run()
    {
        foreach (var p in Main.ActiveProjectiles) p.Kill();
        foreach (var n in Main.ActiveNPCs) n.active = false;
        P.Center = Origin; P.direction = 1;
        P.inventory[0].SetDefaults(ModContent.ItemType<MetalWormWhip>()); P.selectedItem = 0;
        Check("鞭子基础面板", P.HeldItem.damage == 75 && P.HeldItem.useAnimation == 24 && P.HeldItem.knockBack == 2 && P.HeldItem.value == Item.sellPrice(gold: 8));
        P.itemAnimation = P.itemAnimationMax = 24;
        int wi = Projectile.NewProjectile(P.GetSource_ItemUse(P.HeldItem), Origin, Vector2.UnitX * P.HeldItem.shootSpeed,
            P.HeldItem.shoot, 100, 2, P.whoAmI);
        Projectile whip = Main.projectile[wi]; whip.ai[0] = 36; whip.rotation = MathHelper.PiOver2; whip.spriteDirection = 1;
        var points = new List<Vector2>(); Projectile.FillWhipControlPoints(whip, points);
        var vanilla = new Projectile(); vanilla.SetDefaults(ProjectileID.SwordWhip); vanilla.owner = P.whoAmI; vanilla.velocity = whip.velocity;
        vanilla.ai[0] = whip.ai[0]; vanilla.rotation = whip.rotation; vanilla.spriteDirection = 1;
        P.inventory[0].SetDefaults(ItemID.SwordWhip);
        var reference = new List<Vector2>(); Projectile.FillWhipControlPoints(vanilla, reference);
        P.inventory[0].SetDefaults(ModContent.ItemType<MetalWormWhip>());
        Check("鞭子控制点长度匹配迪朗达尔", points.Count == reference.Count && points.Zip(reference).All(pair => Vector2.Distance(pair.First, pair.Second) < .01f));
        whip.Damage(); Check("挥空不生成探测怪", !Main.projectile.Any(p => p.active && p.ModProjectile is MetalWormProbe));
        NPC a = N(points[^1]), b = N(points[^3]); whip.Damage();
        Check("真实挥鞭命中两目标", a.life < a.lifeMax && b.life < b.lifeMax);
        Check("一鞭多目标仅一只探测怪", Main.projectile.Count(p => p.active && p.ModProjectile is MetalWormProbe) == 1);
        Check("鞭打雷附着与标记", a.GetGlobalNPC<MetalWormMark>().HasOwner(P.whoAmI) && a.GetGlobalNPC<CombatGlobalNPC>().Elements.HasState);
        Projectile emitter = Main.projectile.First(p => p.active && p.ModProjectile is MetalWormProbe);
        Check("派生伤害快照50%", emitter.damage == 50);
        Vector2 start = emitter.Center;
        for (int i = 1; i < MetalWormRules.ProbeFireTick; i++) { emitter.ModProjectile.AI(); emitter.position += emitter.velocity; }
        Check("第179刻前不发射且绕敌移动", !Main.projectile.Any(p => p.active && p.ModProjectile is MetalWormProbeShot) && Vector2.Distance(start, emitter.Center) > 30);
        emitter.ModProjectile.AI();
        Check("探测怪发射一次", Main.projectile.Count(p => p.active && p.ModProjectile is MetalWormProbeShot) == 1);
        for (int i = 0; i < 10; i++) emitter.ModProjectile.AI();
        Check("停留期间不重复发射", Main.projectile.Count(p => p.active && p.ModProjectile is MetalWormProbeShot) == 1);
        for (int i = (int)emitter.ai[2]; i < MetalWormRules.ProbeLifetime - 1; i++) emitter.ModProjectile.AI();
        Check("第239刻探测怪仍存在", emitter.active);
        emitter.ModProjectile.AI();
        Check("第240刻探测怪消失", !emitter.active);
        whip.Kill(); a.active = b.active = false;
        foreach (var p in Main.ActiveProjectiles) p.Kill();
        for (int element = 1; element <= 5; element++)
        {
            int index = Projectile.NewProjectile(P.GetSource_Misc("五元素"), Origin, Vector2.UnitX,
                ModContent.ProjectileType<MetalWormProbeShot>(), 50, 0, P.whoAmI, element);
            var p = Main.projectile[index]; var data = p.GetGlobalProjectile<CombatGlobalProjectile>();
            Check("派生零附着神圣标签-" + element, data.ElementAmount == 0 && data.Profile.Tags.Contains(TagEnum.Hallow) &&
                data.Profile.Tags.Contains(DestroyerPhaseOneRules.Tag((ProbeElement)element)) && !ProjectileID.Sets.MinionShot[p.type]);
            p.Kill();
        }
        NPC target = N(Origin + new Vector2(300, 0));
        Projectile blade = Blade(P, target.Center);
        var water = blade.GetGlobalProjectile<BladeStaffWater>();
        for (int i = 0; i < 5; i++) Hit(blade, target);
        Check("原版刃杖五次真实命中耗尽", water.Remaining == 0 && target.life < target.lifeMax, water.Remaining);
        int life = target.life; Hit(blade, target);
        Check("耗尽返程无伤害", target.life == life);
        blade.Center = P.Center; P.smolstar = true; water.PreAI(blade);
        Check("接近玩家瞬间补满且恢复原AI", water.Remaining == 10 && water.PreAI(blade));
        target.GetGlobalNPC<MetalWormMark>().Mark(target, P.whoAmI);
        for (int i = 0; i < 9; i++) Hit(blade, target);
        Check("同owner九次命中仍有储量", water.Remaining == 1, water.Remaining);
        Hit(blade, target); Check("同owner第十次命中耗尽", water.Returning);
        blade.Center = P.Center; water.PreAI(blade);
        var other = N(Origin + new Vector2(500, 0)); other.GetGlobalNPC<MetalWormMark>().Mark(other, (P.whoAmI + 1) % Main.maxPlayers);
        Hit(blade, other); Check("其他owner标记不减耗", water.Remaining == 8);
        var modifiers = other.GetIncomingStrikeModifiers(DamageClass.Summon, 0);
        other.GetGlobalNPC<MetalWormMark>().ModifyHitByProjectile(other, blade, ref modifiers);
        Check("其他owner标记不加伤", modifiers.FlatBonusDamage.Value == 0);
        modifiers = target.GetIncomingStrikeModifiers(DamageClass.Summon, 0);
        target.GetGlobalNPC<MetalWormMark>().ModifyHitByProjectile(target, blade, ref modifiers);
        Check("同owner标记遵循原版倍率", modifiers.FlatBonusDamage.Value == (int)(12 * ProjectileID.Sets.SummonTagDamageMultiplier[blade.type]));
        blade.Kill(); target.active = other.active = false;
        var boss = new NPC(); boss.SetDefaults(NPCID.TheDestroyer);
        int loot = ModContent.ItemType<MetalWormWhip>(); int before = Main.item.Count(i => i.active && i.type == loot);
        var drop = boss.GetGlobalNPC<MetalWormLoot>(); drop.OnKill(boss);
        Check("普通毁灭者不掉新鞭", Main.item.Count(i => i.active && i.type == loot) == before);
        boss.GetGlobalNPC<DestroyerBehavior>().SetHallow(boss, true); boss.Center = Origin;
        drop.OnKill(boss); Check("神圣毁灭者额外掉落", Main.item.Count(i => i.active && i.type == loot) == before + 1);
        marked = N(Origin + new Vector2(600, 0)); marked.GetGlobalNPC<MetalWormMark>().Mark(marked, P.whoAmI);
    }
    private void Draw(On_Main.orig_Draw orig, Main self, GameTime time)
    {
        orig(self, time);
        if (!ran || captured || Main.gameMenu) return;
        captured = true; Directory.CreateDirectory(Output);
        var device = self.GraphicsDevice; var old = device.GetRenderTargets(); var viewport = device.Viewport;
        using var target = new RenderTarget2D(device, 900, 400); using var batch = new SpriteBatch(device);
        try
        {
            device.SetRenderTarget(target); device.Clear(new Color(28, 34, 44));
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            var texture = ModContent.Request<Texture2D>(MetalWormRules.TexturePath).Value;
            Check("素材裁切在实际图内", MetalWormRules.HeadFrame.Right <= texture.Width && MetalWormRules.TailFrame.Bottom <= texture.Height);
            for (int i = 0; i < 16; i++)
            {
                float t = i / 15f; var position = new Vector2(110 + t * 650, 210 - MathF.Sin(t * MathHelper.Pi) * 70);
                var frame = i == 15 ? MetalWormRules.HeadFrame : i == 0 ? MetalWormRules.TailFrame : MetalWormRules.BodyFrame;
                batch.Draw(texture, position, frame, Color.White, MathHelper.PiOver2, frame.Size() * .5f,
                    new Vector2(30f / frame.Width, 48f / frame.Height), SpriteEffects.None, 0);
            }
            Utils.DrawBorderString(batch, "金属蠕虫鞭：30像素宽素材预览（非实际挥鞭轨迹）", new Vector2(70, 320), Color.White);
            batch.End();
        }
        finally { device.SetRenderTargets(old); device.Viewport = viewport; }
        using var output = File.Create(Path.Combine(Output, "whip-sprites.png")); target.SaveAsPng(output, target.Width, target.Height);
    }
}

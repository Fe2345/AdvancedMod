using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AdvancedMod.Basic.Element;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Contents.Hallow.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using S = AdvancedMod.Contents.Hallow.NPCs.DestroyerBehavior.PhaseTwoState;

namespace AdvancedMod.Contents.Development;

/// <summary>隔离客户端：实际NPC链与原生元素伤害，随后直接步进运动以覆盖多轮状态。</summary>
public sealed class DestroyerPhaseTwoLiveTests : ModSystem
{
    internal static bool Enabled => Environment.GetEnvironmentVariable("ADVANCEDMOD_DESTROYER_PHASE2_TEST") == "1";
    private readonly List<object> results = new();
    private readonly List<object> transitions = new();
    private NPC head;
    private int ticks;
    private bool ran, captured;
    private string Output => Path.Combine(Main.SavePath, "PhaseTwoCaptures");
    private static Player P => Main.LocalPlayer;
    private void Check(string name, bool pass, object detail = null) => results.Add(new { name, status = pass ? "PASS" : "FAIL", detail });
    public override void Load() { if (Enabled && !Main.dedServ) On_Main.Draw += Draw; }
    public override void Unload() => On_Main.Draw -= Draw;
    public override void PostUpdateEverything()
    {
        if (!Enabled || Main.gameMenu) return;
        ticks++;
        Main.dayTime = false; Main.time = 18000; Main.autoSave = false;
        P.dead = false; P.statLife = P.statLifeMax2 = 5000; P.velocity = Vector2.Zero; P.immune = true; P.immuneTime = 600;
        if (ticks == 10)
        {
            foreach (NPC n in Main.ActiveNPCs) n.active = false;
            P.Center = new Vector2(Main.maxTilesX * 8, (float)Main.worldSurface * 16 - 1000);
            int index = NPC.NewNPC(new EntitySource_Misc("二阶段隔离测试"), (int)P.Center.X - 1100, (int)P.Center.Y, NPCID.TheDestroyer);
            head = Main.npc[index]; head.GetGlobalNPC<DestroyerBehavior>().SetHallow(head, true);
        }
        if (ticks == 15 && !ran)
        {
            ran = true;
            try { Run(); } catch (Exception ex) { Check("运行异常", false, ex.ToString()); }
        }
        if (ticks > 30 && captured)
        {
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, "report.json"), JsonSerializer.Serialize(new { results, transitions }, new JsonSerializerOptions { WriteIndented = true }));
            Main.instance.Exit();
        }
    }
    private void Enter(DestroyerBehavior b, S state) => typeof(DestroyerBehavior).GetMethod("Enter", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(b, new object[] { head, state });
    private void Step(DestroyerBehavior b)
    {
        b.PreAI(head); head.position += head.velocity;
        foreach (NPC part in Main.npc.Where(n => n.active && n.realLife == head.whoAmI && n.whoAmI != head.whoAmI)) part.GetGlobalNPC<DestroyerBehavior>().PreAI(part);
    }
    private void Run()
    {
        int receiptPlayer = Main.maxPlayers - 1;
        DestroyerReactionCounter.ResetConnection(receiptPlayer);
        Check("反应回执允许事务完成顺序变化", DestroyerReactionCounter.AcceptReceipt(receiptPlayer, 2) && DestroyerReactionCounter.AcceptReceipt(receiptPlayer, 1));
        Check("重复反应回执不再累计", !DestroyerReactionCounter.AcceptReceipt(receiptPlayer, 2));
        DestroyerReactionCounter.ResetConnection(receiptPlayer);
        Check("重连清除旧回执身份", DestroyerReactionCounter.AcceptReceipt(receiptPlayer, 1));
        DestroyerReactionCounter.ResetConnection(receiptPlayer);
        var b = head.GetGlobalNPC<DestroyerBehavior>();
        var bar = head.GetGlobalNPC<DestroyerBarState>();
        Check("未清空探测怪保持一阶段", !b.PhaseTwo && bar.Maximum == 80);
        foreach (NPC body in Main.npc.Where(n => n.active && n.type == NPCID.TheDestroyerBody && n.realLife == head.whoAmI).ToArray())
        {
            for (int i = 0; i < 2000 && body.ai[2] == 0; i++) body.HitEffect();
            foreach (NPC probe in Main.npc.Where(n => n.active && n.type == NPCID.Probe).ToArray()) { probe.SimpleStrikeNPC(100000, 0, noPlayerInteraction: true); }
        }
        Check("原生释放击败后账本归零", bar.Remaining == 0);
        Step(b); Check("自动进入升空准备", b.PhaseState == S.Prepare);
        Check("二阶段接触有神圣资格但无元素附着", head.GetGlobalNPC<CombatGlobalNPC>().Profile.Tags.Contains(TagEnum.Hallow) && head.GetGlobalNPC<CombatGlobalNPC>().ElementAmount == 0);
        float maxDelta = 0, priorSpeed = head.velocity.Length(); int crashStarts = 0, idleEntries = 0;
        S prior = b.PhaseState;
        for (int i = 0; i < 6000 && idleEntries < 2; i++)
        {
            Step(b);
            float speed = head.velocity.Length(); maxDelta = Math.Max(maxDelta, Math.Abs(speed - priorSpeed)); priorSpeed = speed;
            if (b.PhaseState != prior)
            {
                transitions.Add(new { tick = i, from = prior.ToString(), to = b.PhaseState.ToString(), distance = head.Distance(P.Center), speed });
                if (b.PhaseState == S.Crash) { crashStarts++; Check("冲撞起点距离-" + crashStarts, head.Distance(P.Center) >= 880 && head.Distance(P.Center) <= 1225); }
                if (b.PhaseState == S.Idle) idleEntries++;
                prior = b.PhaseState;
            }
        }
        Check("自然两冲一巡航完成两轮", crashStarts == 4 && idleEntries == 2, new { crashStarts, idleEntries, b.PhaseState });
        Check("零反应不释放Ultimate", b.UltimateSerial == 0);
        Check("速度连续无瞬时跳档", maxDelta < 1, maxDelta);
        Check("二阶段不产生身体激光", !Main.projectile.Any(p => p.active && p.type == ProjectileID.DeathLaser));
        Vector2 anchor = P.Center;
        foreach (string scenario in new[] { "跑鞋匀速7", "往返7", "空中横移10" })
        {
            P.Center = anchor; head.Center = anchor - new Vector2(1100, 0); head.velocity = Vector2.UnitX * 14;
            Enter(b, S.Reposition); int completed = b.CompletedCrashes; int steps = 0;
            for (; steps < 4000 && b.CompletedCrashes - completed < 2; steps++)
            {
                P.velocity = scenario == "跑鞋匀速7" ? new Vector2(7, 0) : scenario == "往返7" ? new Vector2(steps / 180 % 2 == 0 ? 7 : -7, 0) : new Vector2(10, (float)Math.Cos(steps / 60f) * 4);
                P.position += P.velocity; Step(b);
            }
            Check("移动目标完成双冲-" + scenario, b.CompletedCrashes - completed >= 2, new { steps, state = b.PhaseState.ToString(), distance = head.Distance(P.Center) });
        }
        P.Center = anchor; P.velocity = Vector2.Zero;
        head.Center = P.Center - new Vector2(1100, 0); head.velocity = Vector2.UnitX * 14; Enter(b, S.Crash);
        Reaction(false, true); Check("普通反应不充能", b.ReactionCount == 0);
        Reaction(true, false); Check("非该Boss来源不充能", b.ReactionCount == 0);
        Reaction(true, true, stale: true); Check("失效战斗身份不充能", b.ReactionCount == 0);
        Reaction(true, true, attachment: false); Check("仅附着不充能", b.ReactionCount == 0);
        Reaction(true, true, true); Check("免疫取消命中不充能", b.ReactionCount == 0);
        for (int i = 0; i < 20; i++) Reaction(true, true);
        Check("20次真实神圣反应记满", b.ReactionCount == 20, b.ReactionCount);
        Reaction(true, true); Check("充能封顶20", b.ReactionCount == 20);
        Check("满充不打断正在冲撞", b.PhaseState == S.Crash && b.UltimateSerial == 0);
        for (int i = 0; i < 200 && !b.Hidden; i++) Step(b);
        Check("当前技能结束必放并清零", b.Hidden && b.ReactionCount == 0 && b.UltimateSerial == 1);
        int warning = 0, strike = 0; prior = S.Inactive;
        for (int i = 0; i < 250 && b.Hidden; i++)
        {
            if (b.PhaseState != prior) { if (b.PhaseState == S.UltimateWarning) warning++; else strike++; prior = b.PhaseState; }
            CheckOnceHidden(b); Step(b);
        }
        Check("Ultimate三次预警三次鞭打状态", warning == 3 && strike == 3, new { warning, strike });
        Check("Ultimate结束恢复两冲循环", b.PhaseState == S.Reposition && !b.Hidden);
        int cooldown = 0; Check("结束后恢复接触伤害", b.CanHitPlayer(head, P, ref cooldown));
        Check("流程版没有鞭打伤害弹幕", Main.projectile.Where(p => p.active && p.ModProjectile is Contents.Hallow.Projectiles.DestroyerUltimatePreview)
            .All(p => !p.hostile && p.ModProjectile.CanDamage() == false));
        Main.dayTime = true;
        foreach (NPC body in Main.npc.Where(n => n.active && n.type == NPCID.TheDestroyerBody && n.realLife == head.whoAmI)) body.localAI[0] = 100000;
        Step(b); Main.dayTime = false;
        Check("白天撤退仍不恢复身体激光", !Main.projectile.Any(p => p.active && p.type == ProjectileID.DeathLaser));
        // 画面保留7点充能，使用正式血条入口截图。
        Enter(b, S.Idle); for (int i = 0; i < 7; i++) Reaction(true, true);
        Check("后续充能重新累计", b.ReactionCount == 7);
        Check("神圣图标资源可加载", ModContent.Request<Texture2D>("AdvancedMod/Assets/UI/HallowIcon", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value.Width > 1);
        head.Center = P.Center + new Vector2(260, -100); head.velocity = Vector2.UnitX * 14;
        Main.hideUI = false; Main.mapStyle = 0; Main.playerInventory = false;
    }
    private bool hiddenChecked;
    private void CheckOnceHidden(DestroyerBehavior b)
    {
        if (hiddenChecked) return; hiddenChecked = true; Step(b); int slot = 0;
        Check("Ultimate整条链隐藏且无接触伤害", Main.npc.Where(n => n.active && n.whoAmI == head.whoAmI || n.realLife == head.whoAmI)
            .All(n => n.alpha == 255 && !n.GetGlobalNPC<DestroyerBehavior>().CanHitPlayer(n, P, ref slot) && !n.GetGlobalNPC<DestroyerBehavior>().CanHitNPC(n, head)));
    }
    private void Reaction(bool holy, bool own, bool immune = false, bool stale = false, bool attachment = true)
    {
        P.GetModPlayer<CombatPlayer>().SetNetworkState(new ElementTarget(ElementTarget.DefaultOperationLimit));
        if (attachment) P.GetModPlayer<CombatPlayer>().Elements.Attach(new(4, 1.5, 20));
        P.statLife = P.statLifeMax2 = 5000; P.dead = false; P.immune = immune; P.immuneTime = immune ? 60 : 0;
        P.hurtCooldowns[0] = P.hurtCooldowns[1] = 0;
        IEntitySource source = own ? head.GetSource_FromAI() : new EntitySource_Misc("其他来源");
        int index = Projectile.NewProjectile(source, P.Center, Vector2.Zero, ProjectileID.DeathLaser, 20, 0, Main.myPlayer);
        Projectile p = Main.projectile[index]; p.hostile = true; p.friendly = false; p.penetrate = -1;
        if (stale) p.GetGlobalProjectile<DestroyerAttackOwner>().Encounter++;
        var data = p.GetGlobalProjectile<CombatGlobalProjectile>();
        var tags = new List<TagEntry> { new(false, TagEnum.Pyro) }; if (holy) tags.Add(new(false, TagEnum.Hallow));
        data.Profile = new(new TagSet(tags), "Destroyer.PhaseTwo.Test", isManagedAttack: true); data.ElementAmount = 1.5;
        p.Damage(); p.Kill(); P.immune = true; P.immuneTime = 600;
    }
    private void Draw(On_Main.orig_Draw orig, Main self, GameTime time)
    {
        orig(self, time);
        if (!ran || captured || ticks < 24) return;
        Directory.CreateDirectory(Output); captured = true;
        var pixels = new Color[Main.screenWidth * Main.screenHeight]; self.GraphicsDevice.GetBackBufferData(pixels);
        using var texture = new Texture2D(self.GraphicsDevice, Main.screenWidth, Main.screenHeight); texture.SetData(pixels);
        using var file = File.Create(Path.Combine(Output, "phase2-world.png")); texture.SaveAsPng(file, texture.Width, texture.Height);
        CaptureBar(self.GraphicsDevice);
    }
    private void CaptureBar(GraphicsDevice device)
    {
        var previous = device.GetRenderTargets(); var viewport = device.Viewport;
        int width = Main.screenWidth, height = Main.screenHeight;
        using var target = new RenderTarget2D(device, 1000, 180); using var batch = new SpriteBatch(device);
        try
        {
            Main.screenWidth = 1000; Main.screenHeight = 180; device.SetRenderTarget(target); device.Clear(new Color(18, 22, 30));
            batch.Begin(); var icon = ModContent.Request<Texture2D>("AdvancedMod/Assets/UI/HallowIcon").Value;
            Basic.Graphics.BossBars.DestroyerBossBar.DrawBar(batch, "The Destroyer", "Phase II - Hallowed reactions", 1, 1,
                head.GetGlobalNPC<DestroyerBehavior>().ReactionCount, DestroyerPhaseTwoRules.ChargeMaximum, icon, icon.Bounds);
            batch.End();
        }
        finally { Main.screenWidth = width; Main.screenHeight = height; device.SetRenderTargets(previous); device.Viewport = viewport; }
        using var stream = File.Create(Path.Combine(Output, "phase2-bar.png")); target.SaveAsPng(stream, target.Width, target.Height);
    }
}

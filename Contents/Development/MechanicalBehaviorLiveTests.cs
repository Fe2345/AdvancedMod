using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AdvancedMod.Contents.Hallow.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace AdvancedMod.Contents.Development;

/// <summary>隔离世界中通过原生物品使用验证三王行为分流；不直接调用 UseItem 或伪造召唤结果。</summary>
public sealed class MechanicalBehaviorLiveTests : ModSystem
{
    internal static string Mode => Environment.GetEnvironmentVariable("ADVANCEDMOD_MECHANICAL_TEST");
    internal static bool Enabled => Mode is "empty" or "state";
    private static readonly int[] Items = { ItemID.MechanicalWorm, ItemID.MechanicalEye, ItemID.MechanicalSkull };
    private readonly List<object> results = new();
    private readonly List<Vector2> trajectory = new();
    private int tick, round, frame;
    private Vector2 anchor;
    private bool initialized;
    private string capture;
    private int initialStack;
    private bool biomeObserved;
    private readonly HashSet<string> states = new();
    public override void Load() { if (Enabled && !Main.dedServ) On_Main.Draw += Draw; }
    public override void Unload() => On_Main.Draw -= Draw;

    internal static void Drive(Player player)
    {
        if (!Enabled || Main.gameMenu || Main.netMode != NetmodeID.SinglePlayer || player.whoAmI != Main.myPlayer) return;
        var test = ModContent.GetInstance<MechanicalBehaviorLiveTests>();
        test.Prepare(player);
        player.Center = test.anchor;
        player.velocity = Vector2.Zero;
        player.immune = true; player.immuneTime = 600; player.statLife = player.statLifeMax2;
        player.controlUseItem = test.frame == 120;
        player.releaseUseItem = true;
        player.selectedItem = 0;
        Main.dayTime = false; Main.time = 18000;
        Main.autoSave = false; Main.playerInventory = false; Main.mapStyle = 0; Main.hideUI = true;
    }

    private void Prepare(Player player)
    {
        if (initialized) return;
        initialized = true;
        anchor = new Vector2(Main.maxTilesX / 2 * 16, (float)Main.worldSurface * 16 - 200);
        BeginRound(player);
    }

    private void BeginRound(Player player)
    {
        foreach (NPC npc in Main.ActiveNPCs) npc.active = false;
        foreach (Projectile projectile in Main.ActiveProjectiles) projectile.active = false;
        frame = 0; trajectory.Clear(); states.Clear(); biomeObserved = false;
        bool hallow = round % 2 == 0;
        int cx = (int)anchor.X / 16, cy = (int)anchor.Y / 16;
        // 真实世界方块触发生物群系扫描；不直接写 ZoneHallow。
        for (int x = cx - 85; x <= cx + 85; x++)
        for (int y = cy - 65; y <= cy + 65; y++)
        {
            if (!WorldGen.InWorld(x, y, 10)) continue;
            var tile = Main.tile[x, y];
            tile.ClearEverything();
            if (y >= cy + 12)
            {
                tile.HasTile = true;
                tile.TileType = hallow && Math.Abs(x - cx) <= 20 && y < cy + 32 ? TileID.Pearlstone : TileID.Stone;
            }
        }
        player.inventory[0] = new Item(Items[round / 2], 3);
        initialStack = player.inventory[0].stack;
        player.itemAnimation = player.itemTime = 0;
    }

    public override void PostUpdateEverything()
    {
        if (!Enabled || !initialized || Main.gameMenu || Main.netMode != NetmodeID.SinglePlayer) return;
        tick++; frame++;
        bool hallow = round % 2 == 0;
        var player = Main.LocalPlayer;
        if (frame == 120) biomeObserved = player.ZoneHallow == hallow;
        var bosses = Main.npc.Where(n => n.active && HallowMechanicalSummonItem.MaskForNPC(n.type) != 0).ToArray();
        if (frame == 350 && round < 2 && bosses.Length == 1)
        {
            // 将整条链平移到玩家附近的空气中，保持其实际形状，便于验收原版绘制。
            Vector2 offset = anchor + new Vector2(-150, -60) - bosses[0].Center;
            foreach (NPC segment in Main.ActiveNPCs)
                if (segment.type is NPCID.TheDestroyer or NPCID.TheDestroyerBody or NPCID.TheDestroyerTail) segment.position += offset;
        }
        foreach (NPC segment in Main.ActiveNPCs)
            if (segment.type is NPCID.TheDestroyer or NPCID.TheDestroyerBody or NPCID.TheDestroyerTail) Lighting.AddLight(segment.Center, 2f, 2f, 2f);
        if (frame > 122 && bosses.Length > 0)
        {
            trajectory.Add(bosses[0].Center);
            if (bosses[0].type == NPCID.TheDestroyer && hallow && Mode == "state")
                states.Add(bosses[0].GetGlobalNPC<DestroyerBehavior>().State.ToString());
        }
        if (frame == 180) capture = $"{Mode}-{round}-overview";
        if (frame == 360 && round < 2) capture = $"{Mode}-{round}-chain";
        if (frame < (round < 2 ? 480 : 210)) return;
        string prefix = $"{Items[round / 2]}-{(hallow ? "神圣" : "普通")}";
        int expectedHeads = round / 2 == 1 ? 2 : 1;
        Check(prefix + "真实生物群系", biomeObserved);
        Check(prefix + "原生召唤数量", bosses.Length == expectedHeads, bosses.Select(n => n.type).ToArray());
        Check(prefix + "消耗一次", player.inventory[0].stack == initialStack - 1, player.inventory[0].stack);
        Check(prefix + "独属组件标记", bosses.Length == expectedHeads && bosses.All(n => HallowMechanicalBehavior.TryGet(n, out var b) && b.Hallow == hallow));
        bool moved = trajectory.Count > 2 && trajectory.Any(v => Vector2.DistanceSquared(v, trajectory[0]) > 4);
        bool shouldMove = !hallow || (Mode == "state" && round / 2 == 0);
        Check(prefix + "AI运动分流", moved == shouldMove, new { moved, expected = shouldMove });
        if (round / 2 == 0)
        {
            var segments = Main.npc.Where(n => n.active && n.type is NPCID.TheDestroyerBody or NPCID.TheDestroyerTail).ToArray();
            Check(prefix + "链生成", segments.Length == (shouldMove ? NPC.GetDestroyerSegmentsCount() + 1 : 0), segments.Length);
            if (segments.Length > 0)
            {
                Check(prefix + "子节标记继承", segments.All(n => n.GetGlobalNPC<DestroyerBehavior>().Hallow == hallow));
                // 屏外且纹理尚未载入的尾部 frame.Height 可以为 0；实际绘制另由可见截图验收。
                Check(prefix + "淡入完成与有限旋转", segments.All(n => n.alpha == 0 && float.IsFinite(n.rotation)),
                    new { faded = segments.Count(n => n.alpha == 0), finite = segments.Count(n => float.IsFinite(n.rotation)), total = segments.Length });
            }
            if (Mode == "state" && hallow) Check(prefix + "状态可观察", states.Count > 0, states.ToArray());
        }
        if (++round < 6) { BeginRound(player); return; }
        if (Mode == "state")
        {
            try { CheckDestroyerMechanics(player); }
            catch (Exception error) { Check("状态机机制检查异常", false, error.ToString()); }
        }
        string dir = Path.Combine(Main.SavePath, "MechanicalCaptures"); Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"{Mode}-report.json"), JsonSerializer.Serialize(new { mode = Mode, results }, new JsonSerializerOptions { WriteIndented = true }));
        Main.instance.Exit();
    }

    private void Check(string name, bool passed, object detail = null) => results.Add(new { name, status = passed ? "PASS" : "FAIL", detail });

    private static NPC Spawn(int type, Vector2 center, bool hallow)
    {
        int index = NPC.NewNPC(new EntitySource_Misc("毁灭者机制实机检查"), (int)center.X, (int)center.Y, type);
        NPC npc = Main.npc[index]; npc.Center = center; npc.target = Main.myPlayer; npc.alpha = 0;
        if (npc.TryGetGlobalNPC<DestroyerBehavior>(out var behavior)) behavior.SetHallow(npc, hallow);
        return npc;
    }

    private void CheckDestroyerMechanics(Player player)
    {
        foreach (NPC npc in Main.ActiveNPCs) npc.active = false;
        foreach (Projectile p in Main.ActiveProjectiles) p.active = false;
        // 同一真实引擎内对运动内核作逐刻对照，不要求随机或浮点逐位一致。
        foreach (bool goodWorld in new[] { false, true })
        foreach (bool retreat in new[] { false, true })
        foreach (bool underground in new[] { false, true })
        {
            Main.getGoodWorld = goodWorld; Main.dayTime = retreat;
            Vector2 center = anchor + new Vector2(100, underground ? 400 : -150);
            NPC vanilla = Spawn(NPCID.TheDestroyer, center, false);
            NPC candidate = Spawn(NPCID.TheDestroyer, center, true);
            vanilla.ai[0] = candidate.ai[0] = 1;
            float maxPositionError = 0, maxRotationError = 0;
            for (int i = 0; i < 90; i++)
            {
                vanilla.oldVelocity = vanilla.velocity; candidate.oldVelocity = candidate.velocity;
                vanilla.VanillaAI(); candidate.GetGlobalNPC<DestroyerBehavior>().PreAI(candidate);
                vanilla.position += vanilla.velocity; candidate.position += candidate.velocity;
                maxPositionError = Math.Max(maxPositionError, Vector2.Distance(vanilla.Center, candidate.Center));
                maxRotationError = Math.Max(maxRotationError, Math.Abs(MathHelper.WrapAngle(vanilla.rotation - candidate.rotation)));
                if (!vanilla.active || !candidate.active) break;
            }
            // 当前要求是基本行为一致：90 刻内位置偏差小于半格、转角小于约 3 度。
            Check($"运动对照-FTW{goodWorld}-撤退{retreat}-地下{underground}", maxPositionError < 8f && maxRotationError < 0.05f,
                new { maxPositionError, maxRotationError });
            vanilla.active = candidate.active = false;
        }
        Main.getGoodWorld = false; Main.dayTime = false;
        NPC head = Spawn(NPCID.TheDestroyer, anchor + new Vector2(200, -80), true);
        head.ai[0] = 1;
        NPC body = Spawn(NPCID.TheDestroyerBody, anchor + new Vector2(250, -80), true);
        body.ai[1] = body.ai[3] = body.realLife = head.whoAmI;
        body.localAI[0] = 26000;
        body.GetGlobalNPC<DestroyerBehavior>().PreAI(body);
        Check("身体激光真实生成", Main.projectile.Any(p => p.active && p.type == ProjectileID.DeathLaser && p.timeLeft == 300));
        Check("前节间距约束", Math.Abs(Vector2.Distance(body.Center, head.Center) - (int)(44f * body.scale)) < 1f);
        int oldLife = head.life;
        body.SimpleStrikeNPC(100, 1, noPlayerInteraction: true);
        Check("原版共享生命伤害", head.life < oldLife && body.life == head.life);
        for (int i = 0; i < 500 && body.ai[2] == 0; i++) body.HitEffect();
        Check("原版受击释放探针", body.ai[2] == 1 && Main.npc.Any(n => n.active && n.type == NPCID.Probe));
        int oldLasers = Main.projectile.Count(p => p.active && p.type == ProjectileID.DeathLaser);
        body.localAI[0] = 26000;
        body.GetGlobalNPC<DestroyerBehavior>().PreAI(body);
        Check("释放探针后身体仍发射", Main.projectile.Count(p => p.active && p.type == ProjectileID.DeathLaser) == oldLasers + 1);
        NPC ordinary = Spawn(NPCID.BlueSlime, anchor, false);
        Check("普通NPC不挂三王组件", !HallowMechanicalBehavior.TryGet(ordinary, out _));
        body.SetDefaults(NPCID.TheDestroyerBody);
        Check("复用槽位清除Hallow", !body.GetGlobalNPC<DestroyerBehavior>().Hallow);
        foreach (NPC npc in Main.ActiveNPCs) npc.active = false;
    }
    private void Draw(On_Main.orig_Draw orig, Main self, GameTime time)
    {
        orig(self, time);
        if (capture == null) return;
        string name = capture; capture = null;
        var device = self.GraphicsDevice;
        int width = device.PresentationParameters.BackBufferWidth, height = device.PresentationParameters.BackBufferHeight;
        var pixels = new Color[width * height]; device.GetBackBufferData(pixels);
        using var texture = new Texture2D(device, width, height); texture.SetData(pixels);
        string dir = Path.Combine(Main.SavePath, "MechanicalCaptures"); Directory.CreateDirectory(dir);
        using var output = File.Create(Path.Combine(dir, name + ".png")); texture.SaveAsPng(output, width, height);
    }
}

public sealed class MechanicalBehaviorTestPlayer : ModPlayer
{
    public override bool PreItemCheck() { MechanicalBehaviorLiveTests.Drive(Player); return true; }
}



using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using AdvancedMod.Contents.Hallow.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

/// <summary>真实 TCP 召唤与晚加入客户端验收，仅在隔离进程启用。</summary>
public sealed class MechanicalBehaviorNetworkTests : ModSystem
{
    internal static string Role => Environment.GetEnvironmentVariable("ADVANCEDMOD_MECHANICAL_NET_TEST");
    internal static bool Enabled => Role is "server" or "client1" or "client2";
    private int ticks, lateTicks, consumed;
    private bool reported;
    internal static int UnmarkedTicks;
    private Vector2 Anchor => new(Main.maxTilesX / 2 * 16, (float)Main.worldSurface * 16 - 200);
    public override void Load() { if (Enabled) On_Player.LoadPlayer += Rename; }
    public override void Unload() { On_Player.LoadPlayer -= Rename; UnmarkedTicks = 0; }
    private PlayerFileData Rename(On_Player.orig_LoadPlayer orig, string path, bool cloud)
    {
        var data = orig(path, cloud); data.Player.name = "Mechanical-" + Role; return data;
    }
    public override void OnWorldLoad()
    {
        if (!Enabled || Main.netMode != NetmodeID.Server) return;
        int cx = (int)Anchor.X / 16, cy = (int)Anchor.Y / 16;
        Main.spawnTileX = cx; Main.spawnTileY = cy;
        for (int x = cx - 85; x <= cx + 85; x++)
        for (int y = cy - 65; y <= cy + 65; y++)
        {
            var tile = Main.tile[x, y]; tile.ClearEverything();
            if (y >= cy + 12)
            {
                tile.HasTile = true;
                tile.TileType = Math.Abs(x - cx) <= 20 && y < cy + 32 ? TileID.Pearlstone : TileID.Stone;
            }
        }
    }
    internal static void Drive(Player player)
    {
        if (!Enabled || Main.netMode != NetmodeID.MultiplayerClient || player.whoAmI != Main.myPlayer) return;
        var test = ModContent.GetInstance<MechanicalBehaviorNetworkTests>();
        player.Center = test.Anchor; player.velocity = Vector2.Zero;
        player.immune = true; player.immuneTime = 600; player.statLife = player.statLifeMax2;
        player.selectedItem = 0; player.releaseUseItem = true;
        player.controlUseItem = Role == "client1" && test.ticks is 130 or 260 or 390;
    }
    public override void PostUpdateEverything()
    {
        if (!Enabled || Main.gameMenu || reported) return;
        Main.autoSave = false;
        Main.dayTime = false; Main.time = 18000;
        if (Role == "server")
        {
            if (!Main.player.Any(p => p.active && p.name == "Mechanical-client2")) return;
            if (++lateTicks < 180) return;
            Report(); return;
        }
        if (Main.netMode != NetmodeID.MultiplayerClient || !Main.LocalPlayer.active) return;
        ticks++;
        if (Role == "client1")
        {
            int item = ticks switch { 60 => ItemID.MechanicalWorm, 190 => ItemID.MechanicalEye, 320 => ItemID.MechanicalSkull, _ => 0 };
            if (item != 0)
            {
                Main.LocalPlayer.inventory[0] = new Item(item, 3);
                NetMessage.SendData(MessageID.SyncEquipment, number: Main.myPlayer, number2: 0);
            }
            if (ticks is 180 or 310 or 440 && Main.LocalPlayer.inventory[0].stack == 2) consumed++;
            if (ticks >= 600) Report();
        }
        else if (ticks >= 180) Report();
    }
    private void Report()
    {
        reported = true;
        var heads = Main.npc.Where(n => n.active && HallowMechanicalSummonItem.MaskForNPC(n.type) != 0).ToArray();
        var segments = Main.npc.Where(n => n.active && n.type is NPCID.TheDestroyerBody or NPCID.TheDestroyerTail).ToArray();
        var checks = new System.Collections.Generic.Dictionary<string, bool>
        {
            ["三王四个头部均生成"] = heads.Length == 4,
            ["头部Hallow同步"] = heads.Length == 4 && heads.All(n => HallowMechanicalBehavior.TryGet(n, out var b) && b.Hallow),
            ["完整毁灭者子节同步"] = segments.Length == NPC.GetDestroyerSegmentsCount() + 1,
            ["子节Hallow同步"] = segments.Length > 0 && segments.All(n => n.GetGlobalNPC<DestroyerBehavior>().Hallow),
            ["首次AI前已有标记"] = UnmarkedTicks == 0,
            ["机械骷髅王空AI未生成手臂"] = !Main.npc.Any(n => n.active && n.type is NPCID.PrimeCannon or NPCID.PrimeSaw or NPCID.PrimeVice or NPCID.PrimeLaser)
        };
        if (Role == "client1") { checks["原生三次使用各消耗一个"] = consumed == 3; checks["真实神圣环境"] = Main.LocalPlayer.ZoneHallow; }
        string dir = Path.Combine(Main.SavePath, "MechanicalCaptures"); Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "network-report.json"), JsonSerializer.Serialize(new
        {
            role = Role, unmarkedTicks = UnmarkedTicks, consumed,
            players = Main.player.Where(p => p.active).Select(p => new { p.name, p.ZoneHallow, held = p.HeldItem.type }).ToArray(),
            results = checks.Select(c => new { name = c.Key, status = c.Value ? "PASS" : "FAIL" })
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}

public sealed class MechanicalNetworkTestPlayer : ModPlayer
{
    public override bool PreItemCheck() { MechanicalBehaviorNetworkTests.Drive(Player); return true; }
}

public sealed class MechanicalNetworkTestObserver : GlobalNPC
{
    public override bool IsLoadingEnabled(Mod mod) => MechanicalBehaviorNetworkTests.Enabled;
    public override bool PreAI(NPC npc)
    {
        if (HallowMechanicalBehavior.TryGet(npc, out var behavior) && !behavior.Hallow) MechanicalBehaviorNetworkTests.UnmarkedTicks++;
        return true;
    }
}


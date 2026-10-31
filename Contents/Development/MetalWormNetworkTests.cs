using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AdvancedMod.Contents.Hallow.Weapons;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

/// <summary>文件仅编排阶段与断言期望；标记、储量和运动状态通过真实TCP同步。</summary>
public sealed class MetalWormNetworkTests : ModSystem
{
    private string role, run;
    private int stage = -1, observed = -1, age;
    private NPC target;
    private Projectile first, second;
    private Player owner, other;
    private readonly List<object> results = new();
    private bool done;
    private readonly HashSet<int> probes = new(), shots = new();
    private bool probeDamage, shotTags;
    private const float Marker = 87310;
    private record Expected(int Stage, int Owner, int Other, int First, int Second, int ResourceA, int ResourceB, int Npc);
    private string Output => Path.Combine(Main.SavePath, "MetalWormCaptures");
    public override void Load()
    {
        role = Environment.GetEnvironmentVariable("ADVANCEDMOD_METAL_NET"); run = Environment.GetEnvironmentVariable("ADVANCEDMOD_METAL_NET_RUN");
        if (role != null) On_Player.LoadPlayer += Rename;
    }
    public override void Unload() => On_Player.LoadPlayer -= Rename;
    private PlayerFileData Rename(On_Player.orig_LoadPlayer orig, string path, bool cloud)
    { var file = orig(path, cloud); file.Player.name = "Metal-" + role; return file; }
    public override void PostUpdatePlayers()
    {
        if (role == null || Main.gameMenu) return;
        foreach (var p in Main.ActivePlayers)
        { p.smolstar = true; p.statLife = p.statLifeMax2; p.immune = true; p.immuneTime = 60; }
    }
    private void Check(string name, bool ok) => results.Add(new { name, status = ok ? "PASS" : "FAIL" });
    public override void PostUpdateEverything()
    {
        if (role == null || run == null || done || Main.gameMenu) return;
        Main.autoSave = false;
        try { if (Main.netMode == NetmodeID.Server) Server(); else Client(); }
        catch (Exception ex) { results.Add(new { name = "运行异常", status = "FAIL", detail = ex.ToString() }); Finish(); }
    }
    private void Publish(int a, int b)
    {
        age = 0;
        File.WriteAllText(Path.Combine(run, "expected.json"), JsonSerializer.Serialize(new Expected(stage, owner.whoAmI, other.whoAmI, first.identity, second.identity, a, b, target.whoAmI)));
    }
    private void Server()
    {
        if (stage >= 3)
        {
            foreach (var p in Main.ActiveProjectiles)
            {
                if (p.owner != owner.whoAmI) continue;
                if (p.ModProjectile is MetalWormProbe) { probes.Add(p.identity); probeDamage |= p.damage == 50 && p.ai[1] >= 1 && p.ai[1] <= 5; }
                if (p.ModProjectile is MetalWormProbeShot)
                {
                    shots.Add(p.identity); var data = p.GetGlobalProjectile<CombatGlobalProjectile>();
                    shotTags |= p.damage == 50 && p.ai[0] >= 1 && p.ai[0] <= 5 && data.ElementAmount == 0 && data.Profile.Tags.Contains(TagEnum.Hallow);
                }
            }
            if (stage == 4)
            {
                if (++age < MetalWormRules.ProbeLifetime + 60) return;
                Check("服务器重建鞭子经owner生成一次50%探测怪", probes.Count == 1 && probeDamage);
                Check("真实网络派生弹一次且元素参数标签完整", shots.Count == 1 && shotTags);
                Finish(); return;
            }
        }
        if (stage < 0)
        {
            var players = Main.player.Where(p => p.active).Take(2).ToArray(); if (players.Length < 2) return;
            if (++age < 60) return;
            owner = players[0]; other = players[1];
            int id = NPC.NewNPC(new EntitySource_Misc("蠕虫鞭联机"), (int)owner.Center.X, 800, ModContent.NPCType<ElementProbeNPC>());
            target = Main.npc[id]; target.damage = 0; target.ai[2] = Marker; target.dontTakeDamage = true;
            target.GetGlobalNPC<MetalWormMark>().Mark(target, owner.whoAmI);
            first = MetalWormLiveTests.Blade(owner, owner.Center); second = MetalWormLiveTests.Blade(other, other.Center);
            first.ai[2] = second.ai[2] = Marker;
            NetMessage.SendData(MessageID.SyncNPC, number: id);
            NetMessage.SendData(MessageID.SyncProjectile, number: first.whoAmI); NetMessage.SendData(MessageID.SyncProjectile, number: second.whoAmI);
            stage = 0; Publish(10, 10); return;
        }
        if (++age % 60 == 0) target.GetGlobalNPC<MetalWormMark>().Mark(target, owner.whoAmI);
        // 仅固定远离玩家的位置以观察耗尽同步；回充阶段放行正式PreAI。
        if (stage == 2)
        {
            first.Center = owner.Center + new Vector2(0, -500); second.Center = other.Center + new Vector2(0, -500);
        }
        if (!File.Exists(Path.Combine(run, "client1", "MetalWormCaptures", $"ack-{stage}.txt")) ||
            !File.Exists(Path.Combine(run, "client2", "MetalWormCaptures", $"ack-{stage}.txt"))) return;
        Check("阶段" + stage + "双客户端确认", true);
        stage++;
        if (stage == 1)
        {
            target.dontTakeDamage = false;
            MetalWormLiveTests.Hit(first, target); MetalWormLiveTests.Hit(second, target);
            target.dontTakeDamage = true;
            Check("服务器同owner减耗且异owner不减耗", first.GetGlobalProjectile<BladeStaffWater>().Remaining == 9 && second.GetGlobalProjectile<BladeStaffWater>().Remaining == 8);
            Publish(9, 8);
        }
        else if (stage == 2)
        {
            target.dontTakeDamage = false;
            for (int i = 0; i < 9; i++) MetalWormLiveTests.Hit(first, target);
            for (int i = 0; i < 4; i++) MetalWormLiveTests.Hit(second, target);
            target.dontTakeDamage = true;
            first.Center = owner.Center + new Vector2(0, -500); second.Center = other.Center + new Vector2(0, -500);
            NetMessage.SendData(MessageID.SyncProjectile, number: first.whoAmI); NetMessage.SendData(MessageID.SyncProjectile, number: second.whoAmI);
            Check("服务器耗尽两把独立储量", first.GetGlobalProjectile<BladeStaffWater>().Returning && second.GetGlobalProjectile<BladeStaffWater>().Returning);
            Publish(0, 0);
        }
        else if (stage == 3)
        {
            first.Center = owner.Center; second.Center = other.Center;
            first.GetGlobalProjectile<BladeStaffWater>().PreAI(first); second.GetGlobalProjectile<BladeStaffWater>().PreAI(second);
            Check("服务器回充恢复", first.GetGlobalProjectile<BladeStaffWater>().Remaining == 10 && second.GetGlobalProjectile<BladeStaffWater>().Remaining == 10);
            // 模拟网络实例仅SetDefaults而没有本地OnSpawn，验证派生伤害不退化到1。
            var remoteWhip = new Projectile(); remoteWhip.SetDefaults(ModContent.ProjectileType<MetalWormWhipProjectile>());
            remoteWhip.owner = owner.whoAmI; remoteWhip.damage = 100;
            remoteWhip.ModProjectile.OnHitNPC(target, default, 1);
            Publish(10, 10);
        }
        else age = 0;
    }
    private void Client()
    {
        string path = Path.Combine(run, "expected.json"); if (!File.Exists(path)) return;
        Expected e; try { e = JsonSerializer.Deserialize<Expected>(File.ReadAllText(path)); } catch (JsonException) { return; }
        if (e == null || e.Stage == observed) return;
        var a = Main.projectile.FirstOrDefault(p => p.active && p.owner == e.Owner && p.identity == e.First && p.type == ProjectileID.Smolstar);
        var b = Main.projectile.FirstOrDefault(p => p.active && p.owner == e.Other && p.identity == e.Second && p.type == ProjectileID.Smolstar);
        NPC n = Main.npc[e.Npc];
        if (a == null || b == null || !n.active || n.ai[2] != Marker ||
            a.GetGlobalProjectile<BladeStaffWater>().Remaining != e.ResourceA || b.GetGlobalProjectile<BladeStaffWater>().Remaining != e.ResourceB ||
            !n.GetGlobalNPC<MetalWormMark>().HasOwner(e.Owner) || n.GetGlobalNPC<MetalWormMark>().HasOwner(e.Other)) return;
        Check("阶段" + e.Stage + "储量与独立owner镜像", true);
        if (e.Stage == 2) Check("耗尽客户端禁止伤害", a.GetGlobalProjectile<BladeStaffWater>().CanDamage(a) == false && b.GetGlobalProjectile<BladeStaffWater>().CanDamage(b) == false);
        observed = e.Stage; Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, $"ack-{e.Stage}.txt"), "通过");
        if (e.Stage == 3) Finish();
    }
    private void Finish()
    {
        done = true; Directory.CreateDirectory(Output);
        File.WriteAllText(Path.Combine(Output, "network-report.json"), JsonSerializer.Serialize(new { role, results }, new JsonSerializerOptions { WriteIndented = true }));
    }
}

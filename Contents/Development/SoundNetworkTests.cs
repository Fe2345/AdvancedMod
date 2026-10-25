using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AdvancedMod.Basic.Global;
using AdvancedMod.Contents.Acoustics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

/// <summary>真实 TCP 专服与双客户端验收；不在普通游戏注册任何测试 Hook。</summary>
public sealed class SoundNetworkTests : ModSystem
{
    private const int Marker = 9320, StageTicks = 240;
    private string role;
    private bool started, reported;
    private int tick, stage, owner, hits;
    private double expectedElement;
    private NPC target;
    private readonly List<object> results = new();
    private readonly HashSet<ulong> waveIds = new(), hitIds = new();
    private bool sawReflection, sawConstructive, sawDestructive;
    private bool duplicateHit, badDamage;
    public override void Load()
    {
        role = Environment.GetEnvironmentVariable("ADVANCEDMOD_SOUND_NET_TEST");
        if (string.IsNullOrEmpty(role)) return;
        On_Player.LoadPlayer += Rename;
        ElementProbeNPC.AfterProjectileHit = ObserveHit;
    }
    public override void Unload()
    { On_Player.LoadPlayer -= Rename; if (!string.IsNullOrEmpty(role)) ElementProbeNPC.AfterProjectileHit = null; }
    private PlayerFileData Rename(On_Player.orig_LoadPlayer orig, string path, bool cloud)
    { var data = orig(path, cloud); data.Player.name = "SoundNet-" + role; return data; }
    private void ObserveHit(Projectile p, NPC npc)
    {
        if (p.ModProjectile is not SoundWave w || npc.ai[0] < Marker || npc.ai[0] > Marker + 2) return;
        if (Main.netMode != NetmodeID.Server) { hits++; return; }
        if (npc != target) return;
        duplicateHit |= !hitIds.Add(w.Id);
        badDamage |= npc.lifeMax - npc.life != p.damage;
        hits++; expectedElement += p.GetGlobalProjectile<CombatGlobalProjectile>().ElementAmount;
    }
    private void Result(string name, bool pass, object detail = null) => results.Add(new { name, status = pass ? "PASS" : "FAIL", detail });
    public override void PostUpdateEverything()
    {
        if (string.IsNullOrEmpty(role) || reported || Main.gameMenu) return;
        Main.autoSave = false;
        foreach (var p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is not SoundWave w || w.Id == 0) continue;
            waveIds.Add(w.Id);
            sawReflection |= w.Reflections > 0;
            sawConstructive |= w.FusionDepth > 0 && w.Coherence > 0.85f;
            sawDestructive |= w.FusionDepth > 0 && w.Coherence < 0.15f;
        }
        if (Main.netMode != NetmodeID.Server)
        {
            // 等服务器最后的完成标记及元素快照到达；不靠客户端墙钟猜测阶段。
            var last = Main.npc.FirstOrDefault(n => n.active && n.ai[0] == Marker + 2 && n.ai[1] == 1);
            if (last == null || ++tick < 30) return;
            Result("客户端观察服务器唯一ID与三种子波", waveIds.Count >= 8 && sawReflection && sawConstructive && sawDestructive,
                new { ids = waveIds.OrderBy(x => x).ToArray(), sawReflection, sawConstructive, sawDestructive });
            Result("客户端不执行托管伤害", hits == 0);
            foreach (var n in Main.npc.Where(n => n.active && n.ai[0] >= Marker && n.ai[0] <= Marker + 2))
                Result($"阶段{n.ai[0] - Marker}伤害与风库存镜像", n.life < n.lifeMax && n.GetGlobalNPC<CombatGlobalNPC>().Elements.Snapshot.Elements[6].Total > 0,
                    new { damage = n.lifeMax - n.life, element = n.GetGlobalNPC<CombatGlobalNPC>().Elements.Snapshot.Elements[6].Total });
            Result("客户端波包和哨兵清理", !Main.projectile.Any(p => p.active && p.ModProjectile is SoundWave or SoundSentry));
            Report(); return;
        }
        if (!started)
        {
            if (Main.player.Count(p => p.active) < 2) return;
            if (++tick < 120) return;
            owner = Main.player.First(p => p.active).whoAmI;
            started = true; Begin();
        }
        if (++tick < StageTicks) return;
        double actual = target.GetGlobalNPC<CombatGlobalNPC>().Elements.Snapshot.Elements[6].Total;
        Result($"服务器阶段{stage}一次伤害与风提交", hits == 1 && !duplicateHit && !badDamage && Math.Abs(actual - expectedElement) < 0.0001,
            new { hits, damage = target.lifeMax - target.life, actual, expectedElement });
        SoundDemoSystem.Clear(owner);
        target.dontTakeDamage = true; target.netUpdate = true;
        if (++stage < 3) { Begin(); return; }
        target.ai[1] = 1; target.netUpdate = true;
        Result("服务器三种交互与有界清理", sawReflection && sawConstructive && sawDestructive &&
            !Main.projectile.Any(p => p.active && p.ModProjectile is SoundWave or SoundSentry));
        Report();
    }
    private void Begin()
    {
        tick = hits = 0; expectedElement = 0; badDamage = duplicateHit = false; hitIds.Clear();
        var player = Main.player[owner];
        Vector2 at = player.Center - new Vector2(0, SoundRules.DemoHeight + stage * 160);
        Vector2 position = stage == 0 ? at - new Vector2(0, 180) : at + new Vector2(360, 0);
        int index = NPC.NewNPC(new EntitySource_Misc("SoundNetworkTest"), (int)position.X, (int)position.Y, ModContent.NPCType<ElementProbeNPC>());
        target = Main.npc[index]; target.Center = position; target.damage = 0; target.ai[0] = Marker + stage; target.netUpdate = true;
        SoundDemoSystem.Sentry(player, at - new Vector2(SoundRules.DemoSeparation, 0), Vector2.UnitX, 0, true);
        if (stage == 0) SoundDemoSystem.Sentry(player, at, Vector2.Normalize(new(1, 1)), 0, false);
        else SoundDemoSystem.Sentry(player, at + new Vector2(SoundRules.DemoSeparation, 0), -Vector2.UnitX, stage == 2 ? MathHelper.Pi : 0, true);
    }
    private void Report()
    {
        reported = true;
        string directory = Path.Combine(Main.SavePath, "Logs", "AdvancedMod"); Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "sound-network-tests.json"), JsonSerializer.Serialize(new { role, results }, new JsonSerializerOptions { WriteIndented = true }));
    }
}

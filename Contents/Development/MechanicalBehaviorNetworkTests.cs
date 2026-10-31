using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using AdvancedMod.Contents.Hallow.NPCs;
using AdvancedMod.Contents.Hallow.Projectiles;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
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
    internal static bool PhaseOne => Environment.GetEnvironmentVariable("ADVANCEDMOD_PHASE1_NET") == "1";
    internal static bool PhaseTwo => Environment.GetEnvironmentVariable("ADVANCEDMOD_PHASE2_NET") == "1";
    internal static bool Attacks => Environment.GetEnvironmentVariable("ADVANCEDMOD_ATTACK_NET") == "1";
    private bool attackGalleryReady;
    private bool phaseTwoInitialized;
    private int phaseTwoFrames;
    private bool phaseTwoSettled;
    private bool awaitingReaction;
    private int chargeBeforeShot, shotFrame;
    private string previousPhaseDiagnostic;
    private readonly System.Collections.Generic.List<object> phaseDiagnostics = new();
    private bool phaseTwoCharging;
    private bool phaseInitialized;
    private int serverFrames;
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
        player.immune = !(PhaseOne && Role == "client1" && test.ticks >= 450);
        if (PhaseTwo && Role == "client1")
        {
            // 此专项明确控制原生命中调用时机，避免玩家Update中的受伤冷却干扰样本。
            player.immune = true;
        }
        player.immuneTime = player.immune ? 600 : 0;
        if (!player.immune) player.hurtCooldowns[0] = player.hurtCooldowns[1] = 0;
        player.statLifeMax2 = player.statLife = PhaseOne || PhaseTwo ? 5000 : player.statLifeMax2;
        player.selectedItem = 0; player.releaseUseItem = true;
        player.controlUseItem = Role == "client1" && test.ticks is 130 or 260 or 390;
    }
    public override void PostUpdateEverything()
    {
        if (!Enabled || Main.gameMenu) return;
        if (PhaseTwo && Role == "client1") ApplyPhaseTwoTestHits();
        if (reported)
        {
            if (PhaseOne && Role == "server") UpdatePhaseOne();
            if (PhaseTwo && Role == "server") UpdatePhaseTwo();
            return;
        }
        Main.autoSave = false;
        Main.dayTime = false; Main.time = 18000;
        if (Role == "server")
        {
            if (PhaseOne) UpdatePhaseOne();
            if (PhaseTwo) UpdatePhaseTwo();
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
            if (ticks >= (PhaseTwo ? 1800 : 600)) Report();
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
        if (PhaseOne)
        {
            var destroyer = heads.FirstOrDefault(n => n.type == NPCID.TheDestroyer);
            var probes = Main.npc.Where(n => n.active && n.type == NPCID.Probe && n.GetGlobalNPC<HallowProbeBehavior>().Element != ProbeElement.None).ToArray();
            var shots = Main.projectile.Where(p => p.active && p.TryGetGlobalProjectile<HallowProbeShot>(out var s) && s.Element != ProbeElement.None).ToArray();
            checks["身体元素首次与晚加入同步"] = segments.Where(n => n.type == NPCID.TheDestroyerBody).All(n => n.GetGlobalNPC<DestroyerBehavior>().SegmentElement != ProbeElement.None);
            checks["五种探测怪元素同步"] = probes.Select(n => n.GetGlobalNPC<HallowProbeBehavior>().Element).Distinct().Count() == 5;
            checks["五种弹幕同步"] = shots.Select(p => p.GetGlobalProjectile<HallowProbeShot>().Element).Distinct().Count() == 5;
            checks["弹幕元素与非神圣资格同步"] = shots.Length > 0 && shots.All(p =>
                p.GetGlobalProjectile<CombatGlobalProjectile>().Profile.Tags.Contains(DestroyerPhaseOneRules.Tag(p.GetGlobalProjectile<HallowProbeShot>().Element)) &&
                !p.GetGlobalProjectile<CombatGlobalProjectile>().Profile.Tags.Contains(TagEnum.Hallow));
            checks["击败数和减伤同步"] = destroyer != null && destroyer.GetGlobalNPC<DestroyerBarState>().Remaining == NPC.GetDestroyerSegmentsCount() - 1 &&
                Math.Abs(destroyer.GetIncomingStrikeModifiers(DamageClass.Generic, 0).FinalDamage.ApplyTo(100) - 21) < .01;
            checks["实际远端命中水弹返航同步"] = shots.Any(p => p.GetGlobalProjectile<HallowProbeShot>().Returning);
        }
        if (PhaseTwo)
        {
            var destroyer = heads.FirstOrDefault(n => n.type == NPCID.TheDestroyer);
            var behavior = destroyer?.GetGlobalNPC<DestroyerBehavior>();
            checks["二阶段自动进入并同步"] = behavior?.PhaseTwo == true;
            checks["探测怪归零同步"] = destroyer?.GetGlobalNPC<DestroyerBarState>().Remaining == 0;
            checks["真实远端反应完成Ultimate"] = behavior?.UltimateSerial == 1;
            checks["释放后真实反应重新充能7"] = behavior?.ReactionCount == 7;
            checks["Ultimate三轮完成恢复可见"] = behavior?.Hidden == false && behavior?.UltimateRepetition == 3;
        }
        if (Attacks)
        {
            var shots = Main.projectile.Where(p => p.active && p.ModProjectile is DestroyerCrashProjectile).ToArray();
            var preview = Main.projectile.FirstOrDefault(p => p.active && p.ModProjectile is DestroyerUltimatePreview);
            checks["五类Crash弹幕首次及晚加入同步"] = shots.Select(p => ((DestroyerCrashProjectile)p.ModProjectile).Kind).Distinct().Count() == 5;
            checks["新弹幕神圣标签与Boss来源同步"] = shots.Length > 0 && shots.All(p => p.GetGlobalProjectile<CombatGlobalProjectile>().Profile.Tags.Contains(TagEnum.Hallow) &&
                p.GetGlobalProjectile<DestroyerAttackOwner>().Head >= 0 && p.GetGlobalProjectile<DestroyerAttackOwner>().Encounter > 0);
            checks["旋风锁定中心同步"] = shots.Any(p => p.ModProjectile is DestroyerCrashProjectile s && s.Kind == DestroyerAttackKind.Water && Vector2.Distance(s.Anchor, Anchor) < 30);
            checks["Ultimate预警角度与无伤害同步"] = preview?.ModProjectile is DestroyerUltimatePreview u && u.Encounter > 0 && u.Age >= 30 &&
                Math.Abs(Math.Abs(u.WarningAngle - u.StartAngle) - MathHelper.Pi) < .001 && u.CanDamage() == false;
        }
        string dir = Path.Combine(Main.SavePath, "MechanicalCaptures"); Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "network-report.json"), JsonSerializer.Serialize(new
        {
            role = Role, unmarkedTicks = UnmarkedTicks, consumed,
            remaining = heads.FirstOrDefault(n => n.type == NPCID.TheDestroyer)?.GetGlobalNPC<DestroyerBarState>().Remaining,
            phaseTwo = heads.Where(n => n.type == NPCID.TheDestroyer).Select(n => n.GetGlobalNPC<DestroyerBehavior>())
                .Select(b => new { state = b.PhaseState.ToString(), b.ReactionCount, b.UltimateSerial, b.UltimateRepetition }).ToArray(),
            phaseDiagnostics,
            shots = Main.projectile.Where(p => p.active && p.TryGetGlobalProjectile<HallowProbeShot>(out var s) && s.Element != ProbeElement.None)
                .Select(p => new { p.identity, p.owner, element = p.GetGlobalProjectile<HallowProbeShot>().Element.ToString(), p.GetGlobalProjectile<HallowProbeShot>().Returning }).ToArray(),
            players = Main.player.Where(p => p.active).Select(p => new { p.name, p.ZoneHallow, held = p.HeldItem.type }).ToArray(),
            results = checks.Select(c => new { name = c.Key, status = c.Value ? "PASS" : "FAIL" })
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void ApplyPhaseTwoTestHits()
    {
        var player = Main.LocalPlayer;
        foreach (var shot in Main.projectile.Where(p => p.active && p.GetGlobalProjectile<CombatGlobalProjectile>().Profile.AttackId == "Destroyer.Network.Test").ToArray())
        {
            player.GetModPlayer<CombatPlayer>().SetNetworkState(new Basic.Element.ElementTarget(Basic.Element.ElementTarget.DefaultOperationLimit));
            player.GetModPlayer<CombatPlayer>().Elements.Attach(new(4, 1.5, 20));
            player.immune = false; player.immuneTime = 0; player.hurtCooldowns[0] = player.hurtCooldowns[1] = 0;
            player.statLifeMax2 = player.statLife = 5000;
            shot.Center = player.Center; shot.Damage(); shot.Kill();
            player.immune = true; player.immuneTime = 600;
        }
    }

    private void UpdatePhaseTwo()
    {
        NPC head = Main.npc.FirstOrDefault(n => n.active && n.type == NPCID.TheDestroyer);
        if (head == null || head.GetGlobalNPC<DestroyerBarState>().Maximum == 0) return;
        if (!phaseTwoInitialized)
        {
            phaseTwoInitialized = true;
            foreach (NPC body in Main.npc.Where(n => n.active && n.type == NPCID.TheDestroyerBody && n.realLife == head.whoAmI).ToArray())
            {
                for (int i = 0; i < 2000 && body.ai[2] == 0; i++) body.HitEffect();
                foreach (NPC probe in Main.npc.Where(n => n.active && n.type == NPCID.Probe).ToArray()) probe.SimpleStrikeNPC(100000, 0, noPlayerInteraction: true);
            }
        }
        phaseTwoFrames++;
        var b = head.GetGlobalNPC<DestroyerBehavior>();
        if (b.PhaseTwo && b.UltimateSerial == 0 && b.ReactionCount < DestroyerPhaseTwoRules.ChargeMaximum)
        {
            // 运动已由客户端专项覆盖；此处隔离满20的技能结束边界，避免Idle概率提前释放。
            if (!phaseTwoCharging)
            {
                typeof(DestroyerBehavior).GetMethod("Enter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(b, new object[] { head, DestroyerBehavior.PhaseTwoState.Crash });
                phaseTwoCharging = true;
            }
            typeof(DestroyerBehavior).GetProperty(nameof(DestroyerBehavior.PhaseTimer)).SetValue(b, 0);
            head.Center = Anchor - new Vector2(1100, 0); head.velocity = Vector2.UnitX * 14;
            if (phaseTwoFrames % 15 == 0) NetMessage.SendData(MessageID.SyncNPC, number: head.whoAmI);
        }
        string diagnostic = $"{b.PhaseState}:{b.ReactionCount}:{b.UltimateSerial}";
        if (diagnostic != previousPhaseDiagnostic)
        {
            phaseDiagnostics.Add(new { tick = phaseTwoFrames, state = b.PhaseState.ToString(), b.ReactionCount, b.UltimateSerial, awaitingReaction, chargeBeforeShot });
            previousPhaseDiagnostic = diagnostic;
        }
        if (b.Hidden || b.ReactionCount != chargeBeforeShot || phaseTwoFrames - shotFrame >= 120) awaitingReaction = false;
        if (b.UltimateSerial > 0 && !b.Hidden && b.ReactionCount >= 7)
        {
            // 固定最终展示窗口供晚加入验收，避免等待期间又随机释放一次。
            if (!phaseTwoSettled)
            {
                typeof(DestroyerBehavior).GetMethod("Enter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(b, new object[] { head, DestroyerBehavior.PhaseTwoState.Idle });
                phaseTwoSettled = true;
            }
            typeof(DestroyerBehavior).GetProperty(nameof(DestroyerBehavior.PhaseTimer)).SetValue(b, 0);
            if (phaseTwoFrames % 15 == 0) NetMessage.SendData(MessageID.SyncNPC, number: head.whoAmI);
            if (Attacks) MaintainAttackGallery(head);
            return;
        }
        var player = Main.player.FirstOrDefault(p => p.active && p.name == "Mechanical-client1");
        if (player == null || !b.PhaseTwo || b.Hidden || awaitingReaction || b.ReactionCount >= DestroyerPhaseTwoRules.ChargeMaximum ||
            b.UltimateSerial > 0 && b.ReactionCount >= 7 || phaseTwoFrames % 12 != 0) return;
        int slot = Projectile.NewProjectile(head.GetSource_FromAI(), player.Center, Vector2.Zero, ProjectileID.DeathLaser, 20, 0, Main.myPlayer);
        if (slot >= Main.maxProjectiles) return;
        var shot = Main.projectile[slot]; shot.tileCollide = false; shot.timeLeft = 20; shot.penetrate = 1;
        awaitingReaction = true; chargeBeforeShot = b.ReactionCount; shotFrame = phaseTwoFrames;
        var data = shot.GetGlobalProjectile<CombatGlobalProjectile>();
        data.Profile = new(new TagSet(new[] { new TagEntry(false, TagEnum.Pyro), new TagEntry(false, TagEnum.Hallow) }), "Destroyer.Network.Test", isManagedAttack: true);
        data.ElementAmount = 1.5;
        NetMessage.SendData(MessageID.SyncProjectile, number: slot);
    }

    private void MaintainAttackGallery(NPC head)
    {
        if (!attackGalleryReady)
        {
            attackGalleryReady = true;
            foreach (var p in Main.projectile.Where(p => p.active && (p.ModProjectile is DestroyerCrashProjectile or DestroyerUltimatePreview)).ToArray()) p.Kill();
            foreach (DestroyerAttackKind kind in Enum.GetValues<DestroyerAttackKind>())
            {
                DestroyerBehavior.SpawnCrashAttack(head, kind);
                var group = Main.projectile.Where(p => p.active && p.ModProjectile is DestroyerCrashProjectile c && c.Kind == kind).ToArray();
                foreach (var extra in group.Skip(1)) extra.Kill();
            }
            Projectile.NewProjectile(head.GetSource_FromAI(), Anchor - new Vector2(700, 0), Vector2.Zero,
                ModContent.ProjectileType<DestroyerUltimatePreview>(), 0, 0, Main.myPlayer, head.whoAmI, -MathHelper.PiOver2, 1);
        }
        foreach (var p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is DestroyerCrashProjectile shot)
            {
                typeof(DestroyerCrashProjectile).GetProperty(nameof(DestroyerCrashProjectile.Age)).SetValue(shot, 10);
                p.Center = Anchor + new Vector2((int)shot.Kind * 100 - 200, -200); p.timeLeft = 600;
            }
            else if (p.ModProjectile is DestroyerUltimatePreview preview)
            {
                typeof(DestroyerUltimatePreview).GetProperty(nameof(DestroyerUltimatePreview.Age)).SetValue(preview, 30); p.timeLeft = 600;
            }
            else continue;
            if (phaseTwoFrames % 15 == 0) NetMessage.SendData(MessageID.SyncProjectile, number: p.whoAmI);
        }
    }

    private void UpdatePhaseOne()
    {
        serverFrames++;
        NPC head = Main.npc.FirstOrDefault(n => n.active && n.type == NPCID.TheDestroyer);
        if (head == null || head.ai[0] == 0) return;
        if (!phaseInitialized)
        {
            var bodies = Main.npc.Where(n => n.active && n.type == NPCID.TheDestroyerBody && n.realLife == head.whoAmI).Take(6).ToArray();
            if (bodies.Length != 6) return;
            phaseInitialized = true;
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].GetGlobalNPC<DestroyerBehavior>().SegmentElement = (ProbeElement)(i % 5 + 1);
                for (int j = 0; j < 2000 && bodies[i].ai[2] == 0; j++) bodies[i].HitEffect();
                bodies[i].netUpdate = true;
            }
            Main.npc.Last(n => n.active && n.type == NPCID.Probe).SimpleStrikeNPC(100000, 0);
        }
        foreach (NPC probe in Main.ActiveNPCs)
        {
            if (!probe.TryGetGlobalNPC<HallowProbeBehavior>(out var b) || b.Element == ProbeElement.None) continue;
            // 固定五种同步样本不参与额外战斗；已单独原生击杀第六只验证计数。
            probe.immortal = true;
            probe.Center = Anchor + new Vector2(-300 + (int)b.Element * 100, -180);
            probe.velocity = Vector2.Zero;
            if (serverFrames % 90 == 0) probe.localAI[0] = 119;
            if (serverFrames % 15 == 0) probe.netUpdate = true;
        }
        // 固定展示场只用于网络生命周期测试，避免晚加入前弹幕飞远或到期。
        var retained = new System.Collections.Generic.HashSet<(ProbeElement, bool)>();
        foreach (Projectile shot in Main.projectile.Where(p => p.active).ToArray())
        {
            if (!shot.TryGetGlobalProjectile<HallowProbeShot>(out var b) || b.Element == ProbeElement.None) continue;
            if (!retained.Add((b.Element, b.Returning))) { shot.Kill(); continue; }
            shot.Center = Anchor + (b.Element == ProbeElement.Water && !b.Returning ? Vector2.Zero : new Vector2(-300 + (int)b.Element * 100, 90));
            shot.velocity = Vector2.Zero; shot.timeLeft = 600;
            if (serverFrames % 15 == 0) NetMessage.SendData(MessageID.SyncProjectile, number: shot.whoAmI);
        }
    }
}

public sealed class MechanicalNetworkTestPlayer : ModPlayer
{
    public override bool PreItemCheck() { MechanicalBehaviorNetworkTests.Drive(Player); return true; }
    public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot) => !MechanicalBehaviorNetworkTests.PhaseTwo;
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


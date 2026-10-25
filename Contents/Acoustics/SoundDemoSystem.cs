using System;
using System.Collections.Generic;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Basic.Utils;
using AdvancedMod.Basic.Element;
using AdvancedMod.Basic.Global;
using AdvancedMod.Contents.Development;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Acoustics;

/// <summary>服务器每刻先移动全部波包，再依次接收／融合；新波留到下一刻参与交互。</summary>
public sealed class SoundDemoSystem : ModSystem
{
    private static ulong nextId;
    private static int nextNetworkIdentity;
    public static bool Debug;
    private const int TargetMarker = 9324;
    private static readonly TagSet Tags = new(new[] { new TagEntry(false, TagEnum.Anemo) });
    private static readonly TagSet SentryTags = new(Array.Empty<TagEntry>());
    private readonly List<SoundWave> waves = new(SoundRules.MaxWaves);
    private readonly List<SoundSentry> sentries = new(SoundRules.MaxSentries);
    public static bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
    public static SoundWave Spawn(IEntitySource source, Vector2 position, Vector2 direction, int owner, float phase)
    {
        if (!Authority || Count<SoundWave>() >= SoundRules.MaxWaves || !HasProjectileSlot()) return null;
        int index = ElementAttacks.NewProjectile(source, position, direction * SoundRules.Speed,
            ModContent.ProjectileType<SoundWave>(), SoundRules.BaseDamage, 0, Tags, SoundRules.BaseElement, owner);
        if (index >= Main.maxProjectiles) return null;
        var wave = (SoundWave)Main.projectile[index].ModProjectile;
        AssignNetworkIdentity(wave.Projectile);
        wave.Id = ++nextId; wave.Origin = position; wave.Phase = SoundRules.Phase(phase);
        wave.Projectile.netUpdate = true;
        return wave;
    }
    private static int Count<T>() where T : ModProjectile
    {
        int count = 0;
        foreach (var p in Main.ActiveProjectiles) if (p.ModProjectile is T) count++;
        return count;
    }
    private static bool HasProjectileSlot()
    {
        foreach (var p in Main.projectile) if (!p.active) return true;
        return false;
    }
    // 原版以槽号初始化 identity，Kill 通知经 owner 回传时可能误杀刚复用槽位的新实体。
    // 在原生索引空间内轮转空闲身份，避免紧邻复用；不改变玩家归属与现有元素事务。
    private static void AssignNetworkIdentity(Projectile projectile)
    {
        if (Main.netMode != NetmodeID.Server) return;
        int oldIdentity = projectile.identity, owner = projectile.owner;
        Main.projectileIdentity[owner, oldIdentity] = -1;
        foreach (var p in Main.ActiveProjectiles)
            if (p != projectile && p.owner == owner && p.identity == oldIdentity)
                Main.projectileIdentity[owner, oldIdentity] = p.whoAmI;
        for (int attempt = 0; attempt < Main.maxProjectiles; attempt++)
        {
            int candidate = nextNetworkIdentity = (nextNetworkIdentity + 1) % Main.maxProjectiles;
            bool used = false;
            foreach (var p in Main.ActiveProjectiles)
                if (p != projectile && p.owner == owner && p.identity == candidate) { used = true; break; }
            if (used) continue;
            projectile.identity = candidate; Main.projectileIdentity[owner, candidate] = projectile.whoAmI; return;
        }
    }
    public static SoundSentry Sentry(Player player, Vector2 position, Vector2 normal, float phase, bool emit)
    {
        if (!Authority || Count<SoundSentry>() >= SoundRules.MaxSentries || !HasProjectileSlot()) return null;
        int index = ElementAttacks.NewProjectile(player.GetSource_Misc("SoundDemo"), position, Vector2.Zero,
            ModContent.ProjectileType<SoundSentry>(), 0, 0, SentryTags, 0, player.whoAmI, normal.ToRotation(), phase, emit ? SoundRules.EmissionDelay : 0);
        if (index >= Main.maxProjectiles) return null;
        var sentry = (SoundSentry)Main.projectile[index].ModProjectile;
        AssignNetworkIdentity(sentry.Projectile);
        sentry.Id = ++nextId;
        // 本机 TML 只给 Owner == Main.myPlayer 的 NewProjectile 写入 ai 参数。
        sentry.Projectile.ai[0] = normal.ToRotation(); sentry.Projectile.ai[1] = phase; sentry.Projectile.ai[2] = emit ? SoundRules.EmissionDelay : 0;
        if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncProjectile, number: index);
        return sentry;
    }
    public override void PreUpdateProjectiles()
    {
        if (!Authority) return;
        waves.Clear(); sentries.Clear();
        foreach (var p in Main.ActiveProjectiles)
        {
            if (p.ModProjectile is SoundWave w && w.Id != 0) waves.Add(w);
            if (p.ModProjectile is SoundSentry s && s.Id != 0) sentries.Add(s);
        }
        // 单调 ID 给多波同时交汇规定稳定优先级，不用槽位复用顺序决定相位结果。
        waves.Sort(static (a, b) => a.Id.CompareTo(b.Id));
        foreach (var wave in waves) wave.Advance();
        foreach (var wave in waves)
        {
            if (!wave.Projectile.active || wave.Reflections >= SoundRules.MaxReflections) continue;
            foreach (var sentry in sentries)
            {
                if (sentry.Projectile.owner != wave.Projectile.owner || wave.Receivers.Contains(sentry.Id) ||
                    !SoundRules.Overlap(wave.Projectile.Center, wave.Direction, new(SoundRules.HalfLength, SoundRules.HalfWidth),
                        sentry.Projectile.Center, Vector2.UnitX, new(SoundRules.SentryRadius))) continue;
                Vector2 at = sentry.Projectile.Center;
                float phase = SoundRules.PhaseAt(at, wave.Projectile.Center, wave.Direction, wave.Phase, wave.Wavelength);
                var child = Spawn(wave.Projectile.GetSource_FromThis(), at, SoundRules.Reflect(wave.Direction, sentry.Normal), wave.Projectile.owner, phase);
                if (child == null) break;
                child.Energy = wave.Energy * SoundRules.ReflectionRetention; child.Coherence = wave.Coherence;
                child.Reflections = wave.Reflections + 1; child.FusionDepth = wave.FusionDepth;
                child.Distance = wave.Distance; child.Wavelength = wave.Wavelength;
                child.Projectile.timeLeft = wave.Projectile.timeLeft;
                child.Receivers.UnionWith(wave.Receivers); child.Receivers.Add(sentry.Id);
                child.UpdateAttack(); wave.Projectile.Kill(); break;
            }
        }
        for (int i = 0; i < waves.Count; i++)
        {
            var a = waves[i];
            if (!a.Projectile.active || a.FusionDepth >= SoundRules.MaxFusionDepth) continue;
            for (int j = i + 1; j < waves.Count; j++)
            {
                var b = waves[j];
                if (!b.Projectile.active || b.FusionDepth >= SoundRules.MaxFusionDepth || a.Projectile.owner != b.Projectile.owner ||
                    MathF.Abs(a.Wavelength - b.Wavelength) > 0.001f ||
                    !SoundRules.Overlap(a.Projectile.Center, a.Direction, new(SoundRules.HalfLength, SoundRules.HalfWidth),
                        b.Projectile.Center, b.Direction, new(SoundRules.HalfLength, SoundRules.HalfWidth))) continue;
                Vector2 at = (a.Projectile.Center + b.Projectile.Center) * 0.5f;
                var result = SoundRules.Fuse(a.Energy, b.Energy,
                    SoundRules.PhaseAt(at, a.Projectile.Center, a.Direction, a.Phase, a.Wavelength),
                    SoundRules.PhaseAt(at, b.Projectile.Center, b.Direction, b.Phase, b.Wavelength), a.Coherence, b.Coherence);
                // 正对碰的动量为零，沿较早波包方向继续传播，规则不改变干涉数值。
                Vector2 direction = SoundRules.FusionDirection(a.Direction, b.Direction, a.Energy, b.Energy);
                var child = Spawn(a.Projectile.GetSource_FromThis(), at, direction, a.Projectile.owner, result.Phase);
                if (child == null) break;
                child.Energy = result.Energy; child.Coherence = result.Coherence;
                child.FusionDepth = Math.Max(a.FusionDepth, b.FusionDepth) + 1;
                child.Reflections = Math.Max(a.Reflections, b.Reflections);
                child.Distance = Math.Max(a.Distance, b.Distance); child.Wavelength = a.Wavelength;
                child.Projectile.timeLeft = Math.Min(a.Projectile.timeLeft, b.Projectile.timeLeft);
                child.Receivers.UnionWith(a.Receivers); child.Receivers.UnionWith(b.Receivers);
                child.UpdateAttack();
                // 在同一权威线程先失活，再继续循环；原波不可能参与第三列融合或后续 Damage。
                a.Projectile.Kill(); b.Projectile.Kill(); break;
            }
        }
    }
    public static void Clear(int owner)
    {
        if (!Authority) return;
        foreach (var p in Main.ActiveProjectiles)
            if (p.owner == owner && p.ModProjectile is SoundWave or SoundSentry) p.Kill();
        foreach (var npc in Main.ActiveNPCs)
            if (npc.ai[2] == TargetMarker && npc.ai[3] == owner)
            {
                npc.active = false;
                if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
            }
    }
    internal static void Target(Player player, Vector2 at)
    {
        int index = NPC.NewNPC(player.GetSource_Misc("SoundDemo"), (int)at.X, (int)at.Y, ModContent.NPCType<ElementProbeNPC>());
        if (index >= Main.maxNPCs) return;
        var npc = Main.npc[index]; npc.Center = at; npc.damage = 0;
        npc.ai[2] = TargetMarker; npc.ai[3] = player.whoAmI; npc.netUpdate = true;
    }
    internal static void InspectTargets(CommandCaller caller)
    {
        foreach (var npc in Main.ActiveNPCs)
            if (npc.ai[2] == TargetMarker && npc.ai[3] == caller.Player.whoAmI)
                caller.Reply($"靶标#{npc.whoAmI} 累计伤害={npc.lifeMax - npc.life} 风附着={npc.GetGlobalNPC<CombatGlobalNPC>().Elements.Snapshot.Elements[ElementSlots.FromTag(TagEnum.Anemo).Value].Total:F3}");
    }
    public override void OnWorldUnload() { waves.Clear(); sentries.Clear(); Debug = false; }
}

public sealed class SoundDemoCommand : ModCommand
{
    public override string Command => "sounddemo";
    public override CommandType Type => CommandType.World;
    public override string Usage => "/sounddemo single|reflect|same|opposite|partial|stress [target]，或 clear|inspect";
    public override string Description => "在玩家上方生成声学哨兵与测试波包";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        string mode = args.Length == 0 ? "single" : args[0].ToLowerInvariant();
        if (mode == "inspect")
        {
            foreach (var p in Main.ActiveProjectiles)
                if (p.owner == caller.Player.whoAmI && p.ModProjectile is SoundWave w)
                    caller.Reply($"#{w.Id} φ={w.Phase:F2} E={w.Energy:F2} 距离={w.Distance:F0} 反射={w.Reflections} 融合={w.FusionDepth} {SoundRules.Kind(w.Coherence)} 白值={p.damage}");
            SoundDemoSystem.InspectTargets(caller);
            return;
        }
        if (mode is not ("single" or "reflect" or "same" or "opposite" or "partial" or "stress" or "clear"))
        { caller.Reply(Usage); return; }
        SoundDemoSystem.Clear(caller.Player.whoAmI);
        if (mode == "clear") return;
        Vector2 at = caller.Player.Center - new Vector2(0, SoundRules.DemoHeight);
        float phase = mode == "opposite" ? MathHelper.Pi : mode == "partial" ? MathHelper.PiOver2 : 0;
        SoundDemoSystem.Sentry(caller.Player, at - Vector2.UnitX * SoundRules.DemoSeparation, Vector2.UnitX, 0, true);
        if (mode is "same" or "opposite" or "partial" or "stress")
            SoundDemoSystem.Sentry(caller.Player, at + Vector2.UnitX * SoundRules.DemoSeparation, -Vector2.UnitX, phase, true);
        if (mode is "reflect" or "stress")
            SoundDemoSystem.Sentry(caller.Player, at, Vector2.Normalize(new Vector2(1, 1)), 0, false);
        if (mode == "stress")
        {
            SoundDemoSystem.Sentry(caller.Player, at - Vector2.UnitY * SoundRules.DemoSeparation, Vector2.UnitY, MathHelper.PiOver2, true);
            SoundDemoSystem.Sentry(caller.Player, at + Vector2.UnitY * SoundRules.DemoSeparation, -Vector2.UnitY, MathHelper.Pi, true);
        }
        if (args.Length > 1 && args[1].Equals("target", StringComparison.OrdinalIgnoreCase))
            SoundDemoSystem.Target(caller.Player, mode == "reflect" ? at - new Vector2(0, SoundRules.DemoReflectTargetDistance) : mode == "single" ? at : at + new Vector2(SoundRules.DemoTargetDistance, 0));
        caller.Reply("哨兵将在30刻后发射；15秒后清理。/sounddebug 切换本地标签，/sounddemo inspect 查看权威状态。");
    }
}

public sealed class SoundDebugCommand : ModCommand
{
    public override string Command => "sounddebug";
    public override CommandType Type => CommandType.Chat;
    public override string Usage => "/sounddebug";
    public override string Description => "切换本地声波状态标签";
    public override void Action(CommandCaller caller, string input, string[] args)
    { SoundDemoSystem.Debug = !SoundDemoSystem.Debug; caller.Reply($"声波调试：{SoundDemoSystem.Debug}"); }
}


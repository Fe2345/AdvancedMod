#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AdvancedMod.Basic.Element;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Calculate.Tml;
using AdvancedMod.Basic.Element.Tml;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Basic.Utils;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

/// <summary>在真实客户端世界中执行的开发验收，不替代联机测试。</summary>
public sealed class ElementLiveTests : ModSystem
{
    private bool ran;
    private static readonly List<NPC> spawnedNpcs = new();
    private static readonly List<Projectile> spawnedProjectiles = new();
    private static readonly List<ElementResolution> resolutions = new();
    public override void PostUpdateEverything() => RunAutomatic();

    internal void RunAutomatic()
    {
        if (ran || Main.gameMenu || Main.netMode != NetmodeID.SinglePlayer ||
            Environment.GetEnvironmentVariable("ADVANCEDMOD_ELEMENT_LIVE_TEST") != "1") return;
        ran = true;
        Main.autoSave = false;
        Run();
        // 自动模式只用于隔离存档，测试报告写入后退出，不保存世界或玩家。
        Main.instance.Exit();
    }

    internal static void Run()
    {
        if (Main.netMode != NetmodeID.SinglePlayer) throw new InvalidOperationException("当前测试仅支持单人。");
        var results = new List<object>();
        ElementHitSystem.Resolved += resolutions.Add;
        var tests = new (string, Action)[]
        {
            ("嵌套生成保留来源且不继承标签", NestedSpawn),
            ("弹幕水雷125伤害与生成标签", ProjectileReaction),
            ("水风生命门槛与火冰175伤害", OtherCoefficients),
            ("水风成功支付与失败不支付", LifePayment),
            ("导电逐跳防御与保底前终止", ConductiveChain),
            ("导电抗性独立且源增伤不重复", ConductiveResistance),
            ("导电距离与墙体阻挡", ConductiveGeometry),
            ("近战真实入口与元素量", WeaponHit),
            ("CD阻塞后普通附着", Cooldown),
            ("NPC到玩家伤害及取消闪避免疫", PlayerHurt),
            ("敌对弹幕到玩家", HostileProjectile),
            ("致死命中仍记录反应", Lethal),
            ("每刻静置环境反应", Idle),
            ("原生弹幕回调重入串行", NativeReentry),
            ("原生异常与实体复用收尾", NativeFailure),
        };
        try
        {
            foreach (var (name, action) in tests)
            {
                ModContent.GetInstance<ElementLiveTests>().Mod.Logger.Info($"开始元素实机用例：{name}");
                resolutions.Clear();
                try { action(); results.Add(new { name, status = "PASS" }); }
                catch (Exception e) { results.Add(new { name, status = "FAIL", error = e.ToString() }); }
                finally
                {
                    foreach (var projectile in spawnedProjectiles) projectile.active = false;
                    foreach (var npc in spawnedNpcs) npc.active = false;
                    spawnedProjectiles.Clear();
                    spawnedNpcs.Clear();
                    ElementProbePlayer.Cancel = ElementProbePlayer.Dodge = false;
                    ElementProbeNPC.AfterProjectileHit = null;
                    ElementProbeProjectile.Spawned = null;
                }
            }
        }
        finally { ElementHitSystem.Resolved -= resolutions.Add; }
        string report = JsonSerializer.Serialize(new { mode = "真实 TML 单人客户端", version = Main.versionNumber,
            utc = DateTimeOffset.UtcNow, results }, new JsonSerializerOptions { WriteIndented = true });
        string directory = Path.Combine(Main.SavePath, "Logs", "AdvancedMod");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "element-live-tests.json"), report);
        ModContent.GetInstance<ElementLiveTests>().Mod.Logger.Info(report);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static void Near(double actual, double expected)
    {
        Check(Math.Abs(actual - expected) < 0.00001, $"实际 {actual}，期望 {expected}");
    }
    private static TagSet Tags(TagEnum tag) => new(new[] { new TagEntry(false, tag) });
    private static NPC Target(int slot, double amount = 1.5)
    {
        Vector2 position = Main.LocalPlayer.Center + new Vector2(400, -400);
        int index = NPC.NewNPC(new EntitySource_Misc("元素实机测试"), (int)position.X, (int)position.Y,
            ModContent.NPCType<ElementProbeNPC>());
        var npc = Main.npc[index];
        spawnedNpcs.Add(npc);
        npc.GetGlobalNPC<CombatGlobalNPC>().Elements.Attach(new(slot, amount, 100));
        return npc;
    }
    private static Projectile Shot(Vector2 position, TagEnum tag, double amount = 1)
    {
        int index = ElementAttacks.NewProjectile(new EntitySource_Misc("元素实机测试"), position,
            Vector2.Zero, ModContent.ProjectileType<ElementProbeProjectile>(), 100, 0, Tags(tag), amount, Main.myPlayer);
        var projectile = Main.projectile[index];
        spawnedProjectiles.Add(projectile);
        projectile.CritChance = 0;
        return projectile;
    }
    private static void ProjectileReaction()
    {
        var npc = Target(3);
        var p = Shot(npc.Center, TagEnum.Hydro);
        Check(p.GetGlobalProjectile<CombatGlobalProjectile>().Profile.Tags.Contains(TagEnum.Hydro), "生成标签丢失");
        int before = npc.life;
        p.Damage();
        Near(before - npc.life, 125);
        Check(resolutions.Single().Outcome?.ReactionSlot == 1, "未提交水雷反应");
        Near(npc.GetGlobalNPC<CombatGlobalNPC>().Elements.Snapshot.ReactionCooldowns[1], 60);
    }
    private static void NestedSpawn()
    {
        var source = new EntitySource_Misc("嵌套生成测试");
        Projectile? raw = null, wrapped = null;
        ElementProbeProjectile.Spawned = (parent, receivedSource) =>
        {
            Check(ReferenceEquals(source, receivedSource), "生成工具替换了来源");
            ElementProbeProjectile.Spawned = null;
            raw = Main.projectile[Projectile.NewProjectile(source, parent.Center, Vector2.Zero,
                parent.type, 100, 0, Main.myPlayer)];
            wrapped = Main.projectile[ElementAttacks.NewProjectile(source, parent.Center, Vector2.Zero,
                parent.type, 100, 0, Tags(TagEnum.Cryo), 2, Main.myPlayer)];
            spawnedProjectiles.Add(raw); spawnedProjectiles.Add(wrapped);
        };
        var p = Main.projectile[ElementAttacks.NewProjectile(source, Main.LocalPlayer.Center + new Vector2(400, -400),
            Vector2.Zero, ModContent.ProjectileType<ElementProbeProjectile>(), 100, 0, Tags(TagEnum.Hydro), 1, Main.myPlayer)];
        spawnedProjectiles.Add(p);
        Check(raw is not null && !raw.GetGlobalProjectile<CombatGlobalProjectile>().Profile.IsManagedAttack, "普通子弹幕错误继承标签");
        Check(wrapped is not null && wrapped.GetGlobalProjectile<CombatGlobalProjectile>().Profile.Tags.Contains(TagEnum.Cryo), "工具子弹幕配置丢失");
        Check(p.GetGlobalProjectile<CombatGlobalProjectile>().Profile.Tags.Contains(TagEnum.Hydro), "父弹幕配置被嵌套覆盖");
        Near(p.GetGlobalProjectile<CombatGlobalProjectile>().ElementAmount, 1);
    }
    private static void OtherCoefficients()
    {
        Main.LocalPlayer.statLifeMax2 = 400;
        Main.LocalPlayer.statLife = 99;
        foreach (var (tag, slot, amount, expected) in new[] { (TagEnum.Anemo, 2, 0.75d, 150), (TagEnum.Pyro, 4, 1.5d, 175) })
        {
            var npc = Target(slot);
            var p = Shot(npc.Center, tag, amount);
            int before = npc.life;
            p.Damage();
            Near(before - npc.life, expected);
            p.active = false;
            npc.active = false;
        }
    }
    private static void WeaponHit()
    {
        var npc = Target(4, 1.5);
        var item = new Item(ItemID.WoodenSword);
        ElementAttacks.ConfigureWeapon(item, Tags(TagEnum.Pyro), 1.5);
        item.GetGlobalItem<CombatGlobalItem>().ElementConsumptionMultiplier = 0.5f;
        var player = Main.LocalPlayer;
        MethodInfo method = typeof(Player).GetMethod("ProcessHitAgainstNPC", BindingFlags.Instance | BindingFlags.NonPublic)!;
        int before = npc.life;
        method.Invoke(player, new object[] { item, npc.Hitbox, 100, 0f, npc.whoAmI });
        Near(before - npc.life, 175);
        var state = npc.GetGlobalNPC<CombatGlobalNPC>().Elements.Snapshot;
        Near(state.Elements[1].Total, 0.75);
        Near(state.Elements[4].Total, 0.75);
        var shot = Shot(npc.Center, TagEnum.Pyro, 1.5);
        Near(shot.GetGlobalProjectile<CombatGlobalProjectile>().ElementConsumptionMultiplier, 1);
    }
    private static void LifePayment()
    {
        Player player = Main.LocalPlayer;
        foreach (var (life, cost, damage) in new[] { (400, 6, 210), (100, 2, 170), (99, 0, 150) })
        {
            player.statLifeMax2 = 400;
            player.statLife = life;
            var npc = Target(2);
            var shot = Shot(npc.Center, TagEnum.Anemo, 0.75);
            int before = npc.life;
            shot.Damage();
            Near(before - npc.life, damage);
            Near(player.statLife, life - cost);
            Near(resolutions.Last().Plan.LifeCost, cost);
            Near(npc.GetGlobalNPC<CombatGlobalNPC>().Elements.Snapshot.ReactionCooldowns[2], 40);
            npc.active = shot.active = false;
        }
        player.statLife = 400;
        var insufficient = Target(2, 1);
        var p = Shot(insufficient.Center, TagEnum.Anemo, 0.75);
        p.Damage();
        Near(player.statLife, 400);
        Check(resolutions.Last().Plan.Route == ElementRoute.AttachOnly, "元素不足仍触发反应");
        p.active = insufficient.active = false;
        var canceled = Target(2);
        canceled.dontTakeDamage = true;
        Shot(canceled.Center, TagEnum.Anemo, 0.75).Damage();
        Near(player.statLife, 400);
        Near(canceled.GetGlobalNPC<CombatGlobalNPC>().Elements.Snapshot.Elements[2].Total, 1.5);
    }

    private static void ConductiveChain()
    {
        Vector2 origin = new(Main.spawnTileX * 16, 1600);
        var first = Target(3);
        var second = Target(0, 0);
        var third = Target(0, 0);
        var stop = Target(0, 0);
        var beyond = Target(0, 0);
        NPC[] nodes = { first, second, third, stop, beyond };
        for (int i = 0; i < nodes.Length; i++) nodes[i].Center = origin + new Vector2(i * 64, 0);
        second.defense = 20;
        third.defense = 40;
        stop.defense = 1000;
        int[] before = nodes.Select(n => n.life).ToArray();
        Shot(first.Center, TagEnum.Hydro).Damage();
        Near(before[0] - first.life, 125);
        Near(before[1] - second.life, 77);
        Near(before[2] - third.life, 41);
        Near(before[3] - stop.life, 0);
        Near(before[4] - beyond.life, 0);
        foreach (NPC node in nodes.Skip(1))
            Check(!node.GetGlobalNPC<CombatGlobalNPC>().Elements.HasState, "电弧附着或增加了 CD");
    }
    private static void ConductiveResistance()
    {
        var registry = new CombatRegistry();
        foreach (var rule in DefaultTagRules.All) registry.Register(rule);
        registry.Register(new ArcTestContributor());
        registry.Freeze();
        var system = ModContent.GetInstance<CalculateSystem>();
        var field = typeof(CalculateSystem).GetField("pipeline", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var previous = field.GetValue(system);
        field.SetValue(system, new CombatPipeline(registry, (_, error) => throw error));
        try
        {
            var first = Target(3);
            var second = Target(0, 0);
            var third = Target(0, 0);
            first.Center = new(Main.spawnTileX * 16, 1600);
            second.Center = first.Center + new Vector2(64, 0);
            third.Center = second.Center + new Vector2(64, 0);
            first.ai[1] = .5f;
            second.ai[1] = .25f;
            int a = first.life, b = second.life, c = third.life;
            Shot(first.Center, TagEnum.Hydro).Damage();
            // 源反应增伤2倍：250；首靶抗性不传递，后续为175×.75、122.5×1。
            Near(a - first.life, 125);
            Near(b - second.life, 131);
            Near(c - third.life, 122);
        }
        finally { field.SetValue(system, previous); }
    }
    private static void ConductiveGeometry()
    {
        var first = Target(3);
        var second = Target(0, 0);
        first.Center = new(Main.spawnTileX * 16 + 8, 1608);
        second.Center = first.Center + new Vector2(81, 0);
        int before = second.life;
        var shot = Shot(first.Center, TagEnum.Hydro);
        shot.Damage();
        Near(before - second.life, 0);
        first.GetGlobalNPC<CombatGlobalNPC>().SetDefaults(first);
        first.GetGlobalNPC<CombatGlobalNPC>().Elements.Attach(new(3, 1.5, 100));
        second.Center = first.Center + new Vector2(64, 0);
        shot.active = false;
        int tileX = (int)first.Center.X / 16 + 2, tileY = (int)first.Center.Y / 16;
        var tile = Main.tile[tileX, tileY];
        bool oldHasTile = tile.HasTile;
        ushort oldType = tile.TileType;
        try
        {
            tile.HasTile = true; tile.TileType = TileID.Stone;
            Shot(first.Center, TagEnum.Hydro).Damage();
            Near(before - second.life, 0);
        }
        finally { tile.HasTile = oldHasTile; tile.TileType = oldType; }
    }
    private static void Cooldown()
    {
        var npc = Target(3, 3);
        var p = Shot(npc.Center, TagEnum.Hydro);
        p.Damage();
        p.localNPCImmunity[npc.whoAmI] = 0;
        npc.immune[Main.myPlayer] = 0;
        int before = npc.life;
        p.Damage();
        Near(before - npc.life, 100);
        Check(resolutions.Last().Plan.Route == ElementRoute.AttachOnly, "CD 未阻止真实反应");
        Near(npc.GetGlobalNPC<CombatGlobalNPC>().Elements.Snapshot.Elements[2].Total, 1);
    }
    private static Player PlayerTarget()
    {
        Player player = Main.LocalPlayer;
        player.GetModPlayer<CombatPlayer>().Initialize();
        player.GetModPlayer<CombatPlayer>().Elements.Attach(new(3, 1.5, 100));
        player.immune = false;
        Array.Clear(player.hurtCooldowns);
        player.creativeGodMode = false;
        player.statDefense = default;
        player.statLifeMax2 = player.statLife = 5000;
        player.dead = false;
        return player;
    }
    private static void PlayerHurt()
    {
        var source = Target(1);
        ElementAttacks.ConfigureNpcContact(source, Tags(TagEnum.Hydro), 1);
        var player = PlayerTarget();
        double damage = player.Hurt(PlayerDeathReason.ByNPC(source.whoAmI), 100, 0, dodgeable: false);
        Check(damage > 0 && resolutions.Last().Outcome?.ReactionSlot == 1, "NPC 伤害路径未提交反应");
        foreach (string mode in new[] { "取消", "闪避", "免疫" })
        {
            player = PlayerTarget();
            ElementProbePlayer.Cancel = mode == "取消";
            ElementProbePlayer.Dodge = mode == "闪避";
            player.immune = mode == "免疫";
            int count = resolutions.Count;
            damage = player.Hurt(PlayerDeathReason.ByNPC(source.whoAmI), 100, 0, dodgeable: true);
            Near(damage, 0);
            Near(player.GetModPlayer<CombatPlayer>().Elements.Snapshot.Elements[3].Total, 1.5);
            Check(resolutions.Skip(count).All(r => !r.Committed), $"{mode}仍提交了状态");
        }
    }
    private static void HostileProjectile()
    {
        var player = PlayerTarget();
        var p = Shot(player.Center, TagEnum.Hydro);
        p.friendly = false;
        p.hostile = true;
        p.Damage();
        Check(resolutions.Any(r => r.Plan.Hit.Kind == Basic.Calculate.Core.HitKind.ProjectileToPlayer && r.Outcome is not null),
            "敌对弹幕未进入玩家反应路径");
    }
    private static void Lethal()
    {
        var npc = Target(3);
        npc.life = 10;
        Shot(npc.Center, TagEnum.Hydro).Damage();
        Check(!npc.active && resolutions.Single().Outcome is not null, "致死命中未记录真实反应");
    }
    private static void Idle()
    {
        var npc = Target(3);
        npc.GetGlobalNPC<CombatGlobalNPC>().Elements.Attach(new(2, 1, 100));
        int before = npc.life;
        ModContent.GetInstance<ElementHitSystem>().PostUpdateEverything();
        Near(before - npc.life, 125);
        Check(resolutions.Any(r => r.Outcome?.Source?.Kind == Basic.Calculate.Core.SourceKind.Environment), "静置归属非环境");
    }
    private static void NativeReentry()
    {
        var npc = Target(3, 2);
        var first = Shot(npc.Center, TagEnum.Hydro);
        var second = Shot(npc.Center, TagEnum.Hydro);
        var target = npc.GetGlobalNPC<CombatGlobalNPC>().Elements;
        ElementProbeNPC.AfterProjectileHit = (source, _) =>
        {
            if (source != first) return;
            Near(target.Snapshot.ReactionCooldowns[1], 0);
            second.Damage();
            Check(resolutions.Count == 0, "重入请求抢先提交");
        };
        int before = npc.life;
        first.Damage();
        Near(before - npc.life, 225);
        Check(resolutions.Count == 2 && resolutions[0].Outcome is not null &&
            resolutions[1].Plan.Route == ElementRoute.AttachOnly, "重入未读取提交后的 CD");
        Near(target.Snapshot.Elements[2].Total, 1);
    }
    private static void NativeFailure()
    {
        var npc = Target(3);
        var p = Shot(npc.Center, TagEnum.Hydro);
        var old = npc.GetGlobalNPC<CombatGlobalNPC>().Elements;
        ElementProbeNPC.AfterProjectileHit = (_, _) => throw new InvalidOperationException("预期的测试异常");
        try { p.Damage(); throw new Exception("原生回调异常未抛出"); }
        catch (InvalidOperationException e) when (e.Message == "预期的测试异常") { }
        Check(!old.IsResolving, "异常后目标仍被占用");
        Near(old.Snapshot.Elements[3].Total, 1.5);
        Near(old.Snapshot.ReactionCooldowns[1], 0);
        ElementProbeNPC.AfterProjectileHit = (_, current) => current.SetDefaults(ModContent.NPCType<ElementProbeNPC>());
        p.localNPCImmunity[npc.whoAmI] = 0;
        npc.immune[Main.myPlayer] = 0;
        p.Damage();
        Check(!ReferenceEquals(old, npc.GetGlobalNPC<CombatGlobalNPC>().Elements), "未模拟实体代次变更");
        Near(npc.GetGlobalNPC<CombatGlobalNPC>().Elements.Snapshot.Elements[2].Total, 0);
        Check(resolutions.All(r => !r.Committed), "异常或实体复用仍提交了状态");
    }
}

[CombatContributor(901001)]
internal sealed class ArcTestContributor : ICombatContributor
{
    public HitKinds SupportedHits => HitKinds.ProjectileToNpc;
    public bool IsActive(in CombatContext context) => context.TargetNpc?.type == ModContent.NPCType<ElementProbeNPC>();
    public void Collect(in CombatContext context, ContributionWriter writer)
    {
        writer.SetEnabled("测试抗性", ModifierTarget.Resistance, true, "隔离测试");
        writer.AddValue("目标抗性", ModifierTarget.Resistance, context.TargetNpc!.ai[1]);
        writer.AddValue("源反应增伤", ModifierTarget.Reaction(1), 1);
    }
}

public sealed class ElementTestCommand : ModCommand
{
    public override CommandType Type => CommandType.Chat;
    public override string Command => "elementtest";
    public override string Usage => "/elementtest";
    public override string Description => "在测试世界中运行元素框架实机验收（会修改测试玩家状态）";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        ElementLiveTests.Run();
        caller.Reply("测试完成，详情见 Logs/AdvancedMod/element-live-tests.json。");
    }
}

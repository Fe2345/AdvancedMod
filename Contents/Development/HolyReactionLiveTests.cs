#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Calculate.Tml;
using AdvancedMod.Basic.Element;
using AdvancedMod.Basic.Element.Tml;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Basic.Utils;
using AdvancedMod.Contents.Hallow.Weapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

/// <summary>隔离世界中的神圣反应真实引擎验收；不把单人结果当作联机证据。</summary>
public sealed class HolyReactionLiveTests : ModSystem
{
    private bool ran;
    private long clockStart;
    private bool clock900;
    private readonly List<object> clockResults = new();
    private static readonly List<NPC> npcs = new();
    private static readonly List<Projectile> projectiles = new();
    private static Player P => Main.LocalPlayer;
    private static Vector2 Origin => new(Main.spawnTileX * 16, 1000);
    public override void PostUpdateEverything()
    {
        if (Main.gameMenu || Main.netMode != NetmodeID.SinglePlayer || Environment.GetEnvironmentVariable("ADVANCEDMOD_HOLY_TEST") != "1") return;
        if (ran) { ClockTest(); return; }
        ran = true; Main.autoSave = false;
        var results = new List<object>();
        var tests = new (string, Action)[] {
            ("正式融化175及导电125", Coefficients), ("神圣汽化斩杀支付与记录", Payment),
            ("未成功命中不创建结界不支付", Cancelled), ("神圣附着跨攻击保留", StoredHoly),
            ("结界移动速度与超距传送", Movement), ("同队伍最高减抗与无队伍隔离", Teams),
            ("仅结界外反应更新移动目标且与主人站位无关", MovementTrigger),
            ("单节点桥接仅衰减一次", Bridge), ("禁止连续两个节点", DoubleBridge),
            ("节点刷新及十节点上限", Nodes), ("同次导电核心最多充能一次", CoreCharge),
            ("天火一次冷却仅触发一次", SkyFire), ("真实伤害保留增伤且忽略防御抗性", TrueDamage),
            ("龙卷两目标往返与孤立目标终止", Tornado), ("龙卷首轮及结束计时", Timers),
            ("任意弦覆盖与交叉不重复伤害", Discharge), ("Level实时读取角色记录", Levels),
            ("龙卷保留100%暴击", Critical),
            ("实际汽化支付实时提高范围内水风伤害", LiveReduction),
            ("减抗半径边界与进出即时判定", ReductionBoundary),
            ("移动保持当前目的地且减抗随中心移动", MovingBoundary),
            ("超距反应提交立即传送并迁移作用范围", ImmediateTeleport),
            ("主动关闭到期及主人死亡立即停止减抗", ClosingBoundary),
            ("生命记录过期即时降低减抗", RecordExpiry),
            ("天裁物品神圣资格及共鸣网络往返", SwordProfile),
            ("天裁真实挥刀触发神圣汽化并展开结界", SwordReaction),
        };
        foreach (var (name, test) in tests)
        {
            try { Reset(); test(); results.Add(new { name, status = "PASS" }); }
            catch (Exception e) { results.Add(new { name, status = "FAIL", error = e.ToString() }); }
            finally { Reset(); }
        }
        clockResults.AddRange(results);
        clockStart = HolyFieldSystem.Tick;
        HolyFieldSystem.RecordLife(Field(), 150);
    }
    private void ClockTest()
    {
        P.statLife = P.statLifeMax2; P.immune = true; P.immuneTime = 60;
        long elapsed = HolyFieldSystem.Tick - clockStart;
        if (!clock900 && elapsed >= 901)
        {
            clock900 = true;
            bool ok = HolyFieldSystem.Fields.TryGetValue(P.whoAmI, out var f) && f.TornadoDue >= clockStart + 1800;
            clockResults.Add(new { name = "真实游戏900刻召唤并结束后重新计时", status = ok ? "PASS" : "FAIL" });
        }
        if (elapsed < 1201) return;
        clockResults.Add(new { name = "真实游戏1200刻结界关闭", status = HolyFieldSystem.Fields.ContainsKey(P.whoAmI) ? "FAIL" : "PASS" });
        string directory = Path.Combine(Main.SavePath, "Logs", "AdvancedMod"); Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "holy-live.json"), JsonSerializer.Serialize(new { mode = "真实TML单人客户端，包含1201刻持续观察；其余边界直接驱动状态更新", utc = DateTimeOffset.UtcNow, results = clockResults }, new JsonSerializerOptions { WriteIndented = true }));
        Main.instance.Exit();
    }
    private static void Reset()
    {
        foreach (var n in npcs) n.active = false;
        foreach (var p in projectiles) p.active = false;
        npcs.Clear(); projectiles.Clear(); HolyFieldSystem.Clear();
        P.statLife = P.statLifeMax2 = 400; P.dead = false; P.team = 0;
    }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    private static void Near(double value, double expected) => Check(Math.Abs(value - expected) < .001, $"实际{value}，预期{expected}");
    private static NPC N(float x = 0, int slot = 0, double amount = 0)
    {
        int index = NPC.NewNPC(new EntitySource_Misc("神圣反应实机"), (int)Origin.X, (int)Origin.Y, ModContent.NPCType<ElementProbeNPC>());
        var npc = Main.npc[index]; npc.Center = Origin + new Vector2(x, 0); npcs.Add(npc);
        if (amount > 0) npc.GetGlobalNPC<CombatGlobalNPC>().Elements.Attach(new(slot, amount, 100));
        return npc;
    }
    private static void Hit(NPC target, TagEnum tag, double amount, bool holy = true, int damage = 100)
    {
        var tags = new List<TagEntry> { new(false, tag) }; if (holy) tags.Add(new(false, TagEnum.Hallow));
        int index = ElementAttacks.NewProjectile(new EntitySource_Misc("神圣反应实机"), target.Center, Vector2.Zero,
            ModContent.ProjectileType<ElementProbeProjectile>(), damage, 0, new TagSet(tags), amount, P.whoAmI);
        var p = Main.projectile[index]; p.CritChance = 0; projectiles.Add(p); p.Damage(); p.active = false;
    }
    private static HolyFieldSystem.Field Field() => HolyFieldSystem.Activate(P, Origin);
    private static void Coefficients()
    {
        var a = N(0, 4, 1.5); Hit(a, TagEnum.Pyro, 1.5, false); Near(a.lifeMax - a.life, 175);
        a.active = false; var b = N(0, 3, 1.5); Hit(b, TagEnum.Hydro, 1, false); Near(b.lifeMax - b.life, 125);
    }
    private static void Payment()
    {
        var npc = N(0, 2, 1.5); npc.life = 10; Hit(npc, TagEnum.Anemo, .75);
        Near(P.statLife, 399); Near(HolyFieldSystem.Fields[P.whoAmI].Life.Total, 1);
    }
    private static int DirectDamage(NPC npc, TagEnum element)
    {
        int before = npc.life; Hit(npc, element, 0, false, 10000); return before - npc.life;
    }
    private static void LiveReduction()
    {
        var a = N(0, 2, 1.5); Hit(a, TagEnum.Anemo, .75); var f = HolyFieldSystem.Fields[P.whoAmI];
        Near(f.Life.Total, 6); Check(f.Center == a.Center, "首次未在反应目标中心展开");
        var probe = N(100); Near(DirectDamage(probe, TagEnum.Hydro), 10012); Near(DirectDamage(probe, TagEnum.Anemo), 10012);
        Near(DirectDamage(probe, TagEnum.Pyro), 10000);
        var b = N(200, 2, 1.5); Hit(b, TagEnum.Anemo, .75);
        Check(ReferenceEquals(f, HolyFieldSystem.Fields[P.whoAmI]), "重复反应重建结界");
        Near(f.Life.Total, 12); Near(DirectDamage(probe, TagEnum.Hydro), 10024);
    }
    private static void ReductionBoundary()
    {
        var f = Field(); HolyFieldSystem.RecordLife(f, 750); var probe = N();
        probe.Center = f.Center + new Vector2(f.Radius, 0); Near(DirectDamage(probe, TagEnum.Hydro), 11500);
        probe.Center += Vector2.UnitX; Near(DirectDamage(probe, TagEnum.Hydro), 10000);
        probe.Center = f.Center; Near(DirectDamage(probe, TagEnum.Anemo), 11500);
        f.RadiusMultiplier = .5f; probe.Center = f.Center + new Vector2(f.Radius + 1, 0);
        Near(DirectDamage(probe, TagEnum.Anemo), 10000);
    }
    private static void MovingBoundary()
    {
        var f = Field(); HolyFieldSystem.RecordLife(f, 750); var probe = N(-f.Radius + 1);
        Near(DirectDamage(probe, TagEnum.Hydro), 11500);
        HolyFieldSystem.Activate(P, Origin + new Vector2(1200, 0)); HolyFieldSystem.Update(f);
        Near(Vector2.Distance(f.Center, Origin), HolyReactionRules.FieldSpeed); Near(DirectDamage(probe, TagEnum.Hydro), 10000);
        HolyFieldSystem.Activate(P, Origin + new Vector2(2400, 0));
        HolyFieldSystem.Activate(P, f.Center);
        Check(f.Destination == Origin + new Vector2(1200, 0), "中途改写当前目的地");
        Check(f.NextDestination == Origin + new Vector2(2400, 0), "内部反应覆盖待执行目的地");
        for (int i = 0; i < 63; i++) HolyFieldSystem.Update(f);
        Check(f.Destination == Origin + new Vector2(2400, 0), "到达后未转向最新外部目标");
        probe.Center = f.Center; Near(DirectDamage(probe, TagEnum.Hydro), 11500);
        Near(f.Life.Total, 750);
    }
    private static void ImmediateTeleport()
    {
        var f = Field(); HolyFieldSystem.RecordLife(f, 750); var old = N();
        var position = Origin + new Vector2(f.Radius * 4 + 1, 0);
        HolyFieldSystem.Activate(P, position);
        Check(f.Center == position, "传送延迟到世界更新");
        Near(DirectDamage(old, TagEnum.Hydro), 10000);
        var current = N(); current.Center = position; Near(DirectDamage(current, TagEnum.Hydro), 11500);
        Near(f.Life.Total, 750);
    }
    private static void ClosingBoundary()
    {
        var probe = N(); var f = Field(); HolyFieldSystem.RecordLife(f, 750); HolyFieldSystem.Close(P);
        Near(DirectDamage(probe, TagEnum.Hydro), 10000); Check(!f.Contains(probe.Center), "关闭后旧实例仍生效");
        f = Field(); HolyFieldSystem.RecordLife(f, 750); f.Expires = HolyFieldSystem.Tick;
        Near(DirectDamage(probe, TagEnum.Hydro), 10000);
        var ordinary = N(300, 3, 1.5); Hit(ordinary, TagEnum.Hydro, 1, false);
        Check(!f.IsActive, "普通反应复活已到期的旧结界");
        var fresh = Field(); Check(!ReferenceEquals(fresh, f), "复用到期结界记录"); Near(fresh.Life.Total, 0);
        HolyFieldSystem.RecordLife(fresh, 750); P.dead = true;
        Near(HolyFieldSystem.Reduction(P, probe), 0); P.dead = false;
    }
    private static void RecordExpiry()
    {
        var f = Field(); f.Life.Add(HolyFieldSystem.Tick - HolyReactionRules.RecordWindow, 750);
        var probe = N(); Near(DirectDamage(probe, TagEnum.Hydro), 10000); Near(f.Life.Total, 0);
        HolyFieldSystem.RecordLife(f, 150); Near(DirectDamage(probe, TagEnum.Hydro), 10300);
    }
    private static void SwordProfile()
    {
        var item = new Item(ModContent.ItemType<HeavensJudgment>()); var sword = (HeavensJudgment)item.ModItem;
        var data = item.GetGlobalItem<CombatGlobalItem>();
        Check(data.Profile.Tags.Contains(TagEnum.Hallow) && !data.Profile.Tags.Contains(TagEnum.Hallowed), "新物品资格标签错误");
        Near(data.ElementAmount, 0);
        foreach (var element in new[] { TagEnum.Hydro, TagEnum.Pyro, TagEnum.Cryo, TagEnum.Anemo, TagEnum.Electro })
        {
            sword.SetResonance(element); Check(data.Profile.Tags.Contains(element) && data.Profile.Tags.Contains(TagEnum.Hallow), "切换共鸣丢失资格");
            Near(data.ElementAmount, HallowActionRules.Attachment);
        }
        using var bytes = new MemoryStream(); sword.NetSend(new BinaryWriter(bytes)); bytes.Position = 0;
        var copy = new Item(ModContent.ItemType<HeavensJudgment>()); ((HeavensJudgment)copy.ModItem).NetReceive(new BinaryReader(bytes));
        Check(copy.GetGlobalItem<CombatGlobalItem>().Profile.Tags.Contains(TagEnum.Hallow) && ((HeavensJudgment)copy.ModItem).Resonance == TagEnum.Electro, "网络往返丢失神圣共鸣");
    }
    private static void SwordReaction()
    {
        int selected = P.selectedItem; Item old = P.inventory[selected]; Vector2 position = P.Center;
        try
        {
            var item = new Item(ModContent.ItemType<HeavensJudgment>()); P.inventory[selected] = item;
            var sword = (HeavensJudgment)item.ModItem; sword.SetResonance(TagEnum.Anemo);
            P.Center = Origin; P.altFunctionUse = 0; P.itemAnimationMax = HallowBladeRules.BaseUseTicks; P.noItems = false;
            Check(!P.CCed, "测试玩家处于控制状态");
            var target = N(140, 2, 1.5); target.damage = 0;
            sword.Shoot(P, new EntitySource_ItemUse_WithAmmo(P, item, 0), P.MountedCenter, Vector2.UnitX, item.shoot, P.GetWeaponDamage(item), item.knockBack);
            var projectile = Main.projectile.First(p => p.active && p.owner == P.whoAmI && p.type == item.shoot); projectiles.Add(projectile);
            var data = projectile.GetGlobalProjectile<CombatGlobalProjectile>();
            Check(data.Profile.Tags.Contains(TagEnum.Hallow) && data.Profile.Tags.Contains(TagEnum.Anemo), "实际挥刀标签错误");
            Near(data.ElementAmount, 1.5);
            var swing = (HeavensJudgmentSwing)projectile.ModProjectile;
            for (int i = 0; i < HallowBladeRules.BaseUseTicks * projectile.MaxUpdates && !HolyFieldSystem.Fields.ContainsKey(P.whoAmI); i++)
            { swing.AI(); projectile.Damage(); }
            Check(HolyFieldSystem.Fields.TryGetValue(P.whoAmI, out var f) && f.Life.Total == 6 && f.Center == target.Center,
                "天裁挥刀未通过神圣汽化创建结界及记录支付");
            Near(P.statLife, 394);
        }
        finally { P.inventory[selected] = old; P.Center = position; P.itemAnimation = P.itemTime = 0; }
    }
    private static void Cancelled()
    {
        var npc = N(0, 2, 1.5); npc.dontTakeDamage = true; Hit(npc, TagEnum.Anemo, .75);
        Near(P.statLife, 400); Check(HolyFieldSystem.Fields.Count == 0, "未命中也创建结界");
    }
    private static void StoredHoly()
    {
        var npc = N(); Hit(npc, TagEnum.Hydro, 1.5); Check(HolyFieldSystem.Fields.Count == 0, "仅附着创建结界");
        Hit(npc, TagEnum.Anemo, .75, false); Check(HolyFieldSystem.Fields.ContainsKey(P.whoAmI), "已附着神圣属性丢失");
    }
    private static void Movement()
    {
        var f = Field(); f.NextDestination = Origin + new Vector2(100, 0); HolyFieldSystem.Update(f);
        Near(Vector2.Distance(f.Center, Origin), HolyReactionRules.FieldSpeed);
        f.NextDestination = Origin + new Vector2(6000, 0); HolyFieldSystem.Update(f); Near(f.Center.X, Origin.X + 6000);
    }
    private static void MovementTrigger()
    {
        var f = Field(); var playerPosition = P.Center;
        try
        {
            P.Center = Origin + new Vector2(f.Radius * 6, 0);
            f.Expires = HolyFieldSystem.Tick + 10;
            HolyFieldSystem.Activate(P, Origin + new Vector2(f.Radius, 0));
            Check(f.NextDestination == Origin && f.Center == Origin, "边界内反应触发移动");
            Check(f.Expires == HolyFieldSystem.Tick + HolyReactionRules.Lifetime, "内部反应未续时");
            P.Center = Origin;
            Vector2 outside = Origin + new Vector2(f.Radius + 1, 0);
            HolyFieldSystem.Activate(P, outside);
            Check(f.NextDestination == outside, "主人在内部时外部反应未触发移动");
        }
        finally { P.Center = playerPosition; }
    }
    private static void Teams()
    {
        var npc = N(); var f = Field(); HolyFieldSystem.RecordLife(f, 150);
        var other = new Player { whoAmI = 254, active = true, team = 1 };
        var g = HolyFieldSystem.Activate(other, Origin); HolyFieldSystem.RecordLife(g, 750);
        Near(HolyFieldSystem.Reduction(P, npc), .03); P.team = 1; Near(HolyFieldSystem.Reduction(P, npc), .15);
        P.team = 2; Near(HolyFieldSystem.Reduction(P, npc), .03);
    }
    private static void Bridge()
    {
        var f = Field(); f.PlaceNode(Origin + new Vector2(50, 0)); var a = N(0, 3, 1.5); var b = N(100);
        Hit(a, TagEnum.Hydro, 1); Near(a.lifeMax - a.life, 125); Near(b.lifeMax - b.life, 87);
    }
    private static void DoubleBridge()
    {
        var f = Field(); f.PlaceNode(Origin + new Vector2(50, 0)); f.PlaceNode(Origin + new Vector2(100, 0));
        var a = N(0, 3, 1.5); var b = N(150); Hit(a, TagEnum.Hydro, 1); Near(b.life, b.lifeMax);
    }
    private static void Nodes()
    {
        var f = Field(); for (int i = 0; i < 10; i++) f.PlaceNode(Origin + new Vector2(i, 0));
        f.PlaceNode(Origin); f.PlaceNode(Origin + new Vector2(10, 0)); Near(f.Nodes.Count, 10);
        Check(f.Nodes.Contains(Origin) && !f.Nodes.Contains(Origin + Vector2.UnitX), "刷新未更新淘汰顺序");
    }
    private static void CoreCharge()
    {
        var f = Field(); var a = N(-50, 3, 1.5); var b = N(50); Hit(a, TagEnum.Hydro, 1);
        Near(f.Charge, 1); Near(b.lifeMax - b.life, 87);
    }
    private static void SkyFire()
    {
        var f = Field(); var a = N(0, 4, 10); Hit(a, TagEnum.Pyro, 1.5);
        int before = a.life; Hit(a, TagEnum.Pyro, 1.5); Near(before - a.life, 100 + HolyFieldSystem.Level(P));
        before = a.life; Hit(a, TagEnum.Pyro, 1.5); Near(before - a.life, 100);
    }
    private static void TrueDamage()
    {
        // 原版>=9999防御进入SuperArmor专用保底分支，并非普通防御公式；此项验收普通高防御穿透。
        var npc = N(); npc.defense = 1000; var f = Field();
        var registry = new CombatRegistry(); foreach (var rule in DefaultTagRules.All) registry.Register(rule);
        registry.Register(new HolyFieldContributor()); registry.Register(new HolyTestContributor()); registry.Register(new HolyTrueDamageRule()); registry.Freeze();
        var system = ModContent.GetInstance<CalculateSystem>(); var member = typeof(CalculateSystem).GetField("pipeline", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var previous = member.GetValue(system); member.SetValue(system, new CombatPipeline(registry, (_, e) => throw e));
        try { HolyFieldSystem.Deal(f, npc, TagEnum.Hydro, TagEnum.HydroAnemo, HolyFieldSystem.TornadoAttack, 100); Near(npc.lifeMax - npc.life, 300); }
        finally { member.SetValue(system, previous); }
    }
    private static void Tornado()
    {
        var f = Field(); var a = N(10); var b = N(40); f.Storm = new(Origin, 3);
        for (int i = 0; i < 90 && f.Storm is not null; i++) HolyFieldSystem.Update(f);
        Check(f.Storm is null && a.life < a.lifeMax && b.life < b.lifeMax, "未按不同目标往返");
        a.active = false; int before = b.life; f.Storm = new(Origin, 3); HolyFieldSystem.Update(f);
        Check(f.Storm is null && b.life == before, "孤立目标仍受弹射伤害");
    }
    private static void Timers()
    {
        var f = Field(); HolyFieldSystem.RecordLife(f, 149); Check(f.TornadoDue is null, "未到阈值已计时");
        HolyFieldSystem.RecordLife(f, 1); Near(f.TornadoDue!.Value - HolyFieldSystem.Tick, 900);
        f.TornadoDue = HolyFieldSystem.Tick; HolyFieldSystem.Update(f);
        Near(f.TornadoDue!.Value - HolyFieldSystem.Tick, 900); Near(f.Life.Total, 150);
    }
    private static void Discharge()
    {
        var f = Field(); var a = N(-200); var b = N(0); var c = N(200);
        a.Center += new Vector2(0, 200); b.Center += new Vector2(0, 200); c.Center += new Vector2(0, 200);
        var chosen = HolyChordSelection.Select(f.Center, f.Radius, new[] { a.Hitbox, b.Hitbox, c.Hitbox });
        Check(chosen.Count == 1 && chosen[0].Targets.Length == 3, "非核心弦未覆盖三目标");
        HolyFieldSystem.Discharge(f); foreach (var n in new[] { a, b, c }) Near(n.lifeMax - n.life, HolyFieldSystem.Level(P));
    }
    private static void Levels()
    {
        int before = HolyFieldSystem.Level(P), consumed = P.ConsumedLifeCrystals;
        P.ConsumedLifeCrystals++; Near(HolyFieldSystem.Level(P), before + 1); P.ConsumedLifeCrystals = consumed;
    }
    private static void Critical()
    {
        int index = NPC.NewNPC(new EntitySource_Misc("神圣暴击实机"), (int)Origin.X, (int)Origin.Y, ModContent.NPCType<HolyCriticalProbeNPC>());
        var npc = Main.npc[index]; npcs.Add(npc); npc.Center = Origin;
        float previous = P.GetCritChance(DamageClass.Generic); P.GetCritChance(DamageClass.Generic) = 100;
        try { HolyFieldSystem.Deal(Field(), npc, TagEnum.Hydro, TagEnum.HydroAnemo, HolyFieldSystem.TornadoAttack, 100); Near(npc.lifeMax - npc.life, 200); }
        finally { P.GetCritChance(DamageClass.Generic) = previous; }
    }
}

public sealed class HolyCriticalProbeNPC : ModNPC
{
    public override string Texture => "Terraria/Images/NPC_1";
    public override void SetDefaults()
    {
        NPC.width = 32; NPC.height = 48; NPC.lifeMax = 100000; NPC.defense = 1000;
        NPC.noGravity = NPC.noTileCollide = true; NPC.aiStyle = -1;
    }
    public override bool CheckActive() => false;
    public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers) => modifiers.DamageVariationScale *= 0;
}

[CombatContributor(901302)]
internal sealed class HolyTestContributor : ICombatContributor
{
    public HitKinds SupportedHits => HitKinds.ProjectileToNpc;
    public bool IsActive(in CombatContext context) => context.Profile.AttackId == HolyFieldSystem.TornadoAttack;
    public void Collect(in CombatContext context, ContributionWriter writer)
    {
        writer.SetEnabled("测试抗性", ModifierTarget.Resistance, true, "实机验收");
        writer.AddValue("测试抗性", ModifierTarget.Resistance, 1);
        writer.AddValue("测试水增伤", ModifierTarget.ElementDamage(2), 1);
        writer.AddValue("测试擢升", ModifierTarget.Ascension(1), .5f);
    }
}

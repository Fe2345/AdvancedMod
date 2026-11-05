#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AdvancedMod.Basic.Calculate.DamageOverTime;
using AdvancedMod.Basic.Calculate.Tml;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Basic.Element.Tml;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

/// <summary>只在隔离测试启动参数开启时直接驱动真实生命恢复方法，不改变日常游戏。</summary>
public sealed class DotLiveTests : ModSystem
{
    internal static bool Enabled => Environment.GetEnvironmentVariable("ADVANCEDMOD_DOT_TEST") == "1";
    private static readonly MethodInfo npcRegen = typeof(NPC).GetMethod("UpdateNPC_BuffApplyDOTs", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private readonly List<NPC> spawned = new();
    private readonly List<object> results = new();
    private sealed record Observed(string text, Color color, bool crit, bool dot);
    private static readonly List<Observed> observed = new();
    private int ticks;
    private NPC? naturalTarget;
    private int naturalLife;
    private static Player P => Main.LocalPlayer;
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static AttackProfile Profile(string id, TagEnum element = TagEnum.Pyro) => new(new TagSet(new[] { new TagEntry(false, element) }), id);
    private static Observed[] Texts => observed.ToArray();
    public override void Load() { if (Enabled) On_CombatText.NewText_Rectangle_Color_int_bool_bool += Observe; }
    public override void Unload() { On_CombatText.NewText_Rectangle_Color_int_bool_bool -= Observe; observed.Clear(); }
    private static int Observe(On_CombatText.orig_NewText_Rectangle_Color_int_bool_bool orig,
        Rectangle bounds, Color color, int damage, bool critical, bool dot)
    {
        observed.Add(new(damage.ToString(), color, critical, dot));
        return orig(bounds, color, damage, critical, dot);
    }
    private static void ClearText() { CombatText.clearAll(); observed.Clear(); }
    private static int Total(Color? color = null) => Texts.Where(t => !color.HasValue || t.color == color.Value).Sum(t => int.Parse(t.text));
    private static void Flush() => ModContent.GetInstance<DotSystem>().PostUpdateEverything();
    private static void RunNpc(NPC npc)
    {
        // 原生 UpdateNPC 在进入此私有方法之前重置这两个每刻字段；测试必须模拟该前置步骤。
        npc.lifeRegen = 0; npc.lifeRegenExpectedLossPerSecond = 0;
        npcRegen.Invoke(npc, null);
    }
    private NPC N()
    {
        int index = NPC.NewNPC(new EntitySource_Misc("持续伤害隔离验收"), (int)P.Center.X + 100, (int)P.Center.Y, ModContent.NPCType<ElementProbeNPC>());
        var npc = Main.npc[index]; spawned.Add(npc); npc.justHit = false; return npc;
    }
    private static void Submit(NPC npc, string id, double damage, TagEnum element = TagEnum.Pyro, double crit = 0, bool separate = true)
    {
        Check(DotService.TrySubmit(CombatContext.ForDamageOverTime(npc, Profile(id, element), P), new(damage, crit), out _, separate), "NPC 提交被拒绝");
    }
    private static void SubmitPlayer(string id, double damage, TagEnum element = TagEnum.Pyro) =>
        Check(DotService.TrySubmit(CombatContext.ForDamageOverTime(P, Profile(id, element)), new(damage), out _), "玩家提交被拒绝");
    private void Reset()
    {
        foreach (var npc in spawned) { npc.GetGlobalNPC<DotNpc>().Current?.Reset(); npc.active = false; }
        spawned.Clear();
        P.GetModPlayer<DotPlayer>().OnRespawn();
        P.ResetEffects(); P.statLife = P.statLifeMax2 = 400; P.dead = false; P.ghost = false;
        P.lifeRegen = P.lifeRegenCount = 0; P.lifeRegenTime = 0;
        P.immune = true; P.immuneTime = 123;
        Main.SceneMetrics.HasCampfire = false; Main.SceneMetrics.HasHeartLantern = false;
        ClearText(); DotHurtProbe.Count = 0;
    }
    private void Test(string name, Action test)
    {
        try { Reset(); test(); results.Add(new { name, status = "PASS" }); }
        catch (Exception e) { results.Add(new { name, status = "FAIL", error = e.ToString() }); }
        finally { Reset(); }
    }
    public override void PostUpdateEverything()
    {
        if (!Enabled || !Main.dedServ && Main.gameMenu) return;
        ticks++;
        if (ticks == 34)
        {
            bool ok = naturalTarget is { active: true } && naturalTarget.life == naturalLife - 10;
            results.Add(new { name = "真实游戏连续四帧仅扣一次十点", status = ok ? "PASS" : "FAIL" });
            Reset(); Finish(); return;
        }
        if (ticks != 30) return;
        int originalMode = Main.netMode, originalPlayer = Main.myPlayer;
        // 无界面测试直接驱动同一原生方法；临时执行单人权威规则，不声称这是联机测试。
        Main.netMode = NetmodeID.SinglePlayer;
        if (Main.dedServ) Main.myPlayer = 0;
        P.active = true;
        Main.autoSave = false;
        Test("NPC两元素分拆与原版余额守恒且不触发普通受击", () =>
        {
            var npc = N(); npc.lifeRegenCount = -6 * 120;
            Submit(npc, "火", 6); Submit(npc, "雷", 4, TagEnum.Electro);
            int before = npc.life; RunNpc(npc); Flush();
            Check(before - npc.life == 16 && Total() == 16, "扣血或数字总和错误");
            Check(Total(ElementTextSystem.ColorFor(TagEnum.Pyro)) == 6 && Total(ElementTextSystem.ColorFor(TagEnum.Electro)) == 4, "元素分配错误");
            Check(Total(CombatText.LifeRegenNegative) == 6 && !npc.justHit, "原版余额或受击状态错误");
            ClearText(); RunNpc(npc); Flush(); Check(Texts.Length == 0 && before - npc.life == 16, "重复注入");
        });
        Test("同来源同帧跨多次结算合并且暴击保留", () =>
        {
            var npc = N(); Submit(npc, "火", 6); RunNpc(npc);
            Submit(npc, "火", 2, crit: 100); RunNpc(npc); Flush();
            Check(Texts.Length == 1 && Texts[0].text == "10" && Texts[0].crit && Texts[0].dot, "同帧合并错误");
        });
        Test("相同元素不同来源不合并", () =>
        {
            var npc = N(); Submit(npc, "火一", 4); Submit(npc, "火二", 6); RunNpc(npc); Flush();
            Check(Texts.Length == 2 && Total() == 10, "不同来源被合并");
        });
        Test("独立显示关闭仍保留原版数字", () =>
        {
            var npc = N(); Submit(npc, "普通", 4, separate: false); Submit(npc, "雷", 6, TagEnum.Electro);
            RunNpc(npc); Flush();
            Check(Total(CombatText.LifeRegenNegative) == 4 && Total(ElementTextSystem.ColorFor(TagEnum.Electro)) == 6, "关闭独立显示分配错误");
        });
        Test("小数与原生两点分组余额跨更新保留", () =>
        {
            var npc = N(); int before = npc.life;
            Submit(npc, "火", .5); RunNpc(npc); Flush();
            Check(Texts.Length == 0 && npc.life == before, "小数提前跳字");
            Submit(npc, "火", .5); RunNpc(npc); Flush();
            Check(Total() == 1 && npc.life == before - 1, "小数累加丢失");
            ClearText(); Submit(npc, "火", 3); RunNpc(npc); Flush();
            Check(Total() == 2, "原生分组被修改");
            ClearText(); RunNpc(npc); Flush(); Check(Total() == 1, "分组尾数丢失");
        });
        Test("不同目标账本隔离与重置清理", () =>
        {
            var a = N(); var b = N(); Submit(a, "火", 8); RunNpc(b); Flush();
            Check(Texts.Length == 0, "其他目标消费了余额");
            a.SetDefaults(ModContent.NPCType<ElementProbeNPC>()); RunNpc(a); Flush();
            Check(Texts.Length == 0, "实体重置保留旧余额");
        });
        Test("多节目标沿原版扣主体生命", () =>
        {
            var root = N(); var part = N(); part.realLife = root.whoAmI;
            int before = root.life; Submit(part, "火", 10); RunNpc(part); Flush();
            Check(root.life == before - 10 && Total() == 10, "主体扣血或文字错误");
        });
        Test("玩家十点一次扣除且不触发OnHurt或改无敌时间", () =>
        {
            SubmitPlayer("火", 6); SubmitPlayer("雷", 4, TagEnum.Electro);
            P.UpdateLifeRegen(); Flush();
            Check(P.statLife == 390 && Total() == 10 && Texts.Length == 2, "玩家伤害或跳字错误");
            Check(DotHurtProbe.Count == 0 && P.immuneTime == 123, "进入普通受伤流程");
            ClearText(); P.lifeRegen = 0; P.UpdateLifeRegen(); Flush(); Check(P.statLife == 390 && Texts.Length == 0, "玩家重复注入");
        });
        Test("窒息五点分组等待后按FIFO分配", () =>
        {
            P.suffocating = true; SubmitPlayer("火", 3); P.UpdateLifeRegen(); Flush();
            Check(P.statLife == 400 && Texts.Length == 0, "窒息未保留分组");
            P.lifeRegen = 0; SubmitPlayer("雷", 2, TagEnum.Electro); P.UpdateLifeRegen(); Flush();
            Check(P.statLife == 395 && Total() == 5 && Total(ElementTextSystem.ColorFor(TagEnum.Pyro)) == 3, "窒息分配错误");
        });
        Test("饥饿沙漠倍伤保留且额外部分为原版数字", () =>
        {
            P.starving = true; P.ZoneDesert = true; SubmitPlayer("火", 8); P.UpdateLifeRegen(); Flush();
            Check(P.statLife == 384 && Total() == 16 && Total(ElementTextSystem.ColorFor(TagEnum.Pyro)) == 8, "饥饿语义错误");
            Check(Total(CombatText.LifeRegen) == 8, "饥饿额外部分未保留原版数字");
            P.ZoneDesert = false;
        });
        Test("NPC客户端与玩家服务器拒绝重复提交", () =>
        {
            var npc = N(); var npcContext = CombatContext.ForDamageOverTime(npc, Profile("火"), P);
            var playerContext = CombatContext.ForDamageOverTime(P, Profile("火"));
            int mode = Main.netMode;
            try
            {
                Main.netMode = NetmodeID.MultiplayerClient;
                Check(!DotService.TrySubmit(npcContext, new(10), out _), "客户端接受NPC扣血");
                Main.netMode = NetmodeID.Server;
                Check(!DotService.TrySubmit(playerContext, new(10), out _), "服务器重复玩家扣血");
            }
            finally { Main.netMode = mode; }
        });
        Test("零伤害不注入不跳字与死亡账本清理", () =>
        {
            var npc = N(); Submit(npc, "火", 0); RunNpc(npc); Flush(); Check(Texts.Length == 0, "零伤害跳字");
            Submit(npc, "火", 10); var state = npc.GetGlobalNPC<DotNpc>().Current!; npc.active = false; Flush();
            Check(!state.Ledger.HasBalance, "失效目标残留账本");
            SubmitPlayer("火", 10); var playerState = P.GetModPlayer<DotPlayer>().State;
            P.GetModPlayer<DotPlayer>().OnRespawn(); Check(!playerState.Ledger.HasBalance, "复活残留账本");
        });
        naturalTarget = N(); naturalLife = naturalTarget.life; Submit(naturalTarget, "真实循环", 10);
        Main.netMode = originalMode; Main.myPlayer = originalPlayer;
    }

    private void Finish()
    {
        string path = Path.Combine(Main.SavePath, "Logs", "AdvancedMod", "dot-live.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new { mode = Main.dedServ ? "无界面TML内临时单人上下文，直接驱动真实引擎恢复方法；无视觉及联机证据" : "真实TML单人客户端直接驱动恢复方法；不代表联机验证", utc = DateTimeOffset.UtcNow, results }, new JsonSerializerOptions { WriteIndented = true }));
        if (Main.dedServ) Environment.Exit(0);
        else Main.instance.Exit();
    }
}

public sealed class DotHurtProbe : ModPlayer
{
    internal static int Count;
    public override void OnHurt(Player.HurtInfo info) { if (DotLiveTests.Enabled) Count++; }
}

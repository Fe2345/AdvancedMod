using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using AdvancedMod.Contents.Hallow.Weapons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AdvancedMod.Contents.Development;

/// <summary>仅隔离存档启用；原生ItemCheck驱动右键，固定湿润状态与脚下方块以验收动作及伤害入口。</summary>
public sealed class HallowActionsLiveTests : ModSystem
{
    internal static bool Enabled => Environment.GetEnvironmentVariable("ADVANCEDMOD_HALLOW_ACTIONS_TEST") == "1";
    internal static bool Running;
    private static readonly List<object> results = new();
    private static readonly HashSet<string> captures = new();
    private static readonly List<Projectile> counters = new();
    private static readonly List<int> counterHits = new();
    private static Vector2 anchor;
    private static int phase, tick, total, successTick, injectedAge, sourceDamage;
    private static ulong guardStarted;
    private static Projectile incoming;
    private static NPC target;
    private static bool observed, injected;
    private static string pendingCapture;
    private static TagEnum? previous;
    private static readonly TagEnum[] elements = { TagEnum.Hydro, TagEnum.Pyro, TagEnum.Cryo, TagEnum.Electro };
    private static Player P => Main.LocalPlayer;
    private static HeavensJudgment Sword => (HeavensJudgment)P.HeldItem.ModItem;
    private static void Check(string name, bool pass, object actual = null) => results.Add(new { name, status = pass ? "PASS" : "FAIL", actual });
    public override void Load() { if (Enabled && !Main.dedServ) On_Main.Draw += Capture; }
    public override void Unload() { On_Main.Draw -= Capture; Running = false; }
    public override void ModifyScreenPosition()
    {
        if (Running) Main.screenPosition.Y -= 120; // 给最大展开的上方虚影留出截图空间。
    }
    internal static void Start()
    {
        if (!Enabled || Running || Main.netMode != NetmodeID.SinglePlayer) return;
        HallowShaderLiveTests.Run();
        Main.autoSave = false; Running = true; phase = tick = total = 0;
        results.Clear(); captures.Clear(); counters.Clear(); counterHits.Clear();
        P.inventory[0] = new Item(ModContent.ItemType<HeavensJudgment>()); P.selectedItem = 0;
        anchor = P.Center - new Vector2(0, 200); previous = null;
        foreach (var npc in Main.ActiveNPCs) if (!npc.townNPC) npc.active = false;
    }
    private static NPC Target(Vector2 offset)
    {
        int id = NPC.NewNPC(P.GetSource_Misc("HallowActionsTest"), (int)anchor.X, (int)anchor.Y, ModContent.NPCType<ElementProbeNPC>());
        var npc = Main.npc[id]; npc.Center = anchor + offset; npc.damage = 0; return npc;
    }
    internal static void Drive(Player p)
    {
        if (!Running || p.whoAmI != Main.myPlayer) return;
        Main.hideUI = true; Main.dayTime = true; Main.time = 27000;
        p.Center = anchor; p.velocity = Vector2.Zero; p.selectedItem = 0;
        p.statLife = p.statLifeMax2 = 5000; p.statDefense = default;
        p.GetDamage(DamageClass.Generic) = p.GetDamage(DamageClass.Melee) = StatModifier.Default;
        p.GetAttackSpeed(DamageClass.Generic) = p.GetAttackSpeed(DamageClass.Melee) = 1;
        // PreItemCheck位于原生右键转译之后，传入其已转译的altFunctionUse=1与controlUseItem状态。
        p.altFunctionUse = p.itemAnimation == 0 ? 1 : 2; p.controlUseItem = true; p.controlUseTile = true; p.releaseUseTile = true; p.releaseUseItem = true;
        Main.mouseRight = true; Main.mouseRightRelease = true;
        Main.mouseX = (int)(anchor.X + 300 - Main.screenPosition.X); Main.mouseY = (int)(anchor.Y - Main.screenPosition.Y);
        p.wet = phase == 0 || phase == 1; p.lavaWet = phase == 1; p.honeyWet = p.shimmerWet = false;
        int y = (int)((p.Bottom.Y + 2) / 16);
        for (int x = (int)(p.Left.X / 16); x <= (int)(p.Right.X / 16); x++)
        {
            var tile = Framing.GetTileSafely(x, y);
            tile.HasTile = phase is 2 or 3;
            if (tile.HasTile) tile.TileType = phase == 2 ? TileID.IceBlock : TileID.Cloud;
        }
        if (phase < 4)
        {
            // 共鸣结束后发起一次真实挥刀，采集共鸣后的剑气颜色。
            if (tick >= 64) { p.altFunctionUse = 0; p.controlUseTile = false; p.controlUseItem = tick == 65; Main.mouseRight = false; }
            return;
        }
        if (phase >= 12) { p.controlUseTile = false; return; }
        if (tick == 0)
        {
            // 独立用例重置玩家冷却；另在用例内单独验证完整360刻门槛，不把这个重置当玩法。
            p.GetModPlayer<HallowGuardPlayer>().OnEnterWorld(); p.itemTime = p.itemAnimation = 0;
            Sword.SetResonance(phase % 2 == 0 ? null : elements[(phase - 4) % 4]);
            Sword.Combo.Complete(0, 1.2f, Main.GameUpdateCount);
            observed = injected = false; successTick = -1; counters.Clear(); counterHits.Clear(); incoming = null;
            if (target != null) target.active = false;
            target = phase == 8 ? null : Target(phase == 9 ? new(-220, 0) : phase == 10 ? new(540, 0) : new(240, 0));
        }
        var guard = Main.projectile.FirstOrDefault(x => x.active && x.ModProjectile is HallowGuard && x.owner == p.whoAmI)?.ModProjectile as HallowGuard;
        int desiredAge = phase == 5 ? 19 : phase == 6 ? 20 : phase == 7 ? 30 : 5;
        if (guard != null && !injected && guard.Age >= desiredAge)
        {
            injected = true; injectedAge = guard.Age;
            Vector2 point = guard.Grip + new Vector2(0, guard.Radius / 2);
            if (phase == 11) { target.Center = point; target.damage = 100; }
            else
            {
                int id = Projectile.NewProjectile(p.GetSource_Misc("HallowParryTest"), point, new Vector2(0, 2), ModContent.ProjectileType<ElementProbeProjectile>(), 100, 0, p.whoAmI);
                incoming = Main.projectile[id]; incoming.hostile = true; incoming.friendly = false; incoming.timeLeft = 300;
            }
            sourceDamage = (int)Hurt(phase == 11 ? PlayerDeathReason.ByNPC(target.whoAmI) : PlayerDeathReason.ByProjectile(incoming.owner, incoming.whoAmI));
            P.immune = false; P.immuneTime = 0; Array.Clear(P.hurtCooldowns);
        }
        if (observed) { p.controlUseItem = false; p.controlUseTile = false; Main.mouseRight = false; }
    }
    public override void PostUpdateEverything()
    {
        if (!Running) return;
        total++;
        if (total > 2200) { Check("完整运行", false, new { phase, tick }); Finish(); return; }
        if (phase < 4)
        {
            var action = Main.projectile.FirstOrDefault(x => x.active && x.ModProjectile is HallowResonance)?.ModProjectile as HallowResonance;
            if (action != null)
            {
                if (action.Age == 29) { Check($"{elements[phase]}提交前保持原元素", Sword.Resonance == previous); pendingCapture = $"resonance-{phase}-before"; }
                if (action.Age == 30)
                {
                    Check($"{elements[phase]}第30刻提交且六虚影无伤害", Sword.Resonance == elements[phase] && action.Committed && action.CanDamage() == false);
                    pendingCapture = $"resonance-{phase}-after";
                }
            }
            var swing = Main.projectile.FirstOrDefault(x => x.active && x.ModProjectile is HeavensJudgmentSwing);
            if (swing?.ModProjectile is HeavensJudgmentSwing blade && blade.Progress > 0.45f && blade.Progress < 0.55f)
            {
                var combat = swing.GetGlobalProjectile<CombatGlobalProjectile>();
                Check($"{elements[phase]}普通攻击附着量", combat.ElementAmount == 1.5);
                pendingCapture = $"slash-{phase}";
            }
            if (++tick >= 115)
            {
                Check($"{elements[phase]}共鸣完成", Sword.Resonance == elements[phase]);
                var saved = ItemIO.Load(ItemIO.Save(P.HeldItem));
                Check($"{elements[phase]}存档与克隆保存元素", ((HeavensJudgment)saved.ModItem).Resonance == Sword.Resonance && ((HeavensJudgment)P.HeldItem.Clone().ModItem).Resonance == Sword.Resonance);
                using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); Sword.NetSend(writer); stream.Position = 0;
                var copy = (HeavensJudgment)new Item(ModContent.ItemType<HeavensJudgment>()).ModItem;
                using var reader = new BinaryReader(stream); copy.NetReceive(reader);
                Check($"{elements[phase]}网络元素往返", copy.Resonance == Sword.Resonance);
                previous = Sword.Resonance; Next();
            }
            return;
        }
        if (phase >= 12) { Finish(); return; }
        var state = P.GetModPlayer<HallowGuardPlayer>();
        if (!observed && state.SuccessCount > 0)
        {
            observed = true; successTick = tick;
            bool perfect = injectedAge < 20;
            Check($"场景{phase}窗口与一次成功", state.LastPerfect == perfect && state.SuccessCount == 1, new { injectedAge, state.LastPerfect });
            Check($"场景{phase}格挡结束且无直接伤害", !Main.projectile.Any(x => x.active && x.ModProjectile is HallowGuard), state.Cooldown);
            bool expectedCounter = perfect && phase != 8 && phase != 9 && phase != 10;
            Check($"场景{phase}反击双剑触发", counters.Count == (expectedCounter ? 2 : 0), counters.Count);
            if (perfect) Check($"场景{phase}保持下一段", Sword.Combo.NextStage == 1);
            if (incoming != null) { incoming.Center = anchor + new Vector2(1000, 0); incoming.velocity = Vector2.Zero; }
            if (target != null && phase == 11) target.Center = anchor + new Vector2(240, 0);
            CheckHurt(perfect ? 0 : sourceDamage / 2, $"场景{phase}招架减伤与无敌帧");
            var other = Target(new(1000, 1000));
            Check("其他伤害源不受保护", Hurt(PlayerDeathReason.ByNPC(other.whoAmI)) == 100); other.active = false;
            pendingCapture = $"counter-{phase}";
        }
        if (observed && tick == successTick + 59) CheckHurt(state.LastPerfect ? 0 : sourceDamage / 2, $"场景{phase}第59刻仍保护");
        if (observed && tick == successTick + 60) CheckHurt(sourceDamage, $"场景{phase}第60刻保护结束", false);
        if (phase == 4 && Main.GameUpdateCount - guardStarted == 359) Check("冷却359刻尚未结束", !state.CanStart && state.Cooldown == 1, state.Cooldown);
        if (phase == 4 && Main.GameUpdateCount - guardStarted == 360) Check("冷却360刻可重新起手", state.CanStart && state.Cooldown == 0, state.Cooldown);
        if (++tick >= (phase == 4 ? 365 : 100))
        {
            Check($"场景{phase}原生右键触发成功", observed);
            if (phase != 4) Check($"场景{phase}360刻冷却未提前结束", !state.CanStart && state.Cooldown > 250);
            if (counters.Count > 0) Check($"场景{phase}反击实际命中1.0倍率", counterHits.Count == 2 && counterHits.All(d => d == 135), counterHits.ToArray());
            Next();
        }
    }
    private static double Hurt(PlayerDeathReason reason)
    {
        P.immune = false; P.immuneTime = 0; Array.Clear(P.hurtCooldowns); P.statLife = 5000;
        return P.Hurt(reason, 100, 0, dodgeable: false);
    }
    private static void CheckHurt(int expected, string name, bool noImmunity = true)
    {
        var reason = phase == 11 ? PlayerDeathReason.ByNPC(target.whoAmI) : PlayerDeathReason.ByProjectile(incoming.owner, incoming.whoAmI);
        double actual = Hurt(reason);
        Check(name, actual == expected && (!noImmunity || !P.immune && P.immuneTime == 0 && P.hurtCooldowns.All(x => x == 0)), new { actual, P.immune, P.immuneTime });
    }
    internal static void Spawn(Projectile projectile)
    {
        if (Running && projectile.ModProjectile is HallowGuard) guardStarted = Main.GameUpdateCount;
        if (!Running || projectile.ModProjectile is not HallowCounterSword) return;
        counters.Add(projectile);
        var combat = projectile.GetGlobalProjectile<CombatGlobalProjectile>();
        // 起手瞄准会包含原生发射点修正，验证相对锁定方向的垂线而非假定屏幕竖直。
        var guard = Main.projectile.First(x => x.active && x.ModProjectile is HallowGuard).ModProjectile as HallowGuard;
        Vector2 offset = projectile.Center - P.Center;
        Check($"场景{phase}反击偏移及白值附着", Math.Abs(offset.Length() - 192) < 0.01f && Math.Abs(Vector2.Dot(offset, guard.Direction)) < 0.01f && projectile.damage == 135 && combat.ElementAmount == 0 && HallowActionRules.Decode((int)projectile.ai[0]) == Sword.Resonance,
            new { offset.X, offset.Y, projectile.damage, combat.ElementAmount, element = projectile.ai[0] });
    }
    internal static void Hit(Projectile projectile, int damage)
    { if (Running && projectile.ModProjectile is HallowCounterSword) counterHits.Add(damage); }
    private static void Next()
    {
        foreach (var projectile in Main.ActiveProjectiles) if (projectile.owner == P.whoAmI) projectile.Kill();
        phase++; tick = 0; P.itemTime = P.itemAnimation = 0;
    }
    private static void Finish()
    {
        Running = false; if (target != null) target.active = false;
        string directory = Path.Combine(Main.SavePath, "HallowActionsCaptures"); Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "report.json"), JsonSerializer.Serialize(new { utc = DateTimeOffset.UtcNow, results, captures }, new JsonSerializerOptions { WriteIndented = true }));
        Main.instance.Exit();
    }
    private void Capture(On_Main.orig_Draw orig, Main self, GameTime time)
    {
        orig(self, time);
        if (pendingCapture == null) return;
        string name = pendingCapture; pendingCapture = null;
        try
        {
            var device = self.GraphicsDevice; int width = device.PresentationParameters.BackBufferWidth, height = device.PresentationParameters.BackBufferHeight;
            var data = new Color[width * height]; device.GetBackBufferData(data);
            using var texture = new Texture2D(device, width, height); texture.SetData(data);
            string directory = Path.Combine(Main.SavePath, "HallowActionsCaptures"); Directory.CreateDirectory(directory);
            using var output = File.Create(Path.Combine(directory, name + ".png")); texture.SaveAsPng(output, width, height); captures.Add(name);
        }
        catch (Exception e) { Check("截图" + name, false, e.Message); }
    }
}
public sealed class HallowActionsTestPlayer : ModPlayer
{
    public override void OnEnterWorld() { if (HallowActionsLiveTests.Enabled) Main.QueueMainThreadAction(HallowActionsLiveTests.Start); }
    public override bool PreItemCheck() { HallowActionsLiveTests.Drive(Player); return true; }
}
public sealed class HallowActionsTestProjectile : GlobalProjectile
{
    public override void OnSpawn(Projectile projectile, IEntitySource source) => HallowActionsLiveTests.Spawn(projectile);
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone) => HallowActionsLiveTests.Hit(projectile, damageDone);
}

using System;
using System.Linq;
using System.IO;
using AdvancedMod.Basic.Calculate.Diagnostics;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Tag;

// 直接编译纯计算源文件，不加载 Terraria；失败时进程返回非零状态。
var cases = new (string Name, Action Run)[]
{
    ("JSONL 往返保留身份标签和原生计划", () =>
    {
        var collector = NewCollector();
        collector.Collect(10, 0, w =>
        {
            w.AddSource(new(SourceKind.Item, "测试:物品", PlayerIndex: 1));
            Add(w, "反应", ModifierTarget.Reaction(0), 0.5f);
            w.AddNative("原生", NativeField.SourceDamage, NumericOperation.AddBase, 5f);
            w.SetCriticalStrike("暴击", CriticalStrikePolicy.Force);
        });
        var hit = Hit() with { Tags = new TagSet(new[] { new TagEntry(true, TagEnum.Pyro) }) };
        var input = collector.Seal(hit);
        var trace = CombatTrace.Create(Guid.NewGuid(), input, DamageCombiner.Evaluate(input, true));
        var text = new StringWriter();
        using (var writer = new CombatTraceWriter(text, leaveOpen: true)) writer.Write(trace);
        string json = text.ToString();
        Check(json.Count(c => c == '\n') == 1, "单条 Trace 被拆成多行");
        var decoded = CombatTraceJson.Deserialize(json);
        Check(decoded.Input.Hit.Tags[0].cannotBeCovered, "标签覆盖标志丢失");
        Check(CombatTraceReplay.Replay(decoded).Matches, "JSONL 往返重放不一致");
        if (args.Length == 2 && args[0] == "--write-trace") File.WriteAllText(args[1], json);
        var changed = decoded with { Expected = decoded.Expected with { ReactionFactor = 2f } };
        Check(!CombatTraceReplay.Replay(changed).Matches, "无法发现倍率被篡改");
        var changedGates = decoded.Input.Gates.Select(g => g with { Enabled = false }).ToArray();
        Check(!CombatTraceReplay.Replay(decoded with { Input = decoded.Input with { Gates = changedGates } }).Matches, "无法发现启停被篡改");
        Throws<NotSupportedException>(() => CombatTraceReplay.Replay(decoded with { SchemaVersion = 999 }));
    }),
    ("Trace 筛选按维度组合且默认关闭", () =>
    {
        var hit = Hit() with { AttackId = "测试:攻击", Attacker = new(SourceKind.Player, "Terraria:Player", 1), Target = new(SourceKind.Npc, "测试:目标") };
        Check(!CombatTraceOptions.Disabled.Matches(hit), "默认追踪没有关闭");
        var options = new CombatTraceOptions(true, new[] { "测试:攻击", "测试:其他" }, new[] { 1 }, new[] { "测试:目标" });
        Check(options.Matches(hit), "筛选命中丢失");
        Check(!options.Matches(hit with { Attacker = new(SourceKind.Player, "Terraria:Player", 2) }), "玩家筛选未生效");
        Check(!options.Matches(hit with { Target = new(SourceKind.Npc, "测试:其他目标") }), "目标筛选未生效");
    }),
    ("原生字段默认启用且保持独立分量", () =>
    {
        var collector = NewCollector();
        collector.Collect(10, 0, w =>
        {
            w.AddNative("白值", NativeField.SourceDamage, NumericOperation.AddBase, 5f);
            w.AddNative("百分比", NativeField.SourceDamage, NumericOperation.AddPercent, 0.2f);
            w.AddNative("倍率", NativeField.SourceDamage, NumericOperation.Multiply, 1.5f);
            w.AddNative("尾数", NativeField.SourceDamage, NumericOperation.AddFlat, 3f);
        });
        var result = DamageCombiner.Evaluate(collector.Seal(Hit()), true);
        var stat = result.Plan.Fields.Single(f => f.Field == NativeField.SourceDamage).Stat!;
        Equal(5f, stat.Base); Equal(0.2f, stat.AdditiveBonus); Equal(1.5f, stat.Multiplicative); Equal(3f, stat.Flat);
        Check(result.Decisions.All(d => d.Disposition == ContributionDisposition.Accepted), "原生字段未默认启用");
    }),
    ("原生控制最后覆盖及更严格上限", () =>
    {
        var collector = NewCollector();
        collector.Collect(10, 0, w => { w.SetCriticalStrike("强制", CriticalStrikePolicy.Force); w.SetMaximumDamage("上限", 50); });
        collector.Collect(20, 1, w => { w.SetCriticalStrike("禁暴", CriticalStrikePolicy.Disable); w.SetMaximumDamage("宽上限", 100); });
        var result = DamageCombiner.Evaluate(collector.Seal(Hit()), true);
        Check(result.Plan.Fields.Single(f => f.Field == NativeField.CriticalStrike).ControlValue == (int)CriticalStrikePolicy.Disable, "暴击策略未最后覆盖");
        Check(result.Plan.Fields.Single(f => f.Field == NativeField.MaximumDamage).ControlValue == 50, "上限没有取更严格值");
        Check(result.Decisions.Any(d => d.Disposition == ContributionDisposition.Overridden) && result.Decisions.Any(d => d.Disposition == ContributionDisposition.LessStrictCap), "缺少覆盖诊断");
    }),
    ("后置关闭原生字段与方向能力检查", () =>
    {
        var collector = NewCollector();
        collector.Collect(10, 0, w => w.AddNative("穿甲", NativeField.ArmorPenetration, NumericOperation.Add, 10f));
        collector.RunPostRule("测试:禁用", w => w.SetEnabled("穿甲", ModifierTarget.Native(NativeField.ArmorPenetration), false, "后置关闭"));
        var result = DamageCombiner.Evaluate(collector.Seal(Hit()), true);
        Equal(0f, result.Plan.Fields.Single(f => f.Field == NativeField.ArmorPenetration).NumericValue!.Value);
        Check(result.Decisions.Single().Disposition == ContributionDisposition.TargetDisabled, "关闭原生字段未排除候选");
        Throws<ArgumentException>(() => new HitProposalCollector(Array.Empty<string>(), HitKind.NpcToPlayer)
            .Collect(10, 0, w => w.SetCriticalStrike("非法", CriticalStrikePolicy.Force)));
        Throws<ArgumentException>(() => NewCollector().Collect(10, 0,
            w => w.AddNative("非法", NativeField.ArmorPenetration, NumericOperation.Multiply, 2f)));
    }),
    ("标签规则组合条件与路径筛选", () =>
    {
        var rule = new TagGateRule("测试:组合", ModifierTarget.Reaction(0), true,
            new[] { TagEnum.Pyro }, new[] { TagEnum.Hydro, TagEnum.Electro },
            new[] { TagEnum.Cryo }, HitKinds.ItemToNpc);
        var hit = Hit() with { Tags = new TagSet(new[] { new TagEntry(false, TagEnum.Pyro), new TagEntry(true, TagEnum.Hydro) }) };
        Check(rule.Matches(hit), "满足组合条件时没有匹配");
        Check(!rule.Matches(hit with { Kind = HitKind.ProjectileToNpc }), "规则忽略了命中路径");
        Check(!rule.Matches(hit with { Tags = new TagSet(hit.Tags.Append(new(false, TagEnum.Cryo))) }), "排除标签没有生效");
        Check(!rule.Matches(Hit()), "空标签错误匹配了条件");
        var unconstrained = new TagGateRule("测试:空条件", ModifierTarget.Reaction(0), true,
            Array.Empty<TagEnum>(), Array.Empty<TagEnum>(), Array.Empty<TagEnum>());
        Check(unconstrained.Matches(Hit()), "空 Any 被错误地当作永不匹配");
    }),
    ("后置规则读取身份来源且历史快照隔离", () =>
    {
        var collector = NewCollector();
        collector.Collect(10, 0, w =>
        {
            w.AddSource(new(SourceKind.Npc, "测试:Boss", EntityIndex: 4));
            w.SetEnabled("在场", ModifierTarget.Ascension(0), true, "只提交决定");
        });
        RuleContext before = collector.CreateRuleView(Hit());
        Check(before.HasContributor(10) && before.GetSources(10).Single().ContentId == "测试:Boss", "缺少规则型贡献者的身份和来源");
        Check(!before.HasContributor(20) && before.GetSources(20).Count == 0, "未知身份被误报为激活");
        collector.RunPostRule("测试:后置", w => w.SetEnabled("禁用", ModifierTarget.Ascension(0), false, "Boss 存在"));
        RuleContext after = collector.CreateRuleView(Hit());
        Check(before.Gates.Count == 1 && after.Gates.Count == 2, "规则视图不是独立快照");
        Equal(1f, DamageCombiner.Evaluate(collector.Seal(Hit())).AfterDefenseFactor);
    }),
    ("标签重复与输入快照", () =>
    {
        var entries = new[] { new TagEntry(true, TagEnum.Pyro) };
        var tags = new TagSet(entries);
        entries[0] = new(false, TagEnum.Hydro);
        Check(tags.TryGet(TagEnum.Pyro, out var found) && found.cannotBeCovered, "输入数组污染了快照");
        Check(!tags.Contains(TagEnum.Hydro), "快照出现了后来加入的标签");
        Throws<ArgumentException>(() => new TagSet(new[] { new TagEntry(true, TagEnum.Pyro), new TagEntry(false, TagEnum.Pyro) }));
        Throws<ArgumentOutOfRangeException>(() => new TagEntry(false, (TagEnum)999));
    }),
    ("默认关闭且诊断不改变结果", () =>
    {
        var collector = NewCollector();
        collector.Collect(10, 0, w => w.AddValue("增幅", ModifierTarget.Reaction(0), 0.5f));
        var input = collector.Seal(Hit());
        var result = DamageCombiner.Evaluate(input, true);
        Equal(1f, result.ReactionFactor);
        Check(result.Decisions.Single().Disposition == ContributionDisposition.TargetDisabled, "关闭槽未排除候选");
        Check(input.Contributions.Count == 1, "关闭槽丢失了候选");
        var quiet = DamageCombiner.Evaluate(input);
        Check(result.Targets.SequenceEqual(quiet.Targets), "诊断改变了计算");
        Check(!quiet.IncludesDiagnostics && quiet.Decisions.Count == 0, "关闭诊断仍生成明细");
    }),
    ("反应增幅累加及空槽单位倍率", () =>
    {
        var collector = NewCollector();
        collector.Collect(10, 0, w => w.AddValue("第一项", ModifierTarget.Reaction(0), 0.2f));
        collector.Collect(20, 1, w =>
        {
            w.AddValue("第二项", ModifierTarget.Reaction(0), 0.3f);
            w.SetEnabled("启用", ModifierTarget.Reaction(0), true, "验证晚到的启用决定");
            w.SetEnabled("空槽", ModifierTarget.Reaction(9), true, "无贡献时倍率为一");
        });
        var result = DamageCombiner.Evaluate(collector.Seal(Hit()));
        Equal(1.5f, result.ReactionFactor);
        Equal(1f, result.Targets.Single(t => t.Target == ModifierTarget.Reaction(9)).Factor);
    }),
    ("Boss 后置决定关闭既有饰品贡献", () =>
    {
        var collector = NewCollector();
        collector.Collect(10, 0, w =>
        {
            w.AddSource(new(SourceKind.Item, "测试:饰品", PlayerIndex: 0, InventorySlot: 3));
            w.AddValue("擢升", ModifierTarget.Ascension(0), 0.2f);
            w.SetEnabled("启用", ModifierTarget.Ascension(0), true, "饰品启用");
        });
        collector.Collect(20, 1, w => w.SetEnabled("在场", ModifierTarget.Ascension(0), true, "只提交规则的贡献者"));
        collector.RunPostRule("测试:Boss", w =>
        {
            for (int i = 0; i < DamageConstants.AscensionSlotCount; i++)
                w.SetEnabled($"关闭{i}", ModifierTarget.Ascension(i), false, "Boss 在场");
        });
        var input = collector.Seal(Hit());
        var result = DamageCombiner.Evaluate(input, true);
        Equal(1f, result.AfterDefenseFactor);
        Check(input.Contributors.Count == 2 && input.Contributors[0].Sources.Count == 1, "贡献者来源丢失");
        var decision = result.Decisions.Single();
        Check(decision.Disposition == ContributionDisposition.TargetDisabled && decision.GateSequence.HasValue, "未追踪后置关闭原因");
    }),
    ("标签决定之后普通决定最后生效", () =>
    {
        var collector = NewCollector();
        collector.RunTagRule("测试:基础", w => w.SetEnabled("基础", ModifierTarget.ElementDamage(0), true, "测试用基础决定"));
        collector.Collect(10, 0, w =>
        {
            w.AddValue("增伤", ModifierTarget.ElementDamage(0), 0.1f);
            w.SetEnabled("关闭", ModifierTarget.ElementDamage(0), false, "先关闭");
            w.SetEnabled("开启", ModifierTarget.ElementDamage(0), true, "后开启");
        });
        Equal(1.1f, DamageCombiner.Evaluate(collector.Seal(Hit())).AfterDefenseFactor);
    }),
    ("防御前后独立及第十槽参与", () =>
    {
        var collector = NewCollector();
        collector.Collect(10, 0, w =>
        {
            Add(w, "反应", ModifierTarget.Reaction(0), 0.5f);
            Add(w, "抗性", ModifierTarget.Resistance, 0.15f);
            Add(w, "元素", ModifierTarget.ElementDamage(9), 0.2f);
            Add(w, "擢升甲", ModifierTarget.Ascension(0), 0.1f);
            Add(w, "擢升乙", ModifierTarget.Ascension(1), 0.1f);
        });
        var result = DamageCombiner.Evaluate(collector.Seal(Hit()));
        Equal(1.5f, result.ReactionFactor);
        Equal(0.85f, result.ResistanceFactor);
        Near(1.452f, result.AfterDefenseFactor);
    }),
    ("负抗性零倍率和负倍率不钳制", () =>
    {
        foreach (var pair in new[] { (-0.15f, 1.15f), (1f, 0f), (1.2f, -0.2f) })
        {
            var collector = NewCollector();
            collector.Collect(10, 0, w => Add(w, "抗性", ModifierTarget.Resistance, pair.Item1));
            Near(pair.Item2, DamageCombiner.Evaluate(collector.Seal(Hit())).ResistanceFactor);
        }
    }),
    ("精通先求和再代公式", () =>
    {
        var collector = NewCollector();
        collector.Collect(10, 0, w =>
        {
            Add(w, "精通甲", ModifierTarget.Mastery, 500f);
            w.AddValue("精通乙", ModifierTarget.Mastery, 500f);
        });
        Equal(3f, DamageCombiner.Evaluate(collector.Seal(Hit())).AfterDefenseFactor);
        collector = NewCollector();
        collector.Collect(10, 0, w => Add(w, "负精通", ModifierTarget.Mastery, -500f));
        Equal(0.5f, DamageCombiner.Evaluate(collector.Seal(Hit())).AfterDefenseFactor);
    }),
    ("备用键按稳定顺序折叠", () =>
    {
        var collector = new HitProposalCollector(new[] { "测试:Z", "测试:A" });
        collector.Collect(10, 0, w =>
        {
            Add(w, "乙", ModifierTarget.Reserve("测试:Z"), 0.1f);
            Add(w, "甲", ModifierTarget.Reserve("测试:A"), -0.1f);
        });
        var result = DamageCombiner.Evaluate(collector.Seal(Hit()));
        Check(result.Targets.Where(t => t.Target.Kind == ModifierTargetKind.Reserve).Select(t => t.Target.Key)
            .SequenceEqual(new[] { "测试:A", "测试:Z" }), "备用键顺序不稳定");
        Near(0.99f, result.AfterDefenseFactor);
    }),
    ("拒绝越界重复与非有限贡献", () =>
    {
        Throws<ArgumentOutOfRangeException>(() => ModifierTarget.Reaction(10));
        Throws<ArgumentOutOfRangeException>(() => ModifierTarget.Ascension(-1));
        Throws<ArgumentException>(() => NewCollector().Collect(10, 0, w => w.AddValue("未知", ModifierTarget.Reserve("未注册"), 1f)));
        Throws<ArgumentException>(() => NewCollector().Collect(10, 0, w => w.AddValue("无效", ModifierTarget.Mastery, float.NaN)));
        Throws<ArgumentException>(() => NewCollector().Collect(10, 0, w =>
        {
            w.AddValue("重复", ModifierTarget.Mastery, 1f);
            w.AddValue("重复", ModifierTarget.Resistance, 1f);
        }));
    }),
    ("写入器失效且异常输入不可继续", () =>
    {
        var collector = NewCollector();
        ContributionWriter? saved = null;
        collector.Collect(10, 0, w => saved = w);
        Throws<InvalidOperationException>(() => saved!.AddValue("迟到", ModifierTarget.Mastery, 1f));
        collector.Seal(Hit());
        Throws<InvalidOperationException>(() => collector.Collect(20, 1, _ => { }));
        collector = NewCollector();
        Throws<InvalidOperationException>(() => collector.Collect(10, 0, _ => throw new InvalidOperationException("测试异常")));
        Throws<InvalidOperationException>(() => collector.Seal(Hit()));
    }),
    ("拒绝注册倒序和阶段回退", () =>
    {
        var collector = NewCollector();
        collector.Collect(10, 1, _ => { });
        Throws<ArgumentException>(() => collector.Collect(20, 0, _ => { }));
        collector.RunPostRule("测试:末尾", _ => { });
        Throws<InvalidOperationException>(() => collector.Collect(30, 2, _ => { }));
    })
};

int failed = 0;
foreach (var test in cases)
{
    try { test.Run(); Console.WriteLine($"通过：{test.Name}"); }
    catch (Exception exception) { failed++; Console.Error.WriteLine($"失败：{test.Name}\n{exception}"); }
}
Console.WriteLine($"共 {cases.Length} 项，通过 {cases.Length - failed} 项，失败 {failed} 项。");
return failed == 0 ? 0 : 1;

static HitProposalCollector NewCollector() => new(Array.Empty<string>());
static HitSnapshot Hit() => new(1, HitKind.ItemToNpc, ExecutionSide.SinglePlayer, TagSet.Empty);
static void Add(ContributionWriter writer, string name, ModifierTarget target, float value)
{
    writer.AddValue(name, target, value);
    writer.SetEnabled(name, target, true, "测试显式启用");
}
static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
static void Equal(float expected, float actual) => Check(expected == actual, $"预期 {expected}，实际 {actual}");
static void Near(float expected, float actual) => Check(Math.Abs(expected - actual) < 0.00001f, $"预期约 {expected}，实际 {actual}");
static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new Exception($"预期抛出 {typeof(T).Name}");
}

using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Element;
using AdvancedMod.Basic.Tag;

var tests = new (string, Action)[]
{
    ("四路线与伪反应不消耗", Routes),
    ("三反应系数与槽序", Coefficients),
    ("高优先级资源不足及 CD 回退", Fallback),
    ("作者火冰消耗样例", AuthorExample),
    ("主动低白值与静置高白值加权", Weighted),
    ("伤害后提交及重入串行", Reentrancy),
    ("每刻扫描、CD 递减及环境归属", Ticks),
    ("取消、异常和目标失效", Failure),
    ("快照、分量合并与目标隔离", Isolation),
    ("参数验证与派生链上限", Validation),
    ("Calculate 标签门禁与乘区不重复", CalculateFlow),
    ("小数分量消耗与总量一致", Fractional),
    ("网络快照往返与坏包原子拒绝", Wire),
    ("正式反应消耗、CD与生命支付计划", Formal),
};
int passed = 0;
foreach (var (name, run) in tests)
{
    try { run(); passed++; Console.WriteLine($"通过：{name}"); }
    catch (Exception e) { Console.Error.WriteLine($"失败：{name}\n{e}"); }
}
Console.WriteLine($"元素框架：{passed}/{tests.Length} 通过。");
return passed == tests.Length ? 0 : 1;

static void Check(bool condition, string message = "断言失败")
{
    if (!condition) throw new Exception(message);
}
static void Formal()
{
    var table = AmplificationReactions.Create();
    foreach (var (tag, other, attached, incoming, slot, cd) in new[] {
        (TagEnum.Hydro, 3, 1.5, 1.0, 1, 60), (TagEnum.Anemo, 2, 1.5, 0.75, 2, 40),
        (TagEnum.Pyro, 4, 1.5, 1.5, 3, 40) })
    {
        var target = Target(new ElementAttachment(other, attached, 100));
        target.Submit(Request(tag, incoming), table, plan => {
            Near(plan.DamageSeed, 150); Check(plan.Definition!.Slot == slot); Near(plan.CooldownIncrease, cd); return true;
        });
        Near(target.Snapshot.Elements[other].Total, 0);
    }
    Near(AmplificationReactions.LifeCost(400, 400), 6);
    Near(AmplificationReactions.LifeCost(100, 400), 2);
    Near(AmplificationReactions.LifeCost(99, 400), 0);
    var water = Target(new ElementAttachment(2, 1.5, 100));
    water.Submit(new(Hit(TagEnum.Anemo), 100, .75, proposedLifeCost: 6), table, plan => {
        Near(plan.LifeCost, 6); Near(plan.DamageSeed, 210); return false;
    });
    Near(water.Snapshot.Elements[2].Total, 1.5);
    Near(water.Snapshot.ReactionCooldowns[2], 0);
    var insufficient = Target(new ElementAttachment(2, 1, 100));
    insufficient.Submit(new(Hit(TagEnum.Anemo), 100, .75, proposedLifeCost: 6), table, plan => {
        Near(plan.LifeCost, 0); Near(plan.DamageSeed, 100); return true;
    });
}
static void Near(double actual, double expected)
{
    if (Math.Abs(actual - expected) > 1e-9) throw new Exception($"期望 {expected}，实际 {actual}");
}
static void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception($"应当抛出 {typeof(T).Name}");
}
static HitSnapshot Hit(params TagEnum[] tags) => new(1, HitKind.ProjectileToNpc,
    ExecutionSide.SinglePlayer, new TagSet(tags.Select(t => new TagEntry(false, t))),
    Source: new(SourceKind.Projectile, "测试弹幕"), Target: new(SourceKind.Npc, "测试目标"));
static ElementRequest Request(TagEnum tag, double amount = 1, int panel = 100, float consume = 1, float cd = 1) =>
    new(Hit(tag), panel, amount, consume, cd);
static ElementRequest Idle() => new(Hit(), 0, isIdle: true);
static ElementTarget Target(params ElementAttachment[] initial)
{
    var target = new ElementTarget(64);
    foreach (var attachment in initial) target.Attach(attachment);
    return target;
}
static ElementResolution Resolve(ElementTarget target, ElementRequest request)
{
    ElementResolution? result = null;
    target.Submit(request, TestReactions.Table, _ => true, r => result = r);
    return result!;
}
static void Routes()
{
    var target = Target(new ElementAttachment(3, 2, 100));
    var direct = Resolve(target, new(Hit(TagEnum.HydroElectro), 100));
    Check(direct.Plan.Route == ElementRoute.Direct && direct.Outcome is null);
    var fake = Resolve(target, new(Hit(TagEnum.Hydro, TagEnum.HydroElectro), 100));
    Check(fake.Plan.Route == ElementRoute.DirectReaction && fake.Outcome is null && fake.Presentations.Count == 1);
    Near(target.Snapshot.Elements[3].Total, 2);
    Near(target.Snapshot.ReactionCooldowns[1], 0);
    Check(Resolve(target, Request(TagEnum.Cryo)).Plan.Route == ElementRoute.AttachOnly);
    Check(Resolve(target, Request(TagEnum.Hydro)).Plan.Route == ElementRoute.React);
}
static void Coefficients()
{
    foreach (var (tag, other, amount, slot, expected) in new[]
    {
        (TagEnum.Hydro, 3, 1d, 1, 200d), (TagEnum.Anemo, 2, 1d, 2, 150d),
        (TagEnum.Pyro, 4, 1.5d, 3, 100d)
    })
    {
        var result = Resolve(Target(new ElementAttachment(other, 2, 100)), Request(tag, amount));
        Check(result.Outcome!.ReactionSlot == slot);
        Near(result.Plan.DamageSeed, expected);
    }
    var selected = Resolve(Target(new ElementAttachment(3, 2, 100), new(6, 2, 100)), Request(TagEnum.Hydro));
    Check(selected.Outcome!.ReactionSlot == 1);
}
static void Fallback()
{
    var target = Target(new ElementAttachment(3, 0.5, 100), new(6, 3, 100));
    Check(Resolve(target, Request(TagEnum.Hydro)).Outcome!.ReactionSlot == 2);
    target.Attach(new(3, 2, 100));
    Check(Resolve(target, Request(TagEnum.Hydro)).Outcome!.ReactionSlot == 1);
    var attached = Resolve(target, Request(TagEnum.Hydro));
    Check(attached.Plan.Route == ElementRoute.AttachOnly);
    Near(target.Snapshot.Elements[2].Total, 1);
    Near(target.Snapshot.ReactionCooldowns[1], 60);
    Near(target.Snapshot.ReactionCooldowns[2], 60);
    var second = Target(new ElementAttachment(3, 3, 100), new(6, 3, 100));
    Resolve(second, Request(TagEnum.Hydro));
    Check(Resolve(second, Request(TagEnum.Hydro)).Outcome!.ReactionSlot == 2);
}
static void AuthorExample()
{
    var target = Target(new ElementAttachment(4, 1.5, 100));
    var result = Resolve(target, Request(TagEnum.Pyro, 1.5, consume: 0.5f, cd: 0.5f));
    Near(target.Snapshot.Elements[1].Total, 0.75);
    Near(target.Snapshot.Elements[4].Total, 1);
    Near(result.Plan.Consumption.Sum(c => c.Amount), 1.25);
    Near(target.Snapshot.ReactionCooldowns[3], 30);
}
static void Weighted()
{
    var active = Target(new ElementAttachment(1, 1, 100), new(1, 2, 200), new(4, 1, 100));
    var a = Resolve(active, Request(TagEnum.Cryo, 0.1));
    Near(a.Plan.Consumption.Where(c => c.Slot == 1).Sum(c => c.Amount * c.PanelDamage) / 1.5, 400d / 3);
    Near(a.Plan.WeightedPanelDamage, 120);
    var idle = Target(new ElementAttachment(1, 1, 100), new(1, 2, 200), new(4, 1, 100));
    var b = Resolve(idle, Idle());
    Near(b.Plan.WeightedPanelDamage, 160);
    Near(idle.Snapshot.Elements[1].Portions[200], 0.5);
    Near(idle.Snapshot.Elements[1].Portions[100], 1);
}
static void Reentrancy()
{
    var target = Target(new ElementAttachment(3, 1, 100));
    var order = new List<string>();
    target.Submit(Request(TagEnum.Hydro), TestReactions.Table, plan =>
    {
        Near(target.Snapshot.Elements[3].Total, 1);
        Near(target.Snapshot.Elements[2].Total, 0);
        Near(target.Snapshot.ReactionCooldowns[1], 0);
        target.Submit(Request(TagEnum.Hydro), TestReactions.Table, next =>
        {
            order.Add("第二击");
            Check(next.Route == ElementRoute.AttachOnly);
            Near(target.Snapshot.ReactionCooldowns[1], 60);
            return true;
        });
        Check(order.Count == 0);
        order.Add("第一击");
        return true;
    }, _ => order.Add("第一击已提交"));
    Check(string.Join(",", order) == "第一击,第一击已提交,第二击");
    Near(target.Snapshot.Elements[2].Total, 1);
}
static void Ticks()
{
    var target = Target(new ElementAttachment(2, 3, 100), new(3, 3, 100));
    int count = 0;
    void Done(ElementResolution r) { count++; Check(r.Outcome!.Source!.Kind == SourceKind.Environment); }
    target.Tick(0, Idle(), TestReactions.Table, _ => true, Done);
    Near(target.Snapshot.ReactionCooldowns[1], 60);
    target.Tick(0, Idle(), TestReactions.Table, _ => throw new Exception("同刻重复扫描"));
    for (int tick = 1; tick < 60; tick++) target.Tick(tick, Idle(), TestReactions.Table, _ => true, Done);
    Near(target.Snapshot.ReactionCooldowns[1], 1);
    Check(count == 1);
    target.Tick(60, Idle(), TestReactions.Table, _ => true, Done);
    Check(count == 2);
    var fractional = Target(new ElementAttachment(3, 1, 100));
    Resolve(fractional, Request(TagEnum.Hydro, cd: 0.025f));
    fractional.Tick(1, Idle(), TestReactions.Table, _ => true);
    Near(fractional.Snapshot.ReactionCooldowns[1], 0.5);
    fractional.Tick(2, Idle(), TestReactions.Table, _ => true);
    Near(fractional.Snapshot.ReactionCooldowns[1], 0);
}
static void Failure()
{
    var target = Target(new ElementAttachment(3, 1, 100));
    target.Submit(Request(TagEnum.Hydro), TestReactions.Table, _ => false, r =>
        Check(!r.Committed && r.Outcome is null && r.Presentations.Count == 0));
    Near(target.Snapshot.Elements[2].Total, 0);
    Throws<InvalidOperationException>(() => target.Submit(Request(TagEnum.Hydro), TestReactions.Table, _ =>
    {
        target.Attach(new(4, 9, 100));
        throw new InvalidOperationException("模拟伤害入口异常");
    }));
    Check(!target.IsResolving);
    Near(target.Snapshot.Elements[4].Total, 0);
    bool valid = true;
    target.Submit(Request(TagEnum.Hydro), TestReactions.Table, _ => { valid = false; return true; },
        isTargetValid: () => valid);
    Near(target.Snapshot.Elements[3].Total, 1);
    Check(Resolve(target, Request(TagEnum.Hydro)).Outcome is not null);
}
static void Isolation()
{
    var target = Target(new ElementAttachment(1, 0.5, 100), new(1, 0.75, 100));
    var old = target.Snapshot;
    Check(old.Elements.Count == 10 && old.ReactionCooldowns.Count == 20);
    Check(old.Elements[1].Portions.Count == 1);
    Near(old.Elements[1].Total, 1.25);
    target.Attach(new(1, 1, 200));
    Near(old.Elements[1].Total, 1.25);
    Near(target.Snapshot.Elements[1].Total, target.Snapshot.Elements[1].Portions.Values.Sum());
    Near(Target().Snapshot.Elements[1].Total, 0);
}
static void Validation()
{
    Throws<ArgumentOutOfRangeException>(() => Request(TagEnum.Hydro, consume: 0));
    Throws<ArgumentOutOfRangeException>(() => Request(TagEnum.Hydro, consume: float.NaN));
    Throws<ArgumentException>(() => new ElementRequest(Hit(TagEnum.Hydro, TagEnum.Pyro), 100, 1));
    Throws<ArgumentException>(() => new ReactionTable(
        new(TagEnum.HydroElectro, 2, 60, new(2, 1), new(3, 1)),
        new(TagEnum.HydroElectro, 2, 60, new(2, 1), new(3, 1))));
    var target = new ElementTarget(3);
    Func<ReactionPlan, bool>? loop = null;
    loop = _ => { target.Submit(new(Hit(), 1), TestReactions.Table, loop!); return true; };
    Throws<InvalidOperationException>(() => target.Submit(new(Hit(), 1), TestReactions.Table, loop));
    Check(!target.IsResolving);
    Resolve(target, new(Hit(), 1));
}
static void CalculateFlow()
{
    var target = Target(new ElementAttachment(3, 1, 100));
    double damage = 0;
    target.Submit(Request(TagEnum.Hydro), TestReactions.Table, plan =>
    {
        var gates = DefaultTagRules.All.Where(r => r.Matches(plan.Hit)).Select((r, i) =>
            new GateDecision(DecisionStage.TagRule, null, r.Id, "启用", r.Target, true, "测试标签规则", i)).ToArray();
        var input = new CalculationInput(plan.Hit,
            new[] { new ContributorActivation(1, 0, Array.Empty<SourceInfo>()) },
            new[] { new NumericContribution(1, "反应增伤", ModifierTarget.Reaction(1), 0.25f, gates.Length),
                new NumericContribution(1, "元素增伤", ModifierTarget.ElementDamage(2), 0.5f, gates.Length + 1) },
            gates, Array.Empty<string>());
        var result = DamageCombiner.Evaluate(input);
        Near(plan.DamageSeed, 200);
        Near(result.ReactionFactor, 1.25);
        Near(result.AfterDefenseFactor, 1.5);
        // 测试实体无防御、无暴击；不冒充 Terraria 完整伤害公式。
        damage = plan.DamageSeed * result.ReactionFactor * result.ResistanceFactor * result.AfterDefenseFactor;
        return true;
    });
    Near(damage, 375);
}

static void Fractional()
{
    for (int count = 2; count < 40; count++)
    {
        var amounts = Enumerable.Range(1, count).Select(i => i / 100d).ToArray();
        var target = Target();
        for (int i = 0; i < count; i++) target.Attach(new(2, amounts[i], i + 1));
        target.Attach(new(3, 1, 100));
        double total = target.Snapshot.Elements[2].Total;
        var table = new ReactionTable(new ReactionDefinition(TagEnum.HydroElectro, 2, 60,
            new(2, total), new(3, 1)));
        target.Submit(Idle(), table, p =>
        {
            Near(p.Consumption.Where(c => c.Slot == 2).Sum(c => c.Amount), total);
            return true;
        });
        var gauge = target.Snapshot.Elements[2];
        Near(gauge.Total, gauge.Portions.Values.Sum());
        Check(gauge.Portions.Values.All(v => v > 0));
        Near(gauge.Total, 0);
    }
}

static void Wire()
{
    var original = Target();
    original.Attach(new(2, 0.75, 123));
    original.Attach(new(2, 1.25, 456));
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);
    original.WriteState(writer);
    var copy = Target();
    stream.Position = 0;
    copy.ReadState(new BinaryReader(stream));
    Near(copy.Snapshot.Elements[2].Total, 2);
    Near(copy.Snapshot.Elements[2].Portions[123], 0.75);
    byte[] valid = stream.ToArray();
    foreach (int length in new[] { 0, 3, valid.Length - 1 })
    {
        try { copy.ReadState(new BinaryReader(new MemoryStream(valid[..length]))); throw new Exception("截断包未拒绝"); }
        catch (EndOfStreamException) { }
        Near(copy.Snapshot.Elements[2].Total, 2);
    }
    using var invalid = new MemoryStream();
    using var bad = new BinaryWriter(invalid, System.Text.Encoding.UTF8, true);
    bad.Write((ushort)1); bad.Write(100); bad.Write(double.NaN);
    invalid.Position = 0;
    try { copy.ReadState(new BinaryReader(invalid)); throw new Exception("非法数值未拒绝"); }
    catch (InvalidDataException) { }
    Near(copy.Snapshot.Elements[2].Total, 2);
}

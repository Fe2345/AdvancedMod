using System;
using System.Linq;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Calculate.Diagnostics;
using AdvancedMod.Basic.Calculate.DamageOverTime;
using AdvancedMod.Basic.Tag;

int checks = 0;
foreach (var kind in new[] { HitKind.DamageOverTimeToNpc, HitKind.DamageOverTimeToPlayer })
{
    var hit = new HitSnapshot(1, kind, ExecutionSide.SinglePlayer,
        new TagSet(new[] { new TagEntry(false, TagEnum.Hydro) }));
    var collector = new HitProposalCollector(Array.Empty<string>(), kind);
    foreach (var rule in DefaultTagRules.All)
        if (rule.Matches(hit)) collector.RunTagRule(rule.Id, w => w.SetEnabled("标签", rule.Target, true, "测试"));
    collector.Collect(1, 0, w =>
    {
        w.AddValue("水增伤", ModifierTarget.ElementDamage(2), 0.5f);
        w.SetEnabled("反应", ModifierTarget.Reaction(1), true, "测试");
        w.AddValue("反应", ModifierTarget.Reaction(1), 1);
        w.SetEnabled("抗性", ModifierTarget.Resistance, true, "测试");
        w.AddValue("抗性", ModifierTarget.Resistance, 0.5f);
    });
    var input = collector.Seal(hit);
    var modifiers = DamageCombiner.Evaluate(input, true);
    Equal(0, modifiers.Plan.Fields.Count);
    var normal = DotMath.Calculate(new(100), modifiers, 1000, 0);
    Equal(150, normal.Damage);
    Check(!normal.Critical);
    double received = -1;
    var critical = DotMath.Calculate(new(100, 100, 100, (damage, defense) =>
    { received = damage; Equal(30, defense); return damage - defense; }), modifiers, 30, 0.999);
    Equal(100, received);
    Equal(210, critical.Damage);
    Check(critical.Critical);
    Equal(150, DotMath.Calculate(new(100, 50), modifiers, 0, 0.5).Damage);
    Throws(() => DotMath.Calculate(new(100, DefenseFormula: (_, _) => double.NaN), modifiers, 0, 0));
    Throws(() => DotMath.Calculate(new(100, 101), modifiers, 0, 0));
    Throws(() => DotMath.Calculate(new(-1), modifiers, 0, 0));
    var invalid = new HitProposalCollector(Array.Empty<string>(), kind);
    Throws(() => invalid.Collect(2, 0, w => w.AddNative("禁止原生贡献", NativeField.SourceDamage, NumericOperation.Multiply, 2)));
    Check(CombatTraceReplay.Replay(CombatTraceJson.Deserialize(CombatTraceJson.Serialize(
        CombatTrace.Create(Guid.NewGuid(), input, modifiers)))).Matches);
}
Equal(0, DotMath.ApplyDefense(10, 300));
Equal(100, DotMath.ApplyDefense(100, 0));
Equal(1000 - 300 * (0.15 + 0.85 / Math.Sqrt(2)), DotMath.ApplyDefense(1000, 300));
int regen;
var fire = new DotSource("测试火", 1, null);
var electro = new DotSource("测试雷", 1, null);
var ledger = new DotLedger();
ledger.Submit(fire, new(0.001, false)); regen = 0; Check(!ledger.Apply(ref regen, 0));
ledger.Submit(fire, new(0.009, false)); Check(ledger.Apply(ref regen, 0)); Equal(-1, regen);
Equal(0.2 / 120, ledger.PendingDamage); ledger.Clear();
Throws(() => ledger.Submit(fire, new(double.MaxValue, false))); Check(!ledger.HasBalance);
ledger.Submit(fire, new(6, false), TagEnum.Pyro);
ledger.Submit(electro, new(4, true), TagEnum.Electro);
Equal(15, ledger.Allocate(15).NativeDamage); // 尚未注入的伤害不能抢占本次原版数字。
regen = 0; Check(ledger.Apply(ref regen, 0)); Equal(-1200, regen);
var allocation = ledger.Allocate(15);
Equal(5, allocation.NativeDamage); Equal(6, allocation.Separate[0].Damage); Equal(4, allocation.Separate[1].Damage);
Check(allocation.Separate[0].Source == fire && allocation.Separate[1].Source == electro);
Check(!ledger.HasBalance); Check(!ledger.Apply(ref regen, 0));

ledger.Submit(fire, new(6, false), TagEnum.Pyro);
ledger.Submit(electro, new(4, false), TagEnum.Electro);
regen = 0; ledger.Apply(ref regen, 0);
allocation = ledger.Allocate(8);
Equal(0, allocation.NativeDamage); Equal(6, allocation.Separate[0].Damage); Equal(2, allocation.Separate[1].Damage);
Equal(2, ledger.AppliedDamage);
allocation = ledger.Allocate(5); Equal(3, allocation.NativeDamage); Equal(2, allocation.Separate[0].Damage);

ledger.Submit(fire, new(2, false), separate: false);
ledger.Submit(electro, new(3, false), TagEnum.Electro);
regen = 0; ledger.Apply(ref regen, 0);
allocation = ledger.Allocate(4); Equal(2, allocation.NativeDamage); Equal(2, allocation.Separate[0].Damage);
ledger.Clear(); Check(!ledger.HasBalance);
ledger.Submit(fire, new(0.5, false), TagEnum.Pyro);
ledger.Submit(electro, new(0.5, false), TagEnum.Electro);
regen = 0; ledger.Apply(ref regen, 0);
allocation = ledger.Allocate(1); Equal(0, allocation.NativeDamage);
Check(allocation.Separate.Single().Source == electro); Check(!ledger.HasBalance);

var frame = new DotFrameText();
frame.Add(new(fire, 2, TagEnum.Pyro, false));
frame.Add(new(electro, 3, TagEnum.Electro, false));
frame.Add(new(fire, 4, TagEnum.Hydro, true));
Equal(2, frame.Entries.Count); Equal(6, frame.Entries[0].Damage);
Check(frame.Entries[0].Element == TagEnum.Pyro && frame.Entries[0].Critical);
frame.Clear(); Equal(0, frame.Entries.Count);
ledger.Submit(fire, new(1, false)); regen = int.MinValue;
Throws(() => ledger.Apply(ref regen, 0)); Equal(1, ledger.PendingDamage);
regen = -120; Throws(() => ledger.Apply(ref regen, int.MinValue)); Equal(1, ledger.PendingDamage);
ledger.Clear();

// 随机交错提交／注入／结算，验证无重复注入、输出总和守恒、FIFO 及帧合并。
var random = new Random(7341);
var expected = new System.Collections.Generic.Queue<(DotSource Source, int Damage)>();
int expectedPending = 0, emittedTotal = 0, submittedTotal = 0;
for (int iteration = 0; iteration < 300; iteration++)
{
    var source = random.Next(2) == 0 ? fire : electro;
    int value = random.Next(1, 30);
    ledger.Submit(source, new(value, false)); expected.Enqueue((source, value));
    expectedPending += value; submittedTotal += value;
    regen = 0; Check(ledger.Apply(ref regen, 0)); Equal(-value * 120, regen);
    int packet = random.Next(1, 40);
    allocation = ledger.Allocate(packet);
    Equal(packet, allocation.NativeDamage + allocation.Separate.Sum(t => t.Damage));
    int consumed = Math.Min(packet, expectedPending);
    Equal(packet - consumed, allocation.NativeDamage);
    int cursor = 0;
    while (consumed > 0)
    {
        var head = expected.Peek();
        int take = Math.Min(head.Damage, consumed);
        Check(allocation.Separate[cursor].Source == head.Source); Equal(take, allocation.Separate[cursor].Damage);
        expected.Dequeue();
        if (take < head.Damage)
        {
            var rest = expected.ToArray(); expected.Clear(); expected.Enqueue((head.Source, head.Damage - take));
            foreach (var item in rest) expected.Enqueue(item);
        }
        cursor++; consumed -= take; expectedPending -= take; emittedTotal += take;
    }
    Equal(expectedPending, ledger.AppliedDamage);
}
Equal(submittedTotal, emittedTotal + ledger.AppliedDamage);
Console.WriteLine($"持续伤害离线检查通过：{checks} 项。");

void Check(bool value) { checks++; if (!value) throw new Exception("检查失败"); }
void Equal(double expected, double actual) => Check(Math.Abs(expected - actual) < 0.000001);
void Throws(Action action)
{
    try { action(); } catch (ArgumentException) { checks++; return; } catch (OverflowException) { checks++; return; }
    throw new Exception("预期异常未发生");
}

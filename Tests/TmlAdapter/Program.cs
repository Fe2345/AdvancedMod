using System;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Calculate.Tml;
using AdvancedMod.Basic.Tag;
using Terraria;

var collector = new HitProposalCollector(Array.Empty<string>(), HitKind.ItemToNpc);
collector.Collect(10, 0, writer =>
{
    writer.AddValue("反应", ModifierTarget.Reaction(0), 0.5f);
    writer.SetEnabled("反应", ModifierTarget.Reaction(0), true, "测试启用");
    writer.AddNative("白值", NativeField.SourceDamage, NumericOperation.AddBase, 5f);
    writer.AddNative("增幅", NativeField.SourceDamage, NumericOperation.AddPercent, 0.1f);
    writer.AddNative("倍率", NativeField.SourceDamage, NumericOperation.Multiply, 1.1f);
    writer.AddNative("尾数", NativeField.SourceDamage, NumericOperation.AddFlat, 3f);
    writer.AddNative("穿甲", NativeField.ArmorPenetration, NumericOperation.Add, 10f);
    writer.SetIgnoreDefense("无视防御", true);
});
collector.RunPostRule("测试:屏蔽", writer => writer.SetEnabled("关闭穿甲", ModifierTarget.Native(NativeField.ArmorPenetration), false, "测试只移除本模块贡献"));
var result = DamageCombiner.Evaluate(collector.Seal(new(1, HitKind.ItemToNpc, ExecutionSide.SinglePlayer, TagSet.Empty)));
var npc = new NPC.HitModifiers();
npc.SourceDamage.Base = 10f;
npc.SourceDamage += 0.2f;
npc.SourceDamage *= 2f;
npc.SourceDamage.Flat = 7f;
npc.ArmorPenetration += 4f;
npc.ScalingArmorPenetration += 1.2f;
TmlHitAdapter.Apply(result, ref npc);
Near(15f, npc.SourceDamage.Base);
Near(10f, npc.SourceDamage.Flat);
Near((100f + 15f) * 1.3f * 2f * 1.1f * 1.5f + 10f, npc.SourceDamage.ApplyTo(100f));
Near(4f, npc.ArmorPenetration.Value);
Near(1.2f, npc.ScalingArmorPenetration.Value);
Console.WriteLine("通过：NPC 原生分量保留、反应倍率映射、关闭只移除本模块贡献、不降低已有穿甲。");

var hurtCollector = new HitProposalCollector(Array.Empty<string>(), HitKind.NpcToPlayer);
hurtCollector.Collect(10, 0, writer =>
{
    writer.AddValue("抗性", ModifierTarget.Resistance, 0.15f);
    writer.SetEnabled("抗性", ModifierTarget.Resistance, true, "测试启用");
    writer.SetIgnoreDefense("穿透", true);
    writer.SetMaximumDamage("上限", 40);
});
var hurtResult = DamageCombiner.Evaluate(hurtCollector.Seal(new(2, HitKind.NpcToPlayer, ExecutionSide.SinglePlayer, TagSet.Empty)));
var hurt = new Player.HurtModifiers();
hurt.SourceDamage *= 2f;
hurt.SourceDamage.Flat = 7f;
hurt.ScalingArmorPenetration += 0.2f;
TmlHitAdapter.Apply(hurtResult, ref hurt);
Near(177f, hurt.SourceDamage.ApplyTo(100f));
Near(1f, hurt.ScalingArmorPenetration.Value);
Near(40f, hurt.GetDamage(100, 100, 0.5f));
Console.WriteLine("通过：玩家受伤映射、原有 Flat 保留、满比例穿甲与严格上限。");

static void Near(float expected, float actual)
{
    if (Math.Abs(expected - actual) > 0.0001f) throw new Exception($"预期 {expected}，实际 {actual}");
}

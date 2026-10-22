#nullable enable
using System;
using System.Text.Json;
using AdvancedMod.Basic.Calculate.Core;
using Terraria;

namespace AdvancedMod.Basic.Calculate.Tml;

/// <summary>单次命中独立收集、裁决与追踪，不在修改伤害时触发派生攻击。</summary>
public sealed class CombatPipeline
{
    private readonly CombatRegistry registry;
    private readonly Action<string, Exception> logError;
    private readonly Func<HitSnapshot, bool>? traceFilter;
    private readonly Action<CalculationInput, CalculationResult>? writeTrace;

    public CombatPipeline(CombatRegistry registry, Action<string, Exception> logError,
        Func<HitSnapshot, bool>? traceFilter = null, Action<CalculationInput, CalculationResult>? writeTrace = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(logError);
        if (!registry.IsFrozen) throw new ArgumentException("管线只能使用冻结后的注册表。", nameof(registry));
        this.registry = registry;
        this.logError = logError;
        this.traceFilter = traceFilter;
        this.writeTrace = writeTrace;
    }

    public CalculationInput Collect(in CombatContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!registry.IsFrozen) throw new InvalidOperationException("注册表已卸载。");
        HitSnapshot hit = context.CreateSnapshot();
        var collector = new HitProposalCollector(registry.ReserveKeys, context.Kind);
        string stage = "标签规则";
        string author = "未进入作者回调";
        // in 参数不能被闭包捕获；复制的仍是同一个只读实体视图。
        CombatContext current = context;
        try
        {
            foreach (TagGateRule rule in registry.TagRules)
            {
                author = rule.Id;
                if (rule.Matches(hit))
                    collector.RunTagRule(rule.Id, writer => writer.SetEnabled("标签匹配", rule.Target, rule.Enabled, "已匹配注册的标签规则"));
            }
            stage = "普通贡献者";
            foreach (var registration in registry.Contributors)
            {
                author = registration.Id.ToString();
                if (registration.SupportedHits.Includes(context.Kind) && registration.Contributor.IsActive(in context))
                    collector.Collect(registration.Id, registration.Order, writer => registration.Contributor.Collect(in current, writer));
            }
            stage = "后置规则";
            foreach (var registration in registry.Rules)
            {
                author = registration.Id;
                if (!registration.SupportedHits.Includes(context.Kind)) continue;
                RuleContext view = collector.CreateRuleView(hit);
                collector.RunPostRule(registration.Id, writer => registration.Rule.Evaluate(in view, writer));
            }
            stage = "封闭输入";
            return collector.Seal(hit);
        }
        catch (Exception exception)
        {
            // 日志失败不能覆盖贡献者的原始异常；正常游戏宿主接入 Mod.Logger。
            try { logError($"Calculate {stage} 失败；作者={author}；上下文={JsonSerializer.Serialize(hit)}", exception); }
            catch { }
            throw;
        }
    }

    public CalculationResult Calculate(in CombatContext context, bool includeDiagnostics = false)
    {
        CalculationInput input = Collect(in context);
        bool trace = traceFilter?.Invoke(input.Hit) ?? false;
        CalculationResult result;
        try { result = DamageCombiner.Evaluate(input, includeDiagnostics || trace); }
        catch (Exception exception)
        {
            try { logError($"Calculate 折叠失败；上下文={JsonSerializer.Serialize(input.Hit)}", exception); }
            catch { }
            throw;
        }
        if (trace && writeTrace is not null)
        {
            try { writeTrace(input, result); }
            catch (Exception exception)
            {
                // 文件输出不参与伤害裁决；日志失败也不撤销已经完成的计算。
                try { logError($"Calculate Trace 输出失败；命中={input.Hit.HitId}", exception); }
                catch { }
            }
        }
        return result;
    }

    public void ModifyNpcHit(in CombatContext context, ref NPC.HitModifiers modifiers)
    {
        if (context.Kind is not (HitKind.ItemToNpc or HitKind.ProjectileToNpc))
            throw new ArgumentException("该上下文不是 NPC 受击路径。", nameof(context));
        TmlHitAdapter.Apply(Calculate(in context), ref modifiers);
    }

    public void ModifyPlayerHurt(in CombatContext context, ref Player.HurtModifiers modifiers)
    {
        if (context.Kind is not (HitKind.NpcToPlayer or HitKind.ProjectileToPlayer))
            throw new ArgumentException("该上下文不是玩家受伤路径。", nameof(context));
        TmlHitAdapter.Apply(Calculate(in context), ref modifiers);
    }
}

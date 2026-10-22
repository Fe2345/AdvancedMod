#nullable enable
using System;
using System.Threading;
using System.IO;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Calculate.Diagnostics;
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Basic.Calculate.Tml;

/// <summary>在内容完成设置后按明确顺序登记并冻结；卸载时释放管线。</summary>
public sealed class CalculateSystem : ModSystem
{
    private static long nextHitId;
    private CombatRegistry? registry;
    private CombatPipeline? pipeline;
    private CombatTraceWriter? traceWriter;
    private Guid sessionId;
    private bool traceFailed;

    public static CombatPipeline Pipeline => ModContent.GetInstance<CalculateSystem>().pipeline
        ?? throw new InvalidOperationException("Calculate 管线尚未加载或已经卸载。");

    public static long NextHitId() => Interlocked.Increment(ref nextHitId);

    public override void PostSetupContent()
    {
        try
        {
            registry = new CombatRegistry();
            RegisterDefinitions(registry);
            registry.Freeze();
            sessionId = Guid.NewGuid();
            traceFailed = false;
            pipeline = new CombatPipeline(registry, (message, exception) => Mod.Logger.Error(message, exception),
                hit => !traceFailed && ModContent.GetInstance<CombatTraceConfig>().Options.Matches(hit), WriteTrace);
        }
        catch (Exception exception)
        {
            Mod.Logger.Error("Calculate 加载校验失败。", exception);
            Unload();
            throw;
        }
    }

    private static void RegisterDefinitions(CombatRegistry registry)
    {
        // 内容登记统一放在此处，显式调用各内容的登记函数；当前不注册具体标签映射。
    }

    private void WriteTrace(CalculationInput input, CalculationResult result)
    {
        try
        {
            if (traceWriter is null)
            {
                string directory = Path.Combine(Main.SavePath, "Logs", "AdvancedMod", "Calculate");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, $"{sessionId:N}.jsonl");
                traceWriter = new CombatTraceWriter(new StreamWriter(path, append: false));
                Mod.Logger.Info($"Calculate Trace 文件：{path}");
            }
            traceWriter.Write(CombatTrace.Create(sessionId, input, result));
            traceWriter.Flush();
            Mod.Logger.Info($"Calculate 命中={input.Hit.HitId}，路径={input.Hit.Kind}，反应={result.ReactionFactor}，抗性={result.ResistanceFactor}，防御后={result.AfterDefenseFactor}");
        }
        catch
        {
            // 一次 I/O 故障后停止本次加载周期的详细追踪，避免每次命中重复刷错误。
            traceFailed = true;
            throw;
        }
    }

    public override void Unload()
    {
        pipeline = null;
        try { traceWriter?.Dispose(); }
        catch (Exception exception) { Mod.Logger.Error("Calculate Trace 关闭失败。", exception); }
        traceWriter = null;
        registry?.Clear();
        registry = null;
    }
}

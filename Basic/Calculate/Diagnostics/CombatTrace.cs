#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Tag;

namespace AdvancedMod.Basic.Calculate.Diagnostics;

/// <summary>序列化契约使用数组，读取后重新构造并校验纯计算输入。</summary>
public sealed record TraceInput(HitSnapshot Hit, ContributorActivationData[] Contributors,
    NumericContribution[] Contributions, GateDecision[] Gates, string[] ReserveKeys,
    NativeNumericContribution[] NativeContributions, ControlContribution[] Controls)
{
    public static TraceInput From(CalculationInput input) => new(input.Hit,
        input.Contributors.Select(c => new ContributorActivationData(c.ContributorId, c.RegistrationOrder, c.Sources.ToArray())).ToArray(),
        input.Contributions.ToArray(), input.Gates.ToArray(), input.ReserveKeys.ToArray(), input.NativeContributions.ToArray(), input.Controls.ToArray());

    public CalculationInput ToCalculationInput() => new(Hit,
        Contributors.Select(c => new ContributorActivation(c.Id, c.Order, c.Sources)),
        Contributions, Gates, ReserveKeys, NativeContributions, Controls);
}

public sealed record ContributorActivationData(int Id, int Order, SourceInfo[] Sources);
public sealed record TraceExpected(TargetResult[] Targets, ContributionDecision[] Decisions, NativeFieldResult[] NativeFields,
    float ReactionFactor, float ResistanceFactor, float AfterDefenseFactor)
{
    public static TraceExpected From(CalculationResult result) => new(result.Targets.ToArray(), result.Decisions.ToArray(),
        result.Plan.Fields.ToArray(), result.ReactionFactor, result.ResistanceFactor, result.AfterDefenseFactor);
}

public sealed record CombatTrace(int SchemaVersion, string RulesetVersion, Guid SessionId, long HitId,
    TraceInput Input, TraceExpected Expected)
{
    public const int CurrentSchema = 1;
    public const string CurrentRuleset = "calculate-1";
    public static CombatTrace Create(Guid sessionId, CalculationInput input, CalculationResult result)
    {
        if (!result.IncludesDiagnostics) throw new ArgumentException("完整 Trace 需要逐条采纳诊断。", nameof(result));
        return new(CurrentSchema, CurrentRuleset, sessionId, input.Hit.HitId, TraceInput.From(input), TraceExpected.From(result));
    }
}

/// <summary>每行一条 JSON；错误日志与本文件分离，不写入 Mod.Logger 前缀。</summary>
public sealed class CombatTraceWriter : IDisposable
{
    private readonly TextWriter writer;
    private readonly bool leaveOpen;
    private readonly object sync = new();
    private bool disposed;
    public CombatTraceWriter(TextWriter writer, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(writer);
        this.writer = writer;
        this.leaveOpen = leaveOpen;
    }

    public void Write(CombatTrace trace)
    {
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            writer.Write(CombatTraceJson.Serialize(trace));
            writer.Write('\n');
        }
    }
    public void Flush() { lock (sync) { ObjectDisposedException.ThrowIf(disposed, this); writer.Flush(); } }
    public void Dispose()
    {
        lock (sync)
        {
            if (disposed) return;
            disposed = true;
            if (leaveOpen) writer.Flush(); else writer.Dispose();
        }
    }
}

public static class CombatTraceJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();
    public static string Serialize(CombatTrace trace) => JsonSerializer.Serialize(trace, Options);
    public static CombatTrace Deserialize(string line) => JsonSerializer.Deserialize<CombatTrace>(line, Options)
        ?? throw new JsonException("Trace 不能为空。");

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new TargetConverter());
        options.Converters.Add(new TagsConverter());
        return options;
    }

    private sealed record TargetData(ModifierTargetKind Kind, int? Index, string? Key, NativeField? Field);
    private sealed class TargetConverter : JsonConverter<ModifierTarget>
    {
        public override ModifierTarget Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            TargetData data = JsonSerializer.Deserialize<TargetData>(ref reader, options) ?? throw new JsonException("缺少目标。");
            ModifierTarget target = data.Kind switch
            {
                ModifierTargetKind.Reaction when data.Index.HasValue => ModifierTarget.Reaction(data.Index.Value),
                ModifierTargetKind.Resistance => ModifierTarget.Resistance,
                ModifierTargetKind.ElementDamage when data.Index.HasValue => ModifierTarget.ElementDamage(data.Index.Value),
                ModifierTargetKind.Mastery => ModifierTarget.Mastery,
                ModifierTargetKind.Ascension when data.Index.HasValue => ModifierTarget.Ascension(data.Index.Value),
                ModifierTargetKind.Reserve when data.Key is not null => ModifierTarget.Reserve(data.Key),
                ModifierTargetKind.Native when data.Field.HasValue => ModifierTarget.Native(data.Field.Value),
                _ => throw new JsonException("Trace 中的目标组合无效。")
            };
            if (target.Index != data.Index || target.Key != data.Key || target.Field != data.Field)
                throw new JsonException("Trace 目标包含不属于该乘区的字段。");
            return target;
        }
        public override void Write(Utf8JsonWriter writer, ModifierTarget value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, new TargetData(value.Kind, value.Index, value.Key, value.Field), options);
    }

    private sealed class TagsConverter : JsonConverter<TagSet>
    {
        private sealed record EntryData(bool cannotBeCovered, TagEnum type);
        public override TagSet Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            EntryData[] entries = JsonSerializer.Deserialize<EntryData[]>(ref reader, options) ?? throw new JsonException("缺少标签集合。");
            return new TagSet(entries.Select(entry => new TagEntry(entry.cannotBeCovered, entry.type)));
        }
        public override void Write(Utf8JsonWriter writer, TagSet value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value.Select(entry => new EntryData(entry.cannotBeCovered, entry.type)).ToArray(), options);
    }
}

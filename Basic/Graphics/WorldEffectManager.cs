#nullable enable
using System;
using System.Collections.Generic;
using AdvancedMod.Basic.Graphics.Rendering;

namespace AdvancedMod.Basic.Graphics;

/// <summary>纯客户端调度核心，可脱离游戏循环验证。句柄在世界清理时不复用。</summary>
public sealed class WorldEffectManager
{
    private sealed class Entry(WorldEffect effect, double start)
    {
        public readonly WorldEffect Effect = effect;
        public readonly double Start = start;
        public bool Started, Stopped, Removed;
    }

    private readonly Dictionary<ulong, Entry> entries = new();
    private readonly List<ulong> order = new();
    private readonly Queue<Action> commands = new();
    private ulong nextId;
    public const int DefaultBudget = 256;
    public int Budget { get; }
    public double Time { get; private set; }
    public int Count => entries.Count;
    public EnvironmentController Environment { get; } = new();

    public WorldEffectManager(int budget = DefaultBudget)
    {
        if (budget < 1) throw new ArgumentOutOfRangeException(nameof(budget));
        Budget = budget;
    }

    public EffectHandle Create(WorldEffect effect, float delay = 0)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (!float.IsFinite(delay) || delay < 0) throw new ArgumentOutOfRangeException(nameof(delay));
        // 转交所有权后，预算拒绝也必须释放独占资源。
        if (entries.Count >= Budget) { effect.ReleaseOwnedResources(); return default; }
        ulong id = checked(++nextId);
        entries.Add(id, new Entry(effect, Time + delay));
        commands.Enqueue(() => { if (entries.ContainsKey(id)) order.Add(id); });
        return new(id);
    }

    public void SetTarget<T>(EffectHandle handle, T state) where T : struct => commands.Enqueue(() =>
    {
        if (TryMutable(handle, out var e) && e.Effect is IVfxTarget<T> target) target.SetTarget(state);
    });

    public void AppendSample<T>(EffectHandle handle, T sample) where T : struct => commands.Enqueue(() =>
    {
        if (TryMutable(handle, out var e) && e.Effect is IVfxSamples<T> target) target.AppendSample(sample);
    });

    private bool TryMutable(EffectHandle handle, out Entry entry) =>
        entries.TryGetValue(handle.Value, out entry!) && !entry.Stopped && !entry.Removed;

    public void Stop(EffectHandle handle) => commands.Enqueue(() =>
    {
        if (!TryMutable(handle, out var e)) return;
        e.Stopped = true;
        if (!e.Started) e.Removed = true;
        else e.Effect.Stop();
    });

    public void Kill(EffectHandle handle) => commands.Enqueue(() =>
    {
        if (entries.TryGetValue(handle.Value, out var e)) e.Removed = true;
    });

    public EnvironmentHandle AcquireEnvironment(string style, EnvironmentTarget target)
    {
        Environment.Validate(style, target);
        var handle = Environment.ReserveHandle();
        commands.Enqueue(() => Environment.Add(handle, style, target));
        return handle;
    }
    public void SetEnvironmentTarget(EnvironmentHandle handle, EnvironmentTarget target)
    {
        EnvironmentController.ValidateTarget(target);
        commands.Enqueue(() => Environment.SetTarget(handle, target));
    }
    public void ReleaseEnvironment(EnvironmentHandle handle) => commands.Enqueue(() => Environment.Release(handle));

    public void Update(float deltaSeconds)
    {
        if (!float.IsFinite(deltaSeconds) || deltaSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
        int boundary = commands.Count;
        for (int i = 0; i < boundary; i++) commands.Dequeue()();
        Time += deltaSeconds;
        var context = new VfxUpdateContext(deltaSeconds, Time);
        for (int i = order.Count - 1; i >= 0; i--)
        {
            ulong id = order[i];
            var e = entries[id];
            if (!e.Removed && Time >= e.Start)
            {
                e.Started = true;
                e.Effect.Update(context);
                e.Removed = e.Effect.IsFinished;
            }
            if (!e.Removed) continue;
            entries.Remove(id);
            order.RemoveAt(i);
            e.Effect.ReleaseOwnedResources();
        }
        Environment.Update(deltaSeconds);
    }

    public void Collect(VfxDrawQueue queue, in VfxDrawContext context)
    {
        foreach (ulong id in order)
        {
            var e = entries[id];
            if (e.Started && !e.Removed) e.Effect.CollectDrawCommands(queue, context);
        }
    }

    public void Clear()
    {
        foreach (var e in entries.Values) e.Effect.ReleaseOwnedResources();
        entries.Clear(); order.Clear(); commands.Clear(); Environment.Clear(); Time = 0;
    }
}

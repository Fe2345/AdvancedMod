#nullable enable
using System;
using System.Collections.Generic;

namespace AdvancedMod.Basic.Graphics;

public readonly record struct EnvironmentTarget(float Intensity, int Priority = 0);

/// <summary>样式数量固定，切换不会积累天空实例；同类取最大强度，异类按优先级和序号仲裁。</summary>
public sealed class EnvironmentController
{
    public sealed class Layer(string id, float transitionSeconds)
    {
        public string Id { get; } = id;
        public float TransitionSeconds { get; } = transitionSeconds;
        public float Intensity { get; internal set; }
        public double Time { get; internal set; }
        public bool Requested { get; internal set; }
        public bool Visible => Requested || Intensity > 0;
    }
    private readonly record struct Request(string Style, EnvironmentTarget Target);
    private readonly Dictionary<string, Layer> layers = new(StringComparer.Ordinal);
    private readonly Dictionary<ulong, Request> requests = new();
    private ulong nextId;
    public const int MaximumVisibleLayers = 2;
    public IEnumerable<Layer> Layers => layers.Values;
    public int RequestCount => requests.Count;
    public Layer Register(string id, float transitionSeconds)
    {
        if (string.IsNullOrWhiteSpace(id) || !float.IsFinite(transitionSeconds) || transitionSeconds <= 0)
            throw new ArgumentException("天空标识和过渡时间无效。");
        var layer = new Layer(id, transitionSeconds);
        layers.Add(id, layer);
        return layer;
    }
    public Layer Get(string id) => layers[id];
    internal EnvironmentHandle ReserveHandle() => new(checked(++nextId));
    internal void Validate(string id, EnvironmentTarget target)
    {
        if (!layers.ContainsKey(id)) throw new ArgumentException("天空样式尚未注册。", nameof(id));
        ValidateTarget(target);
    }
    internal static void ValidateTarget(EnvironmentTarget target)
    {
        if (!float.IsFinite(target.Intensity) || target.Intensity < 0 || target.Intensity > 1)
            throw new ArgumentOutOfRangeException(nameof(target));
    }
    internal void Add(EnvironmentHandle handle, string id, EnvironmentTarget target) => requests.Add(handle.Value, new(id, target));
    internal void SetTarget(EnvironmentHandle handle, EnvironmentTarget target)
    {
        if (requests.TryGetValue(handle.Value, out var request)) requests[handle.Value] = request with { Target = target };
    }
    internal void Release(EnvironmentHandle handle) => requests.Remove(handle.Value);
    public void Update(float deltaSeconds)
    {
        string? winner = null;
        int priority = int.MinValue;
        foreach (var r in requests.Values)
            if (r.Target.Intensity > 0 && (winner is null || r.Target.Priority > priority ||
                r.Target.Priority == priority && string.CompareOrdinal(r.Style, winner) < 0))
            { winner = r.Style; priority = r.Target.Priority; }
        float intensity = 0;
        foreach (var r in requests.Values)
            if (r.Style == winner) intensity = Math.Max(intensity, r.Target.Intensity);
        foreach (var layer in layers.Values)
        {
            layer.Requested = layer.Id == winner;
            float target = layer.Requested ? intensity : 0;
            float step = deltaSeconds / layer.TransitionSeconds;
            layer.Intensity += Math.Clamp(target - layer.Intensity, -step, step);
            if (layer.Visible) layer.Time += deltaSeconds;
            else layer.Time = 0;
        }
        // 最多保留当前样式与一个旧样式，快速切换也不叠加无限淡出层。
        Layer? fading = null;
        foreach (var layer in layers.Values)
            if (!layer.Requested && layer.Intensity > 0 && (fading is null || layer.Intensity > fading.Intensity ||
                layer.Intensity == fading.Intensity && string.CompareOrdinal(layer.Id, fading.Id) < 0)) fading = layer;
        foreach (var layer in layers.Values)
            if (!layer.Requested && layer != fading) { layer.Intensity = 0; layer.Time = 0; }
    }
    public void ResetStyle(string style)
    {
        var remove = new List<ulong>();
        foreach (var pair in requests) if (pair.Value.Style == style) remove.Add(pair.Key);
        foreach (ulong id in remove) requests.Remove(id);
        var layer = layers[style]; layer.Intensity = 0; layer.Time = 0; layer.Requested = false;
    }
    public void Clear()
    {
        requests.Clear();
        foreach (var layer in layers.Values) { layer.Intensity = 0; layer.Time = 0; layer.Requested = false; }
    }
}

#nullable enable
using Microsoft.Xna.Framework;
using AdvancedMod.Basic.Graphics.Rendering;

namespace AdvancedMod.Basic.Graphics;

public readonly record struct EffectHandle(ulong Value);
public readonly record struct EnvironmentHandle(ulong Value);
public readonly record struct VfxUpdateContext(float DeltaSeconds, double Time);
public readonly record struct VfxDrawContext(Vector2 Camera, Vector2 ViewSize);

/// <summary>仅管理表现；停止后只读取自己的快照，不持有实体引用。</summary>
public abstract class WorldEffect
{
    public abstract bool IsFinished { get; }
    public abstract void Update(in VfxUpdateContext context);
    public abstract void CollectDrawCommands(VfxDrawQueue queue, in VfxDrawContext context);
    public abstract void Stop();
    public virtual void ReleaseOwnedResources() { }
}

/// <summary>状态应是不可变值；可变数组须在提交前复制或转移所有权。</summary>
public interface IVfxTarget<T> where T : struct { void SetTarget(in T target); }
public interface IVfxSamples<T> where T : struct { void AppendSample(in T sample); }

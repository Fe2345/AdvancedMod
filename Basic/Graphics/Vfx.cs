using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Basic.Graphics;

/// <summary>客户端薄入口。调用方先完成机制，再提交不可变的表现快照。</summary>
public static class Vfx
{
    private static WorldEffectManager Manager => ModContent.GetInstance<WorldVfxSystem>().Manager;
    public static double Time => Main.dedServ ? 0 : Manager.Time;
    public static EffectHandle Create(WorldEffect effect, float delay = 0)
    {
        if (!Main.dedServ) return Manager.Create(effect, delay);
        effect.ReleaseOwnedResources();
        return default;
    }
    public static void SetTarget<T>(EffectHandle handle, T state) where T : struct
    { if (!Main.dedServ) Manager.SetTarget(handle, state); }
    public static void AppendSample<T>(EffectHandle handle, T sample) where T : struct
    { if (!Main.dedServ) Manager.AppendSample(handle, sample); }
    public static void Stop(EffectHandle handle) { if (!Main.dedServ) Manager.Stop(handle); }
    public static void Stop<T>(EffectHandle handle, T finalState) where T : struct
    { SetTarget(handle, finalState); Stop(handle); }
    public static void Kill(EffectHandle handle) { if (!Main.dedServ) Manager.Kill(handle); }
    public static EnvironmentHandle AcquireEnvironment(string style, EnvironmentTarget target) =>
        Main.dedServ ? default : Manager.AcquireEnvironment(style, target);
    public static void SetEnvironmentTarget(EnvironmentHandle handle, EnvironmentTarget target)
    { if (!Main.dedServ) Manager.SetEnvironmentTarget(handle, target); }
    public static void ReleaseEnvironment(EnvironmentHandle handle)
    { if (!Main.dedServ) Manager.ReleaseEnvironment(handle); }
}

// 2026-10-25：效果静态定义与唯一的包含关系声明。
using System;

namespace AdvancedMod.Basic.Effects;

/// <summary>共享的无玩家状态定义；冷却、计数必须放在独立 ModPlayer 中。</summary>
public abstract class EffectDefinition { }

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class IncludesEffectAttribute : Attribute
{
    public Type EffectType { get; }
    public int Weight { get; }
    public IncludesEffectAttribute(Type effectType, int weight = 1)
    {
        EffectType = effectType;
        Weight = weight;
    }
}

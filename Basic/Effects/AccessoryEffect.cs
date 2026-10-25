// 2026-10-25：饰品持续属性入口与最小命中事件契约。
using Terraria;

namespace AdvancedMod.Basic.Effects;

public abstract class AccessoryEffect : EffectDefinition
{
    public virtual void Apply(Player player, EffectPlayer effects, int stacks) { }
}

/// <summary>仅派发给开启且实现该接口的效果；继承 tML 命中方客户端执行语义。</summary>
public interface IEffectOnHitNPC
{
    void OnHitNPC(Player player, EffectPlayer effects, int stacks, NPC target, NPC.HitInfo hit, int damageDone);
}

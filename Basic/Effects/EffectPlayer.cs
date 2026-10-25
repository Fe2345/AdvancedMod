// 2026-10-25：逐玩家生命周期适配；装备之后冻结，不发送可推导层数的数据包。
using Terraria;
using Terraria.ModLoader;

namespace AdvancedMod.Basic.Effects;

public sealed class EffectPlayer : ModPlayer
{
    private EffectCollection effects;
    private EffectCollection Effects => effects ??= new EffectCollection(EffectSystem.Graph);

    public void Add<TEffect>(int stacks = 1) where TEffect : AccessoryEffect => Effects.Add<TEffect>(stacks);
    public bool Has<TEffect>() where TEffect : AccessoryEffect => Effects.Has<TEffect>();
    public int Stacks<TEffect>() where TEffect : AccessoryEffect => Effects.Stacks<TEffect>();

    public override void Initialize() => effects = null;
    public override void ResetEffects() => Effects.Reset(!Player.dead);
    public override void UpdateDead() => Effects.Reset(false);
    public override void OnRespawn() => Effects.Reset(false);
    public override void OnEnterWorld() => Effects.Reset(false);
    public override void PlayerDisconnect() => Effects.Reset(false);

    public override void PostUpdateEquips()
    {
        if (Player.dead) { Effects.Reset(false); return; }
        Effects.Freeze();
        Effects.Execute((type, stacks) => EffectSystem.Definitions[type].Apply(Player, this, stacks));
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // 收集期事件不使用上一刻数据，也不提前冻结不完整的装备集合。
        if (Player.dead || !Effects.IsFrozen) return;
        foreach (var entry in EffectSystem.HitHandlers)
        {
            int stacks = Effects.Stacks(entry.Type);
            if (stacks > 0) entry.Handler.OnHitNPC(Player, this, stacks, target, hit, damageDone);
        }
    }
}

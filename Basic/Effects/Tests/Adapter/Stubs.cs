// 2026-10-25：仅用于离线适配器测试，不代表游戏内更新或联网仿真。
using AdvancedMod.Basic.Effects;

namespace Terraria
{
    public sealed class Player { public bool dead; public int Applied; public int Hits; }
    public sealed class NPC { public struct HitInfo { } }
}
namespace Terraria.ModLoader
{
    public abstract class ModPlayer
    {
        public Terraria.Player Player { get; set; } = new();
        public virtual void Initialize() { }
        public virtual void ResetEffects() { }
        public virtual void UpdateDead() { }
        public virtual void OnRespawn() { }
        public virtual void OnEnterWorld() { }
        public virtual void PlayerDisconnect() { }
        public virtual void PostUpdateEquips() { }
        public virtual void OnHitNPC(Terraria.NPC target, Terraria.NPC.HitInfo hit, int damageDone) { }
    }
}
namespace AdvancedMod.Basic.Effects
{
    public static class EffectSystem
    {
        internal static EffectGraph Graph = new(new[] { typeof(Enabled), typeof(Disabled) });
        internal static IReadOnlyDictionary<Type, AccessoryEffect> Definitions = new Dictionary<Type, AccessoryEffect>
        { [typeof(Enabled)] = new Enabled(), [typeof(Disabled)] = new Disabled() };
        internal static (Type Type, IEffectOnHitNPC Handler)[] HitHandlers =
        { (typeof(Enabled), (IEffectOnHitNPC)Definitions[typeof(Enabled)]), (typeof(Disabled), (IEffectOnHitNPC)Definitions[typeof(Disabled)]) };
    }
    public sealed class Enabled : AccessoryEffect, IEffectOnHitNPC
    {
        public override void Apply(Terraria.Player p, EffectPlayer e, int stacks)
        {
            if (e.Stacks<Enabled>() != stacks || e.Has<Disabled>()) throw new Exception("冻结查询错误");
            p.Applied++;
        }
        public void OnHitNPC(Terraria.Player p, EffectPlayer e, int stacks, Terraria.NPC target, Terraria.NPC.HitInfo hit, int damageDone) => p.Hits += stacks;
    }
    public sealed class Disabled : AccessoryEffect, IEffectOnHitNPC
    {
        public void OnHitNPC(Terraria.Player p, EffectPlayer e, int stacks, Terraria.NPC target, Terraria.NPC.HitInfo hit, int damageDone) => throw new Exception("关闭效果不应收到事件");
    }
}

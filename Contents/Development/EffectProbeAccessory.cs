// 2026-10-25：默认不加载的饰品示例，演示菱形包含、固定收益、层数收益和冻结查询。
using AdvancedMod.Basic.Effects;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Development;

public sealed class EffectProbeAccessory : ModItem
{
    public override bool IsLoadingEnabled(Mod mod) => System.Environment.GetEnvironmentVariable("ADVANCEDMOD_EFFECT_EXAMPLE") == "1";
    public override string Texture => "Terraria/Images/Item_53";
    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 20;
        Item.accessory = true;
        Item.rare = ItemRarityID.White;
    }
    public override void UpdateAccessory(Player player, bool hideVisual) => player.GetModPlayer<EffectPlayer>().Add<Crown>();

    [IncludesEffect(typeof(Left))]
    [IncludesEffect(typeof(Right))]
    public sealed class Crown : AccessoryEffect
    {
        private const int CombinedDefense = 3;
        public override void Apply(Player player, EffectPlayer effects, int stacks)
        {
            if (effects.Has<Shared>()) player.statDefense += CombinedDefense;
        }
    }

    [IncludesEffect(typeof(Shared))]
    public sealed class Left : AccessoryEffect
    {
        private const int FixedDefense = 2;
        public override void Apply(Player player, EffectPlayer effects, int stacks) => player.statDefense += FixedDefense;
    }

    [IncludesEffect(typeof(Shared))]
    public sealed class Right : AccessoryEffect { }

    public sealed class Shared : AccessoryEffect, IEffectOnHitNPC
    {
        private const int DefensePerStack = 1;
        public override void Apply(Player player, EffectPlayer effects, int stacks) => player.statDefense += checked(stacks * DefensePerStack);
        public void OnHitNPC(Player player, EffectPlayer effects, int stacks, NPC target, NPC.HitInfo hit, int damageDone)
        {
            var state = player.GetModPlayer<EffectProbeState>();
            state.HitCount = checked(state.HitCount + 1);
        }
    }
}

/// <summary>跨刻状态单独归玩家所有；仅演示命中方本地计数，不参与联网玩法。</summary>
public sealed class EffectProbeState : ModPlayer
{
    public int HitCount { get; internal set; }
    public override void Initialize() => HitCount = 0;
    public override void UpdateDead() => HitCount = 0;
    public override void OnEnterWorld() => HitCount = 0;
}

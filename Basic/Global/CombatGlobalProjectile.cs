using Terraria.ModLoader;
using AdvancedMod.Basic.Calculate.Tml;

namespace AdvancedMod.Basic.Global;

/// <summary>
/// 每个弹幕的攻击描述存储；不自动从当前手持物品或父弹幕推导标签。
/// </summary>
public sealed class CombatGlobalProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    /// <summary>本弹幕独立持有的元素消耗修正，不从武器或父弹幕继承；禁止填零。</summary>
    public float ElementConsumptionMultiplier = 1f;
    /// <summary>本弹幕独立持有的反应 CD 修正，不从武器或父弹幕继承。</summary>
    public float ReactionCooldownMultiplier = 1f;
    public double ElementAmount { get; internal set; }
    public AttackProfile Profile { get; internal set; } = AttackProfile.Unknown;
}

using Terraria.ModLoader;
using AdvancedMod.Basic.Calculate.Tml;

namespace AdvancedMod.Basic.Global;

/// <summary>
/// 每个弹幕的攻击描述存储；不自动从当前手持物品或父弹幕推导标签。
/// </summary>
public sealed class CombatGlobalProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public AttackProfile Profile { get; internal set; } = AttackProfile.Unknown;
}

using Terraria.ModLoader;
using AdvancedMod.Basic.Calculate.Tml;

namespace AdvancedMod.Basic.Global;

/// <summary>
/// 每个物品的攻击描述存储；命中 Hook 只读取，标签修改与继承由后续上游模块设计。
/// </summary>
public sealed class CombatGlobalItem : GlobalItem
{
    public override bool InstancePerEntity => true;
    public AttackProfile Profile { get; internal set; } = AttackProfile.Unknown;
}

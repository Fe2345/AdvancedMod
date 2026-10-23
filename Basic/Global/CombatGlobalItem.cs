using Terraria.ModLoader;
using Terraria;
using System.IO;
using AdvancedMod.Basic.Element.Tml;
using AdvancedMod.Basic.Calculate.Tml;

namespace AdvancedMod.Basic.Global;

/// <summary>
/// 每个物品的攻击描述存储；命中 Hook 只读取，标签修改与继承由后续上游模块设计。
/// </summary>
public sealed class CombatGlobalItem : GlobalItem
{
    public override bool InstancePerEntity => true;
    public override void NetSend(Item item, BinaryWriter writer) =>
        ElementNetwork.WriteAttack(writer, Profile, ElementAmount, ElementConsumptionMultiplier, ReactionCooldownMultiplier);
    public override void NetReceive(Item item, BinaryReader reader)
    {
        var data = ElementNetwork.ReadAttack(reader);
        (Profile, ElementAmount, ElementConsumptionMultiplier, ReactionCooldownMultiplier) =
            (data.Profile, data.Amount, data.Consumption, data.Cd);
    }
    /// <summary>本物品直接命中时的元素消耗修正，不向弹幕继承；禁止填零。</summary>
    public float ElementConsumptionMultiplier = 1f;
    /// <summary>本物品直接命中时的反应 CD 修正，不向弹幕继承。</summary>
    public float ReactionCooldownMultiplier = 1f;
    public double ElementAmount { get; internal set; }
    public AttackProfile Profile { get; internal set; } = AttackProfile.Unknown;
}

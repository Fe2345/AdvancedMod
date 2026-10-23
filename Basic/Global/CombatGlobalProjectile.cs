using Terraria.ModLoader;
using Terraria;
using Terraria.ModLoader.IO;
using System.IO;
using Terraria.ID;
using Terraria.DataStructures;
using AdvancedMod.Basic.Utils;
using AdvancedMod.Basic.Element.Tml;
using AdvancedMod.Basic.Calculate.Tml;

namespace AdvancedMod.Basic.Global;

/// <summary>
/// 每个弹幕的攻击描述存储；不自动从当前手持物品或父弹幕推导标签。
/// </summary>
public sealed class CombatGlobalProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override void OnSpawn(Projectile projectile, IEntitySource source) => ElementAttacks.ApplySpawn(projectile, source);
    public override void OnKill(Projectile projectile, int timeLeft)
    {
        // 原版只由 owner 广播死亡；托管 NPC 命中改在服务器发生后，需要通知 owner 停止弹幕。
        if (Main.netMode == NetmodeID.Server && projectile.owner != Main.myPlayer && Profile.IsManagedAttack)
            NetMessage.SendData(MessageID.KillProjectile, number: projectile.identity, number2: projectile.owner);
    }
    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter writer) =>
        ElementNetwork.WriteAttack(writer, Profile, ElementAmount, ElementConsumptionMultiplier, ReactionCooldownMultiplier);
    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader reader)
    {
        var data = ElementNetwork.ReadAttack(reader);
        (Profile, ElementAmount, ElementConsumptionMultiplier, ReactionCooldownMultiplier) =
            (data.Profile, data.Amount, data.Consumption, data.Cd);
    }
    /// <summary>本弹幕独立持有的元素消耗修正，不从武器或父弹幕继承；禁止填零。</summary>
    public float ElementConsumptionMultiplier = 1f;
    /// <summary>本弹幕独立持有的反应 CD 修正，不从武器或父弹幕继承。</summary>
    public float ReactionCooldownMultiplier = 1f;
    public double ElementAmount { get; internal set; }
    public AttackProfile Profile { get; internal set; } = AttackProfile.Unknown;
}

using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using AdvancedMod.Contents.Hallow.Projectiles;

namespace AdvancedMod.Contents.Hallow.NPCs;

/// <summary>毁灭者原版激光及自定义技能的独立来源标记，不占所有弹幕的实例槽。</summary>
public sealed class DestroyerAttackOwner : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.type is ProjectileID.DeathLaser or ProjectileID.PinkLaser || entity.ModProjectile is DestroyerCrashProjectile;
    internal int Head = -1, Encounter = -1;
    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        if (source is not EntitySource_Parent parent) return;
        if (parent.Entity is NPC npc && npc.TryGetGlobalNPC<DestroyerBehavior>(out var behavior) && behavior.Hallow)
        {
            Head = npc.type == NPCID.TheDestroyer ? npc.whoAmI : npc.realLife;
            if (Head >= 0 && Head < Main.maxNPCs && Main.npc[Head].active)
                Encounter = Main.npc[Head].GetGlobalNPC<DestroyerBarState>().Encounter;
        }
        else if (parent.Entity is Projectile other && other.TryGetGlobalProjectile<DestroyerAttackOwner>(out var owner))
        { Head = owner.Head; Encounter = owner.Encounter; }
    }
    public override void SendExtraAI(Projectile projectile, BitWriter bits, BinaryWriter writer) { writer.Write(Head); writer.Write(Encounter); }
    public override void ReceiveExtraAI(Projectile projectile, BitReader bits, BinaryReader reader) { Head = reader.ReadInt32(); Encounter = reader.ReadInt32(); }
}

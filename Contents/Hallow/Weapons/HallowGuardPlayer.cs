using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace AdvancedMod.Contents.Hallow.Weapons;

/// <summary>按被保护玩家记录该伤害源的60刻减伤，不改写原始damage，也不影响其他玩家。</summary>
internal sealed class HallowParryMarks
{
    private readonly Dictionary<int, (ulong Until, bool Perfect)> marks = new();
    internal void Clear() => marks.Clear();
    internal void Mark(int owner, bool perfect) => marks[owner] = (Main.GameUpdateCount + HallowActionRules.ProtectionTicks, perfect);
    internal bool Apply(int owner, ref Player.HurtModifiers modifiers)
    {
        if (!marks.TryGetValue(owner, out var mark)) return false;
        if (Main.GameUpdateCount >= mark.Until) { marks.Remove(owner); return false; }
        if (mark.Perfect) modifiers.Cancel(); // 原生最终伤害至少1，完全免伤必须取消本次Hurt。
        else modifiers.FinalDamage *= HallowActionRules.GuardDamageMultiplier;
        return true;
    }
}
public sealed class HallowParryProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    internal readonly HallowParryMarks Marks = new();
    public override void OnSpawn(Projectile projectile, IEntitySource source) => Marks.Clear();
}
public sealed class HallowParryNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    internal readonly HallowParryMarks Marks = new();
    public override void OnSpawn(NPC npc, IEntitySource source) => Marks.Clear();
}
public sealed class HallowGuardPlayer : ModPlayer
{
    private ulong nextGuard;
    private bool restoreImmunity, previousImmune;
    private int previousImmuneTime;
    private int[] previousCooldowns;
    public bool CanStart => Main.GameUpdateCount >= nextGuard;
    public int Cooldown => CanStart ? 0 : (int)Math.Min((ulong)HallowActionRules.GuardCooldown, nextGuard - Main.GameUpdateCount);
    internal bool LastPerfect;
    internal int SuccessCount;
    internal void Begin() => nextGuard = Main.GameUpdateCount + HallowActionRules.GuardCooldown;
    public override void OnEnterWorld() { nextGuard = 0; SuccessCount = 0; }
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        int projectile = modifiers.DamageSource.SourceProjectileLocalIndex;
        int npc = modifiers.DamageSource.SourceNPCIndex;
        restoreImmunity = projectile >= 0 && projectile < Main.maxProjectiles
            ? Main.projectile[projectile].GetGlobalProjectile<HallowParryProjectile>().Marks.Apply(Player.whoAmI, ref modifiers)
            : npc >= 0 && npc < Main.maxNPCs && Main.npc[npc].GetGlobalNPC<HallowParryNPC>().Marks.Apply(Player.whoAmI, ref modifiers);
        if (!restoreImmunity) return;
        previousImmune = Player.immune; previousImmuneTime = Player.immuneTime;
        previousCooldowns = (int[])Player.hurtCooldowns.Clone();
    }
    public override void PostHurt(Player.HurtInfo info)
    {
        if (!restoreImmunity) return;
        // 招架保护期间的受伤也不新增原生受击无敌；保留这次伤害之前已有的其他来源免疫。
        Player.immune = previousImmune; Player.immuneTime = previousImmuneTime;
        previousCooldowns.CopyTo(Player.hurtCooldowns, 0);
        restoreImmunity = false;
    }
}

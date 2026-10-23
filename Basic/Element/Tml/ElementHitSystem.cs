#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Calculate.Tml;
using AdvancedMod.Basic.Global;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace AdvancedMod.Basic.Element.Tml;

/// <summary>原生伤害事务桥；不把 Modify／OnHit 假设为必然成对的事件。</summary>
public sealed class ElementHitSystem : ModSystem
{
    public static ReactionTable Reactions { get; private set; } = DevelopmentReactions.Create();
    public static event Action<ElementResolution>? Resolved;
    private static readonly Stack<Frame> frames = new();

    private sealed class Entry
    {
        internal required NativeElementTransaction Transaction;
        internal bool Owned;
        internal bool Armed;
    }
    private sealed class Frame
    {
        internal Item? Item;
        internal Projectile? Projectile;
        internal Player? Player;
        internal int PanelDamage;
        internal int InputDamage;
        internal NPC? OnlyTarget;
        internal Action? ReplayProjectile;
        internal ReactionPlan? EnvironmentPlan;
        internal readonly Dictionary<NPC, Entry> Npcs = new();
        internal readonly HashSet<NPC> Deferred = new();
        internal NativeElementTransaction? PlayerTransaction;
    }

    public override void Load()
    {
        On_Player.ProcessHitAgainstNPC += ItemHit;
        On_Projectile.Damage += ProjectileDamage;
        On_NPC.StrikeNPC_HitInfo_bool_bool += Strike;
        On_Player.Hurt_PlayerDeathReason_int_int_refHurtInfo_bool_bool_int_bool_float_float_float += Hurt;
    }
    public override void Unload()
    {
        On_Player.ProcessHitAgainstNPC -= ItemHit;
        On_Projectile.Damage -= ProjectileDamage;
        On_NPC.StrikeNPC_HitInfo_bool_bool -= Strike;
        On_Player.Hurt_PlayerDeathReason_int_int_refHurtInfo_bool_bool_int_bool_float_float_float -= Hurt;
        frames.Clear();
        Resolved = null;
        Reactions = DevelopmentReactions.Create();
    }

    private static void Complete(ElementResolution result) => Resolved?.Invoke(result);

    private static void ItemHit(On_Player.orig_ProcessHitAgainstNPC orig, Player player, Item item,
        Rectangle rectangle, int damage, float knockback, int npcIndex)
    {
        var data = item.GetGlobalItem<CombatGlobalItem>();
        if (!data.Profile.IsManagedAttack) { orig(player, item, rectangle, damage, knockback, npcIndex); return; }
        NPC npc = Main.npc[npcIndex];
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            if (player.whoAmI == Main.myPlayer) ElementNetwork.RequestItem(player, npc, rectangle);
            return;
        }
        var target = npc.GetGlobalNPC<CombatGlobalNPC>().Elements;
        target.RunNative(Reactions, tx =>
        {
            if (!npc.active || !player.active || player.dead) return;
            var frame = new Frame { Item = item, PanelDamage = damage, InputDamage = damage };
            frame.Npcs.Add(npc, new Entry { Transaction = tx });
            RunFrame(frame, () => orig(player, item, rectangle, damage, knockback, npcIndex));
        }, () => ReferenceEquals(target, npc.GetGlobalNPC<CombatGlobalNPC>().Elements), Complete);
    }

    private static void ProjectileDamage(On_Projectile.orig_Damage orig, Projectile projectile)
    {
        if (!projectile.GetGlobalProjectile<CombatGlobalProjectile>().Profile.IsManagedAttack)
        { orig(projectile); return; }
        RunFrame(new Frame { Projectile = projectile, PanelDamage = projectile.damage,
            InputDamage = projectile.damage, ReplayProjectile = () => orig(projectile) }, () => orig(projectile));
    }

    internal static bool CanHitNpc(NPC npc, Projectile projectile)
    {
        // 只禁止客户端的托管 NPC 命中，保留原生切砖、爆炸自伤及 PvP 路径。
        if (Main.netMode == NetmodeID.MultiplayerClient && projectile.friendly && !projectile.hostile &&
            projectile.GetGlobalProjectile<CombatGlobalProjectile>().Profile.IsManagedAttack) return false;
        if (frames.Count == 0 || frames.Peek().Projectile != projectile) return true;
        Frame frame = frames.Peek();
        if (frame.OnlyTarget is not null && frame.OnlyTarget != npc) return false;
        if (frame.Npcs.ContainsKey(npc)) return true;
        var target = npc.GetGlobalNPC<CombatGlobalNPC>().Elements;
        if (!target.IsResolving) return true;
        if (frame.Deferred.Add(npc))
        {
            var sourceData = projectile.GetGlobalProjectile<CombatGlobalProjectile>();
            int identity = projectile.identity;
            target.RunNative(Reactions, tx =>
            {
                // 重新执行原生碰撞／免疫判定，仅允许原来排队的目标；失效弹幕不复活。
                if (!npc.active || !projectile.active || projectile.identity != identity ||
                    !ReferenceEquals(sourceData, projectile.GetGlobalProjectile<CombatGlobalProjectile>())) return;
                var replay = new Frame { Projectile = projectile, OnlyTarget = npc,
                    PanelDamage = frame.PanelDamage, InputDamage = projectile.damage, ReplayProjectile = frame.ReplayProjectile };
                replay.Npcs.Add(npc, new Entry { Transaction = tx });
                RunFrame(replay, frame.ReplayProjectile!);
            }, () => ReferenceEquals(target, npc.GetGlobalNPC<CombatGlobalNPC>().Elements), Complete);
        }
        return false;
    }

    internal static CombatContext PrepareNpc(CombatContext context, ref NPC.HitModifiers modifiers)
    {
        if (frames.Count == 0) return context;
        Frame frame = frames.Peek();
        if (context.SourceItem != frame.Item || context.SourceProjectile != frame.Projectile) return context;
        NPC npc = context.TargetNpc!;
        if (!frame.Npcs.TryGetValue(npc, out Entry? entry))
        {
            if (frame.Projectile is null) return context;
            var target = npc.GetGlobalNPC<CombatGlobalNPC>().Elements;
            entry = new Entry { Transaction = target.OpenNative(Reactions,
                () => ReferenceEquals(target, npc.GetGlobalNPC<CombatGlobalNPC>().Elements), Complete), Owned = true };
            frame.Npcs.Add(npc, entry);
        }
        var request = MakeRequest(context, frame.PanelDamage);
        ReactionPlan plan = entry.Transaction.Prepare(request);
        if (plan.Route == ElementRoute.React) modifiers.SourceDamage *= SeedRatio(plan, frame.InputDamage);
        // 在 ToHitInfo 完成计算时标记即将进入 Strike 的命中，不从普通 OnHit 推断成功。
        modifiers.ModifyHitInfo += (ref NPC.HitInfo _) => entry.Armed = true;
        return FinalContext(context, plan);
    }

    private static int Strike(On_NPC.orig_StrikeNPC_HitInfo_bool_bool orig, NPC npc,
        NPC.HitInfo hit, bool fromNet, bool noPlayerInteraction)
    {
        Entry? entry = null;
        if (!fromNet && frames.Count != 0 && frames.Peek().Npcs.TryGetValue(npc, out var found) && found.Armed)
        { entry = found; entry.Armed = false; }
        int actual = orig(npc, hit, fromNet, noPlayerInteraction);
        if (entry is not null) entry.Transaction.DamageDone = actual;
        return actual;
    }

    private static double Hurt(On_Player.orig_Hurt_PlayerDeathReason_int_int_refHurtInfo_bool_bool_int_bool_float_float_float orig,
        Player player, PlayerDeathReason reason, int damage, int direction, out Player.HurtInfo info,
        bool pvp, bool quiet, int cooldown, bool dodgeable, float armorPenetration, float scalingArmorPenetration, float knockback)
    {
        if (pvp || Main.netMode != NetmodeID.SinglePlayer && player.whoAmI != Main.myPlayer || !ManagedSource(reason))
            return orig(player, reason, damage, direction, out info, pvp, quiet, cooldown, dodgeable, armorPenetration, scalingArmorPenetration, knockback);
        var target = player.GetModPlayer<CombatPlayer>().Elements;
        Player.HurtInfo captured = default;
        double actual = 0;
        target.RunNative(Reactions, tx =>
        {
            if (!player.active || player.dead) return;
            var frame = new Frame { Player = player, PlayerTransaction = tx, InputDamage = damage };
            RunFrame(frame, () => actual = orig(player, reason, damage, direction, out captured,
                pvp, quiet, cooldown, dodgeable, armorPenetration, scalingArmorPenetration, knockback));
            tx.DamageDone = actual;
        }, () => ReferenceEquals(target, player.GetModPlayer<CombatPlayer>().Elements), Complete);
        // 原生同步返回无法预知排队结果；重入时返回 0，真实结果通过 Resolved 发布。
        info = captured;
        return actual;
    }

    private static bool ManagedSource(PlayerDeathReason reason)
    {
        if (!reason.TryGetCausingEntity(out Entity source)) return false;
        return source switch
        {
            NPC npc => npc.GetGlobalNPC<CombatGlobalNPC>().Profile.IsManagedAttack,
            Projectile projectile => projectile.GetGlobalProjectile<CombatGlobalProjectile>().Profile.IsManagedAttack,
            _ => false
        };
    }

    internal static CombatContext PreparePlayer(CombatContext context, ref Player.HurtModifiers modifiers)
    {
        if (frames.Count == 0 || frames.Peek().Player != context.TargetPlayer) return context;
        var frame = frames.Peek();
        if (frame.PlayerTransaction is null) return context;
        int panel = context.SourceProjectile?.damage ?? context.AttackerNpc?.damage ?? frame.InputDamage;
        var plan = frame.PlayerTransaction.Prepare(MakeRequest(context, panel));
        if (plan.Route == ElementRoute.React) modifiers.SourceDamage *= SeedRatio(plan, frame.InputDamage);
        return FinalContext(context, plan);
    }

    internal static void PrepareEnvironmentalHurt(Player player, ref Player.HurtModifiers modifiers)
    {
        if (frames.Count == 0 || frames.Peek().Player != player || frames.Peek().EnvironmentPlan is not { } plan) return;
        var context = CombatContext.ForEnvironment(plan.Hit.HitId, player, new(plan.Hit.Tags, isManagedAttack: true));
        CalculateSystem.Pipeline.ModifyPlayerHurt(context, ref modifiers);
    }

    private static ElementRequest MakeRequest(CombatContext context, int panel)
    {
        if (context.SourceItem is { } item)
        {
            var data = item.GetGlobalItem<CombatGlobalItem>();
            return new(context.CreateSnapshot(), panel, data.ElementAmount, data.ElementConsumptionMultiplier, data.ReactionCooldownMultiplier);
        }
        if (context.SourceProjectile is { } projectile)
        {
            var data = projectile.GetGlobalProjectile<CombatGlobalProjectile>();
            return new(context.CreateSnapshot(), panel, data.ElementAmount, data.ElementConsumptionMultiplier, data.ReactionCooldownMultiplier);
        }
        return new(context.CreateSnapshot(), panel, context.AttackerNpc!.GetGlobalNPC<CombatGlobalNPC>().ElementAmount);
    }
    private static CombatContext FinalContext(CombatContext context, ReactionPlan plan) =>
        context.WithProfile(new(plan.Hit.Tags, context.Profile.AttackId, context.Profile.IsManagedAttack, context.Profile.SourceItemId));
    private static float SeedRatio(ReactionPlan plan, int damage)
    {
        if (damage <= 0) throw new InvalidOperationException("真实反应的原生基础伤害必须为正。");
        float ratio = (float)(plan.DamageSeed / damage);
        if (!float.IsFinite(ratio)) throw new ArithmeticException("原生反应伤害倍率溢出。");
        return ratio;
    }

    private static void RunFrame(Frame frame, Action execute)
    {
        Exception? error = null;
        bool returned = false;
        frames.Push(frame);
        try { execute(); returned = true; }
        catch (Exception exception) { error = exception; }
        finally { frames.Pop(); }
        foreach (var entry in frame.Npcs.Values.Where(e => e.Owned))
        {
            try { entry.Transaction.Finish(returned); }
            catch (Exception exception) { error ??= exception; }
        }
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }

    public override void PostUpdateEverything()
    {

        long tick = checked((long)Main.GameUpdateCount);
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) continue;
            var target = npc.GetGlobalNPC<CombatGlobalNPC>().Elements;
            if (!target.HasState) continue;
            var context = CombatContext.ForEnvironment(CalculateSystem.NextHitId(), npc, AttackProfile.Unknown);
            target.Tick(tick, new(context.CreateSnapshot(), 0, isIdle: true), Reactions, plan =>
            {
                if (npc.dontTakeDamage || npc.immortal) return false;
                var final = FinalContext(context, plan);
                var modifiers = npc.GetIncomingStrikeModifiers(DamageClass.Generic, 0);
                CalculateSystem.Pipeline.ModifyNpcHit(final, ref modifiers);
                var hit = modifiers.ToHitInfo(CheckedDamage(plan.DamageSeed), false, 0, false);
                int actual = npc.StrikeNPC(hit, false, true);
                if (Main.netMode == NetmodeID.Server) NetMessage.SendStrikeNPC(npc, in hit);
                return actual > 0;
            }, Complete, () => ReferenceEquals(target, npc.GetGlobalNPC<CombatGlobalNPC>().Elements));
        }
        foreach (Player player in Main.ActivePlayers)
        {
            if (player.dead || Main.netMode != NetmodeID.SinglePlayer && player.whoAmI != Main.myPlayer) continue;
            var target = player.GetModPlayer<CombatPlayer>().Elements;
            if (!target.HasState) continue;
            var context = CombatContext.ForEnvironment(CalculateSystem.NextHitId(), player, AttackProfile.Unknown);
            target.Tick(tick, new(context.CreateSnapshot(), 0, isIdle: true), Reactions, plan =>
            {
                double actual = 0;
                RunFrame(new Frame { Player = player, EnvironmentPlan = plan }, () =>
                    actual = player.Hurt(PlayerDeathReason.ByCustomReason(NetworkText.FromLiteral($"{player.name} 被元素反应击败了。")), CheckedDamage(plan.DamageSeed), 0));
                return actual > 0;
            }, Complete, () => ReferenceEquals(target, player.GetModPlayer<CombatPlayer>().Elements));
        }
    }
    private static int CheckedDamage(double damage)
    {
        if (!double.IsFinite(damage) || damage < 0 || damage > int.MaxValue) throw new ArithmeticException("反应伤害超出引擎整数范围。");
        return (int)Math.Round(damage);
    }
}

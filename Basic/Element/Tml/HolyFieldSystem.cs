#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedMod.Basic.Calculate.Core;
using AdvancedMod.Basic.Calculate.Tml;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Basic.Element.Tml;

/// <summary>玩家普通神圣结界；状态及派生伤害仅在NPC权威侧更新。</summary>
public sealed class HolyFieldSystem : ModSystem
{
    internal const string TornadoAttack = "AdvancedMod:HolyTornado";
    internal const string FireAttack = "AdvancedMod:HolySkyFire";
    internal const string BurstAttack = "AdvancedMod:HolyDischarge";
    internal sealed class Field(Player owner, Vector2 center)
    {
        internal readonly long Identity = ++nextIdentity;
        internal readonly long Created = Tick;
        internal int Revision;
        internal readonly Player Owner = owner;
        internal Vector2 Center = center, Destination = center, NextDestination = center;
        internal bool Moving;
        internal float RadiusMultiplier = 1;
        internal float Radius => HolyReactionRules.BaseRadius * RadiusMultiplier;
        internal long Expires = Tick + HolyReactionRules.Lifetime;
        internal readonly HolyLifeRecord Life = new();
        internal readonly List<Vector2> Nodes = new();
        internal int Charge;
        internal long? TornadoDue;
        internal Tornado? Storm;
        internal bool IsActive => Owner.active && !Owner.dead && Tick < Expires &&
            Fields.TryGetValue(Owner.whoAmI, out var current) && ReferenceEquals(current, this);
        internal bool Contains(Vector2 p) => IsActive && Vector2.DistanceSquared(p, Center) <= Radius * Radius;
        internal void PlaceNode(Vector2 point)
        {
            Nodes.Remove(point); Nodes.Add(point);
            if (Nodes.Count > HolyReactionRules.MaximumNodes) Nodes.RemoveAt(0);
        }
    }
    internal sealed class Tornado(Vector2 position, int bounces)
    {
        internal Vector2 Position = position;
        internal int Remaining = bounces, Elapsed, LastRoot = -1;
        internal NPC? Target;
        internal long Identity;
    }
    internal static readonly Dictionary<int, Field> Fields = new();
    private static long nextIdentity;
    private static readonly Dictionary<(int, long), bool> fireUsed = new();
    internal static long Tick => (long)Main.GameUpdateCount;
    internal static int Root(NPC npc) => npc.realLife >= 0 ? npc.realLife : npc.whoAmI;
    internal static bool Enemy(NPC npc) => npc.active && npc.CanBeChasedBy() && !npc.friendly && !npc.dontTakeDamage && !npc.immortal;
    public override void OnWorldLoad() => Clear();
    public override void OnWorldUnload() => Clear();
    public override void Unload() => Clear();
    internal static void Clear() { Fields.Clear(); fireUsed.Clear(); }

    internal static bool Eligible(CombatContext context)
    {
        if (context.AttackerPlayer is not { active: true, dead: false } || context.TargetNpc is not { } target) return false;
        if (context.Profile.Tags.Contains(TagEnum.Hallow) || Fields.Values.Any(f => f.Contains(target.Center))) return true;
        // 使用与原版相同的场景扫描，位置取反应目标，不借用远处攻击者的ZoneHallow。
        var metrics = new SceneMetrics();
        metrics.ScanAndExportToMain(new SceneMetricsScanSettings { BiomeScanCenterPositionInWorld = target.Center });
        return metrics.EnoughTilesForHallow;
    }
    internal static Field Activate(Player owner, Vector2 position)
    {
        if (!Fields.TryGetValue(owner.whoAmI, out var field) || !field.IsActive)
            Fields[owner.whoAmI] = field = new(owner, position);
        // 按反应发生位置判断，边界计入结界内；内部反应仅续时，不改写当前或待执行目的地。
        if (!field.Contains(position)) field.NextDestination = position;
        field.Expires = Tick + HolyReactionRules.Lifetime;
        // 超距反应提交时立即迁移作用范围，不等待下一次世界更新。
        TeleportIfTooFar(field);
        HolyFieldVisuals.Publish();
        return field;
    }
    internal static void Close(Player owner) { Fields.Remove(owner.whoAmI); HolyFieldVisuals.Publish(); }
    private static bool TeleportIfTooFar(Field field)
    {
        if (Vector2.Distance(field.Center, field.NextDestination) <= field.Radius * HolyReactionRules.TeleportRadiusFactor) return false;
        field.Center = field.Destination = field.NextDestination;
        field.Revision++;
        field.Moving = false;
        return true;
    }
    internal static void RecordLife(Field field, int paid)
    {
        field.Life.Add(Tick, paid);
        if (field.Life.Total >= HolyReactionRules.TornadoThreshold && field.TornadoDue is null && field.Storm is null)
            field.TornadoDue = Tick + HolyReactionRules.TornadoCooldown;
    }
    internal static int Level(Player p) => 46 + p.ConsumedLifeCrystals + p.ConsumedManaCrystals + p.ConsumedLifeFruit
        + 3 * Count(Main.hardMode, NPC.downedPlantBoss, NPC.downedMoonlord, NPC.downedEmpressOfLight)
        + 2 * Count(NPC.downedBoss1, NPC.downedBoss2, NPC.downedBoss3, NPC.downedMechBoss1,
            NPC.downedMechBoss2, NPC.downedMechBoss3, NPC.downedAncientCultist)
        + Count(NPC.downedSlimeKing, NPC.downedDeerclops, NPC.downedQueenSlime, NPC.downedGolemBoss, NPC.downedFishron);
    private static int Count(params bool[] values) => values.Count(x => x);

    internal static float Reduction(Player owner, NPC target)
    {
        float best = 0;
        foreach (var field in Fields.Values)
        {
            if (!field.Contains(target.Center) || field.Owner != owner && (owner.team == 0 || field.Owner.team != owner.team)) continue;
            field.Life.Expire(Tick);
            best = Math.Max(best, HolyReactionRules.Reduction(field.Life.Total));
        }
        return best;
    }
    internal static bool TryBridge(Field field, Vector2 from, NPC to, out Vector2 via, out bool core)
    {
        foreach (var point in field.Nodes.Append(field.Center))
        {
            if (!field.Contains(point) || Vector2.Distance(from, point) > HolyReactionRules.BridgeRadius ||
                Vector2.Distance(point, to.Center) > HolyReactionRules.BridgeRadius ||
                !Collision.CanHitLine(from, 1, 1, point, 1, 1) || !Collision.CanHitLine(point, 1, 1, to.Center, 1, 1)) continue;
            via = point; core = point == field.Center; return true;
        }
        via = default; core = false; return false;
    }
    internal static void Charge(Field field)
    {
        if (++field.Charge < HolyReactionRules.ChargeThreshold) return;
        field.Charge -= HolyReactionRules.ChargeThreshold;
        Discharge(field);
    }

    internal static bool CanSkyFire(CombatContext context, ElementRequest request)
    {
        if (context.TargetNpc is not { } npc || request.IncomingSlot is not (1 or 4)) return false;
        var state = npc.GetGlobalNPC<CombatGlobalNPC>().Elements.Snapshot;
        if (state.ReactionCooldowns[3] <= 0) return false;
        double cost = 1.5 * request.ConsumptionMultiplier;
        return state.Elements[1].Total + (request.IncomingSlot == 1 ? request.Amount : 0) >= cost &&
            state.Elements[4].Total + (request.IncomingSlot == 4 ? request.Amount : 0) >= cost && Eligible(context);
    }
    internal static void SkyFire(CombatContext context)
    {
        if (context.TargetNpc is not { active: true } npc || context.AttackerPlayer is not { active: true, dead: false } owner) return;
        NPC root = Main.npc[Root(npc)];
        var key = (root.whoAmI, root.GetGlobalNPC<CombatGlobalNPC>().NetworkIdentity);
        if (!Fields.TryGetValue(owner.whoAmI, out var field)) return;
        if (!fireUsed.TryAdd(key, true)) return;
        Deal(field, npc, TagEnum.Pyro, TagEnum.PyroCryo, FireAttack, Level(owner) * field.RadiusMultiplier);
    }
    internal static void ResetMeltWindow(NPC npc)
    {
        NPC root = Main.npc[Root(npc)];
        fireUsed.Remove((root.whoAmI, root.GetGlobalNPC<CombatGlobalNPC>().NetworkIdentity));
    }

    public override void PostUpdateEverything()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || Main.gameMenu) return;
        foreach (var key in fireUsed.Keys.ToArray())
        {
            var root = Main.npc[key.Item1];
            if (!root.active || root.GetGlobalNPC<CombatGlobalNPC>().NetworkIdentity != key.Item2 ||
                !Main.npc.Any(n => n.active && Root(n) == root.whoAmI && n.GetGlobalNPC<CombatGlobalNPC>().Elements.Snapshot.ReactionCooldowns[3] > 0))
                fireUsed.Remove(key);
        }
        foreach (var field in Fields.Values.ToArray())
        {
            if (!field.Owner.active || field.Owner.dead || Tick >= field.Expires)
            {
                Mod.Logger.Debug($"神圣结界关闭：owner={field.Owner.whoAmI}, identity={field.Identity}, active={field.Owner.active}, dead={field.Owner.dead}, tick={Tick}, expires={field.Expires}");
                Fields.Remove(field.Owner.whoAmI); continue;
            }
            Update(field);
        }
    }
    internal static void Update(Field field)
    {
        field.Life.Expire(Tick);
        if (!TeleportIfTooFar(field))
        {
            if (!field.Moving) { field.Destination = field.NextDestination; field.Moving = true; }
            Vector2 delta = field.Destination - field.Center;
            if (delta.Length() <= HolyReactionRules.FieldSpeed) { field.Center = field.Destination; field.Moving = false; }
            else field.Center += Vector2.Normalize(delta) * HolyReactionRules.FieldSpeed;
        }
        if (field.Storm is null && field.TornadoDue is { } due && Tick >= due && field.Life.Total >= HolyReactionRules.TornadoThreshold)
        { field.Storm = new(field.Center, HolyReactionRules.Bounces(field.Life.Total)); field.TornadoDue = null; }
        if (field.Storm is { } storm) UpdateStorm(field, storm);
    }
    private static void EndStorm(Field field)
    { field.Storm = null; field.TornadoDue = Tick + HolyReactionRules.TornadoCooldown; }
    private static void UpdateStorm(Field field, Tornado storm)
    {
        if (storm.Remaining <= 0) { EndStorm(field); return; }
        var enemies = Main.npc.Where(n => Enemy(n) && field.Contains(n.Center)).ToArray();
        if (enemies.Select(Root).Distinct().Take(2).Count() < 2) { EndStorm(field); return; }
        if (storm.Target is not { } target || !Enemy(target) || !field.Contains(target.Center) ||
            target.GetGlobalNPC<CombatGlobalNPC>().NetworkIdentity != storm.Identity)
        {
            target = enemies.Where(n => Root(n) != storm.LastRoot).OrderBy(n => Vector2.DistanceSquared(n.Center, storm.Position)).ThenBy(n => n.whoAmI).FirstOrDefault();
            if (target is null) { EndStorm(field); return; }
            storm.Target = target; storm.Identity = target.GetGlobalNPC<CombatGlobalNPC>().NetworkIdentity;
        }
        Vector2 delta = target.Center - storm.Position;
        float speed = HolyReactionRules.FlightSpeed(delta.Length(), storm.Elapsed);
        storm.Elapsed++;
        if (delta.Length() > speed) { storm.Position += Vector2.Normalize(delta) * speed; return; }
        storm.Position = target.Center;
        Deal(field, target, TagEnum.Hydro, TagEnum.HydroAnemo, TornadoAttack, Level(field.Owner) * HolyReactionRules.TornadoDamagePerLevel);
        storm.LastRoot = Root(target); storm.Remaining--; storm.Target = null; storm.Elapsed = 0;
        if (storm.Remaining == 0) EndStorm(field);
    }

    internal static void Deal(Field field, NPC npc, TagEnum element, TagEnum reaction, string attack, float seed)
    {
        if (!Enemy(npc)) return;
        var profile = new AttackProfile(new TagSet(new[] { new TagEntry(false, element), new TagEntry(false, reaction), new TagEntry(false, TagEnum.Hallowed) }), attack, true);
        var context = CombatContext.ForHoly(field.Owner, npc, profile);
        var elements = npc.GetGlobalNPC<CombatGlobalNPC>().Elements;
        elements.RunNative(ElementHitSystem.Reactions, tx =>
        {
            if (!Enemy(npc)) return;
            tx.Prepare(new(context.CreateSnapshot(), (int)Math.Round(seed)));
            var modifiers = npc.GetIncomingStrikeModifiers(DamageClass.Generic, 0);
            CalculateSystem.Pipeline.ModifyNpcHit(context, ref modifiers);
            bool crit = Main.rand.NextFloat(100) < field.Owner.GetTotalCritChance(DamageClass.Generic);
            var hit = modifiers.ToHitInfo(seed, crit, 0, false);
            npc.PlayerInteraction(field.Owner.whoAmI);
            tx.DamageDone = npc.StrikeNPC(hit);
            if (Main.netMode == NetmodeID.Server) NetMessage.SendStrikeNPC(npc, in hit);
        }, () => ReferenceEquals(elements, npc.GetGlobalNPC<CombatGlobalNPC>().Elements), ElementHitSystem.Complete);
    }

    // 候选弦由敌人矩形的顶点对确定；在候选覆盖集合上求至多五条线的最大并集。
    internal static void Discharge(Field field)
    {
        var enemies = Main.npc.Where(n => Enemy(n) && field.Contains(n.Center)).GroupBy(Root).Select(g => g.First()).ToArray();
        if (enemies.Length == 0) return;
        var candidates = HolyChordSelection.Select(field.Center, field.Radius, enemies.Select(n => n.Hitbox).ToArray());
        var selected = new HashSet<int>();
        foreach (var line in candidates)
        {
            foreach (int index in line.Targets) selected.Add(index);
        }
        foreach (int index in selected) Deal(field, enemies[index], TagEnum.Electro, TagEnum.HydroElectro, BurstAttack, Level(field.Owner) * field.RadiusMultiplier);
        foreach (var line in candidates) ElementNetwork.ShowReaction(line.Start, line.End, TagEnum.HydroElectro);
    }
}

[CombatContributor(100301)]
internal sealed class HolyFieldContributor : ICombatContributor
{
    public HitKinds SupportedHits => HitKinds.ItemToNpc | HitKinds.ProjectileToNpc;
    public bool IsActive(in CombatContext context) => context.AttackerPlayer is not null && context.TargetNpc is not null;
    public void Collect(in CombatContext context, ContributionWriter writer)
    {
        var tags = context.Profile.Tags;
        var first = tags.Select(t => ElementSlots.FromTag(t.type)).FirstOrDefault(s => s.HasValue);
        if (first is 2 or 6)
        {
            float amount = HolyFieldSystem.Reduction(context.AttackerPlayer!, context.TargetNpc!);
            if (amount > 0)
            {
                writer.SetEnabled("神圣减抗", ModifierTarget.Resistance, true, "自身或同队伍结界");
                writer.AddValue("神圣减抗", ModifierTarget.Resistance, -amount);
            }
        }
        if (context.Profile.AttackId == HolyFieldSystem.TornadoAttack) writer.SetIgnoreDefense("龙卷真实伤害", true);
    }
}

internal sealed class HolyTrueDamageRule : ICombatRule
{
    public string Id => "AdvancedMod:HolyTrueDamage";
    public HitKinds SupportedHits => HitKinds.ProjectileToNpc;
    public void Evaluate(in RuleContext context, GateWriter writer)
    {
        if (context.Hit.AttackId == HolyFieldSystem.TornadoAttack)
            writer.SetEnabled("忽略抗性", ModifierTarget.Resistance, false, "神圣龙卷真实伤害");
    }
}

using System;
using System.IO;
using System.Linq;
using AdvancedMod.Basic.Global;
using AdvancedMod.Basic.Tag;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using R = AdvancedMod.Contents.Hallow.NPCs.DestroyerPhaseTwoRules;

namespace AdvancedMod.Contents.Hallow.NPCs;

public sealed partial class DestroyerBehavior
{
    public enum PhaseTwoState : byte { Inactive, Prepare, Reposition, Telegraph, Crash, Recover, Idle, UltimateWarning, UltimateStrike }
    public PhaseTwoState PhaseState { get; private set; }
    public bool PhaseTwo => PhaseState != PhaseTwoState.Inactive;
    public int ReactionCount { get; private set; }
    public int PhaseTimer { get; private set; }
    public int UltimateRepetition { get; private set; }
    public int UltimateSerial { get; private set; }
    public int CompletedCrashes { get; private set; }
    private int cycleCrashes;
    private Vector2 waypoint, lockedTarget, crashDirection;
    private float entrySpeed;
    internal bool Hidden => PhaseState is PhaseTwoState.UltimateWarning or PhaseTwoState.UltimateStrike;

    private void ResetPhaseTwo()
    {
        PhaseState = PhaseTwoState.Inactive;
        ReactionCount = PhaseTimer = UltimateRepetition = UltimateSerial = CompletedCrashes = cycleCrashes = 0;
        waypoint = lockedTarget = crashDirection = Vector2.Zero; entrySpeed = 0;
        CrashAttackFired = false; SelectedAttack = default;
    }
    internal static DestroyerBehavior HeadBehavior(NPC npc)
    {
        int index = npc.type == NPCID.TheDestroyer ? npc.whoAmI : npc.realLife;
        return index >= 0 && index < Main.maxNPCs && Main.npc[index].active && Main.npc[index].type == NPCID.TheDestroyer
            ? Main.npc[index].GetGlobalNPC<DestroyerBehavior>() : null;
    }
    internal void RecordReaction(NPC head)
    {
        if (!Hallow || !PhaseTwo || ReactionCount >= R.ChargeMaximum) return;
        ReactionCount++; head.netUpdate = true;
    }
    private void Enter(NPC head, PhaseTwoState next)
    {
        PhaseState = next; PhaseTimer = 0; entrySpeed = head.velocity.Length();
        if (next == PhaseTwoState.Prepare)
            waypoint = Main.player[head.target].Center - new Vector2(0, R.Runway);
        if (next == PhaseTwoState.Reposition)
        {
            Vector2 radial = (head.Center - Main.player[head.target].Center).SafeNormalize(Vector2.UnitX);
            waypoint = Main.player[head.target].Center + radial * R.Runway;
        }
        if (next == PhaseTwoState.Crash)
        {
            SelectCrashAttack();
            var player = Main.player[head.target];
            Vector2 lead = player.velocity * R.PredictionTicks;
            if (lead.Length() > R.PredictionLimit) lead = lead.SafeNormalize(Vector2.Zero) * R.PredictionLimit;
            lockedTarget = player.Center + lead;
            crashDirection = (lockedTarget - head.Center).SafeNormalize(Vector2.UnitY);
        }
        if (next == PhaseTwoState.UltimateWarning && UltimateRepetition == 0)
        { ReactionCount = 0; UltimateSerial++; }
        if (next == PhaseTwoState.UltimateWarning) SpawnUltimatePreview(head);
        head.netUpdate = true;
    }
    private bool BeginUltimate(NPC head, bool idleLottery)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return false;
        if (ReactionCount < R.ChargeMaximum && (!idleLottery || Main.rand.NextFloat(R.ChargeMaximum) >= ReactionCount)) return false;
        UltimateRepetition = 0; Enter(head, PhaseTwoState.UltimateWarning); return true;
    }
    private void FinishSkill(NPC head)
    {
        if (BeginUltimate(head, false)) return;
        if (cycleCrashes < 2) Enter(head, PhaseTwoState.Reposition);
        else
        {
            cycleCrashes = 0;
            if (!BeginUltimate(head, true)) Enter(head, PhaseTwoState.Idle);
        }
    }
    private void Steer(NPC npc, Vector2 target, float speed)
    {
        float angle = npc.velocity.LengthSquared() > .01f ? npc.velocity.ToRotation() : (target - npc.Center).ToRotation();
        angle = angle.AngleTowards((target - npc.Center).ToRotation(), MathHelper.ToRadians(R.TurnDegrees));
        npc.velocity = angle.ToRotationVector2() * speed;
    }
    private bool TickPhaseTwo(NPC npc)
    {
        var headData = HeadBehavior(npc);
        if (headData == null) return false;
        if (npc.target < 0 || npc.target >= Main.maxPlayers || !Main.player[npc.target].active || Main.player[npc.target].dead) npc.TargetClosest();
        bool retreat = Main.IsItDay() || !Main.player[npc.target].active || Main.player[npc.target].dead;
        if (retreat) return false;
        if (!headData.PhaseTwo)
        {
            if (npc.type != NPCID.TheDestroyer || Main.netMode == NetmodeID.MultiplayerClient || !IsPhaseTwoReady(npc)) return false;
            Enter(npc, PhaseTwoState.Prepare);
        }
        if (!ValidatePreviousSegment(npc)) return true;
        var combat = npc.GetGlobalNPC<CombatGlobalNPC>();
        if (!combat.Profile.Tags.Contains(TagEnum.Hallow))
            combat.Profile = new(new TagSet(combat.Profile.Tags.Append(new TagEntry(false, TagEnum.Hallow))), "Hallow.Destroyer.Contact", isManagedAttack: true);
        if (npc.type != NPCID.TheDestroyer)
        {
            FollowPrevious(npc, GetNeckIndex(npc)); State = MovementState.Following;
            npc.alpha = headData.Hidden ? 255 : 0; return true;
        }
        npc.alpha = Hidden ? 255 : 0;
        PhaseTimer++;
        bool authority = Main.netMode != NetmodeID.MultiplayerClient;
        float speed = npc.velocity.Length();
        switch (PhaseState)
        {
            case PhaseTwoState.Prepare:
                Steer(npc, waypoint, MathHelper.Lerp(entrySpeed, R.CruiseSpeed, MathHelper.SmoothStep(0, 1, Math.Min(1, PhaseTimer / 30f))));
                if (authority && (npc.Distance(waypoint) < 120 || PhaseTimer >= R.PrepareTicks) && !BeginUltimate(npc, false)) Enter(npc, PhaseTwoState.Reposition);
                break;
            case PhaseTwoState.Reposition:
                Steer(npc, waypoint, MathHelper.Lerp(entrySpeed, R.CruiseSpeed, MathHelper.SmoothStep(0, 1, Math.Min(1, PhaseTimer / (float)R.RecoveryTicks))));
                if (authority && PhaseTimer >= R.RepositionTicks && npc.Distance(Main.player[npc.target].Center) >= R.StartMinimum)
                    Enter(npc, PhaseTwoState.Telegraph);
                break;
            case PhaseTwoState.Telegraph:
                Steer(npc, Main.player[npc.target].Center, R.CruiseSpeed);
                float distance = npc.Distance(Main.player[npc.target].Center);
                float angle = Math.Abs(MathHelper.WrapAngle(npc.velocity.ToRotation() - (Main.player[npc.target].Center - npc.Center).ToRotation()));
                if (authority && PhaseTimer >= R.TelegraphTicks && distance >= R.StartMinimum && distance <= R.StartMaximum && angle <= MathHelper.ToRadians(15)) Enter(npc, PhaseTwoState.Crash);
                else if (authority && (distance < R.StartMinimum || PhaseTimer >= R.PrepareTicks)) Enter(npc, PhaseTwoState.Reposition);
                break;
            case PhaseTwoState.Crash:
                TickCrashAttack(npc);
                speed = MathHelper.Lerp(entrySpeed, R.CrashSpeed, MathHelper.SmoothStep(0, 1, Math.Min(1, PhaseTimer / (float)R.AccelerationTicks)));
                // 锁定后只作有限对齐，穿过目标不回头追踪。
                npc.velocity = npc.velocity.ToRotation().AngleTowards(crashDirection.ToRotation(), MathHelper.ToRadians(.5f)).ToRotationVector2() * speed;
                if (authority && (PhaseTimer >= R.CrashTimeout || PhaseTimer >= R.AccelerationTicks && Vector2.Dot(npc.Center - lockedTarget, crashDirection) > R.PassDistance))
                { CompletedCrashes++; cycleCrashes++; Enter(npc, PhaseTwoState.Recover); }
                break;
            case PhaseTwoState.Recover:
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(entrySpeed, R.CruiseSpeed, MathHelper.SmoothStep(0, 1, Math.Min(1, PhaseTimer / (float)R.RecoveryTicks)));
                if (authority && PhaseTimer >= R.RecoveryTicks) FinishSkill(npc);
                break;
            case PhaseTwoState.Idle:
                Steer(npc, Main.player[npc.target].Center, R.CruiseSpeed);
                if (authority && PhaseTimer >= R.IdleTicks) FinishSkill(npc);
                break;
            case PhaseTwoState.UltimateWarning:
            case PhaseTwoState.UltimateStrike:
                npc.velocity *= .96f;
                int duration = PhaseState == PhaseTwoState.UltimateWarning ? R.UltimateWarningTicks : R.UltimateStrikeTicks;
                if (authority && PhaseTimer >= duration)
                {
                    if (PhaseState == PhaseTwoState.UltimateWarning) Enter(npc, PhaseTwoState.UltimateStrike);
                    else if (++UltimateRepetition < R.UltimateRepeats) Enter(npc, PhaseTwoState.UltimateWarning);
                    else { cycleCrashes = 0; if (!BeginUltimate(npc, false)) Enter(npc, PhaseTwoState.Reposition); }
                }
                break;
        }
        npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
        // 周期校正客户端预测；状态与随机决策只由权威端推进。
        if (authority && PhaseTimer % 30 == 0) npc.netUpdate = true;
        return true;
    }
    public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot) => !(HeadBehavior(npc)?.Hidden ?? false);
    public override bool CanHitNPC(NPC npc, NPC target) => !(HeadBehavior(npc)?.Hidden ?? false);
    private void WritePhaseTwo(BinaryWriter writer)
    {
        writer.Write((byte)PhaseState); writer.Write(ReactionCount); writer.Write(PhaseTimer); writer.Write(UltimateRepetition);
        writer.Write(UltimateSerial); writer.Write(CompletedCrashes); writer.Write(cycleCrashes); writer.Write(entrySpeed);
        writer.Write(waypoint.X); writer.Write(waypoint.Y); writer.Write(lockedTarget.X); writer.Write(lockedTarget.Y); writer.Write(crashDirection.X); writer.Write(crashDirection.Y);
        writer.Write((byte)SelectedAttack); writer.Write(CrashAttackFired);
    }
    private void ReadPhaseTwo(BinaryReader reader)
    {
        PhaseState = (PhaseTwoState)reader.ReadByte(); ReactionCount = reader.ReadInt32(); PhaseTimer = reader.ReadInt32(); UltimateRepetition = reader.ReadInt32();
        UltimateSerial = reader.ReadInt32(); CompletedCrashes = reader.ReadInt32(); cycleCrashes = reader.ReadInt32(); entrySpeed = reader.ReadSingle();
        waypoint = new(reader.ReadSingle(), reader.ReadSingle()); lockedTarget = new(reader.ReadSingle(), reader.ReadSingle()); crashDirection = new(reader.ReadSingle(), reader.ReadSingle());
        SelectedAttack = (Projectiles.DestroyerAttackKind)reader.ReadByte(); CrashAttackFired = reader.ReadBoolean();
    }
}

using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace AdvancedMod.Contents.Hallow.NPCs;

public sealed partial class DestroyerBehavior
{
    /// <summary>由地形与生命周期逐刻决定，攻击和淡入不阻塞运动状态。</summary>
    public enum MovementState { Burrowing, Airborne, Retreating, Following, Attached }
    public MovementState State { get; private set; }

    private const float TileSize = 16f;
    private const float MoveSpeed = 16f;
    private const float RetreatSpeed = 32f;
    private const float Acceleration = 0.1f;
    private const float TurnAcceleration = 0.15f;
    private const float GoodWorldAccelerationMultiplier = 1.2f;
    private const float Gravity = 0.15f;
    private const float SegmentSpacing = 44f;
    private const float RotationOffset = 1.57f;
    private const int FadeStep = 42;
    private const int FadeThreshold = 128;
    private const int CatchUpRange = 1000;
    private const int MechQueenNeckSegments = 10;
    private const int LaserThresholdMin = 1400;
    private const int LaserThresholdMax = 26000;
    private const int LaserTimerStep = 4;
    private const int LaserSpread = 20;
    private const float LaserSpeed = 8f;
    private const float LaserVelocitySpread = 0.05f;
    private const int LaserLifetime = 300;

    private void TickStateMachine(NPC npc)
    {
        int neckIndex = GetNeckIndex(npc);
        if (npc.ai[3] > 0f) npc.realLife = (int)npc.ai[3];
        if (npc.target < 0 || npc.target >= Main.maxPlayers || !Main.player[npc.target].active || Main.player[npc.target].dead)
            npc.TargetClosest();
        UpdateFade(npc);
        if (!ValidatePreviousSegment(npc)) return;

        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            if (npc.type == NPCID.TheDestroyer && npc.ai[0] == 0f) SpawnSegments(npc);
            if (npc.type == NPCID.TheDestroyerBody && HeadBehavior(npc)?.PhaseTwo != true) TickLaser(npc);
        }

        bool burrowing = ScanEnvironment(npc);
        bool retreating = Main.IsItDay() || Main.player[npc.target].dead;
        float speed = MoveSpeed;
        if (retreating)
        {
            burrowing = false;
            npc.velocity.Y += 1f;
            if (npc.position.Y > Main.worldSurface * TileSize) { npc.velocity.Y += 1f; speed = RetreatSpeed; }
            if (npc.position.Y > Main.rockLayer * TileSize)
            {
                // 保留原版按 aiStyle 清理的语义，探针不属于此 AI。
                foreach (NPC other in Main.ActiveNPCs)
                    if (other.aiStyle == npc.aiStyle) other.active = false;
                return;
            }
        }

        Vector2 target = SnapToTile(Main.player[npc.target].Center) - SnapToTile(npc.Center);
        State = npc.type != NPCID.TheDestroyer ? MovementState.Following :
            retreating ? MovementState.Retreating : burrowing ? MovementState.Burrowing : MovementState.Airborne;
        float acceleration = Acceleration * (Main.getGoodWorld ? GoodWorldAccelerationMultiplier : 1f);
        switch (State)
        {
            case MovementState.Following:
                FollowPrevious(npc, neckIndex);
                break;
            case MovementState.Burrowing:
                Burrow(npc, target, speed, acceleration);
                UpdateHeadRotationAndSync(npc, true);
                break;
            case MovementState.Airborne:
            case MovementState.Retreating:
                Fall(npc, target, speed, acceleration);
                UpdateHeadRotationAndSync(npc, false);
                break;
        }
        if (NPC.IsMechQueenUp && npc.type == NPCID.TheDestroyer)
        {
            // 组合模式保持末尾覆盖，避免跳过本刻的其他公共行为。
            State = MovementState.Attached;
            NPC queen = Main.npc[NPC.mechQueen];
            Vector2 center = queen.GetMechQueenCenter();
            float angle = queen.velocity.X * 0.025f;
            npc.position = (center + new Vector2(0, 100)).RotatedBy(angle, center) - npc.Size / 2f + queen.velocity;
            npc.velocity = Vector2.Zero;
            npc.rotation = angle * 0.75f + MathHelper.Pi;
        }
    }

    private static Vector2 SnapToTile(Vector2 point) => new((int)(point.X / TileSize) * TileSize, (int)(point.Y / TileSize) * TileSize);

    private static void UpdateFade(NPC npc)
    {
        int previous = (int)npc.ai[1];
        if (npc.type != NPCID.TheDestroyer && (previous < 0 || previous >= Main.maxNPCs || Main.npc[previous].alpha >= FadeThreshold)) return;
        if (npc.alpha != 0)
            for (int i = 0; i < 2; i++)
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.TheDestroyer, 0, 0, 100, default, 2f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].noLight = true;
            }
        npc.alpha = Math.Max(0, npc.alpha - FadeStep);
    }

    private static bool ValidatePreviousSegment(NPC npc)
    {
        if (npc.type == NPCID.TheDestroyer) return true;
        int previous = (int)npc.ai[1];
        if (previous >= 0 && previous < Main.maxNPCs && Main.npc[previous].active && Main.npc[previous].life > 0) return true;
        npc.life = 0;
        npc.HitEffect();
        npc.active = false;
        npc.netUpdate = true;
        return false;
    }

    private static void SpawnSegments(NPC head)
    {
        head.ai[3] = head.realLife = head.whoAmI;
        int previous = head.whoAmI;
        int count = NPC.GetDestroyerSegmentsCount();
        for (int i = 0; i <= count; i++)
        {
            int type = i == count ? NPCID.TheDestroyerTail : NPCID.TheDestroyerBody;
            int index = NPC.NewNPC(head.GetSource_FromAI(), (int)head.Center.X, (int)head.Bottom.Y, type, head.whoAmI);
            if (index >= Main.maxNPCs) break;
            NPC segment = Main.npc[index];
            segment.ai[3] = segment.realLife = head.whoAmI;
            segment.ai[1] = previous;
            Main.npc[previous].ai[0] = index;
            segment.GetGlobalNPC<DestroyerBehavior>().SetHallow(segment, true);
            NetMessage.SendData(MessageID.SyncNPC, number: index);
            previous = index;
        }
    }

    private static void TickLaser(NPC npc)
    {
        npc.localAI[0] += Main.rand.Next(LaserTimerStep);
        if (npc.localAI[0] < Main.rand.Next(LaserThresholdMin, LaserThresholdMax)) return;
        npc.localAI[0] = 0f;
        npc.TargetClosest();
        Player target = Main.player[npc.target];
        if (!Collision.CanHit(npc.position, npc.width, npc.height, target.position, target.width, target.height)) return;
        Vector2 aim = target.Center - npc.Center + new Vector2(Main.rand.Next(-LaserSpread, LaserSpread + 1), Main.rand.Next(-LaserSpread, LaserSpread + 1));
        Vector2 velocity = aim.SafeNormalize(Vector2.UnitY) * LaserSpeed;
        velocity += new Vector2(Main.rand.Next(-LaserSpread, LaserSpread + 1), Main.rand.Next(-LaserSpread, LaserSpread + 1)) * LaserVelocitySpread;
        int projectile = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + velocity * 5f, velocity,
            ProjectileID.DeathLaser, npc.GetAttackDamage_ForProjectiles(22, 18), 0f, Main.myPlayer);
        if (projectile < Main.maxProjectiles) Main.projectile[projectile].timeLeft = LaserLifetime;
        npc.netUpdate = true;
    }

    private static bool ScanEnvironment(NPC npc)
    {
        int left = Math.Max(0, (int)(npc.position.X / TileSize) - 1);
        int right = Math.Min(Main.maxTilesX, (int)(npc.Right.X / TileSize) + 2);
        int top = Math.Max(0, (int)(npc.position.Y / TileSize) - 1);
        int bottom = Math.Min(Main.maxTilesY, (int)(npc.Bottom.Y / TileSize) + 2);
        bool solid = false;
        for (int x = left; x < right && !solid; x++)
        for (int y = top; y < bottom; y++)
        {
            var tile = Main.tile[x, y];
            bool terrain = tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType] && tile.TileFrameY == 0);
            if ((terrain || tile.LiquidAmount > 64) && npc.position.X + npc.width > x * TileSize && npc.position.X < (x + 1) * TileSize &&
                npc.position.Y + npc.height > y * TileSize && npc.position.Y < (y + 1) * TileSize) { solid = true; break; }
        }
        npc.localAI[1] = solid ? 0f : 1f;
        if (solid) return true;
        if (npc.type != NPCID.TheDestroyerBody || npc.ai[2] != 1f) Lighting.AddLight(npc.Center, 0.3f, 0.1f, 0.05f);
        if (npc.type != NPCID.TheDestroyer || npc.position.Y <= Main.player[npc.target].position.Y) return false;
        foreach (Player player in Main.ActivePlayers)
            if (npc.Hitbox.Intersects(new Rectangle((int)player.position.X - CatchUpRange, (int)player.position.Y - CatchUpRange, CatchUpRange * 2, CatchUpRange * 2))) return false;
        return true;
    }

    private static int GetNeckIndex(NPC npc)
    {
        if (!NPC.IsMechQueenUp || npc.type == NPCID.TheDestroyer) return 0;
        int index = (int)npc.ai[1];
        for (int distance = 1; distance <= MechQueenNeckSegments; distance++)
        {
            if (index < 0 || index >= Main.maxNPCs) return 0;
            NPC previous = Main.npc[index];
            if (!previous.active || previous.type < NPCID.TheDestroyer || previous.type > NPCID.TheDestroyerTail) return 0;
            if (previous.type == NPCID.TheDestroyer) return distance;
            index = (int)previous.ai[1];
        }
        return 0;
    }

    private static void FollowPrevious(NPC npc, int neckIndex)
    {
        NPC previous = Main.npc[(int)npc.ai[1]];
        Vector2 direction = previous.Center - npc.Center;
        Vector2 originalDirection = direction;
        int spacing = (int)(SegmentSpacing * npc.scale);
        if (neckIndex > 0)
        {
            direction.Y += MathHelper.Clamp(spacing - spacing * ((neckIndex - 1f) * 0.1f), 0, spacing);
            spacing = spacing / MechQueenNeckSegments * neckIndex;
        }
        float distance = direction.Length();
        if (distance > 0f) npc.position += direction * ((distance - spacing) / distance);
        npc.velocity = Vector2.Zero;
        npc.rotation = originalDirection.ToRotation() + RotationOffset;
    }

    private static void Fall(NPC npc, Vector2 target, float speed, float acceleration)
    {
        npc.TargetClosest();
        npc.velocity.Y = Math.Min(speed, npc.velocity.Y + Gravity);
        if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)speed * 0.4)
            npc.velocity.X += (npc.velocity.X < 0 ? -1 : 1) * acceleration * 1.1f;
        else if (npc.velocity.Y == speed)
            npc.velocity.X = StepToward(npc.velocity.X, target.X, acceleration);
        else if (npc.velocity.Y > 4f)
            npc.velocity.X += (npc.velocity.X < 0 ? 1 : -1) * acceleration * 0.9f;
    }

    // 保留逐轴加减，不做钳制插值；同向转向和普通加速允许同刻执行。
    private static float StepToward(float value, float target, float step) => value < target ? value + step : value > target ? value - step : value;
    private static bool SameDirection(float a, float b) => a > 0 && b > 0 || a < 0 && b < 0;
    private static bool OppositeDirection(float a, float b) => a > 0 && b < 0 || a < 0 && b > 0;

    private static void Burrow(NPC npc, Vector2 target, float speed, float acceleration)
    {
        float distance = target.Length();
        if (npc.soundDelay == 0)
        {
            npc.soundDelay = (int)MathHelper.Clamp(distance / 40f, 10, 20);
            SoundEngine.PlaySound(SoundID.WormDig, npc.position);
        }
        float length = (float)Math.Sqrt(target.X * target.X + target.Y * target.Y);
        Vector2 desired = length > 0 ? target * (speed / length) : Vector2.UnitY * speed;
        float turn = TurnAcceleration * (Main.getGoodWorld ? GoodWorldAccelerationMultiplier : 1f);
        if (SameDirection(npc.velocity.X, desired.X) && SameDirection(npc.velocity.Y, desired.Y))
        {
            npc.velocity.X = StepToward(npc.velocity.X, desired.X, turn);
            npc.velocity.Y = StepToward(npc.velocity.Y, desired.Y, turn);
        }
        if (SameDirection(npc.velocity.X, desired.X) || SameDirection(npc.velocity.Y, desired.Y))
        {
            npc.velocity.X = StepToward(npc.velocity.X, desired.X, acceleration);
            npc.velocity.Y = StepToward(npc.velocity.Y, desired.Y, acceleration);
            if ((double)Math.Abs(desired.Y) < (double)speed * 0.2 && OppositeDirection(npc.velocity.X, desired.X))
                npc.velocity.Y += (npc.velocity.Y > 0 ? 1 : -1) * acceleration * 2f;
            if ((double)Math.Abs(desired.X) < (double)speed * 0.2 && OppositeDirection(npc.velocity.Y, desired.Y))
                npc.velocity.X += (npc.velocity.X > 0 ? 1 : -1) * acceleration * 2f;
        }
        else if (Math.Abs(target.X) > Math.Abs(target.Y))
        {
            npc.velocity.X = StepToward(npc.velocity.X, desired.X, acceleration * 1.1f);
            if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)speed * 0.5)
                npc.velocity.Y += (npc.velocity.Y > 0 ? 1 : -1) * acceleration;
        }
        else
        {
            npc.velocity.Y = StepToward(npc.velocity.Y, desired.Y, acceleration * 1.1f);
            if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)speed * 0.5)
                npc.velocity.X += (npc.velocity.X > 0 ? 1 : -1) * acceleration;
        }
    }

    private static void UpdateHeadRotationAndSync(NPC npc, bool burrowing)
    {
        npc.rotation = npc.velocity.ToRotation() + RotationOffset;
        float marker = burrowing ? 1f : 0f;
        if (npc.localAI[0] != marker) npc.netUpdate = true;
        npc.localAI[0] = marker;
        if (!npc.justHit && (OppositeDirection(npc.velocity.X, npc.oldVelocity.X) || OppositeDirection(npc.velocity.Y, npc.oldVelocity.Y))) npc.netUpdate = true;
    }
}


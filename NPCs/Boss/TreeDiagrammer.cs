using AdvancedMod.NPCs.Servant;
using AdvancedMod.Projectiles.NPCs;
using AdvancedMod.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace AdvancedMod.NPCs.Boss.TreeDiagrammer
{
    [AutoloadBossHead]
    public sealed class TreeDiagrammer : ModNPC
    {
        public override string Texture => $"{Mod.Name}/Assets/Textures/NPCs/TreeDiagrammer";
        public override string BossHeadTexture => $"{Mod.Name}/Assets/Textures/NPCs/TreeDiagrammer_Head_Boss";

        private const float Tile = 16f;
        private const int BaseDefense = 15;
        private const int GuardProbeDefenseBonus = 10;
        private const int NormalLaserFireInterval = 8;
        private const int StrongLaserFireInterval = 5;
        private const int FrameHeight = 190;
        private const int HitboxHeight = 128;
        private const int HitboxWidth = 160;
        private const float HoverAreaHorizontalTiles = 50f;
        private const float HoverAreaMinHeightTiles = 22f;
        private const float HoverAreaMaxHeightTiles = 38f;
        private const float HoverAreaDamping = 0.96f;

        private enum BossPhase : byte
        {
            Phase1 = 1,
            Phase2 = 2
        }

        private enum BossSkill : byte
        {
            Idle = 0,
            EntryLaser = 1,
            SummonProbes = 2,
            Laser = 3,
            ArcSixPoints = 4,
            PhaseCharge = 5,
            StrongLaser = 6,
            Finisher = 7,
            DashArc = 8,
            LaserNet = 9,
            LightningOrbArc = 10
        }

        private enum ProbeMode : byte
        {
            Guard = 0,
            Suicide = 1,
            Jammer = 2
        }

        private ref float PhaseSlot => ref NPC.ai[0];
        private ref float SkillSlot => ref NPC.ai[1];
        private ref float TimerSlot => ref NPC.ai[2];
        private ref float SubStateSlot => ref NPC.ai[3];

        private BossPhase Phase
        {
            get => (BossPhase)(int)PhaseSlot;
            set => PhaseSlot = (float)value;
        }

        private BossSkill CurrentSkill
        {
            get => (BossSkill)(int)SkillSlot;
            set => SkillSlot = (float)value;
        }

        private int SkillTimer
        {
            get => (int)TimerSlot;
            set => TimerSlot = value;
        }

        private int SubState
        {
            get => (int)SubStateSlot;
            set => SubStateSlot = value;
        }

        private BossSkill lastSkill;
        private int sp;
        private bool initialized;
        private bool phase2Opened;
        private bool cannonUsable = true;
        private int finisherLifeDebt;

        private Vector2 moveAnchor;
        private Vector2 dashStart;
        private Vector2 dashEnd;
        private int patternSeed;

        private int cannonId = -1;

        public override void SetStaticDefaults()
        {
            //Main.npcFrameCount[Type] = FrameCount;
        }

        public override void SetDefaults()
        {
            NPC.width = HitboxWidth;
            NPC.height = HitboxHeight;
            NPC.lifeMax = 30000;
            NPC.damage = 60;
            NPC.defense = BaseDefense;
            NPC.knockBackResist = 0;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.aiStyle = -1;
            NPC.netAlways = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)lastSkill);
            writer.Write(sp);
            writer.Write(initialized);
            writer.Write(phase2Opened);
            writer.Write(cannonUsable);
            writer.Write(finisherLifeDebt);

            writer.Write(moveAnchor.X);
            writer.Write(moveAnchor.Y);
            writer.Write(dashStart.X);
            writer.Write(dashStart.Y);
            writer.Write(dashEnd.X);
            writer.Write(dashEnd.Y);

            writer.Write(patternSeed);
            writer.Write(cannonId);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            lastSkill = (BossSkill)reader.ReadByte();
            sp = reader.ReadInt32();
            initialized = reader.ReadBoolean();
            phase2Opened = reader.ReadBoolean();
            cannonUsable = reader.ReadBoolean();
            finisherLifeDebt = reader.ReadInt32();

            moveAnchor = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            dashStart = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            dashEnd = new Vector2(reader.ReadSingle(), reader.ReadSingle());

            patternSeed = reader.ReadInt32();
            cannonId = reader.ReadInt32();
        }

        public override void AI()
        {
            NPC.TargetClosest(false);

            if (!NPC.HasValidTarget)
            {
                NPC.velocity.Y -= 0.25f;
                NPC.timeLeft = 10;
                return;
            }

            Player player = Main.player[NPC.target];

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                ServerStateUpdate(player);
            }

            ApplyDefenseFromGuardProbes();

            RunMovement(player);
            RunCurrentSkill(player);

            NPC.spriteDirection = NPC.Center.X < player.Center.X ? 1 : -1;
        }

        private void ServerStateUpdate(Player player)
        {
            if (!initialized)
            {
                initialized = true;
                Phase = BossPhase.Phase1;
                BeginSkill(BossSkill.EntryLaser);
                SyncNow();
                return;
            }

            if (!phase2Opened && NPC.life <= NPC.lifeMax / 2)
            {
                phase2Opened = true;
                Phase = BossPhase.Phase2;
                BeginSkill(BossSkill.ArcSixPoints);
                SyncNow();
                return;
            }

            if (CurrentSkill != BossSkill.Idle)
            {
                return;
            }

            if (Phase == BossPhase.Phase2 && IsMutationDifficulty() && CurrentSkill != BossSkill.Finisher)
            {
                sp++;
            }

            if (Phase == BossPhase.Phase2 && IsMutationDifficulty() && sp >= 3000 && cannonUsable)
            {
                sp -= 3000;
                BeginSkill(BossSkill.Finisher);
                SyncNow();
                return;
            }

            if (Phase == BossPhase.Phase2 && IsEndDifficulty() && Vector2.Distance(NPC.Center, player.Center) > 85f * Tile)
            {
                BeginSkill(BossSkill.DashArc);
                SyncNow();
                return;
            }

            BeginSkill(SelectNextSkill());
            SyncNow();
        }

        private void BeginSkill(BossSkill skill)
        {
            CurrentSkill = skill;
            SkillTimer = 0;
            SubState = 0;
            patternSeed = Main.rand.Next();

            if (skill == BossSkill.StrongLaser)
            {
                Player player = Main.player[NPC.target];
                moveAnchor = player.Center + new Vector2(0f, -40f * Tile);
            }

            if (skill == BossSkill.PhaseCharge)
            {
                Player player = Main.player[NPC.target];
                dashEnd = player.Center * 2f - NPC.Center;
            }

            if (skill == BossSkill.DashArc)
            {
                PrepareDashArc(Main.player[NPC.target]);
            }
        }

        private void EndSkill()
        {
            lastSkill = CurrentSkill;
            CurrentSkill = BossSkill.Idle;
            SkillTimer = 0;
            SubState = 0;
            SyncNow();
        }

        private BossSkill SelectNextSkill()
        {
            BossSkill[] pool;

            if (Phase == BossPhase.Phase1)
            {
                pool = (IsEndDifficulty() && Main.masterMode && Main.getGoodWorld)
                    ? new[] { BossSkill.SummonProbes, BossSkill.Laser, BossSkill.PhaseCharge, BossSkill.LightningOrbArc }
                    : new[] { BossSkill.SummonProbes, BossSkill.Laser, BossSkill.PhaseCharge };
            }
            else
            {
                pool = (IsEndDifficulty() && Main.masterMode && Main.getGoodWorld)
                    ? new[] { BossSkill.SummonProbes, BossSkill.StrongLaser, BossSkill.PhaseCharge, BossSkill.LaserNet, BossSkill.LightningOrbArc }
                    : new[] { BossSkill.SummonProbes, BossSkill.StrongLaser, BossSkill.PhaseCharge };
            }

            for (int tries = 0; tries < 16; tries++)
            {
                BossSkill chosen = pool[Main.rand.Next(pool.Length)];

                if (chosen != lastSkill)
                {
                    return chosen;
                }
            }

            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] != lastSkill)
                {
                    return pool[i];
                }
            }

            return pool[0];
        }

        private void RunCurrentSkill(Player player)
        {
            switch (CurrentSkill)
            {
                case BossSkill.EntryLaser:
                    RunEntryLaser(player);
                    break;

                case BossSkill.SummonProbes:
                    RunSummonProbes();
                    break;

                case BossSkill.Laser:
                    RunLaser(player, 900, false);
                    break;

                case BossSkill.ArcSixPoints:
                    RunArcSixPoints(player);
                    break;

                case BossSkill.PhaseCharge:
                    RunPhaseCharge(player);
                    break;

                case BossSkill.StrongLaser:
                    RunLaser(player, 600, true);
                    break;

                case BossSkill.Finisher:
                    RunFinisher(player);
                    break;

                case BossSkill.DashArc:
                    RunDashArc(player);
                    break;

                case BossSkill.LaserNet:
                    RunLaserNet(player);
                    break;

                case BossSkill.LightningOrbArc:
                    RunLightningOrbArc(player);
                    break;
            }

            if (CurrentSkill != BossSkill.Idle)
            {
                SkillTimer++;
            }
        }

        private void RunEntryLaser(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && SkillTimer % NormalLaserFireInterval == 0)
            {
                SpawnAimedLaser(player, strong: false, lifetime: 300);
            }

            if (SkillTimer >= 300)
            {
                BeginSkill(BossSkill.SummonProbes);
                SyncNow();
            }
        }

        private void RunSummonProbes()
        {
            if (SkillTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                EnsureProbe(ProbeMode.Guard);
                EnsureProbe(ProbeMode.Suicide);
                EnsureProbe(ProbeMode.Jammer);
                SyncNow();
            }

            EndSkill();
        }

        private void RunLaser(Player player, int duration, bool strong)
        {
            int fireInterval = strong ? StrongLaserFireInterval : NormalLaserFireInterval;

            if (Main.netMode != NetmodeID.MultiplayerClient && SkillTimer % fireInterval == 0)
            {
                SpawnAimedLaser(player, strong, duration);
            }

            if (SkillTimer >= duration)
            {
                EndSkill();
            }
        }

        private void RunArcSixPoints(Player player)
        {
            if (SkillTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnSixPointArcPattern(player);
            }

            if (SkillTimer >= 180)
            {
                EndSkill();
            }
        }

        private void RunPhaseCharge(Player player)
        {
            if (SkillTimer < 30)
            {
                Vector2 desired = player.Center * 2f - NPC.Center;
                MoveTo(desired, 18f, 12f);
                return;
            }

            if (SkillTimer == 30)
            {
                dashEnd = player.Center * 2f - NPC.Center;
                SyncNow();
            }

            DashTo(dashEnd, 30f);

            if (SkillTimer >= 80 || Vector2.Distance(NPC.Center, dashEnd) < 30f)
            {
                EndSkill();
            }
        }

        private void RunFinisher(Player player)
        {
            if (SkillTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                EnsureProbe(ProbeMode.Guard);
                EnsureProbe(ProbeMode.Suicide);
                EnsureProbe(ProbeMode.Jammer);
                ForceAllProbesToLaserMode();

                finisherLifeDebt = Math.Min(NPC.life - 1, NPC.lifeMax / 10);
                NPC.life -= finisherLifeDebt;

                SpawnMainCannon();
                SyncNow();
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                bool cannonAlive = IsMainCannonAlive();

                if (!cannonAlive)
                {
                    cannonUsable = false;
                    finisherLifeDebt = 0;
                    EndSkill();
                    return;
                }
            }

            if (SkillTimer >= 900)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    RecycleMainCannon();
                    NPC.life = Math.Min(NPC.lifeMax, NPC.life + finisherLifeDebt);
                    NPC.HealEffect(finisherLifeDebt);
                    finisherLifeDebt = 0;
                }

                EndSkill();
            }
        }

        private void RunDashArc(Player player)
        {
            if (SkillTimer < 45)
            {
                MoveTo(moveAnchor, 22f, 8f);
                return;
            }

            if (SkillTimer == 45 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                dashStart = NPC.Center;
                SpawnDashArcTelegraph(dashStart, dashEnd);
                SyncNow();
            }

            DashTo(dashEnd, 36f);

            if (SkillTimer == 52 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnDamagingDashArc(dashStart, dashEnd);
            }

            if (SkillTimer >= 85 || Vector2.Distance(NPC.Center, dashEnd) < 32f)
            {
                EndSkill();
            }
        }

        private void RunLaserNet(Player player)
        {
            int cycle = SkillTimer / 90;
            int frame = SkillTimer % 90;

            if (cycle < 3 && frame == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnLaserNetTelegraph(player, cycle);
            }

            if (cycle < 3 && frame == 45 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnLaserNetDamage(player, cycle);
            }

            if (cycle >= 3)
            {
                EndSkill();
            }
        }

        private void RunLightningOrbArc(Player player)
        {
            int cycle = SkillTimer / 75;
            int frame = SkillTimer % 75;

            if (cycle < 4 && frame == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnLightningOrbArc(player, cycle);
            }

            if (cycle >= 4)
            {
                EndSkill();
            }
        }

        private void RunMovement(Player player)
        {
            switch (CurrentSkill)
            {
                case BossSkill.Finisher:
                case BossSkill.LaserNet:
                    MoveTo(player.Center + new Vector2(0f, -30f * Tile), 12f, 10f);
                    break;

                case BossSkill.StrongLaser:
                    MoveTo(moveAnchor, 24f, 8f);
                    break;

                case BossSkill.PhaseCharge:
                case BossSkill.DashArc:
                    break;

                default:
                    MoveToAllowedHoverArea(player);
                    break;
            }
        }

        private void MoveToAllowedHoverArea(Player player)
        {
            float minX = player.Center.X - HoverAreaHorizontalTiles * Tile;
            float maxX = player.Center.X + HoverAreaHorizontalTiles * Tile;
            float minY = player.Center.Y - HoverAreaMaxHeightTiles * Tile;
            float maxY = player.Center.Y - HoverAreaMinHeightTiles * Tile;

            bool insideHoverArea =
                NPC.Center.X >= minX &&
                NPC.Center.X <= maxX &&
                NPC.Center.Y >= minY &&
                NPC.Center.Y <= maxY;

            if (insideHoverArea)
            {
                NPC.velocity *= HoverAreaDamping;
                return;
            }

            Vector2 closestPoint = new Vector2(
                MathHelper.Clamp(NPC.Center.X, minX, maxX),
                MathHelper.Clamp(NPC.Center.Y, minY, maxY)
            );

            MoveTo(closestPoint, 14f, 14f);
        }

        private void PrepareDashArc(Player player)
        {
            Vector2 velocity = player.velocity;
            Vector2 axis;

            if (velocity.LengthSquared() > 0.01f)
            {
                axis = velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            }
            else
            {
                Vector2 fromPlayerToBoss = NPC.Center - player.Center;
                axis = fromPlayerToBoss.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            }

            Vector2 optionA = player.Center + axis * 26f * Tile;
            Vector2 optionB = player.Center - axis * 26f * Tile;
            moveAnchor = Vector2.DistanceSquared(NPC.Center, optionA) < Vector2.DistanceSquared(NPC.Center, optionB) ? optionA : optionB;

            Vector2 forward = velocity.LengthSquared() > 0.01f
                ? velocity.SafeNormalize(Vector2.UnitX)
                : (player.Center - NPC.Center).SafeNormalize(Vector2.UnitX);

            Vector2 pivot = player.Center + forward * 20f * Tile;
            dashEnd = pivot * 2f - moveAnchor;
        }

        private void MoveTo(Vector2 destination, float maxSpeed, float inertia)
        {
            Vector2 desiredVelocity = destination - NPC.Center;

            if (desiredVelocity.LengthSquared() > maxSpeed * maxSpeed)
            {
                desiredVelocity = desiredVelocity.SafeNormalize(Vector2.Zero) * maxSpeed;
            }

            NPC.velocity = (NPC.velocity * (inertia - 1f) + desiredVelocity) / inertia;
        }

        private void DashTo(Vector2 destination, float speed)
        {
            Vector2 direction = destination - NPC.Center;
            NPC.velocity = direction.SafeNormalize(Vector2.UnitY) * speed;
        }

        private void SyncNow()
        {
            NPC.netUpdate = true;
        }

        public void NotifyProbeKilled()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (Phase == BossPhase.Phase2 && IsMutationDifficulty() && CurrentSkill != BossSkill.Finisher)
            {
                sp += 400;
                SyncNow();
            }
        }

        public void NotifyCannonKilled()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            cannonUsable = false;

            if (CurrentSkill == BossSkill.Finisher)
            {
                finisherLifeDebt = 0;
                EndSkill();
            }

            SyncNow();
        }

        private bool IsMutationDifficulty()
        {
            return AdvancedWorld.MutationMode;
        }

        private bool IsEndDifficulty()
        {
            return AdvancedWorld.EndMode;
        }

        private void ApplyDefenseFromGuardProbes()
        {
            int guardProbeCount = 0;

            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.type == ModContent.NPCType<DiagrammerProbe>() &&
                    n.active &&
                    (int)n.ai[0] == NPC.whoAmI &&
                    (ProbeMode)(int)n.ai[1] == ProbeMode.Guard)
                {
                    guardProbeCount++;
                }
            }

            NPC.defense = BaseDefense + guardProbeCount * GuardProbeDefenseBonus;
        }

        private void EnsureProbe(ProbeMode mode)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.type == ModContent.NPCType<DiagrammerProbe>() &&
                    n.active &&
                    (int)n.ai[0] == NPC.whoAmI &&
                    (ProbeMode)(int)n.ai[1] == mode)
                {
                    return;
                }
            }

            int slot = (int)mode;
            int index = NPC.NewNPC(
                NPC.GetSource_FromAI(),
                (int)NPC.Center.X,
                (int)NPC.Center.Y,
                ModContent.NPCType<DiagrammerProbe>(),
                0,
                NPC.whoAmI,
                (float)mode,
                slot,
                0f,
                NPC.target
            );

            Main.npc[index].netUpdate = true;
        }
        private void ForceAllProbesToLaserMode() { }
        private void SpawnMainCannon() { }
        private bool IsMainCannonAlive() => cannonId >= 0 && Main.npc[cannonId].active;
        private void RecycleMainCannon() { }

        private void SpawnAimedLaser(Player player, bool strong, int lifetime) {
            if (!IsPlayerAvailable(player))
            {
                return;
            }

            float laserSpeed = strong ? 16f : 14f;
            Vector2 targetPosition = player.Center;
            Vector2 shootPosition = NPC.Center;

            Vector2 direction = targetPosition - shootPosition;

            if (!IsFinite(direction) || direction.LengthSquared() <= 0f)
            {
                return;
            }

            direction.Normalize();

            if (strong)
            {
                Vector2 velocity = direction * laserSpeed;

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    shootPosition,
                    velocity,
                    ProjectileID.SaucerLaser,
                    (int)(NPC.damage*1.5f),
                    0
                );
            }
            else
            {
                Vector2 velocity = direction * laserSpeed;
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    shootPosition,
                    velocity,
                    ProjectileID.EyeLaser,
                    NPC.damage,
                    0
                );
            }
            
        }
        private void SpawnSixPointArcPattern(Player player) {
            float PredictFrames = 22f;

            float LowLayerTiles = 11f;
            float HighLayerTiles = 25f;

            float BossClearanceTiles = 5f;
            float MinVerticalSpanTiles = 12f;

            float CenterClampTiles = 22f;

            float LowHalfWidthTiles = 10f;
            float MidHalfWidthTiles = 14f;
            float HighHalfWidthTiles = 18f;

            float LowJitterXTiles = 2f;
            float MidJitterXTiles = 2f;
            float HighJitterXTiles = 1f;

            float JitterYTiles = 1f;
            
            Vector2[] points = Array.Empty<Vector2>();

            UnifiedRandom random = Main.rand;

            float predictedPlayerX = player.Center.X + player.velocity.X * PredictFrames;

            float centerX = predictedPlayerX * 0.75f + NPC.Center.X * 0.25f;

            centerX = MathHelper.Clamp(
                centerX,
                player.Center.X - CenterClampTiles * Tile,
                player.Center.X + CenterClampTiles * Tile
            );

            float lowY = player.Center.Y - LowLayerTiles * Tile;

            float desiredHighY = player.Center.Y - HighLayerTiles * Tile;

            float bossSafeHighY = NPC.Center.Y + BossClearanceTiles * Tile;

            float highY = Math.Max(desiredHighY, bossSafeHighY);

            float verticalSpan = lowY - highY;

            if (verticalSpan < MinVerticalSpanTiles * Tile)
            {
                return;
            }

            float midY = highY + verticalSpan * 0.5f;

            float lowHalfWidth = LowHalfWidthTiles * Tile;
            float midHalfWidth = MidHalfWidthTiles * Tile;
            float highHalfWidth = HighHalfWidthTiles * Tile;

            Vector2 p1 = new Vector2(centerX - lowHalfWidth, lowY);
            Vector2 p2 = new Vector2(centerX + lowHalfWidth, lowY);

            Vector2 p3 = new Vector2(centerX + midHalfWidth, midY);
            Vector2 p4 = new Vector2(centerX - midHalfWidth, midY);

            Vector2 p5 = new Vector2(centerX - highHalfWidth, highY);
            Vector2 p6 = new Vector2(centerX + highHalfWidth, highY);

            p1 = AdvancedExtensions.AddJitter(p1, random, LowJitterXTiles, JitterYTiles);
            p2 = AdvancedExtensions.AddJitter(p2, random, LowJitterXTiles, JitterYTiles);

            p3 = AdvancedExtensions.AddJitter(p3, random, MidJitterXTiles, JitterYTiles);
            p4 = AdvancedExtensions.AddJitter(p4, random, MidJitterXTiles, JitterYTiles); 
            p5 = AdvancedExtensions.AddJitter(p5, random, HighJitterXTiles, JitterYTiles);
            p6 = AdvancedExtensions.AddJitter(p6, random, HighJitterXTiles, JitterYTiles);

            points = new[]
            {
            p1, p2, p3, p4, p5, p6
            };

        }
        private void SpawnDashArcTelegraph(Vector2 start, Vector2 end)
        {
            HostileStaticArcProjectile.SpawnTelegraph(
                NPC.GetSource_FromAI(),
                start,
                end,
                0.35f
            );
        }
        private void SpawnDamagingDashArc(Vector2 start, Vector2 end) {
            int damage = (int)(NPC.damage * 0.5f);
            float knockBack = 0f;
            float durationSeconds = 1.5f;

            HostileStaticArcProjectile.SpawnHostile(
                        NPC.GetSource_FromAI(),
                        start,
                        end,
                        durationSeconds,
                        damage,
                        knockBack
                    );
        }
        private void SpawnLaserNetTelegraph(Player player, int cycle) {
            
        }
        private void SpawnLaserNetDamage(Player player, int cycle) { }
        private void SpawnLightningOrbArc(Player player, int cycle) { }

        private static bool IsPlayerAvailable(Player player)
        {
            return player != null && player.active && !player.dead;
        }

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.X) && float.IsFinite(value.Y);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[Type].Value;

            Rectangle sourceRectangle = new Rectangle(
                0,
                NPC.frame.Y,
                texture.Width,
                FrameHeight
            );

            SpriteEffects spriteEffects = NPC.spriteDirection == 1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            Vector2 drawPosition = new Vector2(
                NPC.position.X + NPC.width / 2f,
                NPC.position.Y
            ) - screenPos + new Vector2(0f, NPC.gfxOffY);

            Vector2 origin = new Vector2(
                sourceRectangle.Width / 2f,
                0f
            );

            spriteBatch.Draw(
                texture,
                drawPosition,
                sourceRectangle,
                drawColor,
                NPC.rotation,
                origin,
                NPC.scale,
                spriteEffects,
                0f
            );

            return false;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Sky,

                new FlavorTextBestiaryInfoElement("Mods.AdvancedMod.Bestiary.TreeDiagrammer")
            });
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            /*
             * 掉落框架，根据后续制作进行补充
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<SpaceBossBag>()));
            npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<SpaceBossRelic>()));

            LeadingConditionRule normalModeRule = new LeadingConditionRule(new Conditions.NotExpert());

            normalModeRule.OnSuccess(ItemDropRule.OneFromOptions(
                1,
                ModContent.ItemType<SpaceSword>(),
                ModContent.ItemType<SpaceGun>(),
                ModContent.ItemType<SpaceStaff>()
            ));

            normalModeRule.OnSuccess(ItemDropRule.Common(
                ModContent.ItemType<SpaceCore>(),
                chanceDenominator: 1,
                minimumDropped: 18,
                maximumDropped: 32
            ));

            normalModeRule.OnSuccess(ItemDropRule.Common(
                ModContent.ItemType<SpaceBossTrophy>(),
                chanceDenominator: 10
            ));

            normalModeRule.OnSuccess(ItemDropRule.ByCondition(
                new CustomDifficultyDropCondition(),
                ModContent.ItemType<SpaceBossExclusiveBlade>(),
                chanceDenominator: 4,
                minimumDropped: 1,
                maximumDropped: 1
            ));

            npcLoot.Add(normalModeRule);
            */
        }
    }
}

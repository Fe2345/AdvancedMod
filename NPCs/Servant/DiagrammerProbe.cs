// 编写日期：2026-05-15
// 用途：TML Boss 探测器 AI 骨架，支持护佑、自毁、妨碍和死亡通知

using AdvancedMod.NPCs.Boss.TreeDiagrammer;
using AdvancedMod.Projectiles.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.NPCs.Servant
{
    public sealed class DiagrammerProbe : ModNPC
    {
        public override string Texture => $"{Mod.Name}/Assets/Textures/NPCs/DiagrammerProbe";

        /// <summary>
        /// ai[0]：所属树状图设计者Boss的NPC索引。
        /// </summary>
        private int BossIndex => (int)NPC.ai[0];

        /// <summary>
        /// ai[1]：探测器当前形态，复用Boss侧定义以避免编号分叉。
        /// </summary>
        private TreeDiagrammer.ProbeMode Mode
        {
            get => (TreeDiagrammer.ProbeMode)(int)NPC.ai[1];
            set => NPC.ai[1] = (float)value;
        }

        /// <summary>
        /// ai[2]：基础探测器使用环绕槽位；追杀探测器后续使用正交矩阵方向，取值为1或-1。
        /// </summary>
        private int Slot => (int)NPC.ai[2];

        /// <summary>
        /// ai[3]：探测器类别，基础探测器为Base，后续追杀探测器为Chase。
        /// </summary>
        private TreeDiagrammer.ProbeKind Kind => (TreeDiagrammer.ProbeKind)(int)NPC.ai[3];
        private const float ProbeLaserSpeed = 11f;
        private const float ExplosionRadius = 72f;

        public override void SetDefaults()
        {
            NPC.width = 36;
            NPC.height = 36;
            NPC.lifeMax = 1200;
            NPC.damage = 40;
            NPC.defense = 10;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.aiStyle = -1;
            NPC.netAlways = true;
        }

        public override void AI()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (!IsBossValid(out NPC boss))
            {
                NPC.active = false;
                return;
            }

            switch (Mode)
            {
                case TreeDiagrammer.ProbeMode.Guard:
                    RunGuardOrbit(boss);
                    break;

                case TreeDiagrammer.ProbeMode.Suicide:
                    RunSuicideChase(boss);
                    break;

                case TreeDiagrammer.ProbeMode.Jammer:
                    RunJammerLaser(boss);
                    break;
            }
        }

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (!IsBossValid(out NPC boss))
            {
                return;
            }

            if (boss.ModNPC is TreeDiagrammer modBoss)
            {
                modBoss.NotifyProbeKilled();
            }
        }

        private bool IsBossValid(out NPC boss)
        {
            boss = null;

            if (BossIndex < 0 || BossIndex >= Main.maxNPCs)
            {
                return false;
            }

            NPC candidate = Main.npc[BossIndex];

            if (!candidate.active || candidate.type != ModContent.NPCType<TreeDiagrammer>())
            {
                return false;
            }

            boss = candidate;
            return true;
        }

        private void RunGuardOrbit(NPC boss)
        {
            float angle = Main.GameUpdateCount * 0.045f + Slot * MathHelper.TwoPi / 3f;
            Vector2 offset = angle.ToRotationVector2() * 150f;
            Vector2 destination = boss.Center + offset;
            MoveTo(destination, 14f, 8f);
        }

        private void RunSuicideChase(NPC boss)
        {
            Player player = Main.player[boss.target];
            MoveTo(player.Center, 18f, 6f);

            if (FindTouchingPlayer() != null)
            {
                SelfDestruct();
            }
        }

        private void RunJammerLaser(NPC boss)
        {
            Player player = Main.player[boss.target];

            if (Main.netMode != NetmodeID.MultiplayerClient && Main.GameUpdateCount % 90 == 0)
            {
                SpawnProbeLaser(player);
            }

            RunGuardOrbit(boss);
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

        private void SelfDestruct()
        {
            SpawnExplosion();
            NPC.StrikeInstantKill();
        }

        private void SpawnExplosion()
        {
            Projectile.NewProjectile(
                NPC.GetSource_FromAI("ContactSelfDestruct"),
                NPC.Center,
                Vector2.Zero,
                ModContent.ProjectileType<DiagrammerProbeExplosion>(),
                NPC.damage,
                7f,
                Main.maxPlayers,
                ExplosionRadius
            );
        }

        private void SpawnProbeLaser(Player player)
        {
            if (!IsPlayerAvailable(player))
            {
                return;
            }

            Vector2 direction = player.Center - NPC.Center;

            if (!IsFinite(direction) || direction.LengthSquared() <= 0f)
            {
                return;
            }

            direction.Normalize();

            Projectile.NewProjectile(
                NPC.GetSource_FromAI("ProbeLaser"),
                NPC.Center,
                direction * ProbeLaserSpeed,
                ModContent.ProjectileType<DiagrammerElectrifiedLaser>(),
                NPC.damage,
                0f
            );
        }

        public override bool CanHitNPC(NPC target)
        {
            return Mode != TreeDiagrammer.ProbeMode.Suicide;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return Mode != TreeDiagrammer.ProbeMode.Suicide;
        }

        private Player FindTouchingPlayer()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];

                if (!player.active || player.dead || player.ghost)
                {
                    continue;
                }

                if (NPC.Hitbox.Intersects(player.Hitbox))
                {
                    return player;
                }
            }

            return null;
        }

        private static bool IsPlayerAvailable(Player player)
        {
            return player != null && player.active && !player.dead && !player.ghost;
        }

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.X) && float.IsFinite(value.Y);
        }
    }

}

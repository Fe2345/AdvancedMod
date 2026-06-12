using AdvancedMod.NPCs.Servant;
using AdvancedMod.Projectiles.Effects;
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

        /// <summary>
        /// Boss整体阶段状态，用于决定技能池、阶段切换和退场优先级。
        /// </summary>
        private enum BossPhase : byte
        {
            /// <summary>入场阶段，负责释放入场激光并补齐基础探测器。</summary>
            SpawnIntro = 0,
            /// <summary>第一阶段，使用基础技能池。</summary>
            PhaseOne = 1,
            /// <summary>二阶段开场，强制释放六点电弧封锁。</summary>
            PhaseTwoOpening = 2,
            /// <summary>第二阶段，启用强化技能池和特殊插入机制。</summary>
            PhaseTwo = 3,
            /// <summary>退场阶段，停止普通攻击并上升离场。</summary>
            Retreat = 4
        }

        /// <summary>
        /// Boss当前技能状态，编号与设计稿保持一致。
        /// </summary>
        private enum BossSkill : byte
        {
            /// <summary>空闲状态，等待服务端选择下一个技能。</summary>
            Idle = 0,
            /// <summary>入场激光。</summary>
            IntroLaser = 1,
            /// <summary>补齐基础探测器。</summary>
            EnsureBaseProbes = 2,
            /// <summary>普通持续激光。</summary>
            NormalLaser = 3,
            /// <summary>六点电弧封锁。</summary>
            SixPointArc = 4,
            /// <summary>强连续激光。</summary>
            HeavyLaser = 5,
            /// <summary>终结技主炮。</summary>
            UltimateCannon = 6,
            /// <summary>追杀探测器封锁。</summary>
            ChaseProbeSeal = 7,
            /// <summary>激光网。</summary>
            LaserGrid = 8,
            /// <summary>闪电珠弧。</summary>
            LightningOrbArc = 9,
            /// <summary>脱离战场。</summary>
            RetreatSkill = 10
        }

        /// <summary>
        /// Boss本体移动状态，与技能状态解耦，避免攻击逻辑直接控制每种移动细节。
        /// </summary>
        private enum MoveState : byte
        {
            /// <summary>无特殊移动，使用普通悬停范围约束。</summary>
            None = 0,
            /// <summary>技能切换时快速接近锁定锚点。</summary>
            SkillReposition = 1,
            /// <summary>围绕锚点进行轻微横向漂移。</summary>
            AnchorDrift = 2,
            /// <summary>在固定轨道之间切换位置。</summary>
            RailShift = 3,
            /// <summary>强制静止或快速阻尼。</summary>
            StaticHold = 4,
            /// <summary>保持横向锚点并进行纵向压制。</summary>
            VerticalPress = 5,
            /// <summary>退场时持续上升。</summary>
            RetreatRise = 6
        }

        /// <summary>
        /// 基础探测器形态，后续会扩展追杀探测器形态。
        /// </summary>
        public enum ProbeMode : byte
        {
            /// <summary>护佑形态，环绕Boss并提供防御加成。</summary>
            Guard = 0,
            /// <summary>自毁形态，追踪玩家并接触爆炸。</summary>
            Suicide = 1,
            /// <summary>妨碍形态，环绕并释放带电激光。</summary>
            Jammer = 2
        }

        /// <summary>
        /// 探测器类别，用于区分常驻基础探测器和后续技能生成的追杀探测器。
        /// </summary>
        public enum ProbeKind : byte
        {
            /// <summary>基础探测器，参与技能2补齐和护佑防御统计。</summary>
            Base = 0,
            /// <summary>追杀探测器，后续由技能7生成，不计入基础探测器数量。</summary>
            Chase = 1
        }

        /// <summary>同步用阶段槽位。</summary>
        private ref float PhaseSlot => ref NPC.ai[0];
        /// <summary>同步用当前技能槽位。</summary>
        private ref float SkillSlot => ref NPC.ai[1];
        /// <summary>同步用当前技能计时槽位。</summary>
        private ref float TimerSlot => ref NPC.ai[2];
        /// <summary>同步用当前技能子状态槽位。</summary>
        private ref float SubStateSlot => ref NPC.ai[3];

        /// <summary>当前Boss整体阶段。</summary>
        private BossPhase Phase
        {
            get => (BossPhase)(int)PhaseSlot;
            set => PhaseSlot = (float)value;
        }

        /// <summary>当前正在执行的技能。</summary>
        private BossSkill CurrentSkill
        {
            get => (BossSkill)(int)SkillSlot;
            set => SkillSlot = (float)value;
        }

        /// <summary>当前技能或当前子状态的服务端计时。</summary>
        private int SkillTimer
        {
            get => (int)TimerSlot;
            set => TimerSlot = value;
        }

        /// <summary>当前技能内部子状态。</summary>
        private int SubState
        {
            get => (int)SubStateSlot;
            set => SubStateSlot = value;
        }

        /// <summary>上一个完成的技能，用于避免普通技能池连续选择同一技能。</summary>
        private BossSkill lastSkill;
        /// <summary>异变及以上二阶段积累的终结技能量。</summary>
        private int sp;
        /// <summary>服务端是否已经完成首次入场初始化。</summary>
        private bool initialized;
        /// <summary>是否已经触发二阶段开场，防止低血量后重复进入二阶段。</summary>
        private bool phase2Opened;
        /// <summary>主炮被击败后会禁用终结技。</summary>
        private bool cannonUsable = true;
        /// <summary>终结技期间暂扣的生命值，正常结束时返还。</summary>
        private int finisherLifeDebt;

        /// <summary>当前移动状态。</summary>
        private MoveState currentMoveState;
        /// <summary>当前技能或移动状态锁定的空间锚点。</summary>
        private Vector2 moveAnchor;
        /// <summary>当前瞄准点，主要用于低频更新瞄准或视觉表现。</summary>
        private Vector2 aimPoint;
        /// <summary>追杀封锁临时线段起点。</summary>
        private Vector2 dashStart;
        /// <summary>追杀封锁临时线段终点。</summary>
        private Vector2 dashEnd;
        /// <summary>当前技能图案随机种子，用于后续可同步图案生成。</summary>
        private int patternSeed;
        /// <summary>当前移动状态持续帧数。</summary>
        private int moveTimer;
        /// <summary>当前8方向移动索引。</summary>
        private int currentMoveDir8;
        /// <summary>8方向移动重新选向冷却。</summary>
        private int moveDirCooldown;
        /// <summary>强激光轨道索引，0左、1中、2右。</summary>
        private int railIndex;
        /// <summary>上一次强激光轨道索引，用于避免连续选择同轨道。</summary>
        private int previousRailIndex;
        /// <summary>瞄准点或压制目标点的低频刷新冷却。</summary>
        private int aimUpdateCooldown;
        /// <summary>锚点漂移方向，取值为-1或1。</summary>
        private int anchorDriftDirection = 1;
        /// <summary>锚点漂移横向范围，单位为格。</summary>
        private float anchorDriftRangeTiles = 12f;
        /// <summary>六点电弧封锁的固定点位，按技能生成时的顺序保存。</summary>
        private readonly Vector2[] sixPointArcPoints = new Vector2[6];

        /// <summary>终结技主炮NPC索引，-1表示当前没有主炮。</summary>
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
            writer.Write((byte)currentMoveState);

            writer.Write(moveAnchor.X);
            writer.Write(moveAnchor.Y);
            writer.Write(aimPoint.X);
            writer.Write(aimPoint.Y);
            writer.Write(dashStart.X);
            writer.Write(dashStart.Y);
            writer.Write(dashEnd.X);
            writer.Write(dashEnd.Y);

            writer.Write(patternSeed);
            writer.Write(moveTimer);
            writer.Write(currentMoveDir8);
            writer.Write(moveDirCooldown);
            writer.Write(railIndex);
            writer.Write(previousRailIndex);
            writer.Write(aimUpdateCooldown);
            writer.Write(anchorDriftDirection);
            writer.Write(anchorDriftRangeTiles);

            for (int i = 0; i < sixPointArcPoints.Length; i++)
            {
                writer.Write(sixPointArcPoints[i].X);
                writer.Write(sixPointArcPoints[i].Y);
            }

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
            currentMoveState = (MoveState)reader.ReadByte();

            moveAnchor = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            aimPoint = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            dashStart = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            dashEnd = new Vector2(reader.ReadSingle(), reader.ReadSingle());

            patternSeed = reader.ReadInt32();
            moveTimer = reader.ReadInt32();
            currentMoveDir8 = reader.ReadInt32();
            moveDirCooldown = reader.ReadInt32();
            railIndex = reader.ReadInt32();
            previousRailIndex = reader.ReadInt32();
            aimUpdateCooldown = reader.ReadInt32();
            anchorDriftDirection = reader.ReadInt32();
            anchorDriftRangeTiles = reader.ReadSingle();

            for (int i = 0; i < sixPointArcPoints.Length; i++)
            {
                sixPointArcPoints[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            }

            cannonId = reader.ReadInt32();
        }

        public override void AI()
        {
            NPC.TargetClosest(false);

            Player player = NPC.HasValidTarget ? Main.player[NPC.target] : null;

            ValidateTargetOrRetreat(player);

            if (!IsPlayerAvailable(player) && CurrentSkill != BossSkill.RetreatSkill)
            {
                NPC.velocity.Y -= 0.25f;
                NPC.timeLeft = 10;
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                ServerStateUpdate(player);
            }

            ApplyDefenseFromGuardProbes();

            RunCurrentSkill(player);
            RunMovement(player);

            UpdateVisualFacing(player);
        }

        /// <summary>
        /// 检查目标是否仍可战斗；无效、死亡或距离过远时切入退场。
        /// </summary>
        private void ValidateTargetOrRetreat(Player player)
        {
            if (IsPlayerAvailable(player) && !player.ghost && Vector2.Distance(NPC.Center, player.Center) <= 180f * Tile)
            {
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && CurrentSkill != BossSkill.RetreatSkill)
            {
                EnterRetreat();
            }
        }

        /// <summary>
        /// 服务端权威状态更新，负责阶段切换、特殊技能插入和普通技能选择。
        /// </summary>
        private void ServerStateUpdate(Player player)
        {
            if (Phase == BossPhase.Retreat)
            {
                return;
            }

            if (!IsPlayerAvailable(player) || player.ghost)
            {
                EnterRetreat();
                return;
            }

            if (!initialized)
            {
                EnterSpawnIntro();
                return;
            }

            if (!phase2Opened && NPC.life <= NPC.lifeMax / 2)
            {
                EnterPhaseTwoOpening();
                return;
            }

            if (CurrentSkill != BossSkill.Idle)
            {
                return;
            }

            if (Phase == BossPhase.PhaseTwo && IsMutationDifficulty() && CurrentSkill != BossSkill.UltimateCannon)
            {
                sp++;
            }

            if (Phase == BossPhase.PhaseTwo && IsMutationDifficulty() && sp >= 3000 && cannonUsable)
            {
                sp -= 3000;
                BeginSkill(BossSkill.UltimateCannon);
                SyncNow();
                return;
            }

            if (Phase == BossPhase.PhaseTwo && IsEndDifficulty() && Vector2.Distance(NPC.Center, player.Center) > 85f * Tile)
            {
                BeginSkill(BossSkill.ChaseProbeSeal);
                SyncNow();
                return;
            }

            BeginSkill(SelectNextSkill());
            SyncNow();
        }

        /// <summary>
        /// 进入入场阶段并启动入场激光。
        /// </summary>
        private void EnterSpawnIntro()
        {
            initialized = true;
            Phase = BossPhase.SpawnIntro;
            BeginSkill(BossSkill.IntroLaser);
            SyncNow();
        }

        /// <summary>
        /// 入场流程结束后进入第一阶段。
        /// </summary>
        private void EnterPhaseOne()
        {
            Phase = BossPhase.PhaseOne;
            CurrentSkill = BossSkill.Idle;
            SkillTimer = 0;
            SubState = 0;
            SetMoveState(MoveState.None);
            SyncNow();
        }

        /// <summary>
        /// 进入二阶段开场，强制执行六点电弧封锁。
        /// </summary>
        private void EnterPhaseTwoOpening()
        {
            phase2Opened = true;
            Phase = BossPhase.PhaseTwoOpening;
            BeginSkill(BossSkill.SixPointArc);
            SyncNow();
        }

        /// <summary>
        /// 二阶段开场技能结束后进入常规第二阶段。
        /// </summary>
        private void EnterPhaseTwo()
        {
            Phase = BossPhase.PhaseTwo;
            CurrentSkill = BossSkill.Idle;
            SkillTimer = 0;
            SubState = 0;
            SetMoveState(MoveState.None);
            SyncNow();
        }

        /// <summary>
        /// 进入退场阶段，终止普通技能选择并切换到上升离场动作。
        /// </summary>
        private void EnterRetreat()
        {
            Phase = BossPhase.Retreat;
            BeginSkill(BossSkill.RetreatSkill);
            SetMoveState(MoveState.RetreatRise);
            NPC.timeLeft = 300;
            SyncNow();
        }

        /// <summary>
        /// 根据目标位置更新贴图朝向；目标无效时保持当前朝向。
        /// </summary>
        private void UpdateVisualFacing(Player player)
        {
            if (!IsPlayerAvailable(player))
            {
                return;
            }

            NPC.spriteDirection = NPC.Center.X < player.Center.X ? 1 : -1;
        }

        /// <summary>
        /// 初始化指定技能的计时、子状态、随机种子和必要的技能锚点。
        /// </summary>
        private void BeginSkill(BossSkill skill)
        {
            CurrentSkill = skill;
            SkillTimer = 0;
            SubState = 0;
            patternSeed = Main.rand.Next();

            SetMoveState(MoveState.None);

            if (skill == BossSkill.IntroLaser && IsPlayerAvailable(Main.player[NPC.target]))
            {
                Player player = Main.player[NPC.target];
                moveAnchor = player.Center + new Vector2(0f, -30f * Tile);
                aimPoint = player.Center;
            }

            if (skill == BossSkill.NormalLaser && IsPlayerAvailable(Main.player[NPC.target]))
            {
                Player player = Main.player[NPC.target];
                moveAnchor = player.Center + new Vector2(0f, -30f * Tile);
                aimPoint = player.Center;
            }

            if (skill == BossSkill.HeavyLaser)
            {
                Player player = Main.player[NPC.target];
                moveAnchor = player.Center + new Vector2(0f, -40f * Tile);
                railIndex = 1;
                previousRailIndex = 1;
                SetMoveState(MoveState.RailShift);
            }

            if (skill == BossSkill.ChaseProbeSeal)
            {
                PrepareChaseProbeSeal(Main.player[NPC.target]);
            }
        }

        /// <summary>
        /// 结束当前技能并返回空闲状态，等待服务端选择下一技能。
        /// </summary>
        private void EndSkill()
        {
            lastSkill = CurrentSkill;
            CurrentSkill = BossSkill.Idle;
            SkillTimer = 0;
            SubState = 0;
            SyncNow();
        }

        /// <summary>
        /// 根据当前阶段从普通技能池中选择下一个技能。
        /// </summary>
        private BossSkill SelectNextSkill()
        {
            BossSkill[] pool;

            if (Phase == BossPhase.PhaseOne)
            {
                pool = (IsEndDifficulty() && Main.masterMode && Main.getGoodWorld)
                    ? new[] { BossSkill.EnsureBaseProbes, BossSkill.NormalLaser, BossSkill.SixPointArc, BossSkill.LightningOrbArc }
                    : new[] { BossSkill.EnsureBaseProbes, BossSkill.NormalLaser, BossSkill.SixPointArc, BossSkill.LightningOrbArc };
            }
            else
            {
                pool = (IsEndDifficulty() && Main.masterMode && Main.getGoodWorld)
                    ? new[] { BossSkill.EnsureBaseProbes, BossSkill.HeavyLaser, BossSkill.LightningOrbArc, BossSkill.LaserGrid }
                    : new[] { BossSkill.EnsureBaseProbes, BossSkill.HeavyLaser, BossSkill.LightningOrbArc };
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

        /// <summary>
        /// 按当前技能状态分发技能逻辑，并推进技能计时。
        /// </summary>
        private void RunCurrentSkill(Player player)
        {
            switch (CurrentSkill)
            {
                case BossSkill.IntroLaser:
                    RunIntroLaser(player);
                    break;

                case BossSkill.EnsureBaseProbes:
                    RunEnsureBaseProbes();
                    break;

                case BossSkill.NormalLaser:
                    RunNormalLaser(player);
                    break;

                case BossSkill.SixPointArc:
                    RunSixPointArc(player);
                    break;

                case BossSkill.HeavyLaser:
                    RunLaser(player, 600, true);
                    break;

                case BossSkill.UltimateCannon:
                    RunUltimateCannon(player);
                    break;

                case BossSkill.ChaseProbeSeal:
                    RunChaseProbeSeal(player);
                    break;

                case BossSkill.LaserGrid:
                    RunLaserGrid(player);
                    break;

                case BossSkill.LightningOrbArc:
                    RunLightningOrbArc(player);
                    break;

                case BossSkill.RetreatSkill:
                    RunRetreatSkill();
                    break;
            }

            if (CurrentSkill != BossSkill.Idle)
            {
                SkillTimer++;
            }
        }

        /// <summary>
        /// 执行入场激光技能，按准备、定位、攻击、收招四个子状态推进。
        /// </summary>
        private void RunIntroLaser(Player player)
        {
            switch (SubState)
            {
                case 0:
                    if (IsPlayerAvailable(player))
                    {
                        moveAnchor = player.Center + new Vector2(0f, -30f * Tile);
                        aimPoint = player.Center;
                    }

                    SetMoveState(MoveState.SkillReposition);
                    SubState = 1;
                    SyncNow();
                    break;

                case 1:
                    if (IsNearAnchor(5f) || moveTimer >= 120)
                    {
                        SetMoveState(MoveState.AnchorDrift);
                        SkillTimer = 0;
                        SubState = 2;
                        SyncNow();
                    }
                    break;

                case 2:
                    if (IsPlayerAvailable(player))
                    {
                        aimPoint = player.Center;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient && SkillTimer % 90 == 0)
                    {
                        SpawnAimedLaser(player, strong: false, lifetime: 300);
                    }

                    if (SkillTimer >= 900)
                    {
                        SetMoveState(MoveState.StaticHold);
                        SkillTimer = 0;
                        SubState = 3;
                        SyncNow();
                    }
                    break;

                case 3:
                    if (SkillTimer >= 15)
                    {
                        BeginSkill(BossSkill.EnsureBaseProbes);
                        SyncNow();
                    }
                    break;
            }
        }

        /// <summary>
        /// 执行基础探测器补齐技能；入场阶段结束时会转入第一阶段。
        /// </summary>
        private void RunEnsureBaseProbes()
        {
            switch (SubState)
            {
                case 0:
                    CountBaseProbes();
                    SubState = 1;
                    SyncNow();
                    break;

                case 1:
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        EnsureBaseProbe(ProbeMode.Guard, 0);
                        EnsureBaseProbe(ProbeMode.Suicide, 1);
                        EnsureBaseProbe(ProbeMode.Jammer, 2);
                        SyncNow();
                    }

                    SubState = 2;
                    break;

                case 2:
                    if (Phase == BossPhase.SpawnIntro)
                    {
                        EnterPhaseOne();
                        return;
                    }

                    EndSkill();
                    break;
            }
        }

        /// <summary>
        /// 执行普通持续激光技能，按定位、蓄能、持续攻击和收招推进。
        /// </summary>
        private void RunNormalLaser(Player player)
        {
            switch (SubState)
            {
                case 0:
                    if (IsPlayerAvailable(player))
                    {
                        moveAnchor = player.Center + new Vector2(0f, -30f * Tile);
                        aimPoint = player.Center;
                    }

                    SetMoveState(MoveState.SkillReposition);
                    SubState = 1;
                    SyncNow();
                    break;

                case 1:
                    if (IsNearAnchor(5f) || moveTimer >= 120)
                    {
                        SetMoveState(MoveState.StaticHold);
                        SkillTimer = 0;
                        SubState = 2;
                        SyncNow();
                    }
                    break;

                case 2:
                    if (SkillTimer >= 90)
                    {
                        SetMoveState(MoveState.AnchorDrift);
                        SkillTimer = 0;
                        SubState = 3;
                        SyncNow();
                    }
                    break;

                case 3:
                    if (IsPlayerAvailable(player) && aimUpdateCooldown <= 0)
                    {
                        aimPoint = player.Center;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient && SkillTimer % 90 == 0)
                    {
                        SpawnAimedLaser(player, strong: false, lifetime: 300);
                    }

                    if (SkillTimer >= 900)
                    {
                        SetMoveState(MoveState.StaticHold);
                        SkillTimer = 0;
                        SubState = 4;
                        SyncNow();
                    }
                    break;

                case 4:
                    if (SkillTimer >= 30)
                    {
                        EndSkill();
                    }
                    break;
            }
        }

        /// <summary>
        /// 执行持续瞄准激光的临时通用逻辑。
        /// </summary>
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

        /// <summary>
        /// 执行六点电弧封锁，按点位生成、预警、压制激光和收招推进。
        /// </summary>
        private void RunSixPointArc(Player player)
        {
            switch (SubState)
            {
                case 0:
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (!SpawnSixPointArcPattern(player))
                        {
                            EndSkill();
                            return;
                        }

                        SpawnSixPointArcWarningMarkers();
                        SpawnSixPointArcTelegraphs();
                    }

                    SetMoveState(MoveState.StaticHold);
                    SkillTimer = 0;
                    SubState = 1;
                    SyncNow();
                    break;

                case 1:
                    if (SkillTimer >= 90)
                    {
                        SetMoveState(MoveState.RailShift);
                        SkillTimer = 0;
                        SubState = 2;
                        SyncNow();
                    }
                    break;

                case 2:
                    if (Main.netMode != NetmodeID.MultiplayerClient && SkillTimer % 90 == 0)
                    {
                        SpawnSixPointArcPressureLasers(player);
                    }

                    if (SkillTimer >= 900)
                    {
                        SetMoveState(MoveState.StaticHold);
                        SkillTimer = 0;
                        SubState = 3;
                        SyncNow();
                    }
                    break;

                case 3:
                    if (SkillTimer >= 30)
                    {
                        if (Phase == BossPhase.PhaseTwoOpening)
                        {
                            EnterPhaseTwo();
                            return;
                        }

                        EndSkill();
                    }
                    break;
            }
        }

        /// <summary>
        /// 执行终结技主炮的当前骨架逻辑。
        /// </summary>
        private void RunUltimateCannon(Player player)
        {
            if (SkillTimer == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                EnsureBaseProbe(ProbeMode.Guard, 0);
                EnsureBaseProbe(ProbeMode.Suicide, 1);
                EnsureBaseProbe(ProbeMode.Jammer, 2);
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

        /// <summary>
        /// 执行追杀探测器封锁的临时占位逻辑，目前仍复用线段冲刺压力。
        /// </summary>
        private void RunChaseProbeSeal(Player player)
        {
            if (SkillTimer < 45)
            {
                MoveTo(moveAnchor, 22f, 8f);
                return;
            }

            if (SkillTimer == 45 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                dashStart = NPC.Center;
                SpawnChaseProbeSealTelegraph(dashStart, dashEnd);
                SyncNow();
            }

            DashTo(dashEnd, 36f);

            if (SkillTimer == 52 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnDamagingChaseProbeSeal(dashStart, dashEnd);
            }

            if (SkillTimer >= 85 || Vector2.Distance(NPC.Center, dashEnd) < 32f)
            {
                EndSkill();
            }
        }

        /// <summary>
        /// 执行激光网的当前骨架逻辑，按三轮预警和伤害帧推进。
        /// </summary>
        private void RunLaserGrid(Player player)
        {
            int cycle = SkillTimer / 90;
            int frame = SkillTimer % 90;

            if (cycle < 3 && frame == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnLaserGridTelegraph(player, cycle);
            }

            if (cycle < 3 && frame == 45 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnLaserGridDamage(player, cycle);
            }

            if (cycle >= 3)
            {
                EndSkill();
            }
        }

        /// <summary>
        /// 执行闪电珠弧的当前骨架逻辑，按四轮释放推进。
        /// </summary>
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

        /// <summary>
        /// 执行退场技能本体逻辑；实际上升速度由退场移动状态处理。
        /// </summary>
        private void RunRetreatSkill()
        {
            NPC.timeLeft = 10;
        }

        /// <summary>
        /// 按当前移动状态分发Boss本体移动逻辑。
        /// </summary>
        private void RunMovement(Player player)
        {
            switch (currentMoveState)
            {
                case MoveState.None:
                    if (IsPlayerAvailable(player))
                    {
                        MoveToAllowedHoverArea(player);
                    }
                    break;

                case MoveState.SkillReposition:
                    RunSkillReposition(player);
                    break;

                case MoveState.AnchorDrift:
                    RunAnchorDrift(player);
                    break;

                case MoveState.RailShift:
                    RunRailShift(player);
                    break;

                case MoveState.StaticHold:
                    RunStaticHold();
                    break;

                case MoveState.VerticalPress:
                    RunVerticalPress(player);
                    break;

                case MoveState.RetreatRise:
                    RunRetreatRise();
                    break;
            }

            moveTimer++;
            moveDirCooldown = Math.Max(0, moveDirCooldown - 1);
            aimUpdateCooldown = Math.Max(0, aimUpdateCooldown - 1);
        }

        /// <summary>
        /// 切换移动状态，并重置移动状态专用计时和冷却。
        /// </summary>
        private void SetMoveState(MoveState state)
        {
            if (currentMoveState == state)
            {
                return;
            }

            currentMoveState = state;
            moveTimer = 0;
            moveDirCooldown = 0;
            aimUpdateCooldown = 0;

            if (state == MoveState.AnchorDrift && Main.netMode != NetmodeID.MultiplayerClient)
            {
                anchorDriftDirection = Main.rand.NextBool() ? 1 : -1;
                anchorDriftRangeTiles = Main.rand.NextFloat(8f, 16f);
            }

            SyncNow();
        }

        /// <summary>
        /// 技能前定位移动：使用8方向量化方向接近锁定锚点。
        /// </summary>
        private void RunSkillReposition(Player player)
        {
            Vector2 direction = moveAnchor - NPC.Center;

            if (direction.LengthSquared() <= 0.01f)
            {
                NPC.velocity *= 0.9f;
                return;
            }

            if (moveDirCooldown <= 0)
            {
                Vector2 moveDirection = QuantizeTo8Direction(direction);
                currentMoveDir8 = DirectionTo8Index(moveDirection);
                moveDirCooldown = 120;
            }

            Vector2 targetDirection = Direction8ToVector(currentMoveDir8);
            Vector2 destination = NPC.Center + targetDirection * Math.Min(direction.Length(), 18f);
            MoveTo(destination, 18f, 10f);
        }

        /// <summary>
        /// 锚点漂移动作：围绕技能锚点横向摆动，并低频更新瞄准点。
        /// </summary>
        private void RunAnchorDrift(Player player)
        {
            if (!IsPlayerAvailable(player))
            {
                NPC.velocity *= HoverAreaDamping;
                return;
            }

            if (aimUpdateCooldown <= 0)
            {
                aimPoint = new Vector2(moveAnchor.X, player.Center.Y - 30f * Tile);
                aimUpdateCooldown = 45;
            }

            if (moveTimer > 0 && moveTimer % 180 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                anchorDriftDirection *= -1;
                anchorDriftRangeTiles = Main.rand.NextFloat(8f, 16f);
                SyncNow();
            }

            float horizontalOffset = anchorDriftDirection * anchorDriftRangeTiles * Tile;
            Vector2 destination = new Vector2(moveAnchor.X + horizontalOffset, aimPoint.Y);
            MoveTo(destination, 10f, 14f);
        }

        /// <summary>
        /// 轨道切换动作：在左、中、右三条轨道之间平滑移动。
        /// </summary>
        private void RunRailShift(Player player)
        {
            if (!IsPlayerAvailable(player))
            {
                NPC.velocity *= HoverAreaDamping;
                return;
            }

            if (moveTimer % 150 == 0)
            {
                previousRailIndex = railIndex;

                do
                {
                    railIndex = Main.rand.Next(3);
                }
                while (railIndex == previousRailIndex);

                SyncNow();
            }

            float railOffset = (railIndex - 1) * 12f * Tile;
            Vector2 destination = new Vector2(moveAnchor.X + railOffset, moveAnchor.Y);
            MoveTo(destination, 24f, 8f);
        }

        /// <summary>
        /// 静止维持动作：快速阻尼速度，避免技能期间继续追踪玩家。
        /// </summary>
        private void RunStaticHold()
        {
            NPC.velocity *= 0.85f;

            if (NPC.velocity.LengthSquared() < 0.04f)
            {
                NPC.velocity = Vector2.Zero;
            }
        }

        /// <summary>
        /// 纵向压制动作：保持横向锚点并低频修正到玩家上方。
        /// </summary>
        private void RunVerticalPress(Player player)
        {
            if (!IsPlayerAvailable(player))
            {
                NPC.velocity *= HoverAreaDamping;
                return;
            }

            if (aimUpdateCooldown <= 0)
            {
                aimPoint = new Vector2(moveAnchor.X, player.Center.Y - 30f * Tile);
                aimUpdateCooldown = 40;
            }

            MoveTo(aimPoint, 10f, 16f);
        }

        /// <summary>
        /// 退场上升动作：持续削减横向速度并向上离开战场。
        /// </summary>
        private void RunRetreatRise()
        {
            NPC.velocity.X *= 0.95f;
            NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y - 0.25f, -12f, 4f);
        }

        /// <summary>
        /// 判断Boss是否已经进入指定格数范围内的移动锚点。
        /// </summary>
        private bool IsNearAnchor(float tileRange)
        {
            return Vector2.DistanceSquared(NPC.Center, moveAnchor) <= tileRange * tileRange * Tile * Tile;
        }

        /// <summary>
        /// 将任意方向量化为8方向单位向量。
        /// </summary>
        private static Vector2 QuantizeTo8Direction(Vector2 direction)
        {
            if (direction.LengthSquared() <= 0.01f)
            {
                return Vector2.Zero;
            }

            direction.Normalize();

            float angle = MathF.Atan2(direction.Y, direction.X);
            float step = MathHelper.PiOver4;
            float snappedAngle = MathF.Round(angle / step) * step;

            return snappedAngle.ToRotationVector2();
        }

        /// <summary>
        /// 将8方向单位向量转换为0到7的方向索引。
        /// </summary>
        private static int DirectionTo8Index(Vector2 direction)
        {
            if (direction.LengthSquared() <= 0.01f)
            {
                return 0;
            }

            float angle = MathF.Atan2(direction.Y, direction.X);
            int index = (int)MathF.Round(angle / MathHelper.PiOver4);

            return (index % 8 + 8) % 8;
        }

        /// <summary>
        /// 将0到7的方向索引转换为对应8方向单位向量。
        /// </summary>
        private static Vector2 Direction8ToVector(int directionIndex)
        {
            return (directionIndex * MathHelper.PiOver4).ToRotationVector2();
        }

        /// <summary>
        /// 普通悬停动作：将Boss限制在玩家上方的一片允许区域内。
        /// </summary>
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

        /// <summary>
        /// 准备追杀封锁的临时锚点和线段终点。
        /// </summary>
        private void PrepareChaseProbeSeal(Player player)
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

        /// <summary>
        /// 按最大速度和惯性平滑靠近目标点。
        /// </summary>
        private void MoveTo(Vector2 destination, float maxSpeed, float inertia)
        {
            Vector2 desiredVelocity = destination - NPC.Center;

            if (desiredVelocity.LengthSquared() > maxSpeed * maxSpeed)
            {
                desiredVelocity = desiredVelocity.SafeNormalize(Vector2.Zero) * maxSpeed;
            }

            NPC.velocity = (NPC.velocity * (inertia - 1f) + desiredVelocity) / inertia;
        }

        /// <summary>
        /// 以固定速度直接冲向目标点。
        /// </summary>
        private void DashTo(Vector2 destination, float speed)
        {
            Vector2 direction = destination - NPC.Center;
            NPC.velocity = direction.SafeNormalize(Vector2.UnitY) * speed;
        }

        /// <summary>
        /// 标记NPC需要同步AI状态。
        /// </summary>
        private void SyncNow()
        {
            NPC.netUpdate = true;
        }

        /// <summary>
        /// 探测器死亡时由探测器调用，用于二阶段SP积累。
        /// </summary>
        public void NotifyProbeKilled()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (Phase == BossPhase.PhaseTwo && IsMutationDifficulty() && CurrentSkill != BossSkill.UltimateCannon)
            {
                sp += 400;
                SyncNow();
            }
        }

        /// <summary>
        /// 主炮被击败时由主炮调用，用于禁用后续终结技。
        /// </summary>
        public void NotifyCannonKilled()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            cannonUsable = false;

            if (CurrentSkill == BossSkill.UltimateCannon)
            {
                finisherLifeDebt = 0;
                EndSkill();
            }

            SyncNow();
        }

        /// <summary>
        /// 判断当前世界是否启用异变难度。
        /// </summary>
        private bool IsMutationDifficulty()
        {
            return AdvancedWorld.MutationMode;
        }

        /// <summary>
        /// 判断当前世界是否启用终焉难度。
        /// </summary>
        private bool IsEndDifficulty()
        {
            return AdvancedWorld.EndMode;
        }

        /// <summary>
        /// 根据存活的护佑探测器数量刷新Boss防御。
        /// </summary>
        private void ApplyDefenseFromGuardProbes()
        {
            int guardProbeCount = 0;

            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.type == ModContent.NPCType<DiagrammerProbe>() &&
                    n.active &&
                    (int)n.ai[0] == NPC.whoAmI &&
                    (ProbeKind)(int)n.ai[3] == ProbeKind.Base &&
                    (ProbeMode)(int)n.ai[1] == ProbeMode.Guard)
                {
                    guardProbeCount++;
                }
            }

            NPC.defense = BaseDefense + guardProbeCount * GuardProbeDefenseBonus;
        }

        /// <summary>
        /// 统计当前仍然存活的基础探测器数量。
        /// </summary>
        private int CountBaseProbes()
        {
            int count = 0;

            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.type == ModContent.NPCType<DiagrammerProbe>() &&
                    n.active &&
                    (int)n.ai[0] == NPC.whoAmI &&
                    (ProbeKind)(int)n.ai[3] == ProbeKind.Base)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 判断指定形态的基础探测器是否已经存在。
        /// </summary>
        private bool HasBaseProbeMode(ProbeMode mode)
        {
            foreach (NPC n in Main.ActiveNPCs)
            {
                if (n.type == ModContent.NPCType<DiagrammerProbe>() &&
                    n.active &&
                    (int)n.ai[0] == NPC.whoAmI &&
                    (ProbeKind)(int)n.ai[3] == ProbeKind.Base &&
                    (ProbeMode)(int)n.ai[1] == mode)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 确保指定基础形态的探测器存在，不存在时按槽位创建一个。
        /// </summary>
        private void EnsureBaseProbe(ProbeMode mode, int slot)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (HasBaseProbeMode(mode))
            {
                return;
            }

            int index = NPC.NewNPC(
                NPC.GetSource_FromAI(),
                (int)NPC.Center.X,
                (int)NPC.Center.Y,
                ModContent.NPCType<DiagrammerProbe>(),
                0,
                NPC.whoAmI,
                (float)mode,
                slot,
                (float)ProbeKind.Base,
                NPC.target
            );

            Main.npc[index].netUpdate = true;
        }
        /// <summary>
        /// 终结技占位：后续强制所有基础探测器进入妨碍形态。
        /// </summary>
        private void ForceAllProbesToLaserMode() { }
        /// <summary>
        /// 终结技占位：后续生成主炮并保存NPC索引。
        /// </summary>
        private void SpawnMainCannon() { }
        /// <summary>
        /// 判断当前记录的主炮是否仍然存活。
        /// </summary>
        private bool IsMainCannonAlive() => cannonId >= 0 && Main.npc[cannonId].active;
        /// <summary>
        /// 终结技占位：后续回收主炮而不触发主炮击败逻辑。
        /// </summary>
        private void RecycleMainCannon() { }

        /// <summary>
        /// 生成临时瞄准激光弹幕。
        /// </summary>
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
                    NPC.damage,
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
        /// <summary>
        /// 计算并保存六点电弧封锁的点位。
        /// </summary>
        private bool SpawnSixPointArcPattern(Player player) {
            if (!IsPlayerAvailable(player))
            {
                return false;
            }

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
                return false;
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

            sixPointArcPoints[0] = p1;
            sixPointArcPoints[1] = p2;
            sixPointArcPoints[2] = p3;
            sixPointArcPoints[3] = p4;
            sixPointArcPoints[4] = p5;
            sixPointArcPoints[5] = p6;

            moveAnchor = (p3 + p4) * 0.5f;
            railIndex = 1;
            previousRailIndex = 1;

            return true;
        }

        /// <summary>
        /// 在六点电弧封锁的每个点位上生成预警标示。
        /// </summary>
        private void SpawnSixPointArcWarningMarkers()
        {
            for (int i = 0; i < sixPointArcPoints.Length; i++)
            {
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI("SixPointArcWarning"),
                    sixPointArcPoints[i],
                    Vector2.Zero,
                    ModContent.ProjectileType<WarningMarkerProjectile>(),
                    0,
                    0f,
                    Main.myPlayer
                );
            }
        }

        /// <summary>
        /// 生成六点之间的电弧预警线。
        /// </summary>
        private void SpawnSixPointArcTelegraphs()
        {
            // 下梯形: p1-p2 底边, p1-p4 左边, p2-p3 右边
            SpawnSixPointArcTelegraphLine(0, 1);
            SpawnSixPointArcTelegraphLine(0, 3);
            SpawnSixPointArcTelegraphLine(1, 2);
            // 上梯形: p5-p6 顶边, p4-p5 左边, p3-p6 右边
            SpawnSixPointArcTelegraphLine(4, 5);
            SpawnSixPointArcTelegraphLine(3, 4);
            SpawnSixPointArcTelegraphLine(2, 5);
            // 共享中边
            SpawnSixPointArcTelegraphLine(2, 3);
            Main.NewText("Debug: Six-point arc pattern generated with points: ", Color.Yellow);
        }

        /// <summary>
        /// 按保存的点位索引生成一条电弧预警线。
        /// </summary>
        private void SpawnSixPointArcTelegraphLine(int startIndex, int endIndex)
        {
            HostileStaticArcProjectile.SpawnTelegraph(
                NPC.GetSource_FromAI("SixPointArcTelegraph"),
                sixPointArcPoints[startIndex],
                sixPointArcPoints[endIndex],
                1.5f
            );
        }

        /// <summary>
        /// 生成六点电弧封锁期间的两发成90度夹角的压制激光。
        /// </summary>
        private void SpawnSixPointArcPressureLasers(Player player)
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

            SpawnLaserByDirection(direction.RotatedBy(MathHelper.PiOver4));
            SpawnLaserByDirection(direction.RotatedBy(-MathHelper.PiOver4));
        }

        /// <summary>
        /// 从Boss中心按指定方向生成普通压制激光。
        /// </summary>
        private void SpawnLaserByDirection(Vector2 direction)
        {
            if (!IsFinite(direction) || direction.LengthSquared() <= 0f)
            {
                return;
            }

            direction.Normalize();

            Projectile.NewProjectile(
                NPC.GetSource_FromAI("SixPointArcPressureLaser"),
                NPC.Center,
                direction * 14f,
                ProjectileID.EyeLaser,
                NPC.damage,
                0f
            );
        }
        /// <summary>
        /// 生成追杀封锁占位线段的预警效果。
        /// </summary>
        private void SpawnChaseProbeSealTelegraph(Vector2 start, Vector2 end)
        {
            HostileStaticArcProjectile.SpawnTelegraph(
                NPC.GetSource_FromAI(),
                start,
                end,
                0.35f
            );
        }
        /// <summary>
        /// 生成追杀封锁占位线段的伤害判定。
        /// </summary>
        private void SpawnDamagingChaseProbeSeal(Vector2 start, Vector2 end) {
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
        /// <summary>
        /// 激光网占位：后续生成当前轮次的判定线预警。
        /// </summary>
        private void SpawnLaserGridTelegraph(Player player, int cycle) {

        }
        /// <summary>
        /// 激光网占位：后续生成当前轮次的伤害线。
        /// </summary>
        private void SpawnLaserGridDamage(Player player, int cycle) { }
        /// <summary>
        /// 闪电珠弧占位：后续生成当前轮次的闪电珠弧弹幕。
        /// </summary>
        private void SpawnLightningOrbArc(Player player, int cycle) { }

        /// <summary>
        /// 判断玩家是否可作为有效攻击目标。
        /// </summary>
        private static bool IsPlayerAvailable(Player player)
        {
            return player != null && player.active && !player.dead;
        }

        /// <summary>
        /// 判断向量的两个分量是否都是有限数值。
        /// </summary>
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

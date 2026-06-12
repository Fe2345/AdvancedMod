# Tree Diagrammer Refactor And Skills Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 将树状图设计者 Boss 从当前雏形重构为和 `docs/designs/Boss.md`、`NPCs/Boss/TreeDiagrammer_Implementation.md` 对齐的阶段、技能、动作、召唤物状态机，并补齐到子技能级别的实现路线。

**Architecture:** Boss 本体只负责阶段决策、技能选择、动作状态、SP、难度插入和召唤物编排；探测器与主炮作为独立 NPC 执行附属行为。每个 Boss 技能拆成 Prepare、Reposition、Telegraph、Active、Recover、End 等子状态，动作由独立 `MoveState` 执行，避免技能逻辑和移动逻辑互相缠绕。

**Tech Stack:** .NET 8.0、C#、tModLoader NPC AI、Terraria Projectile API、ReLogic/FNA、Luminance 可选视觉效果、Windows PowerShell `dotnet build`。

---

## 设计对齐基准

本计划以以下文件为准：

- 基本设计：`docs/designs/Boss.md`
- 详细设计：`NPCs/Boss/TreeDiagrammer_Implementation.md`
- 当前 Boss：`NPCs/Boss/TreeDiagrammer.cs`
- 当前探测器：`NPCs/Servant/DiagrammerProbe.cs`
- 当前主炮：`NPCs/Servant/DiagrammerCannon.cs`
- 可复用电弧弹幕：`Projectiles/NPCs/HostileStaticArcProjectile.cs`
- 可复用带电激光：`Projectiles/NPCs/DiagrammerElectrifiedLaser.cs`

## 技能编号目标

重构后统一使用基本设计稿编号：

```text
0 Idle
1 IntroLaser
2 EnsureBaseProbes
3 NormalLaser
4 SixPointArc
5 HeavyLaser
6 UltimateCannon
7 ChaseProbeSeal
8 LaserGrid
9 LightningOrbArc
10 RetreatSkill
```

---

### Task 1: 建立重构前基线

**Files:**
- Read: `NPCs/Boss/TreeDiagrammer.cs`
- Read: `NPCs/Servant/DiagrammerProbe.cs`
- Read: `NPCs/Servant/DiagrammerCannon.cs`
- Verify: whole project

**Step 1: Run baseline build**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 记录当前是否可编译。若失败，先只修复和本计划直接相关的编译错误，不调整行为。

**Step 2: Capture current Boss placeholders**

Run:

```bash
rg -n "ForceAllProbesToLaserMode|SpawnMainCannon|RecycleMainCannon|SpawnLaserNet|SpawnLightningOrbArc|SpawnSixPointArcPattern" NPCs/Boss/TreeDiagrammer.cs
```

Expected: 明确当前空实现和半实现位置，作为后续逐项替换目标。

---

### Task 2: 统一 Boss 阶段和技能枚举

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`

**Step 1: Replace phase enum**

将阶段枚举调整为：

```csharp
private enum BossPhase : byte
{
    SpawnIntro = 0,
    PhaseOne = 1,
    PhaseTwoOpening = 2,
    PhaseTwo = 3,
    Retreat = 4
}
```

**Step 2: Replace skill enum**

将技能枚举调整为：

```csharp
private enum BossSkill : byte
{
    Idle = 0,
    IntroLaser = 1,
    EnsureBaseProbes = 2,
    NormalLaser = 3,
    SixPointArc = 4,
    HeavyLaser = 5,
    UltimateCannon = 6,
    ChaseProbeSeal = 7,
    LaserGrid = 8,
    LightningOrbArc = 9,
    RetreatSkill = 10
}
```

**Step 3: Update all switch cases and call sites**

把旧名称映射到新名称：

```text
EntryLaser -> IntroLaser
SummonProbes -> EnsureBaseProbes
Laser -> NormalLaser
ArcSixPoints -> SixPointArc
StrongLaser -> HeavyLaser
Finisher -> UltimateCannon
DashArc -> ChaseProbeSeal
LaserNet -> LaserGrid
LightningOrbArc -> LightningOrbArc
```

删除或合并旧的 `PhaseCharge` 概念；基本设计稿没有该 Boss 本体冲刺技能，追杀压力应由技能7的追杀探测器承担。

**Step 4: Build**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 编译通过，行为可暂时不完整。

---

### Task 3: 重排 Boss AI 主循环

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`

**Step 1: Split server decision from execution**

将 `AI()` 调整为固定顺序：

```text
1. TargetClosest
2. ValidateTargetOrRetreat
3. ServerStateUpdate
4. ApplyDefenseFromGuardProbes
5. RunCurrentSkill
6. RunMovement
7. UpdateVisualFacing
```

注意：先执行技能再执行动作，便于技能子状态设置本帧动作目标。

**Step 2: Add retreat priority**

新增逻辑：

```text
玩家无效、死亡、ghost、距离极远时：
Phase = Retreat
CurrentSkill = RetreatSkill
MoveState = RetreatRise
```

退场优先级高于阶段切换、SP终结技、距离惩罚和普通技能池。

**Step 3: Add phase transition methods**

拆出：

```csharp
private void EnterSpawnIntro()
private void EnterPhaseOne()
private void EnterPhaseTwoOpening()
private void EnterPhaseTwo()
private void EnterRetreat()
```

每个方法只负责设置阶段、技能、计时器、必要字段和 `NPC.netUpdate`。

**Step 4: Verify**

Run:

```bash
rg -n "Phase1|Phase2|PhaseCharge|EntryLaser|SummonProbes|Finisher|DashArc|LaserNet" NPCs/Boss/TreeDiagrammer.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 旧阶段名和旧技能名不再出现；编译通过。

---

### Task 4: 引入动作状态 MoveState

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`

**Step 1: Add movement enum**

```csharp
private enum MoveState : byte
{
    None = 0,
    SkillReposition = 1,
    AnchorDrift = 2,
    RailShift = 3,
    StaticHold = 4,
    VerticalPress = 5,
    RetreatRise = 6
}
```

**Step 2: Add movement fields**

新增字段：

```csharp
private MoveState currentMoveState;
private int moveTimer;
private int currentMoveDir8;
private int moveDirCooldown;
private int railIndex;
private int previousRailIndex;
private Vector2 moveAnchor;
private Vector2 aimPoint;
private int aimUpdateCooldown;
```

需要多人同步的字段加入 `SendExtraAI` / `ReceiveExtraAI`。

**Step 3: Implement movement dispatch**

实现：

```csharp
private void SetMoveState(MoveState state)
private void RunMovement(Player player)
private void RunSkillReposition(Player player)
private void RunAnchorDrift(Player player)
private void RunRailShift(Player player)
private void RunStaticHold()
private void RunVerticalPress(Player player)
private void RunRetreatRise()
```

**Step 4: Movement acceptance checks**

实现通用判定：

```csharp
private bool IsNearAnchor(float tileRange)
private static Vector2 QuantizeTo8Direction(Vector2 direction)
```

`SkillReposition` 到达 5 格内或超过 120 刻视为完成。

**Step 5: Build**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 编译通过；普通悬停行为仍可工作。

---

### Task 5: 技能1 IntroLaser 子状态

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`

**Subskills:**

```text
Prepare：锁定玩家上方30格锚点
Reposition：快速到达锚点，最多120刻
Active：每90刻发射普通瞄准激光，持续900刻
Recover：速度阻尼15刻
End：进入技能2 EnsureBaseProbes
```

**Step 1: Implement substate switch**

`RunIntroLaser(Player player)` 不再只依赖 `SkillTimer >= 300`，改用 `SubState` 推进。

**Step 2: Lock aim cadence**

普通激光每 8 刻瞄准玩家当前位置；后续可替换为专用敌对激光弹幕。

**Step 3: Verify**

Run:

```bash
rg -n "RunIntroLaser|BossSkill.IntroLaser|BeginSkill\\(BossSkill.EnsureBaseProbes\\)" NPCs/Boss/TreeDiagrammer.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 技能1结束后必定进入技能2；编译通过。

---

### Task 6: 技能2 EnsureBaseProbes 子状态

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`
- Modify: `NPCs/Servant/DiagrammerProbe.cs`

**Subskills:**

```text
Prepare：统计基础探测器数量
Active：补齐护佑、自毁、妨碍三种基础探测器
Recover：刷新槽位和网络同步
End：入场时进入PhaseOne，普通技能池时回到Idle
```

**Step 1: Mark base probes**

约定基础探测器：

```text
ai[0] = Boss whoAmI
ai[1] = ProbeMode
ai[2] = 追杀模式下正交矩阵的方向，取值为1和-1
ai[3] = ProbeKind，0表示基础探测器
```

**Step 2: Implement count helpers**

新增：

```csharp
private int CountBaseProbes()
private bool HasBaseProbeMode(ProbeMode mode)
private void EnsureBaseProbe(ProbeMode mode, int slot)
```

**Step 3: End behavior by phase**

如果当前阶段是 `SpawnIntro`，技能2完成后调用 `EnterPhaseOne()`；否则 `EndSkill()`。

**Step 4: Verify**

Run:

```bash
rg -n "EnsureBaseProbe|CountBaseProbes|ProbeKind|EnterPhaseOne" NPCs/Boss/TreeDiagrammer.cs NPCs/Servant/DiagrammerProbe.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 基础探测器不会无限重复召唤；编译通过。

---

### Task 7: 技能3 NormalLaser 子状态

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`

**Subskills:**

```text
Prepare：锁定锚点和初始瞄准点
Reposition：移动到玩家上方30格附近
Telegraph：蓄能90刻
Active：持续900刻，每90刻发射普通激光，动作状态为AnchorDrift
Recover：停火并速度阻尼30刻
End：回到Idle
```

**Step 1: Implement anchor drift**

`AnchorDrift` 横向偏移由技能脚本控制，每 180 刻改变一次方向，横移范围 8 到 16 格；纵向每 30 到 45 刻修正一次。

**Step 2: Implement laser firing**

复用 `SpawnAimedLaser(player, strong: false, lifetime: 300)`，但保留后续替换专用弹幕的接口。

**Step 3: Verify**

Run:

```bash
rg -n "RunNormalLaser|AnchorDrift|NormalLaserFireInterval" NPCs/Boss/TreeDiagrammer.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 普通激光不再直接被玩家实时牵引；编译通过。

---

### Task 8: 技能4 SixPointArc 子状态

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`
- Verify: `Projectiles/NPCs/HostileStaticArcProjectile.cs`
- Verify: `Projectiles/Effects/WarningMarkerProjectile.cs`

**Subskills:**

```text
Prepare：计算6个空中点位并保存到字段
Telegraph：生成点间预警电弧
Active：释放两组压制激光束
Recover：电弧残留和停顿
End：二阶段开场时进入PhaseTwo，否则回到Idle
```

**Step 1: Persist pattern points**

当前 `SpawnSixPointArcPattern` 只在局部变量里生成点位。新增字段：

```csharp
private Vector2[] sixPointArcPoints;
```

或用固定 6 个 `Vector2` 字段，确保多人同步时可写入 `SendExtraAI`。

**Step 2: Spawn telegraph arcs**

使用 `HostileStaticArcProjectile.SpawnTelegraph(...)` 连接 6 点：

```text
p1-p3, p3-p5, p2-p4, p4-p6, p3-p4，p5-p6
```

具体连线可在实现时按战斗表现调整，但必须保持“6点电弧封锁”的可见预警。

**Step 3: Spawn damage arcs or laser beams**

至少生成两组压制激光：

每90刻沿着朝向玩家方向，发射一对普通激光（夹角为90度），持续900刻，动作为 `RailShift`，轨道在三个点位中切换。

**Step 4: Phase opening behavior**

如果 `Phase == PhaseTwoOpening`，技能4结束后调用 `EnterPhaseTwo()`。

**Step 5: Verify**

Run:

```bash
rg -n "sixPointArc|SpawnSixPointArc|PhaseTwoOpening|EnterPhaseTwo" NPCs/Boss/TreeDiagrammer.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 技能4有预警、有伤害段、有阶段开场收束；编译通过。

---

### Task 9: 技能5 HeavyLaser 子状态

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`

**Subskills:**

```text
Prepare：锁定中心锚点和轨道列表
Reposition：移动到中心轨道
Telegraph：强激光蓄能60刻
Active：持续720刻，每60刻发射强激光，动作状态为RailShift
Recover：停止激光并阻尼45刻
End：回到Idle
```

**Step 1: Implement rail selection**

轨道：

```text
左轨道：AnchorX - 12格
中轨道：AnchorX
右轨道：AnchorX + 12格
```

每 120 到 150 刻切换一次，不能连续选择同一轨道。

**Step 2: Implement strong laser**

复用 `SpawnAimedLaser(player, strong: true, lifetime: 600)`；伤害倍率保留现有 `NPC.damage * 1.5f`，后续由终焉倍率统一调整。

**Step 3: Verify**

Run:

```bash
rg -n "RunHeavyLaser|RailShift|StrongLaserFireInterval|railIndex" NPCs/Boss/TreeDiagrammer.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 强激光有轨道变化；编译通过。

---

### Task 10: 技能6 UltimateCannon 子状态

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`
- Modify: `NPCs/Servant/DiagrammerProbe.cs`
- Modify: `NPCs/Servant/DiagrammerCannon.cs`

**Boss Subskills:**

```text
Prepare：补齐基础探测器到3个
ProbeOverride：强制基础探测器进入妨碍形态
LifeReserve：扣除10%最大生命值，保存reservedLife
DeployCannon：召唤主炮并保存cannonWhoAmI
Active：最多900刻，Boss动作为StaticHold，主炮独立开火
Recover：回收主炮，恢复reservedLife，清空SP
End：回到Idle
DestroyedEnd：主炮被击败时禁用技能6并提前结束，不返还生命
```

**Step 1: Implement probe override**

实现 `ForceAllProbesToLaserMode()`：

```text
只处理基础探测器
将模式设置为Jammer
或设置强制妨碍标记
同步每个探测器
```

**Step 2: Implement cannon spawn**

实现 `SpawnMainCannon()`：

```text
NPC.NewNPC(..., ModContent.NPCType<DiagrammerCannon>(), ..., NPC.whoAmI)
cannonId = index
Main.npc[index].netUpdate = true
```

**Step 3: Implement cannon recycle**

实现 `RecycleMainCannon()`：

```text
如果主炮仍存活，设置其召回状态
或在第一阶段先主动 StrikeInstantKill/active=false
不要触发 NotifyCannonKilled 禁用终结技
```

如果使用主动消失，需要在 `DiagrammerCannon` 中区分“被击败”和“被回收”。

**Step 4: Implement life reserve**

`reservedLife = Math.Min(NPC.life - 1, NPC.lifeMax / 10)`；正常结束返还，主炮被击败不返还。

**Step 5: Verify**

Run:

```bash
rg -n "ForceAllProbesToLaserMode|SpawnMainCannon|RecycleMainCannon|reservedLife|cannonId|UltimateCannon" NPCs/Boss/TreeDiagrammer.cs NPCs/Servant/DiagrammerCannon.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 技能6不再因 `cannonId == -1` 立即失败；编译通过。

---

### Task 11: 主炮 NPC 子状态

**Files:**
- Modify: `NPCs/Servant/DiagrammerCannon.cs`

**Subskills:**

```text
Deploy：展开30刻，低速移动到玩家上方
Align：对齐玩家上方25格水平线
Fire：每30刻喷射树状图死亡光
Recall：回收到Boss附近并主动消失
Destroyed：被玩家击败，通知Boss禁用技能6
```

**Step 1: Add cannon enum**

```csharp
private enum CannonState : byte
{
    Deploy = 0,
    Align = 1,
    Fire = 2,
    Recall = 3,
    Destroyed = 4
}
```

`ai[1]` 存状态，`ai[2]` 存计时器，`ai[3]` 存图案种子。

**Step 2: Implement smooth height behavior**

主炮持续趋向：

```text
X = player.Center.X
Y = player.Center.Y - 25格
```

每 240 刻只提高修正强度，不做瞬移。

**Step 3: Implement death tree laser placeholder**

如果还没有专用树状死亡光弹幕，先使用多段 `DiagrammerElectrifiedLaser` 或可伤害电弧模拟：

```text
中心瞄准玩家
两侧分支向玩家左右偏移
```

后续可替换为 Luminance 绘制光束。

**Step 4: Distinguish recall from killed**

新增字段或状态，确保 `Recall` 消失不会调用 `NotifyCannonKilled()` 禁用终结技。

**Step 5: Verify**

Run:

```bash
rg -n "CannonState|SpawnDeathTreeLaser|Recall|NotifyCannonKilled" NPCs/Servant/DiagrammerCannon.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 主炮有完整生命周期；编译通过。

---

### Task 12: 技能7 ChaseProbeSeal 子状态

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`
- Modify: `NPCs/Servant/DiagrammerProbe.cs`

**Boss Subskills:**

```text
Prepare：确认没有活跃追杀组且冷却为0
SpawnPair：召唤两个追杀探测器
PairBind：写入互为伴侣关系和正负方向参数
Active：等待追杀组完成拉电弧或死亡
Recover：设置500刻冷却
End：回到Idle
```

**Probe Chase Subskills:**

```text
LockTarget：锁定 玩家位置 + 玩家速度 * 150 经+45/-45度旋转后的预测点
Chase：快速移动到锁定点
TelegraphArc：到位后和伴侣之间生成预警电弧
DamageArc：生成可伤害电弧
SwitchSuicide：进入自毁形态
```

**Step 1: Add ProbeMode.Chase**

```csharp
private enum ProbeMode : byte
{
    Guard = 0,
    Suicide = 1,
    Jammer = 2,
    Chase = 3
}
```

**Step 2: Separate probe kind**

约定：

```text
ai[3] = ProbeKind
0 基础探测器
1 追杀探测器
```

追杀探测器的子状态、伴侣索引、锁定点建议放在 ModNPC 字段并通过 `SendExtraAI` 同步。

**Step 3: Add Boss chase fields**

```csharp
private int chaseProbeCooldown;
private bool chaseProbeGroupActive;
private int chaseProbeA = -1;
private int chaseProbeB = -1;
```

**Step 4: Implement one active group limit**

Boss 选择技能7前必须满足：

```text
EndMode
DistancePunishActive
chaseProbeCooldown <= 0
!chaseProbeGroupActive
```

**Step 5: Verify**

Run:

```bash
rg -n "ChaseProbeSeal|ProbeMode.Chase|chaseProbeCooldown|chaseProbeGroupActive|pairedProbe" NPCs/Boss/TreeDiagrammer.cs NPCs/Servant/DiagrammerProbe.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 技能7不再使用 Boss 本体冲刺；追杀压力由两个探测器完成。

---

### Task 13: 技能8 LaserGrid 子状态

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`

**Subskills:**

```text
PrepareRound：生成本轮激光网参数
Telegraph：渲染判定线45刻
Active：释放激光网伤害线
Recover：间隔45刻
Repeat：重复3轮
End：回到Idle
```

**Step 1: Add laser grid fields**

```csharp
private int laserGridRound;
private Vector2[] laserGridStarts;
private Vector2[] laserGridEnds;
```

如果数组同步成本过高，使用 `patternSeed + round` 在服务端和客户端确定性生成；实际伤害生成只在服务端执行。

**Step 2: Implement pattern generation**

每轮至少生成：

```text
2条竖向线
2条横向线
1到2条斜向线
```

范围围绕玩家当前位置展开，但生成后不追踪玩家。

**Step 3: Implement telegraph and damage**

`Telegraph` 使用预警电弧或专用判定线；`Active` 使用可伤害线段。

**Step 4: Set movement**

技能8全程 `MoveState.StaticHold`。

**Step 5: Verify**

Run:

```bash
rg -n "RunLaserGrid|laserGridRound|SpawnLaserGridTelegraph|SpawnLaserGridDamage|StaticHold" NPCs/Boss/TreeDiagrammer.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 激光网三轮都有预警和伤害；编译通过。

---

### Task 14: 技能9 LightningOrbArc 子状态

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`
- Create or Modify: `Projectiles/NPCs/DiagrammerLightningOrb.cs`

**Subskills:**

```text
PrepareRound：确定本轮释放方向和弧线参数
Telegraph：短暂预警20到30刻
Active：释放本轮闪电珠弧
Recover：间隔45刻
Repeat：重复4轮
End：回到Idle
```

**Step 1: Decide projectile strategy**

优先查找现有闪电/电弧弹幕可复用；如果没有，创建 `DiagrammerLightningOrb`：

```text
hostile = true
tileCollide = false 或按设计开启
timeLeft = 180
沿弧线移动或按初速度+轻微转向
周期性连接短电弧视觉
```

**Step 2: Implement VerticalPress movement**

技能9使用 `MoveState.VerticalPress`：

```text
横向保持AnchorX
纵向维持玩家上方25到35格
每30到45刻更新一次纵向方向
```

**Step 3: Implement four rounds**

四轮方向可交替：

```text
左向右弧
右向左弧
内收弧
外扩弧
```

**Step 4: Verify**

Run:

```bash
rg -n "RunLightningOrbArc|DiagrammerLightningOrb|VerticalPress|lightningOrb" NPCs/Boss/TreeDiagrammer.cs Projectiles/NPCs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 闪电珠弧重复4轮；编译通过。

---

### Task 15: 技能10 RetreatSkill 子状态

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`
- Modify: `NPCs/Servant/DiagrammerProbe.cs`
- Modify: `NPCs/Servant/DiagrammerCannon.cs`

**Subskills:**

```text
Prepare：停止普通技能、清空攻击意图
RecallServants：通知主炮和探测器停止主动攻击
Rise：Boss持续向上离场
Despawn：超过高度或timeLeft归零后消失
```

**Step 1: Implement retreat entry**

`EnterRetreat()` 设置：

```text
Phase = Retreat
CurrentSkill = RetreatSkill
MoveState = RetreatRise
NPC.timeLeft = 300
```

**Step 2: Stop servants**

探测器和主炮在 Boss 进入退场后：

```text
基础探测器停止主动攻击并跟随/消失
追杀探测器停止生成新电弧
主炮进入Recall
```

**Step 3: Verify**

Run:

```bash
rg -n "RetreatSkill|EnterRetreat|RetreatRise|RecallServants" NPCs/Boss/TreeDiagrammer.cs NPCs/Servant
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 目标丢失时不会继续释放攻击技能；编译通过。

---

### Task 16: 普通技能池、SP和距离惩罚

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`

**Subskills:**

```text
PhaseOnePool：2、3、4、9，不能连续同技能
PhaseTwoPool：2、5、9，传奇&终焉额外加入8
SPGain：异变二阶段每帧+1，探测器死亡+400，技能6期间不增长
UltimateInsert：Idle且SP>=3000且未禁用时插入技能6
DistancePunish：终焉距离惩罚插入技能7
Cooldowns：技能5、8、7设置更长冷却
```

**Step 1: Add skill cooldowns**

```csharp
private readonly int[] skillCooldowns = new int[11];
```

服务端每帧递减，技能选择只在服务端执行。

**Step 2: Implement pool selection**

普通池严格按设计：

```text
第一阶段：2、3、4、9
第二阶段：2、5、9、8(传奇&终焉)
```

**Step 3: Implement selection priority**

```text
1. Retreat
2. Phase transition
3. Distance punish skill 7
4. SP ultimate skill 6
5. Current phase pool
```

**Step 4: Verify**

Run:

```bash
rg -n "SelectNextSkill|skillCooldowns|sp \\+\\+|NotifyProbeKilled|DistancePunish|UltimateCannon" NPCs/Boss/TreeDiagrammer.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 技能池和设计稿一致；特殊技能插入优先级稳定。

---

### Task 17: 终焉高度倍率

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`

**Subskills:**

```text
Measure：计算 Boss 高于玩家的高度 - 水平绝对差距
Clamp：每10像素+1%，最多+100%
LegendBonus：终焉且传奇时每10像素额外+1%
Apply：统一作用于Boss本体攻击、探测器攻击、主炮攻击
```

**Step 1: Add multiplier helper**

```csharp
private float GetEndModeAttackMultiplier(Player player)
```

非终焉返回 `1f`。

**Step 2: Apply to Boss projectile damage**

新增：

```csharp
private int ScaleBossDamage(Player player, float baseMultiplier = 1f)
```

所有 Boss 本体发射弹幕统一调用。

**Step 3: Expose servant multiplier**

探测器和主炮可以通过 Boss ModNPC 查询倍率，或由 Boss 在召唤时写入共享参数。优先直接查询 Boss，避免额外同步字段。

**Step 4: Verify**

Run:

```bash
rg -n "GetEndModeAttackMultiplier|ScaleBossDamage|EndMode" NPCs/Boss/TreeDiagrammer.cs NPCs/Servant
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 终焉倍率集中实现，不分散硬编码。

---

### Task 18: 多人同步和网络边界清理

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`
- Modify: `NPCs/Servant/DiagrammerProbe.cs`
- Modify: `NPCs/Servant/DiagrammerCannon.cs`

**Step 1: Audit server-only actions**

确认以下行为只在服务端执行：

```text
NPC.NewNPC
Projectile.NewProjectile
技能选择
SP增加
召唤物死亡通知
生命扣除和返还
```

**Step 2: Audit client visual-only actions**

客户端可以执行：

```text
PreDraw
Dust
纯视觉计时
非权威的本地光效
```

**Step 3: Sync required fields**

Boss 至少同步：

```text
phase
lastSkill
sp
moveState
moveAnchor
aimPoint
railIndex
laserGridRound
chaseProbeCooldown
chaseProbeGroupActive
cannonId
cannonUsable
reservedLife
patternSeed
```

探测器至少同步：

```text
mode
probeKind
pairedProbeWhoAmI
lockedTarget
hasCreatedArc
```

主炮至少同步：

```text
cannonState
cannonTimer
patternSeed
recalledByBoss
```

**Step 4: Verify**

Run:

```bash
rg -n "SendExtraAI|ReceiveExtraAI|NewProjectile|NewNPC|netUpdate" NPCs/Boss/TreeDiagrammer.cs NPCs/Servant/DiagrammerProbe.cs NPCs/Servant/DiagrammerCannon.cs
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 权威逻辑在服务端，表现同步字段完整。

---

### Task 19: 清理命名、注释和死代码

**Files:**
- Modify: `NPCs/Boss/TreeDiagrammer.cs`
- Modify: `NPCs/Servant/DiagrammerProbe.cs`
- Modify: `NPCs/Servant/DiagrammerCannon.cs`
- Modify if needed: `Localization/zh-Hans.hjson`
- Modify if needed: `Localization/en-US.hjson`

**Step 1: Remove obsolete names**

Run:

```bash
rg -n "PhaseCharge|DashArc|LaserNet|Finisher|EntryLaser|SummonProbes|StrongLaser" NPCs/Boss NPCs/Servant
```

Expected: 只允许在迁移注释或兼容说明中出现；最终实现代码中不应出现。

**Step 2: Chinese comments only**

新增注释使用中文，且只解释复杂状态和同步边界。

**Step 3: Remove unused placeholders**

删除空方法或确保已经有实际行为：

```text
ForceAllProbesToLaserMode
SpawnMainCannon
RecycleMainCannon
SpawnLaserGridTelegraph
SpawnLaserGridDamage
SpawnLightningOrbArc
SpawnDeathTreeLaser
```

**Step 4: Verify**

Run:

```bash
rg -n "\\{ \\}|TODO|PhaseCharge|DashArc|LaserNet|Finisher" NPCs/Boss NPCs/Servant
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: 无核心空壳方法；编译通过。

---

### Task 20: 最终验证

**Files:**
- Verify: whole project

**Step 1: Full build**

Run:

```bash
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

Expected: PASS with 0 errors.

**Step 2: Design coverage checklist**

逐项确认：

```text
[ ] 技能1 入场激光 5秒
[ ] 技能2 补齐三种基础探测器
[ ] 技能3 普通持续激光 15秒
[ ] 技能4 六点电弧封锁 + 两组压制线
[ ] 技能5 强连续激光 10秒
[ ] 技能6 终结技主炮 15秒、扣血、返还、禁用条件
[ ] 技能7 两个追杀探测器 + 预测点 + 电弧 + 自毁
[ ] 技能8 激光网 3轮
[ ] 技能9 闪电珠弧 4轮
[ ] 技能10 退场
[ ] 第一阶段技能池 2/3/4/9
[ ] 第二阶段技能池 2/5/9/8(传奇&终焉)
[ ] 异变 SP 机制
[ ] 终焉距离惩罚
[ ] 终焉高度倍率
[ ] 主炮死亡禁用技能6
[ ] 多人同步字段完整
```

**Step 3: Manual in-game smoke test**

在 tModLoader 中至少验证：

```text
普通难度召唤Boss
进入二阶段
异变二阶段SP终结技
终焉距离惩罚追杀探测器
主炮被击败后技能6禁用
目标死亡或远离后退场
```

Expected: 无崩溃、无无限召唤、无技能卡死、无 Boss 永久 Idle。

---

## 建议提交顺序

```text
1. refactor: align tree diagrammer phase and skill state names
2. refactor: split tree diagrammer movement states
3. feat: implement intro and base probe skills
4. feat: implement normal and heavy laser substates
5. feat: implement six point arc and laser grid skills
6. feat: implement ultimate cannon lifecycle
7. feat: implement chase probe seal behavior
8. feat: implement lightning orb arc skill
9. feat: implement retreat and difficulty inserts
10. chore: clean tree diagrammer placeholders and sync fields
```

## 实施风险

- 当前技能编号已经错位，必须先修正，否则后续技能选择和设计文档会持续对不上。
- 技能6依赖 Boss、探测器、主炮三方通信，应该单独提交，不要和激光网、闪电珠弧混在一起。
- 技能7追杀探测器不应复用基础探测器计数，否则技能2和防御加成会被污染。
- 多人同步需要在每个阶段持续检查，不能最后一次性补。
- 如果专用弹幕资源不足，可以先复用现有敌对电弧和带电激光完成玩法闭环，再单独迭代表现。

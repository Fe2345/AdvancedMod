# 毁灭者原版机制与等价状态机调查

日期：2026-10-28。状态：覆盖机制及毁灭者状态机已实现，单人与联机验收通过；以基本行为和绘制一致为目标，不承诺随机序列或浮点逐位一致。第 1—8 节为原版基线。

## 1. 版本与证据

以项目实际引用的 `D:/Program Files (x86)/Steam/steamapps/common/tModLoader/tModLoader.dll` 为基线：Terraria **1.4.4.9 / tModLoader 2026.08.3.0**，提交 `c9545cf27d56c456321e1896274c02adf67d0140`。同时反编译本机 `D:/Program Files (x86)/Steam/steamapps/common/Terraria/Terraria.exe`（**1.4.5.8**）作对照，不能把两个版本混作同一基线。

SHA256：

- tModLoader.dll：`FCC6A9624B12191BE4A15D714A3675440911FEB8C1D374642AB1FD686F0DA0A5`
- Terraria.exe：`960A03BFF6050CF7BE16DFC1A7B19E10FC2C4F8F835A6A3B135A50DD9E6BA2F3`

本机证据位于被忽略的 `obj/DestroyerResearch/`，不发布整份反编译源码。`NPC.tml.resolved.cs` 使用 ILSpy 10.0.1 并指定本机 FNA 引用目录生成；主 AI 已无缺失引用注释。其他大方法仍可能包含未解析的无关类型，不能将反编译文本直接当作可编译工程。

| 文件／入口 | 核对内容 |
| --- | --- |
| `NPC.tml.resolved.cs`，`AI_037_Destroyer`，44100 行 | 头／身体／尾的逐刻 AI，约 497 行 |
| 同文件，`GetDestroyerSegmentsCount`，45324 行 | 80／100 身体节数 |
| 同文件，`AI_005_EaterOfSouls`，44597 行 | 探针共用飞行 AI 与专属分支 |
| 同文件，`HitEffect` 中 87648 行附近 | 身体受击释放探针 |
| 同文件，`SetDefaults`、难度缩放、`StrikeNPC`、`checkDead`、`NPCLoot` | 属性、共享生命、死亡与战利品 |
| `NPCID.cs` | 原版免疫配置与 ID |
| `Projectile.cs` | DeathLaser／PinkLaser 默认值、AI、声音 |
| `Drops.cs`，`RegisterBoss_TheDestroyer` | Boss 与宝袋掉落规则 |
| `NPCLoader.cs`，`NPCAI` | PreAI／VanillaAI／AI／PostAI 调用顺序 |

`AI037.tml.resolved.txt` 与 `AI037.vanilla.txt` 的文本比较只有两处差异：机械美杜莎的链表遍历上限、撤退时的 NPC 清理上限，在本机本体版由 `200` 改成了 `Main.maxNPCs`。这只说明 **AI_037 方法** 的对比结果，不说明所有探针、伤害、生成或渲染代码跨版本相同。

外部交叉核对使用官方 [NPCAIStyleID](https://docs.tmodloader.net/docs/stable/class_n_p_c_a_i_style_i_d.html)、[NPC](https://docs.tmodloader.net/docs/stable/class_n_p_c.html)、[GlobalNPC](https://docs.tmodloader.net/docs/stable/class_global_n_p_c.html) 文档；以下数值与调用顺序以本机程序集为准。

## 2. 实体组成和属性

| 实体 | NPCID | aiStyle | 默认生命 | 默认接触伤害 | 防御 |
| --- | --- | --- | --- | --- | --- |
| 头 | TheDestroyer = 134 | 37 | 80000 | 70 | 0 |
| 身体 | TheDestroyerBody = 135 | 37 | 80000，共享头部生命 | 55 | 30 |
| 尾 | TheDestroyerTail = 136 | 37 | 80000，共享头部生命 | 40 | 35 |
| 探针 | Probe = 139 | 5 | 200，独立生命 | 50 | 20 |

以上是默认值分支中的基值，**不是所有难度的最终面板**。保留原版 SetDefaults 与缩放链：专家及以上的专属修正包括毁灭者生命乘 `0.75 × balance × bossAdjustment`，头部伤害再乘 2，身体／尾伤害再乘 0.85，尺寸比例乘 1.05；这些位于通用难度处理之后。探针生命、伤害也有自己的修正，旅行模式不能简单套专家布尔值。

头身尾均无重力、穿方块、不可击退、在方块后绘制、`netAlways=true`；初始 `alpha=255`，默认 scale=1.25。身体与尾 `dontCountMe=true`，只有头设置 boss。探针穿方块但可以被击退。免疫配置允许鞭类标签等例外，不能概括成“无条件免疫所有 Buff”。

普通世界是 **1 头 + 80 身体 + 1 尾 = 82 个 NPC**。FTW 的 `getGoodWorld` 分支是 1+100+1=102。`GetDestroyerSegmentsCount()` 返回身体数量，生成循环 `j <= count` 包含额外的尾，不能改成 `< count`。

## 3. 字段不是空闲状态槽

| 字段 | 毁灭者头身尾中的职责 |
| --- | --- |
| `ai[0]` | 后一节索引；头部为 0 时触发链生成 |
| `ai[1]` | 前一节索引；决定跟随谁 |
| `ai[2]` | 身体是否已经释放探针：0 未释放、1 已释放；参与帧与灯光选择 |
| `ai[3]` | 头部索引；AI 中大于 0 时回填 realLife |
| `realLife` | 共享生命主体索引；生成时直接设置，即使头索引为 0 也不能丢弃 |
| 头部 `localAI[0]` | 上次最终运动分支为钻行还是空中，用于触发同步 |
| 身体 `localAI[0]` | 激光随机累积计时 |
| `localAI[1]` | 地形扫描后的露出标记；不等同于最终运动分支 |

探针的同名数组是另一套含义：`ai[0]` 是摆动相位；组合 Boss 时 `ai[2]` 存头索引、`ai[3]` 存左右附着标记；`localAI[0]` 是射击计时。不要让两个类型共用状态字段解释。

## 4. 每刻真实执行顺序

`AI_037_Destroyer` 的关键顺序如下。这里列的是同一刻内的顺序，不是各持续一段时间的战斗阶段。

1. 机械美杜莎模式计算靠近头部的节序修正。
2. 恢复 realLife；必要时重新选敌。
3. 淡入与粒子：头部先淡入；后节等前节 alpha<128 才开始，每刻减 42，最低 0。
4. 身体／尾检查前节：`ai[1]<=0` 或前节 life<=0 时置 life=0、调用 HitEffect 和 checkDead。原位置没有统一提前 return。
5. 单机／服务端：头部首次生成整条链；身体更新并尝试发射激光。
6. 扫描方块／液体；更新露出灯光与 localAI[1]；头部执行离玩家太远时的追赶判定。
7. 判断白天或当前目标死亡，执行下沉及可能的清理。
8. 计算目标网格位置、速度参数；执行前节跟随或头部运动。
9. 旋转与头部 netUpdate 判定。
10. 若机械美杜莎仍存在，最后覆盖头部位置、速度与旋转。

首次生成、淡入都不是阻塞其他行为的阶段。把它们变成执行一次就 return 的 Spawn 状态，会延迟原本同刻执行的逻辑。

## 5. 头部运动：由环境每刻决定

### 5.1 地形与远距离追赶

扫描包围盒周围的 tile 范围并夹到世界边界。与 NPC 包围盒相交，且满足以下之一时进入可钻行条件：

- 激活的实心块；
- 激活的 solidTop 块且 frameY==0；
- 液体量大于 64。

不能用单个 `Collision.SolidCollision` 调用直接替换，它未必包含原来完全相同的平台和液体判断。

没有碰到上述地形时先写 `localAI[1]=1` 并按条件加光；头部如果位于当前目标下方，还会检查**所有 active 玩家**周围以玩家 position 为基准的 2000×2000 矩形。与任何矩形都不相交时，仍强制走钻行分支。这解释了“空中却主动追赶”的情形。这里原版没有替该遍历额外筛除 dead 玩家。

因此至少区分：原始地形结果、露出标记、远距修正后的运动条件、撤退覆盖后的运动条件。不能用同一个枚举反推全部结果。

### 5.2 钻行

- 目标速度参数 16；普通加速量 0.1，同向额外调整量 0.15；FTW 将后两者乘 1.2。
- NPC 中心与玩家中心先各自按 16 像素网格截断，再相减求目标向量。
- 朝目标方向计算期望速度后，按各轴符号与相对距离执行转向；两个轴都同向时先执行 0.15 调整，随后还可能执行 0.1 调整。
- 这两个判断是连续的 `if`，第二个还会读到第一个已经修改的速度。不能改成互斥的 `if/else if`。
- 大角度转向时按绝对距离较大的轴调整，并通过另一轴维持转弯；另有期望轴速度小于目标速度 20% 时的补偿。
- 挖掘声音延迟是 `clamp(distance/40, 10, 20)` 后转整数，受 soundDelay 控制。

没有单独的定时蓄力／冲刺／返回状态，也没有低生命阶段。玩家看到的拱出地面、俯冲、绕弯，是这些连续运动规则和地形切换产生的结果。16 也不是全程硬性向量模长上限。

### 5.3 空中

重新 TargetClosest，然后 Y 速度加 0.15，按本刻速度参数封顶。水平速度根据曼哈顿速度 `abs(vx)+abs(vy)` 是否低于速度参数的 40%、是否达到下落上限、Y 是否大于 4 分别加速／靠近目标／减速。

注意：用于部分水平调整的目标差值在这次 TargetClosest **之前**已经计算；提取函数时不要顺便重新计算，目标切换那一刻会产生差异。

旋转使用 `atan2(vy,vx)+1.57f`。等价移植时不要改成“更准确”的 PiOver2，后者会改变数值结果。

### 5.4 撤退

条件为 `Main.IsItDay()` 或当前目标 dead，发生在前面的选敌之后；多人有可选存活目标时不应直接解释为“一名玩家死了就撤退”。

- 强制运动条件为空中，Y 额外加 1。
- 低于 worldSurface 后再额外加 1，将速度参数从 16 改成 32。
- 低于 rockLayer 后遍历 NPC，把 **aiStyle 与自身相同**的 NPC 置为 inactive。不是只清理自己的 realLife 链，也不是遍历探针。
- 原版这一段没有统一 return，后续跟随／空中逻辑及组合 Boss 覆盖仍可能执行。

因此撤退是当刻的前置修正，不适合直接替换为永不返回的终止状态。修改清理范围属于修复／设计变更，应与等价重构分开。

## 6. 身体与尾：约束跟随，并行攻击

有效前节条件是 `ai[1]>0 && ai[1]<Main.npc.Length`。使用前节中心与自身中心的差向量 d，将间距约束到 `s=(int)(44f*scale)`：

`position += d * ((length(d)-s)/length(d))`，随后使用原逻辑的角度；velocity 被归零。这是直接修正 position，不是朝前节赋速度，也不是沿头部历史轨迹插值。

每节独立更新。把整条链集中到头部一次算完，会改变 NPC 槽位更新顺序、前节读取时刻、激光计时和随机数消耗顺序。不能凭最终曲线“看着像”认定等价。

### 身体激光

仅 type=135 且 `netMode!=MultiplayerClient` 执行，每节各自计时：

1. `localAI[0] += Main.rand.Next(4)`，即加 0—3。
2. **每刻重新抽** `Main.rand.Next(1400,26000)`，若累计值达到该阈值，立即清零。
3. 重新选敌，再检查 Collision.CanHit；被遮挡也已经消耗计时并清零。
4. 朝玩家中心加入 X/Y 各 -20—20 的位置扰动，归一到 8，再给速度各加 -1—1、步长 0.05 的扰动。
5. 从身体中心沿扰动后的速度前移 5 倍速度向量，创建 DeathLaser（100），调用 `GetAttackDamage_ForProjectiles(22,18)`，将 timeLeft 改为 300，置 netUpdate。

**这里没有 ai[2]==0 的发射门槛。** 探针已释放只改变身体帧和露出灯光等表现，不会停止这段发射逻辑。头部和尾没有此激光代码。

该算法不能改成“进入冷却时随机一个等待时长”；随机分布、发射时点和后续全局随机序列都会不同。

## 7. 探针：受击事件生成，独立飞行

### 7.1 释放

释放入口在原版 HitEffect，不在 AI_037：身体仍存活、非客户端、ai[2]==0，随机 1/25 成功时把 ai[2] 改为 1，并生成 Probe。机械美杜莎模式概率改为 1/50。生成后同步新 NPC，并令身体 netUpdate。

这是**每次符合条件的受击效果调用**的概率，不是每刻概率，也不是按生命百分比生成。正常链每节至多释放一次；没有 AI 内的补充机制。NPC 槽位耗尽时原代码也不是“生成失败就重新装填”。

状态表示可以是 `Loaded → Released`，但处理事件仍应留在原 HitEffect 路径。只替换 AI 时继续复用它，不再在自定义 OnHit／AI 里生成第二次。

### 7.2 飞行与射击

Probe 使用共享 AI_005，而不是头身尾的 AI_037：

- 普通期望速度 6，加速度 0.05；zenithWorld 将期望速度改为 3。
- 目标位置按 8 像素网格处理；距离大于 100 时用 ai[0] 相位施加 0.023 的摆动，循环跨越 -200 到 200。
- 每轴逐渐接近期望速度。距离大于 600 时还有 X 轴追赶修正：同向且绝对速度小于 12 时乘 1.05，否则反向时乘 0.9；组合模式的阈值为 5。
- localAI[0] 每刻加 1，justHit 时清零；普通阈值 120。非客户端达到阈值后先清零，再检查目标与视线，发射 PinkLaser（84），伤害参数 `GetAttackDamage_ForProjectiles(25,22)`。
- 普通发射向量使用之前计算的期望方向；不是“速度固定为 8”的身体激光公式。
- 有独立朝向、灯光、共享 AI 的碰撞响应；即使默认穿块，也不要擅自删除可能被其他逻辑影响的分支。
- Probe 死亡有 1/2 概率生成心；头部存在时它有特殊的保持活跃规则。毁灭者下沉清理 aiStyle=37 不会顺手清理 aiStyle=5 的探针。

### 7.3 弹幕仍应复用原版

两种激光均为 4×4、hostile、aiStyle=1、penetrate=3、extraUpdates=2。DeathLaser 默认 timeLeft=2700，但身体发射后覆盖为 300；PinkLaser 默认 600。两者 scale 分别为 1.8 与 1.2，首更新声音分别为 Item33 和 Item12。

`extraUpdates` 意味着不能把发射速度 8 解释为“每游戏刻只移动 8 像素”，也不能把 timeLeft=300 直接当作 5 秒；伤害参数还要经过原版敌对弹幕／玩家受击流程。状态机继续生成原版弹幕，不重写这些共享细节。

## 8. 机械美杜莎与其他外围机制

组合模式必须单独覆盖，否则“完整等价”不成立：

- 生成流程额外创建左右两个附着探针，ai[3] 为 -1/+1，ai[2] 为头索引。
- AI_037 先沿前节链计算最多 10 节的序号，用于前部 Y 偏移和间距压缩，保留原整数除法顺序。
- 头部在**普通 AI 处理结束之后**，按机械骷髅王中心、向下 100 偏移、其 vx×0.025 的旋转及其速度覆盖位置；速度归零，角度改为旋转量×0.75+π。
- 附着探针保持相对头部的位置并无敌，失去组合／有效头部后解除附着与无敌。射击计时每刻额外加 2，组合阈值 360；独立探针也受组合阈值影响。
- 组合激光向量使用 `目标中心-自身中心-目标速度×20`，归一到 8；这里是**减号**，不要改成常见的正向预判。
- 还有 ai[2]==0 探针与目标保持 120 距离的末尾修正。

共享生命由 realLife 和原版受击流程实现；不要用 OnHit 再手动扣头部血量。头部死亡掉落位置取离目标玩家中心曼哈顿距离最近的活跃毁灭者段位置，临时移动头部执行 NPCLoot，再恢复位置。此搜索按 type，非严格按所属链；“优化成只搜自己的链”也会改变多条毁灭者场景。

保留原版死亡事件、玩家参与记录、downedMechBoss1／downedMechBossAny、成就和世界同步。掉落规则包括普通神圣锭 15—30、力量之魂 25—40、面具 1/7、纪念章 1/10；专家宝袋的神圣锭为 20—35，并含机械车体等对应内容，大师另有圣物及宠物规则，组合击杀还有专属条件掉落。无需为了 AI 状态机复制掉落表。

生成入口属于物品使用和世界事件，不属于 AI_037。已读本机 `obj/LightningResearch.Main.cs` 的夜晚调度：满足困难模式、祭坛计数、事件／Boss 排除等条件后，通过 1/10 抽取安排尚未击败的机械 Boss；夜间 time>4860 再尝试在符合位置条件的活跃存活玩家处生成。不可把这个外层条件简写为“每晚毁灭者固定 10%”。本轮未逐条审计机械蠕虫物品使用、全部世界事件与玩家受伤实现；建议保持这些原版入口不动，而不是声称已重新实现整套游戏机制。

## 9. 已实现的覆盖结构

覆盖机制位于 Basic/Override/GlobalNPCBehavior.cs，仅定义类型匹配、每实体实例和 PreAI 分流，不包含 Hallow 或具体 Boss 内容。AppliesToEntity 限定类型，普通 NPC 不创建三王组件。

内容位于 Contents/Hallow/NPCs：HallowMechanicalBehavior 保存标记、初始化、父实体继承及 ExtraAI；DestroyerBehavior 仅匹配头身尾；TwinsBehavior 仅匹配两只眼睛；SkeletronPrimeBehavior 匹配头及四种手臂。后两者自定义行为按本次范围仍为空。

Hallow=false 时 PreAI 返回 true；true 时执行具体 BehaviorAI 后返回 false。空行为写在 BehaviorAI，PreAI 为 sealed 统一入口。原版类型、默认属性、HitEffect、FindFrame、死亡与掉落路径保留。其他 GlobalNPC.PreAI 和 PostAI 仍可能执行，不保证与其他 Boss 重做模组兼容。本需求不使用 IL Hook。

| 毁灭者状态 | 作用 |
| --- | --- |
| Burrowing | 地形内或远距追赶的转向加速 |
| Airborne | 下落与水平修正 |
| Retreating | 白天／目标死亡后的下潜和清理 |
| Following | 身体、尾部与前节保持间距 |
| Attached | 组合模式头部最后挂接机械美杜莎 |

状态按条件派生，不占用原 ai 槽位，也无需同步派生状态。公共处理保留选敌、淡入、链生成、环境判定、音效、光照和旋转；身体激光独立于运动状态。探针保留原版 AI 与受击释放，释放后的身体仍发射激光。

保留原版主要常量、条件及运算顺序；零距离、无效前节和实体槽位不足增加边界处理，允许槽位 0 作为头部。边界处理及激光向量归一化不承诺与原版异常输入逐位一致。

## 10. 召唤与同步

HallowMechanicalSummoning 只匹配机械蠕虫、机械魔眼、机械骷髅头。实际原版流程先通过 ApplyItemTime 调用 GlobalItem.UseItem，再执行生成或消息 61，因此可以记录本次神圣召唤意图，继续复用原版生成和消耗。

单人按玩家保存一次性位掩码，BossSpawn 来源在 OnSpawn 消费；双子分别消费自己的位。PostItemCheck 清理剩余标记。Hallow 表示召唤时选定的版本，离开神圣环境不会切换。

多人消息 128 先发送物品类型，后续仍走原版召唤消息；服务器核对玩家、夜晚、神圣环境、手持物品及 SummonItemCheck。意图最长保留 2 刻，头部在首次同步前初始化。原 ElementNetwork 编码保留。子节通过父来源继承，毁灭者创建子节后也在 SyncNPC 前显式设置。ExtraAI 用一个位同步，SetDefaults 重置，覆盖首次生成和晚加入。

本地真实联机已验证消息顺序，高延迟或人为拆分跨刻消息尚未专项验证。

## 11. 验收结果与范围

作者明确只要求基本行为和相关绘制一致，撤销此前随机调用序列、浮点逐位相同和严格全快照回放的验收建议。

- 构建：0 警告、0 错误。
- 第一阶段三王空行为：真实客户端 34/34，报告作为历史阶段证据保留。
- 最终毁灭者状态机：真实客户端 52/52，覆盖真实生物群系与物品使用、一次消耗、原版／自定义分流、链、淡入、激光、共享生命、探针释放和字段隔离。
- 8 组原版 VanillaAI／候选运动对照，各 90 刻，覆盖 FTW 开关、撤退及地形组合；最大位置差 5.390625 像素，最大旋转差 0.03553033 弧度。采用半格（8 像素）和 0.05 弧度容差，不是完整世界逐刻等价证明。
- 真实服务器＋召唤客户端＋晚加入客户端：最终 20/20，确认首刻标记、子节继承、消耗及空骷髅王不生成手臂。
- 两张真实客户端截图已检查，沿用原版贴图、节段外观、旋转、淡入和发光。测试平移完整链并补光便于观察，未替换绘制，不作逐像素比较。

实机种子为大师世界；FTW 数值开关只是运动内核对照，不代表完整特殊世界战斗。机械美杜莎完整战斗、其他难度和模组组合、OpenGL、高延迟压力未覆盖。详情见 [验收记录](testing/mechanical-behavior/README.md)。

## 12. 主流模组覆写方式补充调查（2026-10-28）

本节区分已读取源码和仅定位文件。GitHub API 成功取得目录与提交信息，但后续固定提交文件读取持续出现 TLS／EOF 错误；正文证据来自浏览工具可读取的分支快照，不声称它们全部与目录提交逐字一致。

### Fargo’s Souls：按实体挂行为，按阶段选择接管

已读 [EModeNPCBehaviour.cs](https://github.com/Fargo-Team/FargosSoulsMod/blob/master/Core/Globals/EModeNPCBehaviour.cs)：基类继承 GlobalNPC，InstancePerEntity=true，NPCMatcher 配合 AppliesToEntity 限定类型；sealed PreAI 处理首刻与启用逻辑，再调用 SafePreAI。

已读 [Destroyer.cs](https://github.com/Fargo-Team/FargosSoulsMod/blob/master/Content/Bosses/VanillaEternity/Destroyer.cs)：头部 SafePreAI 在第一阶段返回 true；第二阶段有有效目标时运行 CoilAI／NonCoilAI 并返回 false，失去有效战斗条件时另有放行分支。身体行为每刻清零 localAI[0]，以抑制原版激光，同时在仍放行的路径利用原版更新。新增阶段、计时和方向字段通过 SendExtraAI／ReceiveExtraAI 同步，关键变化设置 netUpdate。

这说明“附加规则”“阶段接管”“完整替换”可以按角色与阶段组合，不必复制整个 NPC，更不必统一重写所有 AI。

### Infernum：集中入口，按 NPCID 分派行为类

已读 [GlobalNPCOverrides.cs](https://github.com/InfernumTeam/InfernumMode/blob/master/Core/GlobalInstances/GlobalNPCOverrides.cs)：InstancePerEntity=true；PreAI 检查 CanUseCustomAIs 和该 NPC 的行为容器，随后调用 container.BehaviorOverride.PreAI(npc)，返回其结果。

已读 [NPCBehaviorOverride.cs](https://github.com/InfernumTeam/InfernumMode/blob/master/Core/OverridingSystem/NPCBehaviorOverride.cs)：启动时枚举派生类，根据 NPCOverrideType 登记到 BehaviorOverrideSet，并记录是否重写相应方法。它是适合大量 Boss 的注册／分派层，不代表每个 Boss 都必须采用同一种状态机。

目录确认毁灭者头、身、尾、探针分别存在行为覆写类。此次未能成功读取其具体战斗方法、OverrideSystemHooks 或完整同步实现，因此不把 Infernum 概括为“只靠普通 PreAI 就解决灾厄冲突”。已读的旧 HookManager.cs 整体处于块注释中，不能将它作为当前生效钩子证据。

### 灾厄：已定位新的专用覆写组件，调用链尚未读全

已读 [CalamityGlobalNPC.cs](https://github.com/CalamityTeam/CalamityModPublic/blob/1.4.4/NPCs/CalamityGlobalNPC.cs)，并取得仓库目录。当前树包含 `CalamityVanillaAIOverrideNPC.cs`、`CalamityVanillaAIOverrideNPC.Global.cs`、`VanillaAIOverride.cs`、`VanillaAIOverrideContext.cs` 和 `Bosses/DestroyerAI.cs`。前几个专用文件正文读取失败，故不根据旧教程推断它仍在 CalamityGlobalNPC.PreAI 内直接调用 DestroyerAI，也不推断使用了哪一种底层 Hook。

目录查询返回的提交：灾厄 `1a8cebd27ec5615316b78f71973446b5528d2b78`；Fargo `226fadeadbe3422785a7708ba2cdf53bd8548c00`；Infernum `399342f1d21dba29de5576f251ab95a02eaeb449`。原始目录在忽略目录 obj/AIOverrideResearch。

### 对本项目建议的修正

此前对精确替换 VanillaAI 的强调适用于“连其他模组 AI 钩子执行语义也必须保留”的严格目标；不应因此把 PreAI 说成只适合试验。

建议默认先评估官方 GlobalNPC.PreAI：只作用于头身尾，在自己的方法内执行等价 AI，返回 false；保留原版类型、其他生命周期和 Probe。实例数据确有需要才使用 InstancePerEntity，不额外占用原 ai 槽位。保持结果不变的声明应限定在已测试的模组组合；若必须保留现有 AI 钩子，再选择精确 Hook。现已按作者要求落实为第 9 节的官方 PreAI 接入。

验证状态：已核实上述可读取源码及本机 NPCLoader 的控制流；未构建或运行这些外部模组。灾厄完整入口、Infernum 底层冲突处理与同步链仍因网络读取失败未核实。

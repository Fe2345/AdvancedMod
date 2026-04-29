# 元素战斗子系统正式设计稿

## 1. 文档目标

本文档用于正式收束当前元素战斗子系统的工程设计，作为后续实现、联调、调试和扩展的统一依据。

本设计重点解决以下问题：

1. 将物品、弹幕、玩家、NPC 统一接入一个标准化战斗入口。
2. 在基础附着与反应系统之外，支持“元素附魔类效果”和“反应转化/变异类效果”。
3. 明确元素施加、元素消耗、反应伤害、DOT、资源产出和额外效果的职责边界。
4. 为后续“攒条类武器/装备”“被动计数器”“事件订阅类玩法”提供可观测性基础。

本文档默认约束如下：

- 附魔和反应变异效果不直接改写 NPC 内部状态。
- 附魔和反应变异效果不直接修改反应规则表本身。
- 所有外部效果最终都转成 plan，由核心系统统一执行写回。
- 整个系统必须具备较强的调试可视化和运行时监控能力。

---

## 2. 总体原则

### 2.1 单一核心入口

所有命中来源最终都必须转为统一的 `AttackPayload`，并通过 `ElementCombatSystem.ProcessHit(...)` 进入元素战斗主链。

允许的攻击来源包括但不限于：

- 物品直接命中
- 弹幕命中
- 召唤物命中
- NPC 伤害源
- 环境机关
- 派生效果

### 2.2 解释和执行分离

本系统明确区分：

- 解释层：决定“这次命中应该被如何理解”
- 计算层：决定“这次命中应该产生什么结果”
- 执行层：决定“这些结果如何真正写回游戏状态”

解释层不能直接写状态，执行层不能临时发明新规则。

### 2.3 Plan 驱动

核心系统中的大部分中间结果都必须表现为 plan，而不是在流程中途直接修改真实状态。

至少包括：

- `InputAugmentPlan`
- `ElementApplyPlan`
- `ElementConsumptionPlan`
- `ReactionTransformPlan`
- `ReactionResult`

### 2.4 可观测优先

这套系统必须从一开始就支持：

- 命中链路追踪
- 关键阶段记录
- 领域事件派发
- 调试 UI 读取

否则后续数值平衡、攒条玩法和复杂被动会非常难做。

---

## 3. 统一生命周期

一次攻击的完整生命周期建议固定为以下阶段：

1. 攻击源生成原始攻击意图 `AttackSourceIntent`
2. 输入解释层对攻击意图进行扩展和修正
3. 将解释结果收束为标准输入 `AttackPayload`
4. 构建元素施加计划 `ElementApplyPlan`
5. 基于临时状态视图收集和验证反应候选
6. 裁决主反应 `ReactionSelectionResult`
7. 反应解释层对主反应进行转化或追加效果
8. 构建元素消耗计划 `ElementConsumptionPlan`
9. 构建反应伤害和其他执行计划 `ReactionResult`
10. 由执行器统一提交元素变化、伤害、状态、资源和额外效果
11. 记录 trace，并向外发出稳定战斗事件

主链示意：

```text
AttackSourceIntent
-> AttackInputModifierPipeline
-> InputAugmentPlan
-> PayloadBuilder
-> AttackPayload
-> ElementApplyService.BuildApplyPlan()
-> TemporaryElementStateView
-> ReactionCandidateCollector
-> ReactionValidator
-> ReactionPriorityResolver
-> ReactionSelectionResult
-> ReactionOutcomeModifierPipeline
-> ReactionTransformPlan
-> ReactionConsumptionCalculator
-> ReactionDamageCalculator
-> ReactionResultBuilder
-> ReactionResult
-> ReactionExecutor
-> HitProcessResult
-> CombatTrace / CombatEvent
```

---

## 4. 分层设计

### 4.1 攻击源层

职责：

- 描述来源对象的原生攻击意图
- 不处理附魔
- 不读取或改写目标元素状态
- 不参与反应判断

核心对象：

- `AttackSourceIntent`
- `AttackSourceDescriptor`
- `AttackElementSeed`

说明：

这一层用于明确区分“武器原生效果”和“后续解释层追加效果”。

### 4.2 输入解释层

职责：

- 处理元素附魔
- 处理输入追加元素
- 处理输入标签修正
- 处理由装备或被动带来的输入侧资源产出标记

这一层不允许：

- 直接改写 `ElementComponent`
- 直接执行伤害
- 直接决定最终反应结果

核心对象：

- `AttackInputResolveContext`
- `IAttackInputModifier`
- `AttackInputBuilder`
- `InputAugmentPlan`
- `AttackInputModifierPipeline`

### 4.3 核心命中计算层

职责：

- 基于标准化 `AttackPayload` 进行附着、DOT、候选收集、候选验证和主反应裁决

核心对象：

- `ElementApplyService`
- `DotService`
- `TemporaryElementStateView`
- `ReactionCandidateCollector`
- `ReactionValidator`
- `ReactionPriorityResolver`
- `ReactionSelectionResult`

### 4.4 反应解释层

职责：

- 对已经选中的主反应进行转化
- 对主反应追加资源产出
- 对主反应追加额外效果
- 对主反应追加异常、生成物或额外伤害段

这一层不允许：

- 直接执行反应伤害
- 直接扣除真实元素槽
- 直接向 NPC 写入异常

核心对象：

- `ReactionTransformContext`
- `IReactionOutcomeModifier`
- `ReactionTransformBuilder`
- `ReactionTransformPlan`
- `ReactionOutcomeModifierPipeline`

### 4.5 数值计算层

职责：

- 计算元素施加量
- 计算元素消耗量
- 计算反应伤害
- 生成最终执行计划

核心对象：

- `ElementApplyPlan`
- `ReactionConsumptionCalculator`
- `ElementConsumptionPlan`
- `ReactionDamageCalculator`
- `ReactionDamageContext`
- `ReactionResultBuilder`

### 4.6 执行层

职责：

- 提交元素施加
- 提交元素消耗
- 施加 DOT 结果
- 执行伤害
- 执行异常
- 执行生成物
- 提交资源产出
- 记录最终执行结果

核心对象：

- `ReactionExecutor`
- `ResourceAwardService`
- `ExtraEffectExecutor`

执行层是唯一允许真正写回游戏状态的统一出口。

### 4.7 可观测性层

职责：

- 记录完整 trace
- 暴露稳定事件
- 支持攒条类玩法订阅
- 支持调试 UI 和日志

核心对象：

- `CombatTrace`
- `CombatTraceEntry`
- `CombatEvent`
- `DebugRecorder`
- `PlayerCombatTracker`

---

## 5. 核心对象设计

### 5.1 攻击源对象

#### `AttackSourceIntent`

表示攻击来源的原始意图，还未经过附魔或输入解释。

建议字段：

- `SourcePlayer`
- `SourceEntityId`
- `SourceKind`
- `DamageClass`
- `BaseDamage`
- `Knockback`
- `CanApplyElement`
- `CanTriggerReaction`
- `CanApplyDot`
- `Flags`
- `ElementSeeds`
- `Tags`

#### `AttackElementSeed`

表示原始元素种子。

建议字段：

- `Element`
- `BaseBuildUp`
- `BaseDotStrength`
- `CooldownGroup`
- `CooldownTicks`
- `Flags`

### 5.2 输入解释对象

#### `AttackInputResolveContext`

输入解释阶段的只读上下文。

建议字段：

- `Intent`
- `CombatContext`
- `SourcePlayer`
- `SourceEntity`
- `Target`
- `CurrentTick`

#### `IAttackInputModifier`

所有输入解释类效果的统一接口。

建议接口：

- `int Order { get; }`
- `bool IsActive(in AttackInputResolveContext context)`
- `void Modify(in AttackInputResolveContext context, AttackInputBuilder builder)`

#### `AttackInputBuilder`

输入解释阶段的可变 builder。

建议字段：

- `SourcePlayerId`
- `SourceEntityId`
- `SourceKind`
- `BaseDamage`
- `CanApplyElement`
- `CanTriggerReaction`
- `CanApplyDot`
- `Flags`
- `Applications`
- `Tags`
- `ResourcePlans`
- `DebugTraces`

建议方法：

- `AddApplication(...)`
- `ReplaceApplication(...)`
- `ScaleBuildUp(...)`
- `AddTag(...)`
- `RemoveTag(...)`
- `AddResourcePlan(...)`
- `DisableElementApply()`
- `DisableReaction()`

#### `InputAugmentPlan`

输入解释阶段最终产物，不可变。

建议字段：

- `FinalApplications`
- `FinalTags`
- `CanApplyElement`
- `CanTriggerReaction`
- `CanApplyDot`
- `ResourcePlans`
- `DebugTraces`

### 5.3 标准输入对象

#### `AttackPayload`

标准命中输入对象。

建议字段：

- `TraceId`
- `SourcePlayerId`
- `SourceEntityId`
- `SourceKind`
- `DamageClass`
- `BaseDamage`
- `CanApplyElement`
- `CanTriggerReaction`
- `CanApplyDot`
- `Flags`
- `Applications`
- `Tags`
- `ContextSnapshot`
- `InputAugmentPlan`

### 5.4 目标状态对象

#### `ElementState`

单元素槽运行时状态。

建议字段：

- `Element`
- `CurrentGauge`
- `MaxGauge`
- `DecayPerTick`
- `LastApplyTick`
- `LastDecayTick`
- `StateFlags`

#### `DotState`

元素附着 DOT 状态。

建议字段：

- `Element`
- `IsActive`
- `OwnerPlayerId`
- `TickInterval`
- `NextTick`
- `SnapshotPower`
- `RemainingTime`
- `RefreshPolicy`
- `Flags`

#### `ApplyCooldownState`

元素注入冷却状态。

建议字段：

- `SourceEntityId`
- `Element`
- `CooldownGroup`
- `NextAllowedTick`

#### `ElementComponent`

NPC 的元素状态容器。

建议字段：

- `ElementStates`
- `DotStates`
- `ApplyCooldownMap`
- `ReactionCooldownUntil`
- `LastReactionRecord`
- `DirtyFlags`
- `Version`

### 5.5 元素施加与临时状态对象

#### `ElementApplyPlan`

本次命中准备施加的元素计划。

建议字段：

- `Entries`

#### `ElementApplyEntry`

建议字段：

- `Element`
- `RequestedBuildUp`
- `FinalBuildUp`
- `RequestedDotStrength`
- `FinalDotStrength`
- `CanApply`
- `RejectReason`

#### `TemporaryElementStateView`

用于在不污染真实状态的前提下，让反应求解看到“当前状态 + 本次施加计划”的临时视图。

职责：

- 读取当前真实元素槽
- 叠加本次计划施加量
- 对候选收集和验证提供一致视图

### 5.6 反应选择和转化对象

#### `ReactionSelectionResult`

主反应裁决结果。

建议字段：

- `HasReaction`
- `Winner`
- `AllCandidates`
- `ValidCandidates`
- `RejectReason`

#### `ReactionTransformContext`

反应解释阶段只读上下文。

建议字段：

- `Payload`
- `CombatContext`
- `Target`
- `Component`
- `Selection`
- `CurrentTick`

#### `IReactionOutcomeModifier`

所有反应转化和附加效果的统一接口。

建议接口：

- `int Order { get; }`
- `bool IsActive(in ReactionTransformContext context)`
- `void Modify(in ReactionTransformContext context, ReactionTransformBuilder builder)`

#### `ReactionTransformBuilder`

反应解释阶段可变 builder。

建议字段：

- `SuppressReaction`
- `OverrideReaction`
- `DamageMultiplier`
- `ConsumptionOverrides`
- `ExtraStatuses`
- `ExtraSpawns`
- `ResourcePlans`
- `ExtraEffects`
- `DebugTraces`

建议方法：

- `ReplaceReaction(...)`
- `MultiplyDamage(...)`
- `Suppress()`
- `AddConsumptionOverride(...)`
- `AddStatus(...)`
- `AddSpawn(...)`
- `AddResourcePlan(...)`
- `AddExtraEffect(...)`

#### `ReactionTransformPlan`

反应解释阶段最终产物，不可变。

建议字段：

- `SuppressReaction`
- `OverrideReaction`
- `DamageMultiplier`
- `ConsumptionOverrides`
- `ExtraStatuses`
- `ExtraSpawns`
- `ResourcePlans`
- `ExtraEffects`
- `DebugTraces`

### 5.7 元素消耗与伤害对象

#### `ElementConsumptionPlan`

表示本次反应最终要扣除的元素计划。

建议字段：

- `Entries`

#### `ElementConsumptionEntry`

建议字段：

- `Element`
- `RequestedConsume`
- `FinalConsume`
- `Reason`

#### `ReactionDamageContext`

反应伤害计算输入。

建议字段：

- `Payload`
- `Selection`
- `TransformPlan`
- `ConsumptionPlan`
- `CombatContext`
- `Target`

#### `DamageInstancePlan`

反应伤害执行计划。

建议字段：

- `Damage`
- `DamageClass`
- `CanCrit`
- `Flags`
- `Reason`

### 5.8 最终结果对象

#### `ReactionResult`

反应执行前的统一结果对象。

建议字段：

- `Triggered`
- `Reaction`
- `TriggerElement`
- `TargetElement`
- `ConsumptionPlan`
- `DamagePlan`
- `StatusPlan`
- `SpawnPlan`
- `ResourcePlan`
- `ExtraEffectPlan`
- `TransformPlan`
- `DebugTraces`

#### `HitProcessResult`

命中处理总结果。

建议字段：

- `TraceId`
- `AppliedElements`
- `AppliedDots`
- `ReactionResult`
- `FinalStateChanges`
- `PublishedEvents`
- `Logs`

---

## 6. 核心服务设计

### 6.1 输入侧

#### `CombatContextBuilder`

职责：

- 从玩家当前装备、饰品、Buff、套装、被动聚合生成战斗上下文

#### `PayloadBuilder`

职责：

- 将 `AttackSourceIntent + InputAugmentPlan + CombatContextSnapshot` 收束成 `AttackPayload`

#### `AttackInputModifierPipeline`

职责：

- 顺序执行所有 `IAttackInputModifier`
- 聚合增量变化
- 生成 `InputAugmentPlan`

### 6.2 元素施加和 DOT

#### `ElementApplyService`

职责：

- 检查是否允许进入元素系统
- 计算本次命中的元素施加计划
- 不直接写回真实 `ElementComponent`

#### `DotService`

职责：

- 判断 DOT 是否可建立或刷新
- 在周期 tick 中执行 DOT

### 6.3 反应求解

#### `ReactionCandidateCollector`

职责：

- 根据临时元素状态和输入元素收集所有潜在反应候选

#### `ReactionValidator`

职责：

- 校验阈值、标签、上下文许可和冷却

#### `ReactionPriorityResolver`

职责：

- 在多个有效候选中选出唯一主反应

固定原则：

- 一次命中只允许一个主反应
- 裁决顺序必须稳定
- 不得依赖容器遍历顺序

### 6.4 反应转化

#### `ReactionOutcomeModifierPipeline`

职责：

- 顺序执行所有 `IReactionOutcomeModifier`
- 聚合反应转化、额外状态、额外生成物、资源产出和额外效果
- 生成 `ReactionTransformPlan`

### 6.5 数值计算

#### `ReactionConsumptionCalculator`

职责：

- 依据反应规则和反应转化结果生成 `ElementConsumptionPlan`

#### `ReactionDamageCalculator`

职责：

- 依据反应规则、上下文乘区、变异结果和元素消耗结果生成 `DamageInstancePlan`

#### `ReactionResultBuilder`

职责：

- 汇总主反应、变异结果、消耗计划、伤害计划和附加计划
- 生成最终 `ReactionResult`

### 6.6 执行

#### `ReactionExecutor`

职责：

- 将所有最终结果统一写回

写回内容包括：

- 元素施加
- 元素消耗
- DOT
- 伤害
- 异常
- 生成物
- 资源
- 调试记录

---

## 7. 元素施加与消耗规则

### 7.1 施加规则

元素施加不应在候选求解中途直接写入真实状态，而应先构建计划。

推荐公式：

```text
FinalApply =
BaseSeed
* InputAugmentMultiplier
* SourceBuildUpMultiplier
* ElementSpecificMultiplier
* TargetApplyTakenMultiplier
```

其中来源包括：

- 武器或弹幕原始元素种子
- 元素附魔追加或替换
- 玩家战斗上下文中的 build up 修正
- 目标对该元素的承受修正
- 元素附着冷却

### 7.2 消耗规则

元素消耗不应在执行器中临时推导，而应由专门计算器构建计划。

推荐公式：

```text
FinalConsume =
ReactionRuleBaseConsume
* ReactionConsumeMultiplier
* ReactionTransformConsumeMultiplier
```

最终再对可用元素量进行裁剪：

```text
Clamp(0, AvailableGauge)
```

### 7.3 反应规则中的消耗模式

`ReactionRule` 建议定义显式消耗模型，而不是在分支逻辑里硬编码。

建议枚举：

- `Fixed`
- `RatioOfCurrentGauge`
- `RatioOfAppliedAmount`
- `All`

建议字段：

- `TriggerConsumeMode`
- `TriggerConsumeValue`
- `TargetConsumeMode`
- `TargetConsumeValue`

---

## 8. 反应伤害计算规则

### 8.1 职责边界

反应伤害应由 `ReactionDamageCalculator` 负责计算，不能放在 `ReactionExecutor` 中临时处理。

职责划分：

- `ReactionRule` 定义基础伤害模型
- `ReactionTransformPlan` 定义变异和额外倍率
- `ReactionDamageCalculator` 计算数值
- `ReactionExecutor` 真正打伤害

### 8.2 `ReactionRule` 建议字段

建议补充：

- `DamageModel`
- `BaseDamageFactor`
- `UseSourceHitDamage`
- `CanCrit`

建议伤害模型枚举：

- `FlatByRule`
- `ScaleFromSourceDamage`
- `ScaleFromConsumedGauge`
- `Hybrid`

### 8.3 反应伤害上下文

伤害计算必须至少读取：

- 本次攻击基础伤害
- 本次选中的反应
- 反应变异结果
- 元素消耗计划
- 玩家当前反应乘区
- 目标当前承伤修正

---

## 9. 乘区和数值顺序设计

### 9.1 统一原则

所有装备、饰品、Buff、套装和被动提供的数值修正，不应让每个效果自己决定“在哪里乘”。

应统一收束到 `CombatContextSnapshot` 的固定乘区桶中。

### 9.2 推荐乘区结构

建议建立 `ReactionDamageModifierSet`。

建议字段：

- `ElementVulnerabilityMult` （元素易伤）
- `ReactionTypeVulnerabilityMult` （反应易伤）
- `ElementResistanceMult` （元素减抗区，=(1-元素抗性)，数值上无限制，1时为元素免疫，负数时可以提高伤害）
- `ReactionResistanceMult` （反应减抗区，和上述类似，主要为让NPC对指定反应伤害降低设计）
- `ElementMult` （元素增伤）
- `ReactionTypeMult` （反应增伤）
- `BasicMult` （反应固有倍率）
- `MasteryMult` （元素精通乘区，=1+6*元素精通/(元素精通+2000) ）
- `ExtraMult` （保留字段，正常弃用，为极为特殊的装备/NPC准备）

### 9.3 推荐计算顺序

上述乘区全部独立，但是同类装备的加成先加算后乘算，不能两个反应增伤直接1.04*1.04；

“全职业增伤”“全元素增伤”之类的都应该处理为 给每一个元素都加一个增伤，“全元素增伤”和“火元素增伤”并不是独立的乘区；

减抗直接作用于原始伤害，收益最高；其他的统一最后再乘算。

### 9.4 同桶与跨桶规则

- 同一乘区桶内先加算
- 不同乘区桶之间再乘算

这样可避免同类词条指数爆炸，同时保持不同语义词条的独立性。

### 9.5 建议额外快照

建议在 `CombatContextSnapshot` 内增加 `ReactionStatSnapshot`。

建议字段：

- `ByReaction`
- `BuildUpAdd`
- `BuildUpMult`
- `ConsumeMult`

用于分离：

- 元素附着修正
- 反应消耗修正
- 反应伤害修正

---

## 10. 可观测性设计

### 10.1 总体原则

可观测性应拆为两层：

1. 调试追踪层：用于开发和排错
2. 战斗事件层：用于玩法订阅和攒条逻辑

两层不能混用。

### 10.2 调试追踪层

#### `CombatTrace`

记录一次命中的完整流水。

建议字段：

- `TraceId`
- `Payload`
- `InputAugmentPlan`
- `ElementApplyPlan`
- `SelectionResult`
- `TransformPlan`
- `ReactionResult`
- `Entries`

#### `CombatTraceEntry`

建议字段：

- `Stage`
- `Source`
- `Message`

### 10.3 领域事件层

建议定义抽象基类 `CombatEvent`，并使用稳定的高层语义事件，而不是直接暴露内部临时对象。

建议通用字段：

- `TraceId`
- `SourcePlayerId`
- `TargetNpcId`
- `Tick`

建议最少支持以下事件：

- `ElementAppliedEvent`
- `ReactionTriggeredEvent`
- `ReactionTransformedEvent`
- `ReactionDamageDealtEvent`
- `ResourceGrantedEvent`

### 10.4 攒条和计数系统

后续攒条类武器、饰品和任务系统，不应直接观察求解器内部候选列表或状态容器。

应统一订阅稳定领域事件。

推荐对象：

- `PlayerCombatTracker`
- `ICombatEventListener`

推荐用法：

- 攒条按反应次数计数
- 攒条按元素施加量计数
- 攒条按反应伤害累计
- 攒条按特定变异反应累计

### 10.5 TraceId

每次命中应生成一个贯穿全链路的 `TraceId`，并写入：

- `AttackPayload`
- `InputAugmentPlan`
- `ReactionResult`
- `CombatEvent`
- `HitProcessResult`

这样才能将一次命中的输入、施加、反应、伤害、资源和调试信息串起来。

---

## 11. 适配层建议

### `PlayerElementAdapter`

建议落点：`ModPlayer`

职责：

- 缓存当前 `CombatContextSnapshot`
- 处理玩家侧战斗事件
- 为攒条、资源和被动计数提供入口

### `ItemElementAdapter`

建议落点：`GlobalItem` 或武器基类

职责：

- 生成 `AttackSourceIntent`
- 在命中或发射弹幕时传递原始元素种子

### `ProjectileElementAdapter`

建议落点：`GlobalProjectile`

职责：

- 保存来源输入
- 在命中时构建命中意图
- 读取系统返回结果做表现

### `NpcElementAdapter`

建议落点：`GlobalNPC`

职责：

- 持有 `ElementComponent`
- 驱动周期 tick
- 提供同步入口

---

## 12. 最小实现顺序

建议严格按以下顺序推进，而不是一开始全做完。

### 第一阶段：静态结构与状态容器

- `ElementType`
- `ReactionType`
- `ReactionRule`
- `ElementComponent`
- `ElementState`
- `DotState`
- `ApplyCooldownState`

### 第二阶段：输入主链

- `AttackSourceIntent`
- `AttackInputResolveContext`
- `IAttackInputModifier`
- `AttackInputBuilder`
- `InputAugmentPlan`
- `AttackInputModifierPipeline`
- `PayloadBuilder`
- `AttackPayload`

### 第三阶段：施加、DOT、临时状态视图

- `ElementApplyPlan`
- `ElementApplyService`
- `TemporaryElementStateView`
- `DotService`

### 第四阶段：反应求解与裁决

- `ReactionCandidateCollector`
- `ReactionValidator`
- `ReactionPriorityResolver`
- `ReactionSelectionResult`

### 第五阶段：反应转化与结果构建

- `ReactionTransformContext`
- `IReactionOutcomeModifier`
- `ReactionTransformBuilder`
- `ReactionTransformPlan`
- `ReactionOutcomeModifierPipeline`
- `ReactionConsumptionCalculator`
- `ReactionDamageCalculator`
- `ReactionResultBuilder`
- `ReactionResult`

### 第六阶段：执行与可观测性

- `ReactionExecutor`
- `CombatTrace`
- `CombatEvent`
- `DebugRecorder`
- `PlayerCombatTracker`
- `HitProcessResult`

### 第七阶段：适配层和实际接入

- `PlayerElementAdapter`
- `ItemElementAdapter`
- `ProjectileElementAdapter`
- `NpcElementAdapter`

---

## 13. 当前定稿结论

本设计最终采用以下核心方向：

1. 使用统一命中入口，而不是让武器、弹幕和 NPC 分散处理反应。
2. 使用两层解释管线：
   - 输入解释层，负责元素附魔和输入扩展
   - 反应解释层，负责反应转化、变异和额外效果
3. 使用 plan 驱动中间结果，而不是中途直接写状态。
4. 使用临时状态视图参与求解，避免中间状态污染和回滚复杂度。
5. 将元素施加、元素消耗、反应伤害和执行彻底拆开。
6. 使用固定乘区桶管理各类装备、饰品和 Buff 提供的数值修正。
7. 使用 trace 和稳定事件双层可观测性，为调试和攒条类玩法提供基础。

这份文档作为当前元素战斗子系统的正式设计基线，后续实现如需调整，应优先保证：

- 生命周期顺序稳定
- plan 与执行分离
- 单一写回出口
- 对外事件语义稳定


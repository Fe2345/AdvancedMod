# 超载与正式反应主链设计稿

## 1. 目标

本文档用于确定元素系统从“雷风原型直写实现”向“正式反应主链”过渡时的第一批落地范围。

本轮目标不是一次性补完整个元素中台，而是在不破坏现有雷风原型可玩性的前提下，先搭出一条正式、可扩展、可继续增量实现的最小主链，并在此基础上落地第一个新反应 `超载` 和第一把新元素武器 `可燃物发射器`。

本轮需要解决的问题：

1. 把当前写死在 `ElementCombatSystem` 中的反应判定与执行逻辑拆分为稳定职责层。
2. 让 `雷风` 与 `超载` 都走同一条正式反应链路。
3. 统一反应文字显示入口，去掉业务流程中写死的 `"TW"`。
4. 为后续继续补 `感电`、`蒸发`、更多元素武器和装备效果建立固定扩展点。

---

## 2. 本轮范围

### 2.1 本轮实现内容

- 新元素：`Pyro`
- 新反应：`Overloaded`
- 新武器：`可燃物发射器`
- 正式反应主链骨架：
  - `ReactionRuleRegistry`
  - `ElementApplyPlan`
  - `TemporaryElementStateView`
  - `ReactionCandidateCollector`
  - `ReactionValidator`
  - `ReactionPriorityResolver`
  - `ReactionExecutor`
- 统一反应文字显示入口
- 兼容并迁移现有 `ThunderWind` 原型链路

### 2.2 本轮明确不做

- `感电`
- `蒸发`
- DOT 正式接入反应主链
- 完整 trace / event / 调试面板体系
- 资源产出、生成物、复杂异常状态

---

## 3. 正式主链

本轮命中主链固定为：

```text
武器/弹幕命中
-> AttackSourceIntent
-> AttackInputModifierPipeline
-> AttackPayload
-> ElementApplyPlan
-> TemporaryElementStateView
-> ReactionCandidateCollector
-> ReactionValidator
-> ReactionPriorityResolver
-> ReactionResult
-> ReactionExecutor
```

### 3.1 各层职责

#### `ElementCombatSystem`

- 负责串联主流程
- 负责从 `AttackSourceIntent` 驱动到 `ReactionExecutor`
- 不再直接硬编码具体反应判定与执行细节

#### `PayloadBuilder`

- 负责把输入解释层结果收束为标准 `AttackPayload`
- 只回答“这次攻击最终携带了什么”

#### `ElementApplyPlan`

- 负责描述本次命中准备施加哪些元素、各自多少附着量
- 本身不直接写回真实状态

#### `TemporaryElementStateView`

- 基于“目标当前真实元素状态 + 本次附着计划”生成投影视图
- 后续反应判定、验证、优先级选择统一读取这里
- 避免边写边算导致的流程耦合

#### `ReactionCandidateCollector`

- 根据 `AttackPayload` 和规则表收集候选反应
- 只负责“可能有哪些”
- 不负责判断是否满足消耗条件

#### `ReactionValidator`

- 校验候选是否成立
- 负责读取规则消耗要求并判断当前投影视图是否满足
- 产出稳定的消耗结果

#### `ReactionPriorityResolver`

- 从多个合法候选里选出主反应
- 本轮即使候选数量少，也先把这一层立住

#### `ReactionExecutor`

- 作为唯一真实写回出口
- 负责元素附着、元素消耗、反应伤害、额外伤害、击退和文字显示

---

## 4. 本轮对象设计

### 4.1 `ReactionRuleRegistry`

集中登记反应规则。

本轮至少提供：

- `ThunderWind`
- `Overloaded`

规则字段至少包括：

- `Reaction`
- `TriggerElement`
- `TargetElement`
- `TriggerConsumeMode`
- `TriggerConsumeValue`
- `TargetConsumeMode`
- `TargetConsumeValue`
- `DamageModel`
- `BaseDamageFactor`
- `DamageElement`
- `Priority`
- `IgnoreOrder`

### 4.2 `ElementApplyPlan`

表示这次命中准备施加的元素计划。

本轮最小结构：

- `ElementApplyPlan`
  - `Entries`
- `ElementApplyEntry`
  - `Element`
  - `AppliedBuildUp`
  - `SourceApplication`

### 4.3 `TemporaryElementStateView`

本轮至少提供：

- `GetCurrentGauge(element)`
- `GetProjectedGauge(element)`

其中：

- `CurrentGauge` 为目标当前真实值
- `ProjectedGauge` 为真实值加本次施加后的投影值

### 4.4 `ReactionCandidate`

表示一个可能成立的反应候选。

本轮建议字段：

- `Rule`
- `TriggerElement`
- `TargetElement`
- `SourceApplication`

### 4.5 `ReactionValidationResult`

表示候选校验结果。

本轮建议字段：

- `IsValid`
- `Rule`
- `TriggerConsumeAmount`
- `TargetConsumeAmount`

### 4.6 `ReactionTextResolver`

统一反应显示文本解析入口。

本轮建议接口：

```csharp
string GetDisplayText(ReactionType reaction)
```

本轮最少支持：

- `ThunderWind -> 雷风`
- `Overloaded -> 超载`

---

## 5. 行为规则

### 5.1 元素

本轮运行时元素：

- `Thunder`
- `Wind`
- `Pyro`

`Hydro` 与 `Dendro` 本轮可以只补枚举占位，不进入运行时反应链。

### 5.2 反应

#### `ThunderWind`

- 保留现有原型反应
- 消耗：`雷(1) + 风(1)`
- 伤害：原始伤害 `x1.5`
- 显示文字：`雷风`

#### `Overloaded`

- 新增正式反应
- 条件：`火(4) + 雷(2)`
- 输出元素：视为雷元素反应伤害
- 伤害：原始伤害 `x2.75`
- 显示文字：`超载`
- 额外效果：中等击退

### 5.3 消耗语义

文档中的 `(n)` 在本轮统一解释为：

- 武器/弹幕输入时：本次附着量
- 反应规则中：该侧消耗要求

执行顺序固定为：

1. 构建 `ElementApplyPlan`
2. 基于投影视图收集并校验反应
3. 由 `ReactionExecutor` 先提交附着
4. 再提交反应消耗
5. 最后执行伤害、击退、文字显示

这样 `Pyro(5)` 命中带有 `Thunder(2)` 的目标时，可以稳定触发 `Overloaded`。

### 5.4 可燃物发射器

本轮设计参数：

- 类型：发射器
- 元素输入：`Pyro(5)`
- 基础伤害：`20`
- 攻击速度：`30`
- 消耗凝胶
- 发射不可穿透物块的弹幕

武器自身不负责任何超载判定逻辑，只负责提供 `Pyro(5)` 输入。

### 5.5 文字显示

反应文字显示规则固定为：

- 业务流程中不允许硬编码具体显示文本
- 统一由 `ReactionTextResolver` 按 `ReactionType` 解析
- `ReactionExecutor` 负责写入：
  - `ElementComponent.LastReactionText`
  - `CombatText.NewText(...)`

### 5.6 击退语义

本轮只做超载击退的最小版：

- 由 `ReactionExecutor` 统一执行
- 不做随机方向
- 使用简化方向或水平击退即可

---

## 6. 兼容要求

现有原型内容必须继续可用：

- `PrototypeThunderSword`
- `PrototypeWindSword`
- `PrototypeThunderStaff`
- `PrototypeWindEnchantAccessory`
- `PrototypeReactionWindBurstAccessory`

它们不再走旧的反应硬编码分支，而是统一迁移到正式主链。

---

## 7. 本轮完成标准

当以下条件全部成立时，本轮视为完成：

1. `Pyro` 与 `Overloaded` 已接入正式反应主链。
2. `ThunderWind` 已迁移到新链路并保持可用。
3. `可燃物发射器` 命中可正确输入 `Pyro(5)`。
4. `超载` 能按 `火(4) + 雷(2)` 规则触发。
5. 反应文字显示统一走 `ReactionTextResolver`，不再存在 `"TW"` 之类的业务硬编码。
6. 现有原型武器与饰品没有被破坏。

# 元素系统命中链 TML 收口重构设计

## 目标

将当前元素系统从“原始命中链和平行元素数值链并存”的状态，重构为“**原始命中完全依赖 TML API，元素系统只负责原生没有的语义**”的稳定架构。

第一阶段的范围固定为：

- 收口原始武器/弹幕命中的伤害、暴击、减伤和命中前倍率修饰
- 保留元素附着、反应判定、元素精通、协同攻击、剧变反应派生伤害
- 暂不让剧变反应和协同攻击参与暴击

## 核心原则

### 1. 尽量复用 TML API

原版已有的统计与结算能力，必须优先交给 TML：

- `DamageClass`
- `player.GetDamage(...)`
- `player.GetCritChance(...)`
- `player.endurance`
- `NPC.HitModifiers`
- `NPC.HitInfo`

元素系统不应再复制一套平行的暴击、全局增伤、全局减伤聚合器。

### 2. 原始命中与元素后处理分层

原始命中链只允许在命中前改写 `HitModifiers`。

元素系统的 on-hit 阶段只负责：

- 附着
- 反应判定
- 元素量消耗
- 反应文字
- 剧变反应派生伤害
- 协同攻击

### 3. snapshot 只保存元素专属语义

`CombatContextSnapshot` 和 `ElementCombatPlayerState` 不再承担“第二套 player 数值系统”的职责。

它们只保留 TML 无法直接表达的元素专属数据，例如：

- 元素精通
- 分元素额外倍率
- 反应专属倍率
- 附着/消耗修正
- 协同攻击配置
- 规则 override / permission

## 当前问题总结

### 1. 原始命中重复实现暴击

当前系统关闭了 `ElementDamage` 的标准暴击，再用 `RollCrit()` 手动掷骰，并通过 `modifiers.SetCrit()` 和 `SimpleStrikeNPC(..., crit, ...)` 重做了一条平行暴击链。

这导致：

- 原始命中不再遵循 TML 的标准暴击流程
- 反应伤害又重复实现了一套独立暴击逻辑
- `player`、`snapshot`、`HitModifiers` 三套职责重叠

### 2. 原始伤害聚合重复实现

当前 `ElementCombatPlayerState` 既遍历装备，又从 `player.GetTotalDamage(...)` 回读，再把这些值塞回 `ReactionStatSnapshot`。

这导致：

- `player` 已经生效的数值又被 snapshot 重新解释
- 原生增伤与元素增伤的边界不清
- 后续非常容易发生重复计数

## 重构后的职责边界

### 原始命中层

职责：

- 基础伤害
- 职业增伤
- 元素职业增伤
- 原生暴击率
- 原生暴击伤害
- 原生减伤
- 原生护甲穿透
- 原生防御结算

实现方式：

- 装备在 `UpdateEquip / UpdateAccessory / UpdateArmorSet` 中直接改 `player`
- 命中前通过 `ModifyHitNPC / ModifyHitByProjectile` 改 `NPC.HitModifiers`

### 元素语义层

职责：

- 元素附着
- 反应候选收集
- 反应优先级
- 元素精通
- 分元素额外倍率
- 反应专属倍率
- 协同攻击配置
- 剧变反应派生伤害

实现方式：

- 通过 `AdvancedPlayer.ElementCombatState` 和 `CombatContextSnapshot` 传递 element-only 数据

## 第一阶段数据流

```text
装备/饰品 UpdateEquip / UpdateAccessory / UpdateArmorSet
-> 原生字段直接写入 player
-> 元素专属字段写入 AdvancedPlayer.ElementCombatState

命中前 ModifyHitNPC / ModifyHitByProjectile
-> 构造最小 AttackSourceIntent
-> 构造 payload
-> 读取 player 当前原生统计
-> 读取 element-only snapshot
-> 改写 NPC.HitModifiers

命中后 OnHitNPC
-> 复用同一套 payload 解析
-> 元素附着
-> 反应判定
-> 增幅反应只做显示/消耗
-> 剧变反应派生执行
-> 协同攻击分发
```

## 关键对象调整

### `ElementDamage`

- 恢复标准暴击
- 删除空操作 `SetDefaultStats`
- 不再关闭 `UseStandardCritCalcs`

### `ElementCombatPlayerState`

保留：

- `ElementMastery`
- `ElementDamageBonusByElement`
- `ReactionModifierByType`
- `CoordinatedAttacks`
- `BuildUp / Consume` 修正

删除：

- `Crit`
- `Defense`
- 原生伤害/暴击/减伤回读逻辑

### `ElementOffenseStatSet`

只保留 element-only 字段：

- `ElementMastery`
- `ElementDamageBonus`

删除：

- `GlobalElementDamageBonus`
- `GlobalClassDamageBonus`
- `SetGlobalClassDamageBonus(...)`

### `ElementCritStatSet`

第一阶段整体下线，不再作为原始命中的平行暴击容器使用。

### `ElementDefenseStatSet`

第一阶段不再参与 snapshot 的原始减伤解释。若后续没有元素专属承伤机制，可继续收缩。

### `ReactionDamageCalculator`

第一阶段只处理：

- 增幅反应倍率
- 剧变反应派生伤害
- 元素精通
- 反应专属修正

不再处理：

- 原始命中的暴击率掷骰
- 原始暴击伤害解释

### `ElementCombatSystem`

保留双入口：

- pre-hit：`ModifyOutgoingHit(...)`
- on-hit：`ProcessHit(...)`

但语义改为：

- `ModifyOutgoingHit(...)` 只改 `HitModifiers`
- `ProcessHit(...)` 只做元素后处理

## 第一阶段明确不做

- 剧变反应暴击
- 协同攻击暴击
- 正式 `DerivedAttack` 层
- 感电 / 蒸发内容接入
- 完整反应专属暴伤体系

## 验证目标

### 代码结构验证

- 原始命中链不再出现手搓 `RollCrit`
- `ElementCombatPlayerState` 不再从 `player` 回读原始伤害与暴击
- `ModifyOutgoingHit(...)` 只改 `HitModifiers`

### 编译验证

使用：

```powershell
powershell.exe -NoLogo -NoProfile -Command "dotnet build AdvancedMod.sln -c Debug /clp:ErrorsOnly"
```

### 实机验证

- `可燃物发射器 + 高级火元素瓶`
  - 本体伤害被正确提升
  - 协同弹正常触发
- `熔骨护腿`
  - 元素武器暴击仍由 TML 正常出现
- `超载`
  - 仍能触发
  - 剧变派生伤害独立存在，但第一阶段不暴击

## 结论

第一阶段重构完成后，系统应达到以下状态：

- 原始元素命中：完全遵循 TML 统计与命中前修饰
- snapshot：只保存元素专属上下文
- 增幅反应：命中前改写原始伤害
- 剧变反应：命中后派生执行，但先不暴击
- 装备接入：原生字段和元素字段职责彻底分离

## 第一阶段落地记录

- `ElementDamage` 已恢复 TML 原生暴击流程，不再关闭标准暴击，也不再隐藏暴击 tooltip。
- 原始命中的 `ModifyOutgoingHit(...)` 只通过 `NPC.HitModifiers.SourceDamage` 应用元素专属倍率和增幅反应倍率，不再手动 `SetCrit()` 或改写 `CritDamage`。
- `ElementCombatPlayerState` 已停止从 `player.GetTotalDamage(...)`、`player.GetCritChance(...)`、`player.endurance` 回读原生字段。
- `ReactionStatSnapshot` 和 `ReactionDamageCalculator` 已移除阶段一反应伤害暴击逻辑；剧变、原型和追加效果伤害暂定为非暴击。
- 装备接入边界明确为：原生支持字段只写 `player`，元素专属字段只写 `ElementCombatPlayerState`。

## 第二阶段暂缓项

- 剧变反应暴击与反应暴伤体系暂缓，后续如果设计需要，应作为反应异化或套装效果接入，而不是恢复平行原始暴击管线。
- `DerivedAttack` 层暂缓，后续用于统一承载协同攻击、追加段数、反应异化伤害等派生命中。
- 反应专属倍率、附着倍率、消耗倍率目前保留结构入口，但实际装备内容仍需后续逐项补充。

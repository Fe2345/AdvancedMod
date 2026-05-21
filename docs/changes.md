# 变更记录

## 2026-05-11 元素系统数值与派生伤害重构

### 目标

本轮重构的目标是将元素系统从“原始命中和平行元素数值链并存”收口到“原始命中尽量复用 tModLoader 原生 API，元素系统只负责原生没有的元素语义”。

核心原则：

- 原生已支持的职业增伤、暴击、暴伤、护甲穿透、目标防御、减伤等流程，优先交给 TML。
- 元素系统只维护元素附着、反应判定、元素精通、分元素倍率、反应倍率、协同攻击和派生追加伤害。
- 所有装备数值贡献必须分清边界：原生字段写 `Player`，元素专属字段写 `ElementCombatPlayerState`。

### 已完成改动

- `ElementDamage` 恢复 TML 原生暴击流程，不再关闭 `UseStandardCritCalcs`，不再隐藏暴击 tooltip。
- `ElementCombatSystem.ModifyOutgoingHit(...)` 只通过 `NPC.HitModifiers.SourceDamage` 修饰原始命中。
- 增幅反应现在修饰原始命中本身，不再伪装成“原始伤害 + 额外伤害段”。
- 原始命中链移除了手动 `RollCrit`、`SetCrit()` 和自定义暴击伤害桶。
- `ElementCombatPlayerState` 不再从 `player.GetTotalDamage(...)`、`player.GetCritChance(...)`、`player.endurance` 回读原生字段。
- `ElementOffenseStatSet` 收缩为元素专属输出数据，只保留元素精通和分元素伤害加成。
- 移除了运行时使用的 `ElementCritStatSet` 和 `ElementDefenseStatSet`。
- `ReactionStatSnapshot` 和 `ReactionDamageModifierSet` 不再保存原始命中的暴击率、暴击伤害、通用增伤、通用减伤镜像。
- `ReactionDamageCalculator` 只负责反应侧数值：增幅倍率、剧变/原型反应派生伤害、元素精通、反应专属倍率。
- `DamageInstancePlan` 扩展为统一的派生追加伤害计划，支持元素、反应类型、伤害职业、击退、源伤倍率、最终倍率、额外暴击率、是否可暴击、是否使用伤害浮动。
- `ReactionExecutor` 的反应派生伤害改为优先调用 `ExtraStrikeHelper.TryStrikeExtraDamage(...)`，不再默认使用 `SimpleStrikeNPC`。
- `ExtraStrikeHelper` 新增 `canCrit` 参数，派生伤害可按 TML 原生 `Player.GetTotalCritChance(damageClass)` 暴击，也可按单段计划关闭暴击。
- 无玩家来源的派生伤害仍保留 `SimpleStrikeNPC(..., crit: false, ...)` 作为兜底，避免伤害静默丢失。
- `IElementStatProvider` 和 `IElementArmorSetProvider` 补充了边界契约注释，明确原生字段与元素专属状态的写入规则。

### 当前数据流

原始命中：

```text
武器/弹幕命中前
-> ModifyHitNPC / ModifyProjectileHitNPC
-> ElementCombatSystem.ModifyItemHit / ModifyProjectileHit
-> 构造 AttackSourceIntent
-> 生成 AttackPayload
-> 读取 CombatContextSnapshot 中的元素专属倍率
-> 若触发增幅反应，将增幅倍率合入 NPC.HitModifiers.SourceDamage
-> 后续暴击、防御、减伤等继续由 TML 原生命中流程处理
```

命中后元素处理：

```text
OnHitNPC
-> ElementCombatSystem.ProcessItemHit / ProcessProjectileHit
-> 元素附着写回
-> 反应候选收集、验证、优先级选择
-> 消耗元素附着量
-> 显示反应文字
-> 执行剧变/原型/额外派生伤害
-> 分发协同攻击
```

派生追加伤害：

```text
ReactionDamageCalculator 生成 DamageInstancePlan
-> ReactionExecutor.ApplyDamage
-> 有 sourcePlayer 时调用 ExtraStrikeHelper.TryStrikeExtraDamage
-> ExtraStrikeHelper 通过 GetIncomingStrikeModifiers / ToHitInfo / StrikeNPCDirect 结算
-> 无 sourcePlayer 时使用非暴击 SimpleStrikeNPC 兜底
```

### 装备接入边界

- 熔骨胸甲的通用伤害加成写入 `player.GetDamage(DamageClass.Generic)`。
- 熔骨护腿的元素暴击写入 `player.GetCritChance(ElementDamage)`。
- 熔骨套装的元素伤害与减伤写入 `player.GetDamage(ElementDamage)` 和 `player.endurance`。
- 熔骨头盔的元素精通写入 `state.Offense.AddElementMastery(...)`。
- 高级元素瓶的分元素伤害加成和协同攻击写入 `state.Offense` 与 `state.CoordinatedAttacks`。

### 后续可直接扩展的入口

- “无条件追加一段某反应伤害”：向 `ReactionResult.AdditionalEffectDamages` 追加一条 `DamageInstancePlan`。
- “某饰品让派生伤害不能暴击”：设置对应 `DamageInstancePlan.CanCrit = false`。
- “某套装给反应追加独立乘区”：设置 `DamageInstancePlan.SourceMultiplier` 或 `DamageInstancePlan.FinalMultiplier`。
- “某效果追加反应暴击率”：设置 `DamageInstancePlan.CritChanceBonus`。
- “某效果改变派生伤害职业”：设置 `DamageInstancePlan.DamageClass`。
- “反应异化或额外段数”：在反应结果生成阶段追加或替换 `DamageInstancePlan` 列表。

### 暂缓项

- 正式 `DerivedAttack` 类型层仍未单独拆出，目前由 `DamageInstancePlan + ExtraStrikeHelper` 承担。
- 反应异化规则、反应专属暴伤体系、更多装备套装效果仍待后续内容补充。
- 协同攻击仍走真实弹幕，后续如果段数变多，需要评估是否也应改为非弹幕派生打击。
- 当前仅完成编译验证，仍需要实机验证暴击显示、无敌帧绕过、多人同步和大量派生伤害性能。

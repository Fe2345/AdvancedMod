# 元素系统扩展编写规范稿

## 1. 文档目的

本文档用于约束后续在新元素战斗系统中如何新增以下内容：

1. 自定义元素
2. 自定义反应
3. 自定义反应效果
4. 元素附魔类效果
5. 反应变异类效果

本规范默认采用**代码注册式**方案，不采用数据表驱动作为主写法。

目标不是为了减少文件数量，而是为了在后续元素、反应、装备和饰品数量快速增长时，仍然保持：

- 主链稳定
- 职责边界清晰
- 扩展点固定
- 调试路径明确

---

## 2. 总体原则

### 2.1 扩展分四层

后续所有扩展内容统一按四层拆分：

1. 元素定义层
2. 反应规则层
3. 反应效果层
4. Modifier 层

### 2.2 不越层写逻辑

必须坚持以下边界：

- 元素定义不写反应逻辑
- 反应规则不写执行逻辑
- 输入附魔不直接改 NPC 状态
- 反应变异不直接自己执行伤害

### 2.3 统一由主系统写回

无论是元素附着、反应伤害、额外伤害、追加状态还是生成物，最终都必须由元素主系统统一生成结果并统一执行。

不要让装备、饰品、武器、弹幕自己绕过系统直接做最终写回。

---

## 3. 元素应如何编写

### 3.1 元素分两层

元素扩展推荐固定为两部分：

1. 元素 ID
2. 元素定义对象

### 3.2 元素 ID

元素 ID 应放在 `ElementDefinitions.cs` 中。

示意：

```csharp
public enum ElementType
{
    None = 0,
    Thunder = 1,
    Wind = 2,
    Fire = 3,
    Ice = 4,
}
```

### 3.3 元素定义对象

建议后续补充独立元素定义对象，而不是只依赖 enum。

示意：

```csharp
public sealed class ElementDefinition
{
    public ElementType Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public Color DebugColor { get; init; }
    public bool CanApplyDot { get; init; }
    public int DefaultGaugePerHit { get; init; }
}
```

### 3.4 元素注册表

建议为每个元素提供一个集中注册表。

示意：

```csharp
public static class ElementRegistry
{
    public static readonly ElementDefinition Thunder = ...;
    public static readonly ElementDefinition Wind = ...;
}
```

### 3.5 新增元素的标准流程

以后新增一个元素时，按以下顺序：

1. 在 `ElementDefinitions.cs` 中加入新 ID
2. 在 `ElementRegistry` 中注册定义
3. 定义该元素的调试色、默认附着量、是否支持 DOT 等静态信息
4. 让武器、弹幕、附魔或反应引用它

### 3.6 元素层禁止事项

元素定义层不能做以下事情：

- 不能直接定义“会和谁反应”
- 不能直接定义“反应后造成什么伤害”
- 不能直接处理饰品逻辑
- 不能直接改写 NPC 元素槽

---

## 4. 反应应如何编写

### 4.1 反应也分两层

反应扩展建议固定为两部分：

1. 反应 ID
2. 反应规则对象

### 4.2 反应 ID

反应 ID 应放在 `ReactionDefinitions.cs` 中。

示意：

```csharp
public enum ReactionType
{
    None = 0,
    ThunderWind = 1,
    Vaporize = 2,
    Melt = 3,
}
```

### 4.3 反应规则对象

反应规则对象只定义“什么条件下成立”和“成立后的基础参数”，不直接执行结果。

示意：

```csharp
public sealed class ReactionRule
{
    public ReactionType Reaction { get; init; }
    public ElementType TriggerElement { get; init; }
    public ElementType TargetElement { get; init; }

    public ReactionConsumeMode TriggerConsumeMode { get; init; }
    public float TriggerConsumeValue { get; init; }

    public ReactionConsumeMode TargetConsumeMode { get; init; }
    public float TargetConsumeValue { get; init; }

    public ReactionDamageModel DamageModel { get; init; }
    public float BaseDamageFactor { get; init; }
    public ElementType DamageElement { get; init; }

    public bool IgnoreOrder { get; init; }
}
```

### 4.4 反应规则注册表

建议所有规则统一走注册表。

示意：

```csharp
public static class ReactionRuleRegistry
{
    public static IReadOnlyList<ReactionRule> Rules => ...;
}
```

### 4.5 新增反应的标准流程

以后新增一个反应时，按以下顺序：

1. 在 `ReactionDefinitions.cs` 中加入新 ID
2. 在 `ReactionRuleRegistry` 中注册一条规则
3. 写清该规则的：
   - 触发元素
   - 目标元素
   - 是否无序
   - 消耗模式
   - 基础倍率
   - 输出伤害元素
4. 再补该反应的效果处理器

### 4.6 反应规则层禁止事项

反应规则层不能做以下事情：

- 不能直接 `StrikeNPC`
- 不能直接生成弹幕
- 不能直接施加 Buff
- 不能读取饰品并直接改变最终结果

规则层只负责“成立条件”和“基础参数”。

---

## 5. 反应效果应如何编写

### 5.1 每个反应最好有独立处理器

不要把所有反应都写死在 `ElementCombatSystem` 中。

建议为每个反应提供独立效果处理器。

示意接口：

```csharp
public interface IReactionEffectHandler
{
    ReactionType Reaction { get; }

    ReactionResult BuildResult(ReactionExecutionContext context);
}
```

### 5.2 独立处理器示例

```csharp
public sealed class ThunderWindReactionHandler : IReactionEffectHandler
{
    public ReactionType Reaction => ReactionType.ThunderWind;

    public ReactionResult BuildResult(ReactionExecutionContext context)
    {
        // 构建主伤害、额外伤害、显示文本等
    }
}
```

### 5.3 效果处理器注册表

建议通过统一注册表检索具体反应处理器。

示意：

```csharp
public static class ReactionEffectRegistry
{
    public static IReactionEffectHandler Get(ReactionType reaction) { ... }
}
```

### 5.4 新增反应效果的标准流程

以后新增一个真实反应效果时，按以下顺序：

1. 新建一个 `XXXReactionHandler.cs`
2. 实现 `IReactionEffectHandler`
3. 在里面构建：
   - 主伤害
   - 额外伤害
   - 状态
   - 生成物
   - 文本与调试信息
4. 注册到 `ReactionEffectRegistry`

### 5.5 反应效果层的职责

反应效果层负责决定：

- 最终主伤害是什么
- 是否附加额外伤害
- 是否附加状态
- 是否生成产物
- 显示文本是什么

它不负责：

- 判断该反应是否成立
- 选择哪个候选反应获胜
- 直接将结果写回到 Terraria

---

## 6. 元素附魔应如何编写

### 6.1 元素附魔属于输入解释层

元素附魔的本质是：

- 改写本次攻击输入
- 追加本次攻击输入
- 替换本次攻击元素

因此必须统一走 `IAttackInputModifier`。

### 6.2 输入 Modifier 接口

示意：

```csharp
public interface IAttackInputModifier
{
    bool IsActive(AttackInputResolveContext context);
    void Modify(AttackInputResolveContext context, AttackInputBuilder builder);
}
```

### 6.3 强制转元素示例

```csharp
public sealed class ForceWindInputModifier : IAttackInputModifier
{
    public bool IsActive(AttackInputResolveContext context)
    {
        return context.SourcePlayer?.Advanced().ForceWindEnchant == true;
    }

    public void Modify(AttackInputResolveContext context, AttackInputBuilder builder)
    {
        builder.ReplaceAllElements(ElementType.Wind);
    }
}
```

### 6.4 追加元素示例

```csharp
public sealed class AddFireEnchantModifier : IAttackInputModifier
{
    public bool IsActive(AttackInputResolveContext context) => ...;

    public void Modify(AttackInputResolveContext context, AttackInputBuilder builder)
    {
        builder.ElementSeeds.Add(new AttackElementSeed
        {
            Element = ElementType.Fire,
            BaseBuildUp = 1,
        });
    }
}
```

### 6.5 饰品和装备如何接入

饰品和装备本身不应直接改 payload。

正确做法是：

1. `ModPlayer` 上挂状态位
2. 输入 Modifier 读取这些状态位
3. 由输入管线统一改写 builder

例如：

```csharp
public bool ForceWindEnchant;
public bool AddFireEnchant;
```

### 6.6 输入附魔层禁止事项

输入附魔层不能做以下事情：

- 不能直接改 NPC 元素槽
- 不能直接触发反应
- 不能直接打伤害
- 不能直接创建弹幕结果

输入附魔层只负责“改输入”。

---

## 7. 反应变异应如何编写

### 7.1 反应变异属于反应结果解释层

反应变异的本质是：

- 改已有反应结果
- 在已有反应后追加结果
- 将一个反应类型重定向为另一个

因此必须统一走 `IReactionOutcomeModifier`。

### 7.2 输出 Modifier 接口

示意：

```csharp
public interface IReactionOutcomeModifier
{
    bool IsActive(ReactionTransformContext context);
    void Modify(ReactionTransformContext context, ReactionTransformBuilder builder);
}
```

### 7.3 追加风伤示例

```csharp
public sealed class AddWindBurstReactionModifier : IReactionOutcomeModifier
{
    public bool IsActive(ReactionTransformContext context)
    {
        return context.SourcePlayer?.Advanced().ReactionExtraWindBurst == true;
    }

    public void Modify(ReactionTransformContext context, ReactionTransformBuilder builder)
    {
        builder.AddExtraDamage(new DamageInstancePlan
        {
            Damage = 10000,
            Element = ElementType.Wind,
        });
    }
}
```

### 7.4 反应替换示例

```csharp
public sealed class VaporizeToAnnihilationModifier : IReactionOutcomeModifier
{
    public bool IsActive(ReactionTransformContext context)
    {
        return context.Selection.Reaction == ReactionType.Vaporize && ...;
    }

    public void Modify(ReactionTransformContext context, ReactionTransformBuilder builder)
    {
        builder.OverrideReaction(ReactionType.Annihilation);
    }
}
```

### 7.5 饰品和装备如何接入

与输入附魔相同，饰品和装备本身不应直接改 `ReactionResult`。

正确做法是：

1. `ModPlayer` 上挂状态位
2. 输出 Modifier 读取这些状态位
3. 由反应结果解释层统一改写 builder

### 7.6 反应变异层禁止事项

反应变异层不能做以下事情：

- 不能直接 `StrikeNPC`
- 不能直接改真实元素槽
- 不能绕过主系统生成独立结算链

它只负责“改结果计划”。

---

## 8. 推荐目录结构

建议后续逐步整理到如下结构：

```text
DamageClasses/Element/
  Definitions/
    AttackDefinitions.cs
    ElementDefinitions.cs
    ReactionDefinitions.cs

  Data/
    AttackSourceIntent.cs
    AttackPayload.cs
    ElementApplication.cs
    CombatContextSnapshot.cs
    ReactionStatSnapshot.cs

  State/
    ElementState.cs
    ElementComponent.cs

  Elements/
    ElementDefinition.cs
    ElementRegistry.cs

  Input/
    IAttackInputModifier.cs
    AttackInputResolveContext.cs
    AttackInputBuilder.cs
    AttackInputModifierPipeline.cs
    PayloadBuilder.cs
    Modifiers/
      ForceWindInputModifier.cs
      AddFireEnchantModifier.cs

  Reactions/
    ReactionRule.cs
    ReactionRuleRegistry.cs
    ReactionResult.cs
    DamageInstancePlan.cs
    IReactionEffectHandler.cs
    ReactionEffectRegistry.cs
    IReactionOutcomeModifier.cs
    ReactionTransformContext.cs
    ReactionTransformBuilder.cs
    Handlers/
      ThunderWindReactionHandler.cs
      MeltReactionHandler.cs
      VaporizeReactionHandler.cs
    Modifiers/
      AddWindBurstReactionModifier.cs
      VaporizeToAnnihilationModifier.cs

  Adapters/
    PlayerElementAdapter.cs
    ItemElementAdapter.cs
    ProjectileElementAdapter.cs
    NpcElementAdapter.cs
```

---

## 9. 新增内容的标准流程

### 9.1 新增一个元素

步骤：

1. 在 `ElementDefinitions.cs` 中加入新元素 ID
2. 在 `ElementRegistry` 中注册定义
3. 定义默认附着量、显示色、DOT 能力等静态属性
4. 让武器、弹幕、附魔或反应引用该元素

### 9.2 新增一个反应

步骤：

1. 在 `ReactionDefinitions.cs` 中加入新反应 ID
2. 在 `ReactionRuleRegistry` 中加入规则
3. 新建该反应的 `ReactionHandler`
4. 注册到 `ReactionEffectRegistry`

### 9.3 新增一个输入附魔

步骤：

1. 在 `ModPlayer` 上增加状态位
2. 新建一个 `IAttackInputModifier`
3. 在输入管线中注册它
4. 让装备或饰品只负责开启该状态位

### 9.4 新增一个反应变异

步骤：

1. 在 `ModPlayer` 上增加状态位
2. 新建一个 `IReactionOutcomeModifier`
3. 在反应变异管线中注册它
4. 让装备或饰品只负责开启该状态位

---

## 10. 最终约束

为了保证这套系统后续还能继续扩展，必须长期坚持以下规则：

1. 元素定义层不写反应逻辑
2. 反应规则层不写执行代码
3. 输入附魔只改输入，不改真实目标状态
4. 反应变异只改结果计划，不直接自己打伤害
5. 所有最终结算必须从元素主链统一写回

只要守住这些约束，后续即使扩展到：

- 多元素
- 多反应
- 多装备联动
- 多种附魔
- 多种反应变异

主链仍然可以保持稳定，系统也不会因为局部功能增长而失控。


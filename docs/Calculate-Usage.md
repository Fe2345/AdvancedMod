# Calculate 当前使用方式

## 构建与登记

运行 `dotnet build AdvancedMod.csproj -p:BuildMod=false` 检查代码与贡献者 ID。贡献者类声明 `[CombatContributor(唯一整数常量)]`，实现 `ICombatContributor`。在 `CalculateSystem.RegisterDefinitions` 中显式登记实例，调用顺序就是注册顺序；不根据 ID 大小排序。

双职业桥接设计已撤销：不要为带元素标签的其他职业攻击额外补算 ElementalDamageClass 的职业加成，也不要登记对应桥接资格规则。职业统计由攻击自身的 DamageClass 与 TML 处理。

“元素伤害”指元素职业的伤害；元素标签不改变职业身份。元素相关自定义增伤是否生效、能否触发反应，由标签及反应规则裁定，不以 DamageClass 判断资格。当前已登记基础槽位映射；反应系统及标签生成仍待实现。

目前已登记 DefaultTagRules.All 中的 12 条基础启用规则，没有具体数值贡献者。先登记备用键，再登记使用它们的规则。标签基础规则使用 `TagGateRule`；后置规则实现 `ICombatRule`。规则 ID 在同一注册表中唯一。注册表冻结后不能修改。


## 当前基础标签映射

以下数字直接表示槽下标，不减一、不使用 TagEnum 的枚举值推导；规则默认覆盖四条正常命中路径。

| 标签 | 启用目标 |
|---|---|
| Pyro、Hydro、Electro、Cryo、Chlorophyte、Anemo | ElementDamage(1)、(2)、(3)、(4)、(5)、(6)，按顺序对应 |
| HydroElectro、HydroAnemo、PyroCryo | Reaction(1)、(2)、(3)，按顺序对应 |
| Hallowed、Lunacy、Originated | Ascension(1)、(2)、(3)，按顺序对应 |
| Hallow、Lunar、Origin | 无；仅表示异化资格，不表示已经异化 |

反应槽容量为 20（0..19），当前仅 1..3 有基础标签映射。未映射的槽保持默认关闭；抗性、精通、备用区也未增加基础启用规则。标签规则只启用槽位，不提供数值、生成标签或执行反应；例如 Hydro 与 Electro 同时存在不会自动启用 HydroElectro 的反应槽。已异化标签按其自身直接匹配，不额外要求基础反应标签或职业条件。普通贡献者与后置规则仍可覆盖基础启用决定，cannotBeCovered 不限制乘区启停。

## 上下文与提交

物品、弹幕和 NPC 各自的 `CombatGlobal*.Profile` 保存 `AttackProfile`，默认 `Unknown`。其中 TagSet 只读；Hook 读取 Profile 并创建本次上下文，不自动继承、合并或推导标签。后续上游模块可在本程序集内提供 Profile。

`ContributionWriter.AddValue` 提交自定义乘区值，`SetEnabled` 单独提交启停；自定义目标默认关闭，原生目标默认开启。反应系数也提交增幅，例如 0.5 表示同槽倍率增加到 1.5。

`AddNative` 提交原生数值，按字段能力选择 AddBase／AddPercent／Multiply／AddFlat／Add；暴击策略、上限、取消等走对应的具名方法。关闭某个原生目标只过滤提交到该目标的候选，不回滚 TML 已有值，也不连带关闭另一个独立自定义槽。

后置规则只获得只读上下文和 GateWriter；多个数组槽必须分别提交决定。所有候选收集完毕后统一裁决，禁用候选仍保留在 Trace 输入中。

## 日志与重放

在本机开发配置 `CombatTraceConfig` 开启 Enabled。SourceIds 匹配攻击身份或直接来源内容 ID，PlayerIndices 匹配本次攻击者／来源／受击玩家索引，TargetIds 匹配目标内容 ID。同维度匹配任一值，不同非空维度同时满足，空列表不限制。

匹配命中的摘要写入 Mod.Logger，详细文件位于 tModLoader 保存目录的 `Logs/AdvancedMod/Calculate/<会话ID>.jsonl`。错误始终记录；一次文件输出故障后，本次加载周期停止详细追踪，伤害计算继续使用已完成的结果。

命令行重放：

```powershell
dotnet run --project Tools/CalculateReplay -- <Trace.jsonl>
```

当前规则集为 calculate-2；反应槽扩容前的 calculate-1 Trace 不支持直接重放，避免逐槽结果数量差异被误判为算法错误。

退出码 0 表示记录与重算一致，1 表示存在差异，2 表示文件无效或版本不受支持。重放只执行候选收集之后的纯计算，不运行 Terraria、内容提供者或标签事务；文件中的字段计划是本模块将写入的增量，不是假定的游戏最终伤害。

验证命令：

```powershell
dotnet run --project Tests/Calculate.Core.Tests.csproj
dotnet run --project Tests/TmlAdapter/TmlAdapter.csproj
pwsh -File Tests/Verify-ContributorIds.ps1
```

TML 适配测试使用本地安装的真实类型，但不启动游戏。游戏内 Hook、逐实体行为、实时弹幕属性和联机仍需实测。

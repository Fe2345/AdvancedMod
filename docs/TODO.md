# Basic/Calculate 实施清单

> 状态：需求已部分锁定，按阶段实施。本文记录 2026-09-17 起与项目作者确认的首版方案；未确认项保持待办，不据此臆造规则。实际构建环境为 tModLoader 1.4.4.9、.NET 8.0。

> 2026-10-22 代码核对：已接入四条正常命中 Hook、逐实体 AttackProfile、加载生命周期、原生数值／控制贡献及 JSONL 重放。当前不登记具体标签映射或内容贡献者，默认 Unknown 与单位修正。元素职业桥接规则已确定交由后续 Tag 初始化 Register，与标签和规则统一补齐，本轮延期；弹幕实时面板原型、上游模块和实机／联机验收仍未完成。使用方式见 [Calculate-Usage.md](Calculate-Usage.md)。

## 状态与验证口径

- `[x]` 表示代码或明确标注的离线验证已完成，不代表实机验收通过。
- 未勾选项分别标注为待实现、待补充离线验证或待实机验证；延期项不视为已完成。
- 2026-10-22 已复核：主项目构建（BuildMod=false）成功，0 警告、0 错误；Core 19/19 通过；TML 适配器 NPC／玩家两组离线验证通过。
- 贡献者 ID 脚本及独立命令行重放样例为此前记录，本次文档同步未重跑；游戏内编译、单人、主机、专用服务器均未验收。

## 目标与边界

- [x] 以 tModLoader 的正常命中流程完成伤害结算。`Calculate` 补全命中上下文、收集具名贡献、折叠后写入 `NPC.HitModifiers` 或 `Player.HurtModifiers`，不重写原版伤害公式。
- [x] 首版覆盖玩家物品命中 NPC、玩家弹幕命中 NPC、NPC 接触玩家、敌对弹幕命中玩家四条路径。DoT、NPC 对 NPC、PvP 及直接调用 `NPC.StrikeNPC(HitInfo)` 等绕过修饰器的路径后续单独适配；本 Mod 内容禁止绕过正常修饰器路径。
- [x] 当前修改阶段只收集与计算，不消耗附着、SP 或生成派生伤害。HitId 和来源摘要已保留，但尚无 OnHit 回调、可靠命中关联或事务提交接口。
- [ ] 待实现：`Basic/Effects` 收集并组合本 Mod 托管的 Buff 类效果；实际伤害计算归 `Basic/Calculate`。内容向管线提交具名贡献，不直接改写 TML 命中修饰器。

## Tag 模块与上游标签事务

- [x] 建立独立基础模块 `Basic/Tag`：保留作者提供的 `TagEnum.cs`，在 `Tag.cs` 中实现 `TagEntry`、`TagSet`；不提前建立标签注册器、事务管理器、继承器或修改接口。
- [x] `TagEntry` 定义为 `{ bool cannotBeCovered, TagEnum type }`，两个属性只读：`cannotBeCovered` 为 true 表示不可被覆盖，`type` 记录标签类型。首版保存并允许读取该标志，覆盖行为留到上游模块设计时实现；它不表示乘区不可禁用。
- [x] `TagSet` 的成员为 `TagEntry`，同一 type 不允许重复，构造时重复即报错，即使 cannotBeCovered 不同也不能重复。构造时复制输入集合，对外只暴露 Count、索引读取、枚举、Contains 和 TryGet；不开放添加、删除、覆盖、合并或继承操作。
- [x] `TagEnum` 使用作者提供的八项：Pyro、Hydro、Electro、Cryo、Chlorophyte、Hallow、Lunar、Origin，保留原有顺序及数值，不根据标签枚举值自动推定乘区槽号。

以下是模块未来职责；注册、更改、继承和事务规则均延期到上游模块设计，本轮只落地 Calculate 所需的读取能力：

| 使用方 | Tag 提供的服务 | 职责边界 |
|---|---|---|
| 游戏内容 `Contents` | 标签注册（后续） | 设计内容模块时再确定注册方式，本版不实现注册服务。 |
| `Basic/Effects` | 标签更改（后续） | 设计效果模块时再确定更改方式，本版不实现覆盖与合并。 |
| `Basic/Element` | 标签更改（后续） | 设计元素反应模块时再确定标签事务，本版不执行反应或标签继承。 |
| `Basic/Calculate` | 标签收集／读取 | 收集上游处理后的当前标签，建立本次命中快照，再执行伤害规则；不反向执行元素反应事务。 |

- [x] Calculate 通过不可变 AttackProfile／TagSet 固定本次标签快照，写入器不提供修改标签接口。
- [ ] 待实现：Effects／Element 的标签更改事务及其在 Calculate 之前的执行顺序。
- [ ] 待实现上游身份捕获与标签更改：攻击来源仍保留可证实的生成时信息；本次计算标签以 Tag 在收集时给出的结果为准。此前“发射时固定元素身份”的约定不得阻止 Effects／Element 经 Tag 显式更改标签。
- [ ] 待设计并实现：后续设计上游模块时再补齐 Tag 的更改对象、作用域、继承、Effects 与 Element 的事务顺序及冲突处理；这些机制不属于本轮实现前置条件。本轮以已提供的 TagSet 作为计算输入，不模拟尚未定义的上游逻辑。

## Global 入口与上下文

- [x] 在 `Basic/Global` 建立 `GlobalNPC`、`GlobalProjectile`、`GlobalItem` 的伤害管线入口文件；玩家受伤入口由 `ModPlayer` 承担。
- [x] `GlobalNPC.ModifyHitByItem` 与 `GlobalNPC.ModifyHitByProjectile` 作为对 NPC 命中的唯一账本写入点；`CombatPlayer.ModifyHitByNPC` 与 `ModifyHitByProjectile` 作为玩家受伤入口。物品／弹幕／NPC 的 Global 均按实体实例保存 Profile，默认 Unknown；Hook 只读取，不生成或继承标签。实际游戏触发次数仍待实机验收。
- [x] CombatContext 提供四种路径的工厂，暴露可读取的 TML 实体引用，持有只读 AttackProfile／TagSet，能生成不带实体引用的 HitSnapshot。仅限本次计算使用的约束已写入接口；四条 Hook 已调用对应工厂；实际触发时序待实机验证。
- [x] Hook 从攻击来源物品／弹幕／NPC 的 Global.Profile 获取 AttackProfile／TagSet；Profile 对外可读、在本程序集内可赋值，标签集合本身仍只读。不自动从职业／枚举推导标签。
- [ ] 待实现：弹幕生成时记录可证实的攻击来源和基础元素身份；子弹幕标签继承与显式改写延期至上游模块设计。物品直接命中在命中时识别当前物品；本轮只读取上游提供的 TagSet，不实现标签推导或继承。
- [x] 四条路径允许使用 Unknown Profile 与缺字段上下文，不从当前手持物品猜测弹幕来源，也不反推职业加成。
- [ ] 待实机验证：原版／第三方攻击的缺字段降级行为和兼容性。

## 职业与弹幕面板

- [x] 定义 `ElementalDamageClass`，仅继承 `DamageClass.Generic` 的统计加成，不继承其他原版职业的属性、效果和前缀。实际元素武器／弹幕使用该类型时由内容指定 DamageType。
- [x] 确定规则归属：“非元素职业攻击带任意现有元素标签”的资格规则交由后续 Tag 初始化 Register 完成，与标签和规则统一补齐。当前延期，不在 Calculate 内硬编码标签判定或自动补算元素职业增伤；Tag Register 尚未实现。
- [ ] 待实现元素职业桥接：原版武器元素化后保留原 `DamageClass`。TML 照常计算原职业；`Calculate` 在命中时读取 `Player.GetDamage(ElementalDamageClass)` 的**直接百分比伤害加成**，作为独立防御前乘区应用。只使用加法增伤，不读取 `GetTotalDamage`，以免再次计入继承的通用加成；不额外给予元素职业暴击、穿甲或击退。
- [ ] 待桥接实现后验证：原生元素职业攻击不重复获得元素职业加成；当前尚无自动桥接贡献，不能将其记为桥接验收通过。
- [ ] 待实现：本 Mod 自有元素弹幕采用 TML 的 `Projectile.ContinuouslyUpdateDamageStats` 实时刷新伤害、暴击率和护甲穿透；确认 `originalDamage` 与 `DamageType` 正确。原版／第三方弹幕沿用已有 `projectile.damage`，不倒算面板。
- [ ] 待原型实现与实机验证：用本地 TML 1.4.4.9 做最小原型，核对实时刷新时机、源伤害种子、父子弹幕来源以及武器专属修正。首版保证本 Mod 的具名贡献；其他 Mod 的 `ModifyWeaponDamage` 武器专属加成不作为兼容承诺。

## 贡献账本与原版字段

- [x] 提供者使用统一注册序号，命中时按序号调用。自定义增伤区（含元素反应系数）同槽贡献先相加、不同槽相乘；互斥判定和覆盖值由最后调用者生效，伤害上限取更严格值。调试记录保留贡献来源及被覆盖项。
- [x] 提供者只改数值与判定，不在贡献阶段改写攻击身份或元素标签。
- [x] 对 NPC 方向实现 TML 可映射的来源／最终伤害、白值、额外伤害、防御及效率、固定／比例穿甲、暴击判定与暴伤、非暴击伤害、波动、击退、上限和即时击杀。暴击概率仍归 TML 属性。
- [x] 玩家受伤仅实现 HurtModifiers 的原生能力；写入器拒绝不支持的字段和操作，不通过末端 HurtInfo 伪造防御或暴击。
- [x] 常规目标易伤／减伤使用 FinalDamage，不开放 TargetDamageMultiplier；无视防御在合并普通穿甲后补足比例穿甲至至少 1，不降低既有更高值。
- [x] 不把显示伤害文字、受伤声音和粒子等表现开关纳入伤害贡献 API。

## 贡献者与乘区开关

本次命中的顺序固定如下；启停决定与数值候选分开保存，直到所有决定提交完毕才筛选数值：

1. Calculate 接收当前 TagSet，建立短生命周期上下文，读取攻击来源并固定本次标签快照。未来的上游 Effects／Element 标签处理发生在该步骤之前，本轮不实现这些事务。
2. 已实现标签规则匹配与调度；正式映射尚未登记。登记后，标签规则根据快照先提交基础启停决定；正式反应标签及其上游处理仍待实现。
3. 按统一注册序号调用有效贡献者，收集数值候选、普通启停决定及实例来源；未来由 `Effects` 提供组合后的贡献者，当前无正式贡献者登记。
4. `Calculate` 的后置特殊规则查看已收集的贡献者与上下文，再提交启停决定。
5. 对每个标量乘区、数组槽和备用键分别按提交顺序取最后一个启停决定；未收到决定的自定义目标保持关闭。
6. 保留所有候选及排除原因，只把启用目标的候选纳入同槽求和与跨槽乘算，最后一次映射到 TML 修饰器。
7. 当前在纯计算完成后、适配器应用前输出筛选命中的 Trace，记录计划而非最终游戏伤害；日志写入不参与数值裁决。

- [x] 贡献者标识由内容使用 `[CombatContributor(1001)]` 显式声明为 `int`，与调用顺序分开。项目构建链中的 Roslyn 分析器发现相同 ID 时报告错误 `ADVCalc001`，包括别名、常量表达式与生成代码。运行时使用 `CombatContributorAttribute.GetId(Type)` 读取同一特性，不维护另一份实例 Id 属性。
- [x] CombatRegistry 从具体贡献者类型的特性读取 ID，拒绝缺失特性或重复登记；冻结后禁止新增。CombatPipeline 按注册顺序与路径筛选贡献者，激活后调用绑定身份的写入器。已接 CalculateSystem 的加载／卸载入口；游戏内生命周期待实机验证。
- [x] CalculateSystem.PostSetupContent 显式调用 RegisterDefinitions、冻结注册表、建立管线；每次命中按 SupportedHits／IsActive 筛选。Unload 清理注册表、管线及 Trace 流。当前登记入口为空，等待内容定义，不自动扫描或注册映射。
- [ ] 待实现：正式标签到乘区的映射登记。匹配与调度机制已实现，但 RegisterDefinitions 为空。以下仅为规则示例：`Calculate` 预先注册标签规则，并在内容贡献者之前提交本次命中的基础启停决定。例如火元素标签使单元素增伤 `[0]` 默认启用。内容随后提交普通的启用或禁用决定；没有额外的“强制启用”层级。同一目标按稳定提交顺序最后一次决定生效，即使该决定允许无对应标签的攻击使用该槽。
- [x] 标量乘区、数组中的每个槽以及备用区的每个键分别启停；不存在“关闭整个数组”的单条决定，需要这种效果的内容逐槽提交。每项启停决定记录目标、布尔值、提交者和提交顺序。自定义乘区默认关闭，必须有明确启用决定才参与计算；原版 TML 字段的具名贡献默认有效。关闭原版字段只排除 Calculate 管理的贡献，不撤回 TML／其他 Mod 已有修正。
- [x] 所有贡献者先提交完启停决定与候选贡献，再统一裁决开关和筛选候选。后提交的决定可以影响先前贡献者的候选；最终禁用的槽仍保留候选，在折叠时排除，并在调试 Trace 中记录原因。收集阶段不修改攻击标签或产生命中副作用。
- [ ] 待实现 Effects：同一种托管效果由多个物品或 Buff 实例激活时，`Effects` 先组合成一个贡献者，再保留各实例的来源明细供规则判断和 Trace 输出。
- [x] 贡献者可以只提交启停决定，不提交数值候选；它仍算本次命中的有效贡献者，可被后置特殊规则按稳定 ID 和来源识别。
- [x] 普通内容决定收集完后，`Calculate` 执行后置特殊规则。这些规则可检查贡献者身份与本次命中上下文，并提交更晚的标量乘区／单槽／单键启停决定，从而稳定覆盖普通决定。例：Boss 在场时依次关闭擢升四槽；饰品提交的擢升候选仍留在 Trace 中但不参与计算。
- [x] ICombatRule 通过只读 RuleContext 查看已激活贡献者、来源、候选和此前决定；GateWriter 只允许追加启停决定。后置规则逐条取得新的快照，后执行规则能看到此前规则的决定，不改写候选数值。

## 自定义乘区

- [x] 元素反应系数区：使用长度为 `10` 的数组，索引 `0..9`；每槽系数与开关独立，默认全部关闭。逻辑阶段为 AfterBaseDamage，实际合入 SourceDamage 乘法项，属于防御前阶段，不并入防御后的单元素增伤区。正常上游反应逻辑下只会有一个槽可用；Calculate 不据此擅自合并数组、任选一槽或增加互斥覆盖规则。
- [ ] 待实现 Element 与正式映射：元素反应模块负责上游标签事务；Calculate 根据收集到的反应标签及已注册规则决定对应槽是否参与，候选仍须保留贡献者与来源，不允许绕过账本直接写数组。
- [x] 元素反应系数提交增幅而非直接倍率：预期倍率 150% 时提交 0.5。同槽累加后取 `1 + Σ增幅`，无贡献时倍率为 1；关闭的槽也以单位倍率参与最终折叠。各槽独立，不增加互斥裁决。
- [x] 本阶段不注册任何具体标签到乘区槽的映射，后续由作者编写规则；不根据 TagEnum 的数值或名称推定映射。
- [x] 元素反应系数写入 SourceDamage 乘法项；影响基础伤害及 SourceDamage.Base，保持 SourceDamage.Flat、FlatBonusDamage 等后续原生伤害项的位置，不额外扩展其作用范围。
- [x] 纯计算结果分别保留 ReactionFactor、ResistanceFactor 和 AfterDefenseFactor；TmlHitAdapter 为 NPC 命中与玩家受伤提供 Apply 重载，由 NativeCombiner 将前两者合入 SourceDamage 乘法增量，将后者合入 FinalDamage 乘法增量。适配器只应用已折叠计划，已接入四条 Hook；实机行为待验证。
- [x] 抗性区：在 TML 防御计算前应用，倍率为 `1 - 抗性`；允许负抗性和零倍率，不在零处钳制或跳过。
- [x] 单元素增伤区：支持 `10` 个固定槽；`0..9` 全部参与计算，但新内容原则上不使用 `4..9`。各槽独立乘算，同槽贡献相加；实际标签映射由作者后续注册。
- [x] 精通区：正精通的提升量为 `6 × 精通 / (2000 + 精通)`；负精通的提升量为 `精通 / (500 - 精通)`；最终倍率为 `1 + 提升量`。
- [x] 擢升区：支持 `4` 个固定槽；同槽贡献先相加，四槽分别取 `1 + 槽值` 后相乘。例如 `[0.1, 0.1, 0, 0]` 得到 `1.21` 倍。
- [x] 备用区：按登记的字符串键累计，每键取 `1 + 值` 后独立相乘，例如两键分别为 -0.1、0.1 时得到 `0.99` 倍。
- [x] 备用键按 StringComparer.Ordinal 排序折叠并保留逐键结果；结果已供 Trace 写入，不依赖字典遍历顺序。
- [x] 元素反应系数和抗性写入防御前的 SourceDamage 乘法项；单元素、精通、擢升、备用区全部写入防御后的 FinalDamage 乘法项。原版攻击额外获得的元素职业增伤尚未实现，规划位置仍为防御前。沿用 TML 各字段的 Base／Flat／额外伤害语义，不反算或搬动这些伤害项。

## 调试与日志

- [x] 实现命令行纯计算重放；CombatTraceConfig 默认关闭，并按来源／玩家／目标筛选。游戏宿主写独立 JSONL，摘要跟随筛选，异常与加载校验错误始终接入 Mod.Logger。游戏内配置及实际文件输出尚待实机验收。
- [x] Trace 采用 JSON Lines，一次命中一条记录并包含格式版本。至少记录命中来源与标签、贡献者及其候选贡献、启停决定与裁决、每项采纳／排除原因、各乘区折叠值以及拟写入的 TML 字段增量。正式运行时避免为未开启追踪的每次命中分配完整日志对象。
- [x] Trace 保存 Calculate 接收到的标签快照（上游事务尚未实现），以及元素反应系数各槽的候选、开关、系数与应用阶段；纯计算重放使用记录的快照，不重新执行 Effects／Element 的标签事务。
- [x] Trace 仅序列化可重放的上下文摘要，包括稳定身份、来源明细、标签和计算所需数值；不序列化 Terraria 玩家、NPC、物品或弹幕对象。完整实体读取限于贡献者收集阶段。
- [x] 首版离线重放范围为候选贡献收集后的纯计算段：从记录的标签、候选贡献与启停决定重算筛选、折叠和 TML 字段映射。完整调度及贡献者读取 Terraria 实体的条件仍待专项验证，现有 Core 测试不覆盖实体读取。
- [x] 重放逐项比较启停、候选采纳、各槽结果和包含自定义倍率映射的原生字段计划；篡改启停／倍率可检出差异，不兼容版本拒绝重放。它不重演贡献者条件或原版最终伤害计算。
- [x] 贡献者或规则抛异常时（不依赖 Trace 开关），先记录贡献者身份、可序列化的上下文摘要与异常，再立即重新抛出，避免以缺少贡献的结果继续命中。
- [ ] 待补充端到端验证：规则冲突、重复登记和缺失来源在完整管线中的诊断；负抗性、零倍率和不同乘区组合已有 Core 测试，游戏 Hook／联机仍待实测。
- [ ] 待补充端到端验证（纯计算禁用逻辑已有测试）：饰品提交擢升 `[0]` 的 `+0.2` 候选及启用决定；没有 Boss 时得到 `1.2` 倍；Boss 在场时后置规则分别禁用四个擢升槽，`+0.2` 候选仍出现在 JSONL 中且标注“槽已禁用”，本次擢升倍率为 `1`。
- [x] 纯计算验收样例（不是正式映射或实机效果）：火标签的基础规则先启用单元素 `[0]`，普通内容先禁用再启用该槽；最终启用状态与决定序列一致，无火标签时内容也可明确启用该槽。

## 已实现类型职责与保留边界

字段、方法签名、记录结构与生命周期见 [Calculate-Design.md](Calculate-Design.md)。2026-09-20 已确认 Calculate 下拆分 Core、Tml、Diagnostics，并预留 DamageOverTime。仅对内容扩展开放贡献者与后置规则两个接口，实际文件可合并，避免为每个平凡步骤创建一个接口。

Tag 是与 Calculate 并列的基础模块。下表及设计稿已按当前代码同步：Calculate 不提供 RegisterTag，标签使用 TagEnum／TagEntry／TagSet；上游注册、继承和更改仍待设计。

| 类型／状态 | 当前或规划位置 | 约束 |
|---|---|---|
| `TagEnum`、`TagEntry`、`TagSet` | `Basic/Tag/TagEnum.cs`、`Basic/Tag/Tag.cs` | 保留作者枚举；仅提供标签数据与读取 API，TagEntry 保存 cannotBeCovered 和 type；注册、继承、修改均延期，不承担伤害公式。 |
| `CombatContext` | `Basic/Calculate/Tml` | 只在一次 Hook 内有效，持有 TML 实体引用与固定攻击身份／标签；不能直接作为 JSONL 序列化对象。 |
| `CombatRegistry`、`CalculateSystem` | `Basic/Calculate/Tml` | 在 PostSetupContent 显式按序登记后冻结；分别保存标签基础规则、普通贡献者和后置特殊规则，命中时只判断激活。 |
| `HitProposalCollector` | `Basic/Calculate/Core` | 接受带贡献者 ID、实例来源、目标乘区／槽、操作和值的候选，以及带顺序的启停决定；不直接写 TML 字段。 |
| `CalculationInput`、`DamageCombiner`、`CalculationResult` | `Basic/Calculate/Core` | 把本次候选和决定转成可重放输入；纯组合器产出开关裁决、候选采纳状态、各区结果及 TML 字段计划，不访问 Terraria 实体或写日志。 |
| `CombatPipeline`、`TmlHitAdapter` | `Basic/Calculate/Tml` | 管线统一调度，适配器分 NPC 命中和玩家受伤两种映射；各 Global／ModPlayer Hook 每次调用管线一次。 |
| `CombatTraceWriter` | `Basic/Calculate/Diagnostics` | 负责 JSONL 编码；Tml 宿主负责命中筛选、文件生命周期和 Mod.Logger 摘要／错误。 |
| Effects 效果组合（待实现，原草案名 EffectCollector） | `Basic/Effects` | 尚无对应类；未来聚合同类 Buff／饰品效果及来源明细，以贡献者身份参与命中。 |
| CombatGlobalItem／NPC／Projectile 与 CombatPlayer | `Basic/Global` | 负责取得来源、建立上下文并调用 `Calculate`；不持有第二套伤害公式。 |

- [x] 核对 Core／Tml／Diagnostics 的依赖方向以及 DamageOverTime 的预留范围。
- [x] 核对六类自定义乘区的防御前／后位置及原生修正保留边界。
- [x] 在既有五类之外加入元素反应系数区，扩展目标键、逐槽启停、Trace 和重放；明确其数值契约及白值后映射。
- [x] 将标签数据和读取职责从 Calculate 拆到 Tag，同步设计稿与项目结构说明；统一 int 贡献者标识及 TagSet／TagEntry／TagEnum 的相关签名。注册、继承、修改机制留到上游模块设计。
- [x] Core、Tml、Diagnostics 生产类已落地；现有字段、签名、来源记录、校验和生命周期描述已按代码同步。

## 当前实现与既有验证记录

- [x] 实现 Core/ModifierTarget.cs：六类自定义目标、数组容量、逐槽目标校验与计算阶段；默认未初始化的目标不可使用。
- [x] 实现 Core/CalculationInput.cs：只读 HitSnapshot、贡献者激活与来源、数值候选、启停决定和复制后的集合；输入检查重复、有限数值、注册顺序与阶段顺序。整数贡献者 ID 由内容特性声明，编译期冲突检查由独立分析器执行。
- [x] HitProposalCollector 支持自定义数值、原生数值与控制候选，统一序号并绑定作者；异常后封闭。NativeCapabilities 校验路径／操作，NativeCombiner 输出逐字段强类型增量计划，控制覆盖与上限胜出记录可追踪。
- [x] 实现 Core/DamageCombiner.cs：全部自定义目标默认关闭、最后启停决定生效、同槽累加、独立乘算、精通点先相加再套公式；可选逐条采纳诊断，保留禁用候选。
- [x] Tests/Calculate.Core.Tests.csproj 的 19 组验收通过，包含原生控制、能力限制、JSONL 往返、篡改检测与筛选。独立命令行工具样例此前记录为 1 条、差异 0 条，本次未重跑该命令行样例。
- [x] Tests/TmlAdapter 使用本地 TML 真实 HitModifiers／HurtModifiers 验证原有 Base／Flat／乘法项保留、反应与抗性位置、穿甲与伤害上限；这是离线结构测试，不等于游戏 Hook／联机验收。
- [x] 实现 CombatRules、CombatContext、CombatRegistry 和 CombatPipeline。管线提供 Collect／Calculate 与 NPC／玩家 Apply 入口，执行中不创建静态“当前命中”；贡献者激活、收集和规则异常记录作者、阶段、上下文后保留堆栈抛出。错误输出由构造参数接入，CalculateSystem 已连接 Mod.Logger；实际游戏错误输出待验证。
- [x] 此前通过 Tests/Verify-ContributorIds.ps1 验证（本次未重跑）：重复 ID／生成代码冲突使编译失败（ADVCalc001）、非常量 ID 被 C# 编译器拒绝（CS0182）、唯一 ID 能构建并读取，分部类与无关同名特性不误报。
- [ ] 待实机验证：tModLoader 的实际开发构建入口。当前分析器接入 AdvancedMod.csproj；不将本次 dotnet 构建验收视为游戏内绕过项目文件重新编译的验收。
- [x] `Basic/Tag/Tag.cs` 数据模型与只读能力已实现，TagSet 已贯通命中上下文、候选收集、启停裁决、计算、Trace 和原生字段映射。
- [ ] 待实现（延期）：标签注册、继承、修改、覆盖执行和上游元素反应事务；当前 cannotBeCovered 只作为标签数据保留。

开发约束：遇到决定代码行为的未明确需求，应向作者核对，不自行选择规则，也不将未确定部分伪装为已完成。

### 当前待继续部分

- [ ] 待实现（延期）：后续补齐 Tag 初始化 Register 中的元素职业桥接资格规则，再接入对应具名贡献；本轮延期，不重复计入 Generic 或原生元素职业伤害。
- [ ] 待实现并验证：自有元素弹幕的实时属性入口及 originalDamage 种子，保留外部武器专属 Hook 的既定兼容边界。
- [ ] 待需求确认：如需独立故障 JSONL，应单独设计；当前仅输出 Mod.Logger 的作者／阶段／上下文与异常，不能将不完整输入记为成功重放记录。
- [ ] 待实现（延期）：Effects 组合、Tag 更改／继承、Element 事务和 DamageOverTime 按既定范围延期；不据这些待办生成空功能。
- [ ] 待实机验证：单人、主机、专用服务器和游戏内编译入口实测，验证实际触发时序、逐实体存储和多人执行侧。

## 已完成离线验证与后续验收

- [x] 纯计算：同槽相加、跨槽相乘、负抗性、零／负倍率、正负精通、反应槽默认关闭、后置禁用及启停次序，Core 19/19 通过。
- [x] TagSet：重复类型拒绝、输入快照隔离、type／cannotBeCovered 在 JSONL 往返中保留；重放不依赖游戏实体。
- [x] 原生适配：NPC／玩家真实 TML 类型离线测试通过，覆盖原有分量保留、倍率位置、穿甲和伤害上限。
- [ ] 待补充离线验证：完整注册与调度、贡献者实体读取和异常路径；现有 Core／适配测试不覆盖完整管线。
- [ ] 待实现并验证：正式反应标签到槽映射、元素职业桥接与自有弹幕实时属性；当前测试使用测试标签和人工贡献。
- [ ] 待实机验证：四条 Hook 调用时序、逐实体 Profile、各执行侧不重复写入，以及加载／卸载与 Trace 配置、文件输出。
- [ ] 待实机验证：单人、主机、专用服务器上的执行侧与结果，不能假设一次命中在所有端合计只执行一次。
- [ ] 待实机验证：游戏内开发构建入口是否运行贡献者 ID 分析器。

## 参考

- [原始架构草案](Pipeline.md)
- [NPC.HitModifiers](https://docs.tmodloader.net/docs/stable/struct_n_p_c_1_1_hit_modifiers.html)
- [Player.HurtModifiers](https://docs.tmodloader.net/docs/stable/struct_player_1_1_hurt_modifiers.html)
- [DamageClass](https://docs.tmodloader.net/docs/stable/class_damage_class.html)
- [Projectile](https://docs.tmodloader.net/docs/stable/class_projectile.html)

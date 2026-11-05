# 持续伤害接口

每次调用提交一次性白值，由 Calculate 完成乘区、防御和暴击计算，再一次性折算为 lifeRegen。上层持续调用形成 DoT，模块不管理周期或 Buff，不接管原版减益，不调用普通 StrikeNPC／Hurt 或触发元素反应事务。

## 使用

```csharp
var profile = new AttackProfile(
    new TagSet(new[] { new TagEntry(false, TagEnum.Pyro) }),
    attackId: "AdvancedMod:BurningReaction");
var context = CombatContext.ForDamageOverTime(targetNpc, profile, owner);
bool accepted = DotService.TrySubmit(context,
    new DotRequest(BaseDamage: 30, CriticalChance: 25, CriticalDamage: 100,
        DefenseFormula: DotMath.ApplyDefense), out DotResult calculated);
```

- 玩家目标使用 ForDamageOverTime 的 Player 重载。显式 SourceInfo 可描述 Buff、NPC 或其他来源；不保存失效来源实体。
- AttackId 必须非空。同一目标内，以 AttackId、施加玩家索引及显式 SourceInfo 的组合区分来源。相同组合重复调用全部累加，不覆盖或刷新。
- NPC 请求只由单人／服务器接受，玩家请求只由单人／所属客户端接受。错误执行侧或无效目标返回 false，不抽随机数、不排队。上层负责在正确执行侧调用；本模块网络包不接收扣血请求。
- separateText 默认 true，颜色采用标签中首个基本元素，没有基本元素则为白色。传 false 保留原版颜色的数字，仍参与 FIFO 账本。
- calculated 是本次计算值及暴击结果，不是本次实际扣血。合法零伤害返回 true，但不写入、不跳字。
- DotService.Calculate(context, request, roll) 只计算不提交；roll 是 [0,1) 的显式投掷值。

## 计算约定

白值已包含职业增伤，下层不补算。顺序为：白值 × 反应 × 抗性 → 防御委托 → 暴击 → 单元素／精通／擢升／备用。负伤害钳为零，不转换成回血。

- 默认暴击率 0%、额外暴伤 100%（暴击为 2 倍）、不计防御。每次提交独立投掷；暴击率限制 0..100，额外暴伤须有限非负。
- 防御委托 Func<double,double,double> 接收当前伤害、目标当前防御，返回有限非负伤害。预制 DotMath.ApplyDefense 每次独立减去 x*(rho+(1-rho)/sqrt(1+(x/K)^2))，默认 K=300、rho=0.15，可用四参数重载配置。
- 自定义乘区复用 CalculateSystem 注册表、基础标签规则及后置规则。贡献者使用 HitKinds.DamageOverTime 或具体目标路径；只支持普通命中的贡献者使用 HitKinds.Normal。
- HitKinds.All 包含 DoT。DoT 路径拒绝所有原生字段候选与控制值；暴击和防御仅由 DotRequest 指定。
- 神圣结界水／风减抗支持 NPC DoT；普通神圣龙卷的原生无视防御规则不移植到 DoT。

## 一次性写入及 FIFO 显示

每点伤害转换为 120 个生命恢复单位，在下一次目标恢复 Hook 中只写入一次。NPC 使用 UpdateLifeRegen，玩家使用较晚的 UpdateLifeRegen，避免请求被前面的饥饿赋值覆盖。之后的回血、原生分组和特殊状态结算仍由引擎决定；不清空其他来源的 lifeRegen 或 lifeRegenCount。

账本区分待注入和已注入待分配，只有后者可消费原生数字。IL 适配只替换 NPC.UpdateNPC_BuffApplyDOTs 与 Player.UpdateLifeRegen 内的整数 CombatText 调用，不修改原生扣血、死亡或免疫流程。找不到预期调用点时停止加载，避免重复或丢失数字。

每个原生 DoT 数字按提交顺序分配：先消费来源余额，指定来源显示独立数字，其余保留原版颜色。同目标同来源在同一更新帧末合并为一条；颜色取首笔，任一分到的笔为暴击则保留暴击强调。原版余量也在帧末合并为一条原版颜色数字。所有输出使用 dot:true，总和等于本帧引擎产生的原始数字总和。

例如原生本次共结算 15，已注入火 6、雷 4：显示火 6、雷 4、原版 5。只结算 8 则显示火 6、雷 2，雷的剩余 2 等后续结算。普通命中及未使用过本接口的目标显示不变。

不足 1/120 生命的尾数保留到下一笔完成恢复单位的来源；跨来源凑出整数生命时，整数归于完成该整数的后一来源。总显示数守恒，不给各小额来源分别向上取整。超过整数容量或相加溢出时明确报错，不截断伤害。

这是约定的显示归属，不是引擎记录的因果归属。回血可能延迟分配；共享池后续原版伤害也可能先偿还本模块的显示余额。窒息仍按 5 点分组，饥饿倍伤超出来源余额的部分归为原版余量。原生跳字可能包含致死过量伤害，不声称等于净生命损失。

## 生命周期与联网

NPC 首次非零提交后同步显示接管标记，直到该 NPC 重置／离开世界；玩家标记保持到退出世界／连接结束。远端不再为有标记目标自行产生原版 DoT 数字，改为接收权威侧的独立数字及原版余量。余额耗尽后保留显示标记，避免停止包时序造成漏字；不改变后续原版扣血。

NPC 标记通过 ExtraAI 支持晚加入，玩家标记通过 SyncPlayer 和专用包同步。服务器拒绝客户端冒充 NPC 或其他玩家的文字，文字包不改生命。原版余量保留颜色，但受管理目标不保留原版每 1／2／5 点一条的碎片分组。

死亡、NPC SetDefaults、玩家复活／断线及世界卸载清空伤害账本；已生成的本帧数字先汇总。多节 NPC 保留 realLife 扣血路由，账本和文字位置属于提交的受击节段。

## 验证

```powershell
dotnet run --project Tests/DamageOverTime/DamageOverTime.Tests.csproj
pwsh -File Tests/Run-DotLive.ps1
```

第二条使用 obj/dot-live 隔离存档，测试后退出，不部署日常 Mods；-Headless 仅验证真实引擎调用与跳字参数，不提供视觉或联机证据。最新验证状态见 CHANGES.md 与 docs/TODO.md。

Calculate Trace 仅记录乘区计划，不记录防御委托、暴击随机数、FIFO 分配或最终生命变化。正常命中设计见 [Calculate-Design.md](../../../docs/Calculate-Design.md)，引擎调查见 [DoT-Engine-Investigation.md](../../../docs/DoT-Engine-Investigation.md)。

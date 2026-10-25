# 饰品效果（2026-10-25）

用途：每刻由饰品提交根贡献，统一传播静态包含关系，在装备收集完毕之后冻结并应用。具体效果仍放在所属饰品文件中；当前示例是 `Contents/Development/EffectProbeAccessory.cs`，默认不加载物品，无配方。设环境变量 `ADVANCEDMOD_EFFECT_EXAMPLE=1` 后重启／重载可启用开发物品。

## 定义与使用

```csharp
// 放在所属 ModItem 内，效果类需 public 无参构造，不能是泛型或抽象类。
[IncludesEffect(typeof(Lower), 2)]
public sealed class Higher : AccessoryEffect
{
    private const int ExtraDefense = 3;
    public override void Apply(Player player, EffectPlayer effects, int stacks)
    {
        if (effects.Has<Lower>()) player.statDefense += ExtraDefense;
    }
}
public sealed class Lower : AccessoryEffect
{
    private const int DefensePerStack = 1;
    public override void Apply(Player player, EffectPlayer effects, int stacks)
        => player.statDefense += checked(DefensePerStack * stacks);
}
public override void UpdateAccessory(Player player, bool hideVisual)
    => player.GetModPlayer<EffectPlayer>().Add<Higher>();
```

在冻结后使用 `player.GetModPlayer<EffectPlayer>().Stacks<Lower>()` / `Has<Lower>()`。层数为零表示关闭，没有独立可写开关；收集期间查询最终状态会报错，避免把不完整结果误当最终结果。首次更新前和死亡清空后的查询为零。

- 每个根贡献和边权必须为正整数，默认为一；所有累加和传播乘法均使用 checked。收益计算本身也应自行 checked。
- A→B、C，B→D，C→D 时，一层 A 得到 A/B/C 各一层、D 两层；不同装备提交相同根继续累加。同源重复边禁止，改用权重。
- 每种开启效果每刻只 Apply 一次，接收真实最终层数。固定收益直接忽略 stacks，不钳制层数；例中 Left 固定加 2 防御，Shared 按层数加防御，Crown 读取 Shared 再追加固定收益。
- Apply 中双方查询彼此合法；不能提交贡献或修改层数。不要依赖另一个效果已经执行后的玩家属性，属性写入顺序不保证交换律。
- 定义实例由所有玩家共享，只能存放静态元数据和无状态逻辑。冷却、累计次数等放在所属内容的独立 ModPlayer 中，明确死亡／进出世界清理策略；示例 EffectProbeState 演示跨刻命中计数。

## 生命周期与事件

`ResetEffects` 清空根和最终层数 → `UpdateAccessory` 收集 → `PostUpdateEquips` 拓扑展开 → 冻结 → Apply。首次访问创建空集合；卸下后下一刻不再提交即归零。死亡 `UpdateDead` 清空并停止收集，重生／进入世界／断开清空，下一次 ResetEffects 开始新一刻。传播或应用失败原样抛出，失败状态不可读，不能继续派发事件。

首版仅提供 `IEffectOnHitNPC`，签名见 `AccessoryEffect.cs`。加载时缓存实现该接口的定义，事件仅调用冻结且层数大于零的订阅者，传入事件所属 Player、最终层数和原始命中参数。无每刻 .NET 事件订阅。冻结之前到达的事件不派发，不使用上一刻状态；该边界是明确契约，不进行排队补发。

本机实际程序集版本为 Terraria 1.4.4.9 / tML 2026.08.3.0（c9545cf）。反编译核对 Player.Update 中 ResetEffects → UpdateEquips（含原版与模组饰品）→ UpdateArmorSets → PlayerLoader.PostUpdateEquips；因此使用 PostUpdateEquips。其他 ModPlayer 不应在其 PostUpdateEquips 中向本框架提交贡献，该阶段相互顺序不受本框架控制。

tML 文档与本机程序集说明：PostUpdateEquips 在本地、服务端和远端客户端执行；OnHitNPC 在命中方客户端执行。装备层数从各端已有装备状态推导，不每刻发包。示例计数只在命中方累计，不保存也不联网同步；首版没有任意冷却／计数的通用同步协议。正式跨端玩法状态应由所属内容明确权威端并单独同步，不能把本地计数当服务器事实。实际伤害计算仍归 Basic/Calculate，本接口不新增直接改写伤害修饰器的路径。

## 编译检查与验证入口

从仓库根目录使用 PowerShell 7：

```powershell
./Basic/Effects/Build-Checked.ps1
dotnet run --project Basic/Effects/Tests/Effects.Tests.csproj -p:NuGetAudit=false
dotnet run --project Basic/Effects/Tests/Adapter/Adapter.Tests.csproj -p:NuGetAudit=false
./Basic/Effects/Tests/Verify-AnalyzerBuild.ps1
# 需要生成 tmod 时：强制分析器检查后将同一程序集交由 tML 打包
./Basic/Effects/Build-Checked.ps1 -Package
```

脚本强制 Rebuild、RunAnalyzers=true 和 RunAnalyzersDuringBuild=true，失败抛出错误；不会启动客户端。分析器使用类型符号解析同一份 IncludesEffect 特性，ADVEFF001 检查类型／构造／目标／权重／重复边，ADVEFF002 为循环 Error，指向闭环声明并列出路径。合法共享下级不会被当成循环。

| 入口 | 分析器行为与验证范围 |
| --- | --- |
| dotnet build / Build-Checked.ps1 | 项目引用加载分析器；已用真实成功／失败构建验证。BuildMod=false 只编译 DLL。 |
| IDE 的 MSBuild 项目构建 | 使用同一 ProjectReference 和 RunAnalyzersDuringBuild；配置支持，未实际启动 IDE 验证，用户全局关闭分析器会绕过，发布使用脚本。 |
| 游戏内“构建并重载” | 本机 ModCompile.CompileMod 直接 CSharpCompilation.Create + Emit，未调用 WithAnalyzers，也不消费 csproj 分析器引用，因此没有上述编译期检查；同源命令行 `dotnet tModLoader.dll -server -build <仓库路径>` 已实际源码编译成功，未操作 UI 实测。 |
| 加载 | EffectSystem 从当前模组程序集注册所有具体效果，重新验证完整图；非法图立即失败。这是运行时契约，不替代编译检查。 |

主项目 Compile Remove 与 build.txt buildIgnore 同时隔离 Analyzers/Tests（包括其 bin/obj）；仅排除 csproj 源码不足以覆盖游戏内编译。第一版仅注册本模组程序集内的具体 AccessoryEffect；不支持跨模组包含、继承包含特性或动态追加边。抽象效果类不能声明包含关系。

本轮实际验证：核心／Roslyn 27/27、真实 EffectPlayer 配合 tML 替身 9/9、五组 MSBuild 成功／预期失败验证通过；Build-Checked.ps1 和 -Package 均成功，MSBuild 0 警告／0 错误。tML 打包器另输出 `WARN: Image loading failed: unknown image type`，未处理无关图片资源。

离线测试不启动游戏。装备实机、死亡重生时序实机与多人玩法仍需验收；不可将测试中的模拟生命周期标为游戏内通过。

// 2026-10-25：针对层数、冻结、隔离、加载契约与编译诊断的离线验证。
using System.Collections.Immutable;
using AdvancedMod.Basic.Effects;
using AdvancedMod.Basic.Effects.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

int passed = 0;
void Test(string name, Action action) { action(); Console.WriteLine($"PASS {name}"); passed++; }
void Equal(int actual, int expected) { if (actual != expected) throw new Exception($"期望 {expected}，实际 {actual}"); }
void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception($"没有抛出 {typeof(T).Name}");
}
var graph = new EffectGraph(new[] { typeof(A), typeof(B), typeof(C), typeof(D), typeof(W) });
EffectCollection New() { var c = new EffectCollection(graph); c.Reset(); return c; }
Test("未启用为零", () => { var c = New(); c.Freeze(); Equal(c.Stacks<A>(), 0); });
Test("首次状态为零且禁止提交", () => { var c = new EffectCollection(graph); Equal(c.Stacks<A>(), 0); Throws<InvalidOperationException>(() => c.Add<A>()); });
Test("多根累加与菱形单次应用", () => {
    var c = New(); c.Add<A>(); c.Add<A>(); c.Add<B>(); c.Freeze();
    Equal(c.Stacks<A>(), 2); Equal(c.Stacks<B>(), 3); Equal(c.Stacks<C>(), 2); Equal(c.Stacks<D>(), 5);
    int calls = 0; c.Execute((type, stacks) => { if (type == typeof(D)) { calls++; Equal(stacks, 5); } }); Equal(calls, 1);
});
Test("一层菱形共享节点为两层", () => { var c = New(); c.Add<A>(); c.Freeze(); Equal(c.Stacks<D>(), 2); });
Test("固定收益忽略真实层数", () => { var c = New(); c.Add<D>(8); c.Freeze(); int benefit = 0; c.Execute((_, _) => benefit += 2); Equal(benefit, 2); Equal(c.Stacks<D>(), 8); });
Test("加权传播", () => { var c = New(); c.Add<W>(3); c.Freeze(); Equal(c.Stacks<D>(), 6); });
Test("根顺序不影响冻结组合判断", () => {
    var x = New(); var y = New(); x.Add<A>(); x.Add<W>(3); y.Add<W>(3); y.Add<A>(); x.Freeze(); y.Freeze();
    foreach (var t in new[] { typeof(A), typeof(B), typeof(C), typeof(D), typeof(W) }) Equal(x.Stacks(t), y.Stacks(t));
    int bx = 0, by = 0; x.Execute((_, _) => { if (x.Has<A>() && x.Has<D>()) bx++; }); y.Execute((_, _) => { if (y.Has<A>() && y.Has<D>()) by++; }); Equal(bx, by); Equal(bx, 5);
});
Test("每刻重置卸下与玩家隔离", () => { var x = New(); var y = New(); x.Add<A>(); x.Freeze(); y.Freeze(); Equal(y.Stacks<D>(), 0); x.Reset(); x.Freeze(); Equal(x.Stacks<D>(), 0); });
Test("死亡清空且重生前不能贡献", () => { var c = New(); c.Add<A>(); c.Freeze(); c.Reset(false); Equal(c.Stacks<A>(), 0); Throws<InvalidOperationException>(() => c.Add<A>()); c.Reset(); c.Freeze(); Equal(c.Stacks<D>(), 0); });
Test("收集阶段禁止查询不完整状态", () => { var c = New(); Throws<InvalidOperationException>(() => c.Has<A>()); });
Test("冻结及执行阶段不可变且不可重复执行", () => {
    var c = New(); c.Add<A>(); c.Freeze(); Throws<InvalidOperationException>(() => c.Add<A>());
    c.Execute((_, _) => { Throws<InvalidOperationException>(() => c.Add<D>()); Throws<InvalidOperationException>(() => c.Reset()); Equal(c.Stacks<D>(), 2); });
    Throws<InvalidOperationException>(() => c.Execute((_, _) => { }));
});
Test("非法根贡献", () => { var c = New(); Throws<ArgumentOutOfRangeException>(() => c.Add<A>(0)); Throws<ArgumentOutOfRangeException>(() => c.Add<A>(-1)); });
Test("根加法溢出", () => { var c = New(); c.Add<D>(int.MaxValue); Throws<OverflowException>(() => c.Add<D>()); });
Test("传播乘法溢出且失败状态不可读", () => { var c = New(); c.Add<W>(int.MaxValue); Throws<OverflowException>(c.Freeze); Throws<InvalidOperationException>(() => c.Has<D>()); });
Test("传播加法溢出", () => { var c = New(); c.Add<A>(int.MaxValue); Throws<OverflowException>(c.Freeze); });
Test("加载期循环拒绝", () => { Throws<InvalidOperationException>(() => new EffectGraph(new[] { typeof(Self) })); Throws<InvalidOperationException>(() => new EffectGraph(new[] { typeof(X), typeof(Y) })); });
Test("加载期非法目标权重重复边与类型拒绝", () => {
    Throws<InvalidOperationException>(() => new EffectGraph(new[] { typeof(BadTarget) }));
    Throws<InvalidOperationException>(() => new EffectGraph(new[] { typeof(BadWeight), typeof(D) }));
    Throws<InvalidOperationException>(() => new EffectGraph(new[] { typeof(Duplicate), typeof(D) }));
    Throws<InvalidOperationException>(() => new EffectGraph(new[] { typeof(string) }));
});

const string prelude = "using AdvancedMod.Basic.Effects; ";
var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
    .Select(p => MetadataReference.CreateFromFile(p)).Append(MetadataReference.CreateFromFile(typeof(EffectDefinition).Assembly.Location)).ToArray();
void Analyze(string name, string source, string id, string path = null)
{
    Test(name, () => {
        var compilation = CSharpCompilation.Create("Probe", new[] { CSharpSyntaxTree.ParseText(prelude + source) }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0) throw new Exception(string.Join("\n", errors.Select(d => d.ToString())));
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new IncludesEffectAnalyzer())).GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
        if (id == null) { Equal(diagnostics.Length, 0); return; }
        if (!diagnostics.Any(d => d.Id == id && d.Severity == DiagnosticSeverity.Error && d.Location.IsInSource && (path == null || d.GetMessage().Contains(path))))
            throw new Exception("缺少预期编译错误：" + string.Join("\n", diagnostics));
    });
}
Analyze("编译合法菱形", "[IncludesEffect(typeof(B))][IncludesEffect(typeof(C))] public class A:EffectDefinition{} [IncludesEffect(typeof(D))] public class B:EffectDefinition{} [IncludesEffect(typeof(D))] public class C:EffectDefinition{} public class D:EffectDefinition{}", null);
Analyze("编译自环", "[IncludesEffect(typeof(A))] public class A:EffectDefinition{}", "ADVEFF002", "A -> A");
Analyze("编译间接循环完整路径", "[IncludesEffect(typeof(B))] public class A:EffectDefinition{} [IncludesEffect(typeof(C))] public class B:EffectDefinition{} [IncludesEffect(typeof(A))] public class C:EffectDefinition{}", "ADVEFF002", "A -> B -> C -> A");
Analyze("编译非法目标", "[IncludesEffect(typeof(string))] public class A:EffectDefinition{}", "ADVEFF001");
Analyze("编译非法权重", "[IncludesEffect(typeof(B), 0)] public class A:EffectDefinition{} public class B:EffectDefinition{}", "ADVEFF001");
Analyze("编译重复边", "[IncludesEffect(typeof(B))][IncludesEffect(typeof(B))] public class A:EffectDefinition{} public class B:EffectDefinition{}", "ADVEFF001");
Analyze("编译抽象声明源", "[IncludesEffect(typeof(B))] public abstract class A:EffectDefinition{} public class B:EffectDefinition{}", "ADVEFF001");
Analyze("编译泛型类型", "public class A<T>:EffectDefinition{}", "ADVEFF001");
Analyze("编译构造契约", "public class A:EffectDefinition{private A(){}}", "ADVEFF001");
Analyze("编译语义别名和同名类型", "namespace N { public class D:EffectDefinition{} } namespace M { public class D {} } [IncludesEffect(typeof(N.D))] public class A:EffectDefinition{}", null);
Console.WriteLine($"PASS {passed}/{passed}");

[IncludesEffect(typeof(B)), IncludesEffect(typeof(C))] public sealed class A : EffectDefinition { }
[IncludesEffect(typeof(D))] public sealed class B : EffectDefinition { }
[IncludesEffect(typeof(D))] public sealed class C : EffectDefinition { }
public sealed class D : EffectDefinition { }
[IncludesEffect(typeof(D), 2)] public sealed class W : EffectDefinition { }
[IncludesEffect(typeof(Self))] public sealed class Self : EffectDefinition { }
[IncludesEffect(typeof(Y))] public sealed class X : EffectDefinition { }
[IncludesEffect(typeof(X))] public sealed class Y : EffectDefinition { }
[IncludesEffect(typeof(string))] public sealed class BadTarget : EffectDefinition { }
[IncludesEffect(typeof(D), 0)] public sealed class BadWeight : EffectDefinition { }
[IncludesEffect(typeof(D)), IncludesEffect(typeof(D))] public sealed class Duplicate : EffectDefinition { }

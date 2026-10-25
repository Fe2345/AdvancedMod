// 2026-10-25：基于类型符号检查包含声明、权重与完整循环路径。
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AdvancedMod.Basic.Effects.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IncludesEffectAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Invalid = new("ADVEFF001", "无效效果声明", "{0}", "Effects", DiagnosticSeverity.Error, true, customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });
    private static readonly DiagnosticDescriptor Cycle = new("ADVEFF002", "效果包含循环", "效果包含循环：{0}", "Effects", DiagnosticSeverity.Error, true, customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Invalid, Cycle);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(Analyze);
    }

    private static IEnumerable<INamedTypeSymbol> Types(INamespaceOrTypeSymbol scope)
    {
        foreach (var member in scope.GetMembers())
        {
            if (member is INamedTypeSymbol type) yield return type;
            if (member is INamespaceOrTypeSymbol nested)
                foreach (var child in Types(nested)) yield return child;
        }
    }

    private static bool Derives(INamedTypeSymbol type, INamedTypeSymbol root)
    {
        for (var current = type.BaseType; current != null; current = current.BaseType)
            if (SymbolEqualityComparer.Default.Equals(current, root)) return true;
        return false;
    }

    private static bool Valid(INamedTypeSymbol type, INamedTypeSymbol root)
    {
        if (type.TypeKind != TypeKind.Class || type.IsAbstract || !Derives(type, root)) return false;
        for (var current = type; current != null; current = current.ContainingType)
            if (current.IsGenericType) return false;
        return type.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);
    }

    private static void Analyze(CompilationAnalysisContext context)
    {
        var root = context.Compilation.GetTypeByMetadataName("AdvancedMod.Basic.Effects.EffectDefinition");
        var attribute = context.Compilation.GetTypeByMetadataName("AdvancedMod.Basic.Effects.IncludesEffectAttribute");
        if (root == null || attribute == null) return;
        var runtimeRoot = context.Compilation.GetTypeByMetadataName("AdvancedMod.Basic.Effects.AccessoryEffect") ?? root;
        var graph = new Dictionary<INamedTypeSymbol, List<(INamedTypeSymbol Target, Location Location)>>(SymbolEqualityComparer.Default);
        foreach (var type in Types(context.Compilation.Assembly.GlobalNamespace))
        {
            var attrs = type.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attribute)).ToArray();
            if (attrs.Length == 0 && !Derives(type, root)) continue;
            bool valid = Valid(type, runtimeRoot);
            if ((!type.IsAbstract || attrs.Length > 0) && !valid)
                context.ReportDiagnostic(Diagnostic.Create(Invalid, type.Locations.FirstOrDefault(), $"{type.ToDisplayString()} 必须是非泛型、非抽象效果类，并提供 public 无参构造函数"));
            var edges = new List<(INamedTypeSymbol, Location)>();
            graph[type] = edges;
            var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var attr in attrs)
            {
                var location = attr.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? type.Locations.FirstOrDefault() ?? Location.None;
                var target = attr.ConstructorArguments.Length > 0 ? attr.ConstructorArguments[0].Value as INamedTypeSymbol : null;
                if (target == null || !Valid(target, runtimeRoot) || !SymbolEqualityComparer.Default.Equals(target.ContainingAssembly, context.Compilation.Assembly))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Invalid, location, "包含目标必须是当前模组程序集中的有效具体效果类型"));
                    continue;
                }
                if (attr.ConstructorArguments.Length > 1 && (attr.ConstructorArguments[1].Value is not int weight || weight <= 0))
                    context.ReportDiagnostic(Diagnostic.Create(Invalid, location, "包含权重必须是正整数"));
                if (!seen.Add(target)) context.ReportDiagnostic(Diagnostic.Create(Invalid, location, "重复包含同一效果，请改用边权重"));
                edges.Add((target, location));
            }
        }
        var colors = new Dictionary<INamedTypeSymbol, int>(SymbolEqualityComparer.Default);
        var path = new List<INamedTypeSymbol>();
        void Visit(INamedTypeSymbol node)
        {
            colors[node] = 1;
            path.Add(node);
            if (graph.TryGetValue(node, out var edges)) foreach (var edge in edges)
            {
                colors.TryGetValue(edge.Target, out int color);
                if (color == 0) Visit(edge.Target);
                else if (color == 1)
                {
                    int start = path.FindIndex(p => SymbolEqualityComparer.Default.Equals(p, edge.Target));
                    var cycle = string.Join(" -> ", path.Skip(start).Concat(new[] { edge.Target }).Select(t => t.ToDisplayString()));
                    context.ReportDiagnostic(Diagnostic.Create(Cycle, edge.Location, cycle));
                }
            }
            path.RemoveAt(path.Count - 1);
            colors[node] = 2;
        }
        foreach (var node in graph.Keys) if (!colors.ContainsKey(node)) Visit(node);
    }
}

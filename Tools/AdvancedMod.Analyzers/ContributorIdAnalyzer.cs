using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AdvancedMod.Analyzers;

/// <summary>按特性语义符号检查当前程序集内的贡献者 ID，不依赖源码文本拼写。</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ContributorIdAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ADVCalc001";
    private const string AttributeMetadataName = "AdvancedMod.Basic.Calculate.Core.CombatContributorAttribute";

    private static readonly DiagnosticDescriptor DuplicateId = new(
        DiagnosticId,
        "贡献者 ID 冲突",
        "贡献者 ID {0} 被多个类型使用：{1}；请为每个贡献者指定唯一 ID",
        "Calculate",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "贡献者 ID 由内容显式声明，必须在当前程序集内唯一.",
        customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DuplicateId);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(start =>
        {
            INamedTypeSymbol? attributeType = start.Compilation.GetTypeByMetadataName(AttributeMetadataName);
            if (attributeType is null)
                return;

            // 状态属于本次 compilation，不能在并行构建或后续 IDE 分析中复用。
            var declarations = new ConcurrentBag<Declaration>();
            start.RegisterSymbolAction(symbolContext =>
            {
                var type = (INamedTypeSymbol)symbolContext.Symbol;
                if (type.TypeKind != TypeKind.Class)
                    return;

                foreach (AttributeData attribute in type.GetAttributes())
                {
                    if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType) ||
                        attribute.ConstructorArguments.Length != 1 ||
                        attribute.ConstructorArguments[0].Value is not int id)
                        continue;

                    Location? location = attribute.ApplicationSyntaxReference?.GetSyntax(symbolContext.CancellationToken).GetLocation();
                    if (location is not null)
                        declarations.Add(new Declaration(id, type.ToDisplayString(), location));
                }
            }, SymbolKind.NamedType);

            start.RegisterCompilationEndAction(end =>
            {
                foreach (var group in declarations.GroupBy(declaration => declaration.Id).OrderBy(group => group.Key))
                {
                    var conflicts = group.OrderBy(declaration => declaration.TypeName, StringComparer.Ordinal).ToArray();
                    if (conflicts.Length < 2)
                        continue;

                    string names = string.Join("、", conflicts.Select(declaration => declaration.TypeName));
                    foreach (Declaration conflict in conflicts)
                        end.ReportDiagnostic(Diagnostic.Create(DuplicateId, conflict.Location, group.Key, names));
                }
            });
        });
    }

    private sealed class Declaration
    {
        public int Id { get; }
        public string TypeName { get; }
        public Location Location { get; }

        public Declaration(int id, string typeName, Location location)
        {
            Id = id;
            TypeName = typeName;
            Location = location;
        }
    }
}

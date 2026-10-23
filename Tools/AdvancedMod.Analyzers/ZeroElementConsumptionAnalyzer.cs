using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AdvancedMod.Analyzers;

/// <summary>检查元素消耗修正的零常量写入，不根据同名字段或源码拼写推断身份。</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ZeroElementConsumptionAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ADVCalc002";
    private static readonly DiagnosticDescriptor ZeroValue = new(
        DiagnosticId, "元素消耗修正不能为零",
        "元素消耗修正 {0} 不能写入零常量", "Calculate", DiagnosticSeverity.Error, true,
        description: "零消耗修正会使按实际消耗量加权的反应白值无法计算.",
        customTags: new[] { WellKnownDiagnosticTags.NotConfigurable });

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ZeroValue);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(start =>
        {
            var item = start.Compilation.GetTypeByMetadataName("AdvancedMod.Basic.Global.CombatGlobalItem");
            var projectile = start.Compilation.GetTypeByMetadataName("AdvancedMod.Basic.Global.CombatGlobalProjectile");
            bool IsTarget(IFieldSymbol field) => field.Name == "ElementConsumptionMultiplier" &&
                field.Type.SpecialType == SpecialType.System_Single &&
                (SymbolEqualityComparer.Default.Equals(field.ContainingType, item) ||
                 SymbolEqualityComparer.Default.Equals(field.ContainingType, projectile));

            start.RegisterOperationAction(c =>
            {
                var assignment = (ISimpleAssignmentOperation)c.Operation;
                if (assignment.Target is IFieldReferenceOperation target && IsTarget(target.Field) && IsZero(assignment.Value))
                    c.ReportDiagnostic(Diagnostic.Create(ZeroValue, assignment.Value.Syntax.GetLocation(), target.Field.Name));
            }, OperationKind.SimpleAssignment);
            start.RegisterOperationAction(c =>
            {
                var initializer = (IFieldInitializerOperation)c.Operation;
                if (!IsZero(initializer.Value)) return;
                foreach (var field in initializer.InitializedFields)
                    if (IsTarget(field))
                        c.ReportDiagnostic(Diagnostic.Create(ZeroValue, initializer.Value.Syntax.GetLocation(), field.Name));
            }, OperationKind.FieldInitializer);
        });
    }

    private static bool IsZero(IOperation value) =>
        value.ConstantValue.HasValue && value.ConstantValue.Value is float number && number == 0f;
}

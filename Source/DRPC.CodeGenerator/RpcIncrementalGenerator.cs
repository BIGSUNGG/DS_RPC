using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using DRPC.CodeGenerator.Reference;

namespace DRPC.CodeGenerator;

/// <summary>
/// <c>partial class X : ClientHub&lt;…&gt;</c> / <c>ServerHub&lt;…&gt;</c> 를 찾아 RPC 스텁을 생성한다.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class RpcIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ClassDeclarationSyntax> candidates =
            context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax classDecl
                    && classDecl.BaseList != null
                    // 구문만 보고 허브 후보를 거른다: partial 이 아닌 클래스도 지나가야 DRPCGEN001 을 낼 수 있다.
                    && classDecl.BaseList.Types.Any(static b => b.Type.ToString().IndexOf("Hub", System.StringComparison.Ordinal) >= 0),
                static (ctx, _) => (ClassDeclarationSyntax)ctx.Node);

        context.RegisterSourceOutput(candidates.Combine(context.CompilationProvider), static (spc, pair) =>
        {
            var (syntax, compilation) = pair;
            SemanticModel semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(syntax) is not INamedTypeSymbol hubSymbol)
            {
                return;
            }

            var references = new AttributeReferences(compilation);
            if (references.RemoteProcedureAttributeType == null)
            {
                return; // DRPC.Attribute 미참조 프로젝트 — 대상 아님.
            }

            string? source = RpcHubSourceGenerator.Generate(hubSymbol, references,
                diagnostic => spc.ReportDiagnostic(diagnostic),
                syntax.Identifier.GetLocation());

            if (source != null)
            {
                spc.AddSource($"{hubSymbol.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        });
    }
}

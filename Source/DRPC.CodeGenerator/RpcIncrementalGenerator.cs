using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DRPC.CodeGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class RpcIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ClassDeclarationSyntax> provider =
            context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) =>
                    node is ClassDeclarationSyntax classDecl
                    && classDecl.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword))
                    && classDecl.BaseList != null,
                static (ctx, _) => (ClassDeclarationSyntax)ctx.Node);

        context.RegisterSourceOutput(
            provider.Combine(context.CompilationProvider),
            static (spc, pair) =>
            {
                var (syntax, compilation) = pair;
                RpcHubSourceGenerator.Generate(syntax, compilation, spc);
            });
    }
}

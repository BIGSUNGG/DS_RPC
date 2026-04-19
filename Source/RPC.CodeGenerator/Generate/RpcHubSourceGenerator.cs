using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RPC.CodeGenerator.Semantic;

namespace RPC.CodeGenerator.Generate;

/// <summary>
/// RpcHub / ClientHub 부분 클래스에서 RPC 보일러플레이트를 생성한 뒤,
/// 같은 컨텍스트에 MessageProtocol 의 MessageCodeGenerator 파이프라인을 등록합니다.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class RpcHubSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var hubClasses = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ClassDeclarationSyntax { BaseList: not null } cds &&
                cds.Modifiers.Any(SyntaxKind.PartialKeyword),
            static (ctx, _) => ctx);

        context.RegisterSourceOutput(
            hubClasses.Collect(),
            static (spc, contexts) =>
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var genCtx in contexts)
                {
                    spc.CancellationToken.ThrowIfCancellationRequested();
                    if (genCtx.Node is not ClassDeclarationSyntax cds)
                        continue;

                    if (genCtx.SemanticModel.GetDeclaredSymbol(cds, spc.CancellationToken) is not { } classSymbol)
                        continue;

                    if (!CouldBeRpcHub(classSymbol))
                        continue;

                    var key = classSymbol.ToDisplayString();
                    if (!seen.Add(key))
                        continue;

                    RpcSemanticAnalyzer.TryAnalyze(cds, genCtx.SemanticModel, spc, spc.CancellationToken);
                }
            });

        RunEmbeddedMessageProtocolGeneratorInitialize(context);
    }

    private static void RunEmbeddedMessageProtocolGeneratorInitialize(
        IncrementalGeneratorInitializationContext context)
    {
        var asm = RpcEmbeddedMessageProtocolAssembly.Assembly;
        var type = RpcEmbeddedMessageProtocolAssembly.MessageCodeGeneratorType
                   ?? throw new InvalidOperationException("MessageCodeGenerator type was not found in embedded assembly.");
        var instance = Activator.CreateInstance(type)!;
        var init = type.GetMethod(
            nameof(IIncrementalGenerator.Initialize),
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: [typeof(IncrementalGeneratorInitializationContext)],
            modifiers: null)!;
        init.Invoke(instance, [context]);
    }

    private static bool CouldBeRpcHub(INamedTypeSymbol classSymbol)
    {
        for (var baseType = classSymbol.BaseType; baseType != null; baseType = baseType.BaseType)
        {
            if (baseType is not { IsGenericType: true, TypeArguments.Length: 2 })
                continue;

            var def = baseType.OriginalDefinition;
            if (def.Name == "ServerHub" &&
                def.ContainingNamespace.ToDisplayString() == "RPC.Client.Network")
                return true;

            if (def.Name == "ClientHub" &&
                def.ContainingNamespace.ToDisplayString() == "RPC.Server.Netwrok")
                return true;
        }

        return false;
    }
}

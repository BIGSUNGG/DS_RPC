using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using RPC.CodeGenerator.Meta;

namespace RPC.CodeGenerator.Generate;

/// <summary>
/// 임시 컴파일로 NonIdMessage 타입 심볼을 얻어 MessageSerializeCodeEmitter 로 직렬화 partial을 생성합니다.
/// </summary>
internal static class RpcNonIdMessageSerializationEmitter
{
    public static void EmitSerializationPartials(
        Compilation compilation,
        SourceProductionContext context,
        string? namespaceName,
        string hubClassName,
        RpcHubKind kind,
        ImmutableArray<RpcMethodEmitMeta> methodsWithParams)
    {
        if (methodsWithParams.IsEmpty)
            return;

        var decl = RpcParameterPayloadFragments.BuildProbeSyntaxTree(namespaceName, hubClassName, kind,
            methodsWithParams);
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var tree = CSharpSyntaxTree.ParseText(decl, parseOptions);

        var probeCompilation = CSharpCompilation.Create(
            "RpcNonIdProbe_" + hubClassName,
            new[] { tree },
            compilation.References,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        var sem = probeCompilation.GetSemanticModel(tree);
        var root = tree.GetRoot(context.CancellationToken);

        foreach (var m in methodsWithParams)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var typeName = m.Name + "_Parameter";
            var typeDecl = root.DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .FirstOrDefault(t => t.Identifier.Text == typeName);
            if (typeDecl == null)
                continue;

            if (sem.GetDeclaredSymbol(typeDecl, context.CancellationToken) is not INamedTypeSymbol named)
                continue;

            var facade = RpcEmbeddedMessageProtocolAssembly.SerializationEmitterFacadeType;
            if (facade == null)
                continue;

            var emit = facade.GetMethod(
                "EmitSerializeImplementationSource",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(INamedTypeSymbol), typeof(Compilation) },
                modifiers: null);
            if (emit == null)
                continue;

            var generatedObj = emit.Invoke(null, new object[] { named, probeCompilation });
            if (generatedObj is not string generated)
                continue;
            var hint = $"{hubClassName}_{m.Name}_Parameter.Serialization.g.cs";
            context.AddSource(hint, SourceText.From(generated, Encoding.UTF8));
        }
    }

    public static ImmutableArray<RpcMethodEmitMeta> CollectMethodsWithParametersDistinct(
        ImmutableArray<RpcMethodEmitMeta> clientProcedures,
        ImmutableArray<RpcMethodEmitMeta> serverProcedures)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var builder = ImmutableArray.CreateBuilder<RpcMethodEmitMeta>();
        foreach (var m in clientProcedures.Concat(serverProcedures))
        {
            if (m.Parameters.Length == 0)
                continue;
            if (!seen.Add(m.Name))
                continue;
            builder.Add(m);
        }

        return builder.ToImmutable();
    }
}

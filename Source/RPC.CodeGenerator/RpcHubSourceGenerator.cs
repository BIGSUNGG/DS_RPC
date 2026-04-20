using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using MessageProtocol.CodeGenerator.Generate;
using RPC.CodeGenerator.Emitter;
using RPC.CodeGenerator.Metadata;
using RPC.CodeGenerator.Reference;

namespace RPC.CodeGenerator;

internal static class RpcHubSourceGenerator
{
    public static void Generate(ClassDeclarationSyntax syntax, Compilation compilation, SourceProductionContext context)
    {
        var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
        if (semanticModel.GetDeclaredSymbol(syntax) is not INamedTypeSymbol hubSymbol)
        {
            return;
        }

        var location = syntax.Identifier.GetLocation();

        if (!syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MustBePartial, location, hubSymbol.Name));
            return;
        }

        if (!TryResolveRpcHub(hubSymbol, out INamedTypeSymbol? hubBase, out bool isServerEndpoint))
        {
            return;
        }

        var attrRefs = new AttributeReferences(compilation);

        var tSpd = hubBase!.TypeArguments[0] as INamedTypeSymbol;
        var tCpd = hubBase.TypeArguments[1] as INamedTypeSymbol;
        if (tSpd == null || tCpd == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidHubBase, location, hubSymbol.Name));
            return;
        }

        var declarationsMetadataCache = BuildDeclarationsMetadataCache(
            new[] { tSpd, tCpd },
            attrRefs);

        var typeMetadata = BuildTypeMetadata(
            hubSymbol,
            tSpd,
            tCpd,
            declarationsMetadataCache,
            context,
            location,
            isServerEndpoint ? NetworkKind.Server : NetworkKind.Client);
        if (typeMetadata is null)
        {
            return;
        }

        string source = RpcHubEmitter.Emit(typeMetadata);
        string fileStem = BuildGeneratedFileName(hubSymbol);

#pragma warning disable RS1035
        try
        {
            if (Directory.Exists(@"C:\Debug"))
            {
                File.WriteAllText(Path.Combine(@"C:\Debug", $"{fileStem}.g.debug.cs"), source);
            }
        }
        catch
        {
        }
#pragma warning restore RS1035

        context.AddSource($"{fileStem}.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    static TypeMetadata? BuildTypeMetadata(
        INamedTypeSymbol hubSymbol,
        INamedTypeSymbol serverDeclarationsInterface,
        INamedTypeSymbol clientDeclarationsInterface,
        Dictionary<INamedTypeSymbol, DeclarationsMetadata> declarationsMetadataCache,
        SourceProductionContext context,
        Location location,
        NetworkKind networkKind)
    {
        if (!declarationsMetadataCache.TryGetValue(serverDeclarationsInterface, out var serverDeclarations))
        {
            return null;
        }

        if (!declarationsMetadataCache.TryGetValue(clientDeclarationsInterface, out var clientDeclarations))
        {
            return null;
        }

        var typeMetadata = new TypeMetadata(
            hubSymbol,
            serverDeclarations,
            clientDeclarations,
            networkKind);
        if (!ValidateTypeMetadata(typeMetadata, context, location))
        {
            return null;
        }

        return typeMetadata;
    }

    static Dictionary<INamedTypeSymbol, DeclarationsMetadata> BuildDeclarationsMetadataCache(
        IEnumerable<INamedTypeSymbol> declarationInterfaces,
        AttributeReferences references)
    {
        var cache = new Dictionary<INamedTypeSymbol, DeclarationsMetadata>(SymbolEqualityComparer.Default);
        foreach (var declarationInterface in declarationInterfaces)
        {
            if (cache.ContainsKey(declarationInterface))
            {
                continue;
            }

            var metadata = new DeclarationsMetadata(declarationInterface, references);
            cache.Add(declarationInterface, metadata);
        }

        return cache;
    }

    static bool ValidateTypeMetadata(
        TypeMetadata typeMetadata,
        SourceProductionContext context,
        Location location)
    {
        if (!ValidateDeclarations(typeMetadata.ServerDeclarations.Symbol, typeMetadata.ServerDeclarations.Methods, context, location))
        {
            return false;
        }

        if (!ValidateDeclarations(typeMetadata.ClientDeclarations.Symbol, typeMetadata.ClientDeclarations.Methods, context, location))
        {
            return false;
        }

        return true;
    }

    static bool ValidateDeclarations(
        INamedTypeSymbol declarationSymbol,
        MethodMetadata[] methods,
        SourceProductionContext context,
        Location location)
    {
        foreach (var methodMetadata in methods)
        {
            var method = methodMetadata.Symbol;

            if (method.IsGenericMethod || declarationSymbol.IsGenericType && method.TypeParameters.Length > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedType,
                    location,
                    method.Name,
                    "generic method"));
                return false;
            }

            foreach (var p in method.Parameters)
            {
                if (p.RefKind != RefKind.None)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.UnsupportedType,
                        location,
                        method.Name,
                        "ref/out parameters"));
                    return false;
                }

                if (!RpcMarshal.IsSupportedDataType(p.Type, allowVoid: false, out var td))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.UnsupportedType,
                        location,
                        method.Name,
                        td));
                    return false;
                }
            }

            if (!RpcMarshal.IsSupportedDataType(method.ReturnType, allowVoid: true, out var rd))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedType,
                    location,
                    method.Name,
                    rd));
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// <c>ClientHub&lt;,&gt;</c>(서버 어셈블리) → 수신 대기용 허브( ListenAsync ).
    /// <c>ServerHub&lt;,&gt;</c>(클라 어셈블리) → 접속용 허브( ConnectAsync ).
    /// </summary>
    static bool TryResolveRpcHub(INamedTypeSymbol hubSymbol, out INamedTypeSymbol? hubBase, out bool isServerEndpoint)
    {
        hubBase = null;
        isServerEndpoint = false;
        INamedTypeSymbol? current = hubSymbol.BaseType;

        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            if (current is INamedTypeSymbol named &&
                named.TypeArguments.Length == 2 &&
                named.OriginalDefinition.TypeParameters.Length == 2)
            {
                string ons = named.OriginalDefinition.ContainingNamespace?.ToDisplayString() ?? "";
                string oname = named.OriginalDefinition.Name;

                if (ons == "RPC.Server.Netwrok" && oname == "ClientHub")
                {
                    hubBase = named;
                    isServerEndpoint = true;
                    return true;
                }

                if (ons == "RPC.Client.Network" && oname == "ServerHub")
                {
                    hubBase = named;
                    isServerEndpoint = false;
                    return true;
                }
            }

            current = current.BaseType;
        }

        return false;
    }

    static string BuildGeneratedFileName(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol.ContainingType == null)
        {
            return typeSymbol.Name;
        }

        var stack = new Stack<string>();
        for (var current = typeSymbol; current != null; current = current.ContainingType)
        {
            stack.Push(current.Name);
        }

        return string.Join("_", stack);
    }
}

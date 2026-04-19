using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RPC.CodeGenerator;
using RPC.CodeGenerator.Generate;
using RPC.CodeGenerator.Meta;

namespace RPC.CodeGenerator.Semantic;

internal static class RpcSemanticAnalyzer
{
    public static void TryAnalyze(
        ClassDeclarationSyntax classSyntax,
        SemanticModel semanticModel,
        SourceProductionContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (semanticModel.GetDeclaredSymbol(classSyntax, cancellationToken) is not INamedTypeSymbol classSymbol)
            return;

        if (!IsPartialClass(classSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                RpcDiagnostics.HubMustBePartial,
                classSyntax.Identifier.GetLocation(),
                classSymbol.Name));
            return;
        }

        if (!TryResolveHub(classSymbol, out var kind, out var serverDecl, out var clientDecl))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                RpcDiagnostics.UnknownHubBase,
                classSyntax.Identifier.GetLocation(),
                classSymbol.Name));
            return;
        }

        var clientMethodSymbols = EnumerateRpcInterfaceMethods(clientDecl).ToList();
        var serverMethodSymbols = EnumerateRpcInterfaceMethods(serverDecl).ToList();

        var clientNameSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in clientMethodSymbols)
        {
            if (!clientNameSet.Add(m.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RpcDiagnostics.DuplicateRpcMethodName,
                    m.Locations.FirstOrDefault() ?? Location.None,
                    m.Name));
                return;
            }
        }

        var serverNameSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in serverMethodSymbols)
        {
            if (!serverNameSet.Add(m.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RpcDiagnostics.DuplicateRpcMethodName,
                    m.Locations.FirstOrDefault() ?? Location.None,
                    m.Name));
                return;
            }
        }

        foreach (var m in serverMethodSymbols)
        {
            if (clientNameSet.Contains(m.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RpcDiagnostics.DuplicateRpcMethodName,
                    m.Locations.FirstOrDefault() ?? Location.None,
                    m.Name));
                return;
            }
        }

        var clientMethods = ImmutableArray.CreateBuilder<RpcMethodEmitMeta>();
        var serverMethods = ImmutableArray.CreateBuilder<RpcMethodEmitMeta>();

        var clientId = 0;
        foreach (var method in clientMethodSymbols)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryBuildMethodEmitMeta(method, clientId, context, out var emit))
                return;
            clientMethods.Add(emit);
            clientId++;
        }

        var serverId = 0;
        foreach (var method in serverMethodSymbols)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryBuildMethodEmitMeta(method, serverId, context, out var emit))
                return;
            serverMethods.Add(emit);
            serverId++;
        }

        string? ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? null
            : classSymbol.ContainingNamespace.ToDisplayString();

        var model = new RpcHubEmitMeta
        {
            Kind = kind,
            Namespace = ns,
            ClassName = classSymbol.Name,
            ClientProcedures = clientMethods.ToImmutable(),
            ServerProcedures = serverMethods.ToImmutable()
        };

        var source = RpcSourceEmitter.Emit(model);
        var fileName = $"{classSymbol.Name}_GeneratedCode.cs";

#pragma warning disable RS1035 // Do not use APIs banned for analyzers
        try
        {
            var debugFilePath = Path.Combine(@"C:\Debug\", $"{classSymbol.Name}_GeneratedCode.debug.cs");
            if (Directory.Exists(@"C:\Debug"))
                File.WriteAllText(debugFilePath, source);
        }
        catch
        {
            // 디버그 파일 생성 실패는 무시
        }
#pragma warning restore RS1035

        context.AddSource(fileName, source);

        var methodsWithParams = RpcNonIdMessageSerializationEmitter.CollectMethodsWithParametersDistinct(
            model.ClientProcedures,
            model.ServerProcedures);
        RpcNonIdMessageSerializationEmitter.EmitSerializationPartials(
            semanticModel.Compilation,
            context,
            ns,
            model.ClassName,
            model.Kind,
            methodsWithParams);
    }

    private static bool IsPartialClass(INamedTypeSymbol classSymbol)
    {
        foreach (var reference in classSymbol.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is ClassDeclarationSyntax cds &&
                cds.Modifiers.Any(SyntaxKind.PartialKeyword))
                return true;
        }
        return false;
    }

    private static bool TryResolveHub(
        INamedTypeSymbol classSymbol,
        out RpcHubKind kind,
        out INamedTypeSymbol serverDeclarations,
        out INamedTypeSymbol clientDeclarations)
    {
        kind = default;
        serverDeclarations = null!;
        clientDeclarations = null!;

        for (var baseType = classSymbol.BaseType; baseType != null; baseType = baseType.BaseType)
        {
            if (baseType is not { IsGenericType: true, TypeArguments.Length: 2 })
                continue;

            var def = baseType.OriginalDefinition;
            if (def.Name == "ServerHub" &&
                def.ContainingNamespace.ToDisplayString() == "RPC.Client.Network")
            {
                kind = RpcHubKind.ClientSideServerHub;
                serverDeclarations = (INamedTypeSymbol)baseType.TypeArguments[0];
                clientDeclarations = (INamedTypeSymbol)baseType.TypeArguments[1];
                return true;
            }

            if (def.Name == "ClientHub" &&
                def.ContainingNamespace.ToDisplayString() == "RPC.Server.Netwrok")
            {
                kind = RpcHubKind.ServerSideClientHub;
                serverDeclarations = (INamedTypeSymbol)baseType.TypeArguments[0];
                clientDeclarations = (INamedTypeSymbol)baseType.TypeArguments[1];
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<IMethodSymbol> EnumerateRpcInterfaceMethods(INamedTypeSymbol iface)
    {
        // AllInterfaces 는 구현/상속 체인만 포함하는 경우가 있어, 선언 인터페이스 자체를 반드시 포함합니다.
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var i in EnumerateSelfAndAllBaseInterfaces(iface))
        {
            if (!visited.Add(i))
                continue;
            if (IsMarkerDeclarationsInterface(i))
                continue;

            foreach (var member in i.GetMembers())
            {
                if (member is not IMethodSymbol method)
                    continue;
                if (method.MethodKind != MethodKind.Ordinary || method.IsStatic)
                    continue;
                if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, i))
                    continue;
                yield return method;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateSelfAndAllBaseInterfaces(INamedTypeSymbol iface)
    {
        yield return iface;
        foreach (var i in iface.AllInterfaces)
            yield return i;
    }

    private static bool IsMarkerDeclarationsInterface(INamedTypeSymbol i)
    {
        if (i.Name is not ("IClientProcedureDeclarations" or "IServerProcedureDeclarations"))
            return false;
        return i.ContainingNamespace.ToDisplayString() == "RPC.Shared.Interface";
    }

    private static bool TryBuildMethodEmitMeta(
        IMethodSymbol method,
        int methodId,
        SourceProductionContext context,
        out RpcMethodEmitMeta emit)
    {
        emit = null!;

        var reliable = GetReliableTypeExpression(method);
        if (reliable == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                RpcDiagnostics.MissingRemoteProcedure,
                method.Locations.FirstOrDefault() ?? Location.None,
                method.ContainingType.Name,
                method.Name));
            return false;
        }

        var retKind = ClassifyReturnType(method.ReturnType);
        if (retKind == RpcEmitReturnKind.Unsupported)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                RpcDiagnostics.UnsupportedReturnType,
                method.Locations.FirstOrDefault() ?? Location.None,
                method.ContainingType.Name,
                method.Name));
            return false;
        }

        var parameters = ImmutableArray.CreateBuilder<(string Name, string TypeDisplay, RpcEmitReturnKind Kind)>();
        foreach (var p in method.Parameters)
        {
            var pk = ClassifyParameterType(p.Type);
            if (pk == RpcEmitReturnKind.Unsupported)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    RpcDiagnostics.UnsupportedParameterType,
                    p.Locations.FirstOrDefault() ?? Location.None,
                    method.ContainingType.Name,
                    method.Name,
                    p.Name));
                return false;
            }

            parameters.Add((p.Name, p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), pk));
        }

        emit = new RpcMethodEmitMeta
        {
            MethodId = methodId,
            Name = method.Name,
            ReliableTypeExpression = reliable,
            Parameters = parameters.ToImmutable(),
            ReturnKind = retKind
        };
        return true;
    }

    private static string? GetReliableTypeExpression(IMethodSymbol method)
    {
        foreach (var attr in method.GetAttributes())
        {
            if (attr.AttributeClass?.Name != "RemoteProcedure")
                continue;
            if (attr.ConstructorArguments.Length == 0)
                continue;
            var arg = attr.ConstructorArguments[0];
            if (arg.Kind != TypedConstantKind.Enum || arg.Type is not INamedTypeSymbol enumType || arg.Value == null)
                continue;

            foreach (var member in enumType.GetMembers())
            {
                if (member is IFieldSymbol { HasConstantValue: true } field &&
                    Equals(field.ConstantValue, arg.Value))
                {
                    return $"{enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{field.Name}";
                }
            }
        }

        return null;
    }

    private static RpcEmitReturnKind ClassifyReturnType(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_Void)
            return RpcEmitReturnKind.Void;

        return MapSpecialType(type);
    }

    private static RpcEmitReturnKind ClassifyParameterType(ITypeSymbol type) => MapSpecialType(type);

    private static RpcEmitReturnKind MapSpecialType(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_Single => RpcEmitReturnKind.Float,
            SpecialType.System_Double => RpcEmitReturnKind.Double,
            SpecialType.System_Int32 => RpcEmitReturnKind.Int,
            SpecialType.System_Int64 => RpcEmitReturnKind.Long,
            SpecialType.System_Int16 => RpcEmitReturnKind.Short,
            SpecialType.System_Byte => RpcEmitReturnKind.Byte,
            SpecialType.System_Boolean => RpcEmitReturnKind.Bool,
            _ => RpcEmitReturnKind.Unsupported
        };
    }
}

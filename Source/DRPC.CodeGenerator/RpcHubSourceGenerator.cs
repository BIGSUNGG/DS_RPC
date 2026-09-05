using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using DRPC.CodeGenerator.Metadata;
using DRPC.CodeGenerator.Reference;

namespace DRPC.CodeGenerator;

/// <summary>
/// 허브 클래스 하나를 검사하고 생성 소스 문자열을 만든다. 진단은 <see cref="System.Action{Diagnostic}"/> 로 보고한다.
/// </summary>
internal static class RpcHubSourceGenerator
{
    public static string? Generate(INamedTypeSymbol hubSymbol, AttributeReferences references,
        System.Action<Diagnostic> report, Location fallbackLocation)
    {
        if (!hubSymbol.DeclaringSyntaxReferences.Any(static s => s.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax c
                && c.Modifiers.Any(static m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword))))
        {
            report(Diagnostic.Create(DiagnosticDescriptors.MustBePartial,
                hubSymbol.Locations.FirstOrDefault() ?? fallbackLocation, hubSymbol.Name));
            return null;
        }

        if (!TryResolveHub(hubSymbol, out INamedTypeSymbol? hubBase, out NetworkKind networkKind, out bool invalidBase))
        {
            if (invalidBase)
            {
                report(Diagnostic.Create(DiagnosticDescriptors.InvalidHubBase,
                    hubSymbol.Locations.FirstOrDefault() ?? fallbackLocation, hubSymbol.Name));
            }

            return null;
        }

        INamedTypeSymbol? serverDeclarations = hubBase!.TypeArguments[0] as INamedTypeSymbol;
        INamedTypeSymbol? clientDeclarations = hubBase.TypeArguments[1] as INamedTypeSymbol;
        if (!Implements(serverDeclarations, AttributeReferences.ServerDeclarationsTypeName) ||
            !Implements(clientDeclarations, AttributeReferences.ClientDeclarationsTypeName))
        {
            report(Diagnostic.Create(DiagnosticDescriptors.InvalidHubBase,
                hubSymbol.Locations.FirstOrDefault() ?? fallbackLocation, hubSymbol.Name));
            return null;
        }

        var server = new DeclarationsMetadata(serverDeclarations!, references);
        var client = new DeclarationsMetadata(clientDeclarations!, references);
        var model = new TypeMetadata(hubSymbol, server, client, networkKind);

        if (!Validate(model.ServerDeclarations, references, report) || !Validate(model.ClientDeclarations, references, report))
        {
            return null;
        }

        return Emitter.RpcHubEmitter.Emit(model);
    }

    static bool Validate(DeclarationsMetadata declarations, AttributeReferences references,
        System.Action<Diagnostic> report)
    {
        var methodIds = new HashSet<int>();
        var methodNames = new HashSet<string>();

        foreach (MethodMetadata method in declarations.Methods)
        {
            IMethodSymbol symbol = method.Symbol;
            Location location = symbol.Locations.FirstOrDefault() ?? Location.None;

            if (!method.HasExplicitMethodId)
            {
                report(Diagnostic.Create(DiagnosticDescriptors.MissingExplicitMethodId, location,
                    symbol.Name, method.MethodId));
            }

            if (!methodIds.Add(method.MethodId))
            {
                report(Diagnostic.Create(DiagnosticDescriptors.DuplicateMethodId, location,
                    method.MethodId, declarations.Symbol.Name));
                return false;
            }

            if (!methodNames.Add(symbol.Name))
            {
                report(Diagnostic.Create(DiagnosticDescriptors.UnsupportedType, location, symbol.Name,
                    declarations.Symbol.Name, "overloaded RPC method name — give one of them a different name"));
                return false;
            }

            if (method.OneWay && symbol.ReturnType.SpecialType != SpecialType.System_Void)
            {
                report(Diagnostic.Create(DiagnosticDescriptors.OneWayRequiresVoid, location, symbol.Name));
                return false;
            }

            if (symbol.IsGenericMethod)
            {
                report(Diagnostic.Create(DiagnosticDescriptors.UnsupportedType, location, symbol.Name,
                    symbol.ToDisplayString(), "generic method"));
                return false;
            }

            foreach (IParameterSymbol parameter in symbol.Parameters)
            {
                if (parameter.RefKind != RefKind.None)
                {
                    report(Diagnostic.Create(DiagnosticDescriptors.UnsupportedType, location, symbol.Name,
                        parameter.Type.ToDisplayString(), "ref/in/out parameter"));
                    return false;
                }

                if (!RpcPayload.IsSupported(parameter.Type, references, out _))
                {
                    report(Diagnostic.Create(DiagnosticDescriptors.UnsupportedType, location, symbol.Name,
                        parameter.Type.ToDisplayString(), UnsupportedReason(parameter.Type)));
                    return false;
                }
            }

            if (!RpcPayload.IsSupported(symbol.ReturnType, references, allowVoid: true, out _))
            {
                report(Diagnostic.Create(DiagnosticDescriptors.UnsupportedType, location, symbol.Name,
                    symbol.ReturnType.ToDisplayString(), UnsupportedReason(symbol.ReturnType)));
                return false;
            }
        }

        return true;
    }

    /// <summary>DRPCGEN003 의 "Reason" 자리 문구.</summary>
    static string UnsupportedReason(ITypeSymbol type)
        => IsTask(type)
            ? "declare the contract with a plain return type (Task<int> -> int); the generated stub is already async"
            : "unsupported type";

    static bool IsTask(ITypeSymbol type)
    {
        string name = type.OriginalDefinition.ToDisplayString();
        return name == "System.Threading.Tasks.Task" || name == "System.Threading.Tasks.Task<TResult>";
    }

    static bool Implements(INamedTypeSymbol? type, string interfaceMetadataName)
        => type != null && type.AllInterfaces.Any(i => i.ToDisplayString() == interfaceMetadataName);

    /// <summary>
    /// 베이스 체인에서 허브 베이스를 찾는다. 클라이언트 측 <c>ClientHub&lt;&gt;</c>(DRPC.Client.Network) 는
    /// client endpoint, 서버 측 <c>ServerHub&lt;&gt;</c>(DRPC.Server.Network) 는 server endpoint(ADR-0001).
    /// </summary>
    static bool TryResolveHub(INamedTypeSymbol hubSymbol, out INamedTypeSymbol? hubBase, out NetworkKind networkKind,
        out bool invalidBase)
    {
        hubBase = null;
        networkKind = NetworkKind.Client;
        invalidBase = false;

        for (INamedTypeSymbol? current = hubSymbol.BaseType;
             current != null && current.SpecialType != SpecialType.System_Object;
             current = current.BaseType)
        {
            if (current.TypeArguments.Length != 2 || current.OriginalDefinition.TypeParameters.Length != 2)
            {
                continue;
            }

            string ns = current.OriginalDefinition.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            string name = current.OriginalDefinition.Name;

            if (ns == "DRPC.Client.Network" && name == "ClientHub")
            {
                hubBase = current;
                networkKind = NetworkKind.Client;
                return true;
            }

            if (ns == "DRPC.Server.Network" && name == "ServerHub")
            {
                hubBase = current;
                networkKind = NetworkKind.Server;
                return true;
            }

            if (ns == "DRPC.Shared.Network" && name == "HubBase")
            {
                // 허브이긴 한데 클라이언트/서버 중 어느 측인지 선언되지 않음 → 생성 불가 사유를 알려준다.
                hubBase = null;
                invalidBase = true;
                return false;
            }
        }

        return false;
    }
}

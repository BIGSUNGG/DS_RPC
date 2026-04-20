using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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

        var attrRefs = new RpcAttributeSymbols(compilation);

        var tSpd = hubBase!.TypeArguments[0] as INamedTypeSymbol;
        var tCpd = hubBase.TypeArguments[1] as INamedTypeSymbol;
        if (tSpd == null || tCpd == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidHubBase, location, hubSymbol.Name));
            return;
        }

        int nextMethodId = 0;
        var serverProcedures = CollectProcedures(
            tSpd,
            attrRefs.RemoteProcedureAttributeType,
            compilation,
            ref nextMethodId,
            context,
            location);

        if (serverProcedures == null)
        {
            return;
        }

        var clientProcedures = CollectProcedures(
            tCpd,
            attrRefs.RemoteProcedureAttributeType,
            compilation,
            ref nextMethodId,
            context,
            location);

        if (clientProcedures == null)
        {
            return;
        }

        RpcProcedureData[] outgoing = isServerEndpoint ? clientProcedures : serverProcedures;
        RpcProcedureData[] incoming = isServerEndpoint ? serverProcedures : clientProcedures;

        bool needsString = NeedsStringReturnHelpers(outgoing, incoming);

        string? ns = hubSymbol.ContainingNamespace?.IsGlobalNamespace == true
            ? null
            : hubSymbol.ContainingNamespace?.ToDisplayString();

        var emitModel = new RpcHubEmitModel
        {
            HubTypeName = hubSymbol.Name,
            Namespace = ns,
            IsServerEndpoint = isServerEndpoint,
            Outgoing = outgoing,
            Incoming = incoming,
            NeedsStringHelpers = needsString
        };

        string source = RpcHubEmitter.Emit(emitModel);
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

    static bool NeedsStringReturnHelpers(RpcProcedureData[] outgoing, RpcProcedureData[] incoming)
    {
        foreach (var p in outgoing.Concat(incoming))
        {
            if (p.ReturnType.SpecialType == SpecialType.System_String)
            {
                return true;
            }
        }

        return false;
    }

    static RpcProcedureData[]? CollectProcedures(
        INamedTypeSymbol iface,
        INamedTypeSymbol? remoteProcedureAttr,
        Compilation compilation,
        ref int nextMethodId,
        SourceProductionContext context,
        Location location)
    {
        if (remoteProcedureAttr == null)
        {
            return Array.Empty<RpcProcedureData>();
        }

        var list = new List<RpcProcedureData>();

        foreach (var member in iface.GetMembers())
        {
            if (member is not IMethodSymbol method || method.MethodKind != MethodKind.Ordinary)
            {
                continue;
            }

            if (member.IsImplicitlyDeclared)
            {
                continue;
            }

            var attr = method.FindAttribute(remoteProcedureAttr);
            if (attr == null)
            {
                continue;
            }

            if (method.IsGenericMethod || iface.IsGenericType && method.TypeParameters.Length > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedType,
                    location,
                    method.Name,
                    "generic method"));
                return null;
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
                    return null;
                }

                if (!RpcMarshal.IsSupportedDataType(p.Type, allowVoid: false, out var td))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.UnsupportedType,
                        location,
                        method.Name,
                        td));
                    return null;
                }
            }

            if (!RpcMarshal.IsSupportedDataType(method.ReturnType, allowVoid: true, out var rd))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.UnsupportedType,
                    location,
                    method.Name,
                    rd));
                return null;
            }

            if (!TryBuildReliableTypeExpression(attr, compilation, out string reliableExpr))
            {
                reliableExpr = "global::Communication.Network.RUDP.Shared.Messages.ReliableType.ReliableOrdered";
            }

            var parameters = method.Parameters
                .Select(p => (p.Name, p.Type))
                .ToArray();

            list.Add(new RpcProcedureData
            {
                MethodName = method.Name,
                MethodId = nextMethodId++,
                ReturnType = method.ReturnType,
                Parameters = parameters,
                ReliableTypeExpression = reliableExpr,
            });
        }

        return list.ToArray();
    }

    static bool TryBuildReliableTypeExpression(AttributeData attr, Compilation compilation, out string expr)
    {
        expr = "";
        if (attr.ConstructorArguments.Length == 0)
        {
            return false;
        }

        var reliableEnum = compilation.GetTypeByMetadataName("Communication.Network.RUDP.Shared.Messages.ReliableType");
        var raw = attr.ConstructorArguments[0].Value;
        int v = Convert.ToInt32(raw);

        if (reliableEnum != null)
        {
            foreach (var m in reliableEnum.GetMembers())
            {
                if (m is IFieldSymbol field &&
                    field.HasConstantValue &&
                    field.ConstantValue is int iv &&
                    iv == v &&
                    field.Name != "value__")
                {
                    expr =
                        $"global::Communication.Network.RUDP.Shared.Messages.ReliableType.{field.Name}";
                    return true;
                }
            }
        }

        expr =
            $"((global::Communication.Network.RUDP.Shared.Messages.ReliableType){v})";
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

internal sealed class RpcAttributeSymbols
{
    public INamedTypeSymbol? RemoteProcedureAttributeType { get; }

    public RpcAttributeSymbols(Compilation compilation)
    {
        RemoteProcedureAttributeType =
            compilation.GetTypeByMetadataName("RPC.Attribute.RemoteProcedure");
    }
}

using Microsoft.CodeAnalysis;
using System.Linq;

namespace DRPC.CodeGenerator.Metadata;

internal enum NetworkKind
{
    Server,
    Client
}

internal sealed class TypeMetadata
{
    public INamedTypeSymbol Symbol { get; }
    public DeclarationsMetadata ServerDeclarations { get; }
    public DeclarationsMetadata ClientDeclarations { get; }
    public NetworkKind NetworkKind { get; }
    public string? Namespace { get; }
    public bool IsServerEndpoint => NetworkKind == NetworkKind.Server;
    public MethodMetadata[] Outgoing => IsServerEndpoint ? ClientDeclarations.Methods : ServerDeclarations.Methods;
    public MethodMetadata[] Incoming => IsServerEndpoint ? ServerDeclarations.Methods : ClientDeclarations.Methods;
    public bool NeedsStringHelpers => Outgoing.Concat(Incoming).Any(m => m.Return.Type.SpecialType == SpecialType.System_String);

    public TypeMetadata(
        INamedTypeSymbol symbol,
        DeclarationsMetadata serverDeclarations,
        DeclarationsMetadata clientDeclarations,
        NetworkKind networkKind)
    {
        Symbol = symbol;
        ServerDeclarations = serverDeclarations;
        ClientDeclarations = clientDeclarations;
        NetworkKind = networkKind;
        Namespace = symbol.ContainingNamespace?.IsGlobalNamespace == true
            ? null
            : symbol.ContainingNamespace?.ToDisplayString();
    }
}

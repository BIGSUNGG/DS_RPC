using Microsoft.CodeAnalysis;

namespace DRPC.CodeGenerator.Metadata;

internal enum NetworkKind
{
    /// <summary>서버 측 허브(피어마다 1개). Incoming = 서버 계약, Outgoing = 클라이언트 계약.</summary>
    Server,

    /// <summary>클라이언트 측 허브. Incoming = 클라이언트 계약, Outgoing = 서버 계약.</summary>
    Client,
}

internal sealed class TypeMetadata
{
    public INamedTypeSymbol Symbol { get; }
    public DeclarationsMetadata ServerDeclarations { get; }
    public DeclarationsMetadata ClientDeclarations { get; }
    public NetworkKind NetworkKind { get; }
    public string? Namespace { get; }

    public bool IsServerEndpoint => NetworkKind == NetworkKind.Server;

    /// <summary>이 허브가 상대에게 보내는 호출 스텁.</summary>
    public MethodMetadata[] Outgoing => IsServerEndpoint ? ClientDeclarations.Methods : ServerDeclarations.Methods;

    /// <summary>이 허브가 받아 구현하는 호출.</summary>
    public MethodMetadata[] Incoming => IsServerEndpoint ? ServerDeclarations.Methods : ClientDeclarations.Methods;

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

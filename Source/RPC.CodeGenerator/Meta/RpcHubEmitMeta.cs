using System.Collections.Immutable;

namespace RPC.CodeGenerator.Meta;

internal enum RpcHubKind
{
    ClientSideServerHub,
    ServerSideClientHub
}

internal enum RpcEmitReturnKind
{
    Void,
    Float,
    Double,
    Int,
    Long,
    Short,
    Byte,
    Bool,
    Unsupported
}

internal sealed class RpcMethodEmitMeta
{
    public int MethodId { get; set; }
    public string Name { get; set; } = "";
    public string ReliableTypeExpression { get; set; } = "";
    public ImmutableArray<(string Name, string TypeDisplay, RpcEmitReturnKind Kind)> Parameters { get; set; }
    public RpcEmitReturnKind ReturnKind { get; set; }
}

internal sealed class RpcHubEmitMeta
{
    public RpcHubKind Kind { get; set; }
    public string? Namespace { get; set; }
    public string ClassName { get; set; } = "";
    /// <summary>클라이언트에서 실행되는 프로시저 (IClientProcedureDeclarations 계열).</summary>
    public ImmutableArray<RpcMethodEmitMeta> ClientProcedures { get; set; }
    /// <summary>서버에서 실행되는 프로시저 (IServerProcedureDeclarations 계열).</summary>
    public ImmutableArray<RpcMethodEmitMeta> ServerProcedures { get; set; }
}

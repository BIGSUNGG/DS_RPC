using Communication.Network.RUDP.Shared.Messages;
using RPC.Attribute;
using RPC.Shared.Interface;

namespace Sandbox;

public interface ISandboxServerProcedureDeclarations : IServerProcedureDeclarations
{
    [RemoteProcedure(ReliableType.ReliableOrdered)]
    int GetProtocolVersion();
}

public interface ISandboxClientProcedureDeclarations : IClientProcedureDeclarations
{
    [RemoteProcedure(ReliableType.ReliableOrdered)]
    float Add(float a, float b);
}

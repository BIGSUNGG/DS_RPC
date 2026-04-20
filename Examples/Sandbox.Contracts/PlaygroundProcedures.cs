using Communication.Network.RUDP.Shared.Messages;
using RPC.Attribute;
using RPC.Shared.Interface;

namespace Examples.Sandbox;

public interface IPlaygroundServerProcedureDeclarations : IServerProcedureDeclarations
{
    [RemoteProcedure(ReliableType.ReliableOrdered)]
    int GetBuildId();
}

public interface IPlaygroundClientProcedureDeclarations : IClientProcedureDeclarations
{
    [RemoteProcedure(ReliableType.ReliableOrdered)]
    float Echo(float value);
}

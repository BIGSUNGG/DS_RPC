using Communication.Network.RUDP.Shared.Messages;
using RPC.Attribute;
using RPC.Shared.Interface;

namespace Examples.Sandbox;

public interface IPlaygroundServerProcedureDeclarations : IServerProcedureDeclarations
{
    [RemoteProcedure(ReliableType.ReliableOrdered)]
    int GetBuildId();
    
    [RemoteProcedure(ReliableType.ReliableOrdered)]
    int Add(int value1, int value2);
}

public interface IPlaygroundClientProcedureDeclarations : IClientProcedureDeclarations
{
    [RemoteProcedure(ReliableType.ReliableOrdered)]
    float Echo(float value);

    [RemoteProcedure(ReliableType.ReliableOrdered)]
    float Echo_List(List<float> value);

}
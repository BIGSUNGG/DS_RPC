using RPC.Shared.Message;

namespace RPC.Shared.Interface;

public interface IHubBase
{
    void OnReceiveRPCRequestMessage(ProcedureCallRequestMessage message);
    void OnReceiveRPCResponseMessage(ProcedureCallResponseMessage message);
}
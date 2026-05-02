using DRPC.Shared.Message;

namespace DRPC.Shared.Interface;

public interface IHubBase
{
    void OnReceiveRPCRequestMessage(ProcedureCallRequestMessage message);
    void OnReceiveRPCResponseMessage(ProcedureCallResponseMessage message);
}
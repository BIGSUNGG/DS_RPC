using MessageProtocol;

namespace RPC.Shared.Message;

[StandaloneMessage(1)]
public partial class ProcedureCallResponseMessage
{
    public uint CallId { get; private set; }
    public byte[] ReturnData { get; private set; }

    public ProcedureCallResponseMessage()
    {
    }
    public ProcedureCallResponseMessage(uint callId, byte[] returnData)
    {
        CallId = callId;
        ReturnData = returnData;
    }
}
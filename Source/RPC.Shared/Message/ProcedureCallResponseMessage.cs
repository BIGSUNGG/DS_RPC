using MessageProtocol;

namespace RPC.Shared.Message;

[MessageStandalone(1)]
public class ProcedureCallResponseMessage
{
    public uint CallId { get; private set; }
    public byte[] ReturnData { get; private set; }
        
    protected ProcedureCallResponseMessage(uint callId, byte[] returnData)
    {
        CallId = callId;
        ReturnData = returnData;
    }
}
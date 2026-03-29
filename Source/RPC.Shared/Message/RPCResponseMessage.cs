using MessageProtocol;

namespace RPC.Shared.Message;

[MessageStandalone(2)]
public abstract class ProcedureCallResponseMessage
{
    public uint CallId { get; private set; }
    public byte[] ReturnData { get; private set; }
        
    protected ProcedureCallResponseMessage(uint callId, byte[] returnData)
    {
        CallId = callId;
        ReturnData = returnData;
    }
}
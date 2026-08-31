using MessageProtocol;

namespace DRPC.Shared.Message;

[StandaloneMessage(2)]
public partial class ProcedureCallErrorMessage
{
    public uint CallId { get; private set; }
    public int ErrorCode { get; private set; }
    public string Message { get; private set; } = string.Empty;

    public ProcedureCallErrorMessage()
    {
    }

    public ProcedureCallErrorMessage(uint callId, int errorCode, string message)
    {
        CallId = callId;
        ErrorCode = errorCode;
        Message = message ?? string.Empty;
    }
}

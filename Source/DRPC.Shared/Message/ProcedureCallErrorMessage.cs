using MessageProtocol;

namespace DRPC.Shared.Message;

/// <summary>RPC 실패 응답. 호출 측은 <c>RpcFaultException</c> 으로 관찰한다.</summary>
[StandaloneMessage(2)]
public partial class ProcedureCallErrorMessage
{
    public uint CallId { get; private set; }

    /// <summary><see cref="RpcErrorCode"/> 값.</summary>
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

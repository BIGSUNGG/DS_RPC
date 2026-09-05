namespace DRPC.Shared;

/// <summary>
/// 피어가 보낸 RPC 오류 응답(<see cref="Message.RpcErrorCode"/>). 호출 측은 await 시 이 예외로 관찰한다.
/// </summary>
public sealed class RpcFaultException : SystemException
{
    /// <summary>오류가 발생한 호출의 CallId.</summary>
    public uint CallId { get; }

    /// <summary><see cref="Message.RpcErrorCode"/> 값.</summary>
    public int ErrorCode { get; }

    public RpcFaultException(uint callId, int errorCode, string message)
        : base(message)
    {
        CallId = callId;
        ErrorCode = errorCode;
    }
}

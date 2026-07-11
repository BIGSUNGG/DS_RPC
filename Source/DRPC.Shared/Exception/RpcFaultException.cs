namespace DRPC.Shared;

/// <summary>
/// 원격 RPC 처리 중 피어가 보낸 오류 응답.
/// </summary>
public sealed class RpcFaultException : Exception
{
    public int ErrorCode { get; }
    public uint CallId { get; }

    public RpcFaultException(uint callId, int errorCode, string message)
        : base(message)
    {
        CallId = callId;
        ErrorCode = errorCode;
    }
}

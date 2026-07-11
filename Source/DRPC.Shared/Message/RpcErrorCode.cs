namespace DRPC.Shared.Message;

public static class RpcErrorCode
{
    public const int Unhandled = 1;
    public const int UnknownMethod = 2;
    public const int Timeout = 3;
    public const int Disconnected = 4;
    /// <summary>Incoming이 <c>MaxConcurrentIncoming</c> 상한을 초과함.</summary>
    public const int Overloaded = 5;
}

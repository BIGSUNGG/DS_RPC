namespace DRPC.Shared.Message;

public static class RpcErrorCode
{
    public const int Unhandled = 1;
    public const int UnknownMethod = 2;
    public const int Timeout = 3;
    public const int Disconnected = 4;
}

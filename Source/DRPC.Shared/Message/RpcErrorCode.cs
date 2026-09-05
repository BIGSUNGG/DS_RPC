namespace DRPC.Shared.Message;

/// <summary>RPC 오류 코드(와이어 값). 오류 응답의 <c>ErrorCode</c> 에 실린다.</summary>
public static class RpcErrorCode
{
    /// <summary>구현(body)에서 예외가 발생함.</summary>
    public const int Unhandled = 1;

    /// <summary>등록되지 않은 MethodId 요청.</summary>
    public const int UnknownMethod = 2;

    /// <summary>응답 대기 상한(<c>HubBase.RpcTimeout</c>) 초과. 호출 측에서 생성한다.</summary>
    public const int Timeout = 3;

    /// <summary>대기 중 연결 끊김. 호출 측에서 생성한다.</summary>
    public const int Disconnected = 4;

    /// <summary>Incoming 처리가 <c>MaxConcurrentIncoming</c> 상한을 초과함.</summary>
    public const int Overloaded = 5;
}

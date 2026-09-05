using MessageProtocol;

namespace DRPC.Shared.Message;

/// <summary>RPC 성공 응답. 요청의 CallId 를 그대로 되돌린다.</summary>
[StandaloneMessage(1)]
public partial class ProcedureCallResponseMessage
{
    public uint CallId { get; private set; }

    /// <summary>직렬화된 반환 값 페이로드. 반환이 void 이면 빈 배열.</summary>
    public byte[] ReturnData { get; private set; } = System.Array.Empty<byte>();

    public ProcedureCallResponseMessage()
    {
    }

    public ProcedureCallResponseMessage(uint callId, byte[] returnData)
    {
        CallId = callId;
        ReturnData = returnData ?? System.Array.Empty<byte>();
    }
}

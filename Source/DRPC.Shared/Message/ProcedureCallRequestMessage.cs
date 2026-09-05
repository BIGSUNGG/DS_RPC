using MessageProtocol;

namespace DRPC.Shared.Message;

/// <summary>RPC 요청. 응답을 기다리는 호출과 one-way 호출이 같은 메시지를 쓴다(one-way 은 <see cref="CallId"/> 가 0).</summary>
[StandaloneMessage(0)]
public partial class ProcedureCallRequestMessage
{
    /// <summary>호출 식별자. 0이면 one-way(응답 없음).</summary>
    public uint CallId { get; private set; }

    /// <summary><c>[RemoteProcedure]</c> 의 MethodId.</summary>
    public int MethodId { get; private set; }

    /// <summary>직렬화된 매개변수 페이로드. 매개변수가 없으면 빈 배열.</summary>
    public byte[] ParameterData { get; private set; } = System.Array.Empty<byte>();

    public ProcedureCallRequestMessage()
    {
    }

    public ProcedureCallRequestMessage(uint callId, int methodId, byte[] parameterData)
    {
        CallId = callId;
        MethodId = methodId;
        ParameterData = parameterData ?? System.Array.Empty<byte>();
    }
}

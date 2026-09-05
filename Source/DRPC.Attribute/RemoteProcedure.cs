namespace DRPC;

/// <summary>
/// 인터페이스 메서드를 RPC 계약으로 표시한다. 소스 생성기(DRPC.CodeGenerator)가 이 특성으로
/// 호출 스텁과 수신 디스패치를 생성하므로, 사용자는 전송·직렬화 코드를 직접 쓰지 않는다.
/// </summary>
/// <example>
/// <code>
/// [RemoteProcedure]                          // 기본 ReliableOrdered
/// int Add(int a, int b);
///
/// [RemoteProcedure(RpcDeliveryMode.Unreliable, 7)]
/// void SetPosition(float x, float y);        // 전송 방식 Overrides
///
/// [RemoteProcedure(RpcDeliveryMode.ReliableUnordered, 8, OneWay = true)]
/// void Chat(string text);                    // 응답 없는 one-way
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RemoteProcedure : System.Attribute
{
    /// <summary>이 메서드의 전송 방식. 기본값은 <see cref="RpcDeliveryMode.ReliableOrdered"/>.</summary>
    public RpcDeliveryMode Mode { get; }

    /// <summary>
    /// 와이어에서 메서드를 식별하는 번호. 생략(기본 -1)하면 선언 순서로 채워지고
    /// 생성기가 DRPCGEN004 경고로 명시 지정을 권고한다.
    /// </summary>
    public int MethodId { get; }

    /// <summary>true이면 요청만 보내고 응답을 기다리지/보내지 않는다. 반환 타입은 void 여야 한다.</summary>
    public bool OneWay { get; set; }

    public RemoteProcedure(
        RpcDeliveryMode mode = RpcDeliveryMode.ReliableOrdered,
        int methodId = -1)
    {
        Mode = mode;
        MethodId = methodId;
    }
}

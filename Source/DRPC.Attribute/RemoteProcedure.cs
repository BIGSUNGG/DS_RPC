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
    /// 와이어에서 메서드를 식별하는 번호. 생략(기본 -1)하면 인터페이스 FQN·메서드명·매개변수 시그니처의
    /// FNV-1a 해시로 자동 할당된다 — 선언 순서와 무관하게 이름이 같으면 항상 같은 값이다.
    /// 해시 충돌(같은 선언 안 중복 MethodId)은 DRPCGEN005 컴파일 에러로 차단된다.
    /// </summary>
    public int MethodId { get; }

    /// <summary>true이면 요청만 보내고 응답을 기다리지/보내지 않는다. 반환 타입은 void 여야 한다.</summary>
    public bool OneWay { get; set; }

    /// <summary>
    /// true이면 디스패치가 <c>_Implementation</c> 호출 전에 <c>_Validate</c> 를 먼저 기다린다.
    /// <c>_Validate</c> 는 사용자가 partial 로 구현하는 <c>Task&lt;bool&gt;</c> 메서드(매개변수 원본과 동일)이고,
    /// true를 반환해야만 <c>_Implementation</c> 이 호출된다. false면 구현을 호출하지 않고
    /// <c>RpcErrorCode.ValidationFailed</c>(7) 오류 응답을 보낸다(one-way 는 조용히 스킵).
    /// 미구현 시 컴파일 에러가 난다(fail-closed).
    /// </summary>
    public bool Validation { get; set; }

    /// <summary>
    /// 이 호출의 응답 대기 상한(밀리초). 기본 -1이면 허브 기본값(<c>HubBase.RpcTimeout</c>)을 따른다.
    /// 양수면 이 호출에만 그 예산이 적용된다 — 느린 배치 호출에만 넉넉한 상한을 주고 나머지는 허브 기본으로
    /// 지키게 하는 용도(호출별 타임아웃 정책). one-way 호출은 응답을 기다리지 않으므로 무의미하다(DRPCGEN011 경고).
    /// 0 이하(-1 제외)는 생성기 진단 DRPCGEN010 으로 거부된다.
    /// </summary>
    public int TimeoutMs { get; set; } = -1;

    public RemoteProcedure(
        RpcDeliveryMode mode = RpcDeliveryMode.ReliableOrdered,
        int methodId = -1)
    {
        Mode = mode;
        MethodId = methodId;
    }
}

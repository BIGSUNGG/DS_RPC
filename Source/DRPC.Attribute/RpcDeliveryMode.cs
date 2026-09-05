namespace DRPC;

/// <summary>
/// RPC 호출의 전송 방식. 통신 스택(DS_Communication RUDP)의 열거형을 참조하지 않도록 DRPC 가 자체 정의한다.
/// DRPC.Shared 에서 전송 옵션으로 매핑한다.
/// </summary>
/// <remarks>
/// 값과 의미는 RUDP 의 <c>RudpDeliveryMethod</c> 와 1:1 대응한다. 대응이 어긋나면 매핑 switch 를 갱신한다.
/// </remarks>
public enum RpcDeliveryMode
{
    /// <summary>유실·중복·순서 역전 가능. 상태 반영이 잦은 주기 전송에 쓴다.</summary>
    Unreliable,

    /// <summary>유실·중복 없음, 순서 없음.</summary>
    ReliableUnordered,

    /// <summary>유실 가능, 중복 없음, 순서 보장 (중간 패킷 유실 시 이전 것만 도달).</summary>
    Sequenced,

    /// <summary>유실·중복 없음, 순서 보장. RPC 기본값.</summary>
    ReliableOrdered,

    /// <summary>마지막 하나만 도달. 조각내기(fragment) 불가.</summary>
    ReliableSequenced,
}

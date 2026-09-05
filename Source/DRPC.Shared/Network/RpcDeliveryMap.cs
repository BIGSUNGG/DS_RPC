using Communication.Network.RUDP;

namespace DRPC.Shared.Network;

/// <summary>
/// DRPC 전송 방식(<see cref="RpcDeliveryMode"/>) → RUDP 송신 옵션 매핑.
/// 전송 열거형을 사용자 계약면에 노출하지 않기 위한 경계이며, 이 파일이 두 스택을 잇는 유일한 지점이다.
/// </summary>
public static class RpcDeliveryMap
{
    /// <summary>전송 방식별 공용 옵션 인스턴스(송신 경로에 할당 없음).</summary>
    /// <exception cref="ArgumentOutOfRangeException">알 수 없는 값.</exception>
    public static RudpSendOptions ToSendOptions(this RpcDeliveryMode mode) => mode switch
    {
        RpcDeliveryMode.Unreliable => RudpSendOptions.Unreliable,
        RpcDeliveryMode.ReliableUnordered => RudpSendOptions.ReliableUnordered,
        RpcDeliveryMode.Sequenced => RudpSendOptions.Sequenced,
        RpcDeliveryMode.ReliableSequenced => RudpSendOptions.ReliableSequenced,
        RpcDeliveryMode.ReliableOrdered => RudpSendOptions.ReliableOrdered,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "알 수 없는 RpcDeliveryMode 입니다."),
    };
}

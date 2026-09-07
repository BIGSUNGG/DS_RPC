using DRPC.Shared.Message;

namespace DRPC.Shared.Interface;

/// <summary>
/// Hub 런타임이 수신 경로(DRPCMessageHandler)에 노출하는 계약.
/// </summary>
public interface IHubBase
{
    void OnReceiveRPCRequestMessage(ProcedureCallRequestMessage message);

    void OnReceiveRPCResponseMessage(ProcedureCallResponseMessage message);

    void OnReceiveRPCErrorMessage(ProcedureCallErrorMessage message);

    /// <summary>대기 중인 outgoing RPC를 <paramref name="reason"/>으로 실패 처리한다.</summary>
    void CancelPendingCalls(Exception reason);

    /// <summary>세션 끊김 통지. pending 취소 후 <c>Disconnected</c> 이벤트를 (1회) 발생시킨다.</summary>
    void NotifyDisconnected(Exception? reason);

    /// <summary>
    /// 끊김 사유를 함께 전달하는 통지(형제 제안 P4 — <c>FlowControl</c> 백프레셔 신호 등).
    /// 기본 구현은 사유 없는 버전에 위임한다(기존 구현 호환).
    /// </summary>
    void NotifyDisconnected(Exception? reason, Communication.Shared.Connection.DisconnectReason disconnectReason)
        => NotifyDisconnected(reason);
}

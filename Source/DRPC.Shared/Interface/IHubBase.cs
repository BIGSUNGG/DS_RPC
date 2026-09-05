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
}
